using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class HintCommand : IAsyncCommand<HintRequestedSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progressionService;
        [Inject] private IAnalyticsService _analyticsService;

        // Fires HintResolvedSignal.Empty on every early-exit path.
        // HintResolvedSignal has only Subscribe handlers (no BindCommand) — Fire() is correct here.
        private void FireEmpty() => _signalBus?.Fire(HintResolvedSignal.Empty);

        public async ValueTask ExecuteAsync(HintRequestedSignal signal, CancellationToken ct)
        {
            if (_model == null)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), "", "Model not bound; hint request ignored.");
                FireEmpty();
                return;
            }

            if (_model.IsGameWon.Value)
            {
                return;
            }

            if (_model.Poles.Count == 0)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), _model.MovesCount.Value.ToString(),
                    "Pole list empty; level not loaded yet.");
                FireEmpty();
                return;
            }

            // C3: Pre-validate solver BEFORE any payment path.
            // This prevents the ad-race problem where a user watches a rewarded ad
            // but the solver cannot find a valid move, leaving the user unrewarded.
            var (current, maxCapacity, portalTargets) = BuildBoardStateFromModel(_model);

            Move firstMove;
            try
            {
                firstMove = await TryFindFirstMoveAsync(current, maxCapacity, portalTargets, ct, _dbConfig?.LevelGen.BombTickMode ?? BombTickMode.AllBombsPerMove);
            }
            catch (System.OperationCanceledException)
            {
                NexusLog.Info("HintCommand", nameof(ExecuteAsync), "",
                    "Hint solver cancelled (likely scene change). Dropping to Empty.");
                FireEmpty();
                return;
            }

            if (firstMove.From == -1 || firstMove.To == -1)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), _model.MovesCount.Value.ToString(),
                    "Solver could not find a first move — level may be unsolvable from current state. Hint request denied before any payment.");
                FireEmpty();
                return;
            }

            if (_economy == null)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), "",
                    "EconomyService not bound. Hint flow will fall back to ad path only.");
            }

            if (_economy != null && _economy.CanAfford(CurrencyIds.Hint, 1))
            {
                ResolveAndFire(firstMove, true);
                return;
            }

            if (_dbConfig == null) throw new System.InvalidOperationException("[HintCommand] GameConfigDatabaseSO not injected!");
            long hintCoinCost = _dbConfig.BalanceConfig.HintCoinCost;
            if (_economy != null && _economy.CanAfford(CurrencyIds.Coins, hintCoinCost))
            {
                ResolveAndFire(firstMove, false);
                return;
            }

            if (_ads != null && _ads.IsRewardedAvailable("Hint"))
            {
                var callback = new HintRewardCallback
                {
                    FirstMove = firstMove,
                    SignalBus = _signalBus
                    // FIX-E1: Command = this removed — field deleted from struct to prevent retention cycle.
                };
                _ads.ShowRewarded("Hint", callback.OnRewardResult);
                return;
            }

            NexusLog.Info("HintCommand", nameof(ExecuteAsync), "",
                "No usable payment channel (Hint, Coins, Ad) — dropping to Empty.");
            FireEmpty();
        }

        private void ResolveAndFire(Move firstMove, bool useHintCurrency)
        {
            if (_model == null) return;

            if (useHintCurrency)
            {
                if (_economy == null || !_economy.Spend(CurrencyIds.Hint, 1, "Hint"))
                {
                    NexusLog.Warn("HintCommand", nameof(ResolveAndFire), "",
                        "Hint currency spend failed (race with another consumer?).");
                    FireEmpty();
                    return;
                }
            }
            else
            {
                long hintCoinCost = _dbConfig.BalanceConfig.HintCoinCost;
                if (_economy == null || !_economy.Spend(CurrencyIds.Coins, hintCoinCost, "Hint"))
                {
                    NexusLog.Warn("HintCommand", nameof(ResolveAndFire), "",
                        "Coins spend failed (insufficient balance or economy unbound).");
                    FireEmpty();
                    return;
                }
            }

            int level = _progressionService?.CurrentLevel.Value ?? 0;
            if (_analyticsService != null)
            {
                _analyticsService.HintUse(level);
            }

            _signalBus?.Fire(new HintResolvedSignal(firstMove.From, firstMove.To, true));
        }

        private static (BoardState Board, int MaxCapacity, int[] PortalTargets) BuildBoardStateFromModel(GameplayModel m)
        {
            var board = new BoardState { PoleCount = m.Poles.Count };
            int maxCapacity = 0;
            int totalRings = 0;

            for (int p = 0; p < m.Poles.Count && p < 12; p++)
            {
                var pole = m.Poles[p];
                if (pole == null)
                {
                    NexusLog.Warn("HintCommand", nameof(BuildBoardStateFromModel), p.ToString(),
                        "Null pole in GameplayModel.Poles at index. Skipping.");
                    continue;
                }
                if (pole.RingCapacity > maxCapacity)
                    maxCapacity = pole.RingCapacity;

                board.SetPoleLocked(p, pole.IsLocked);

                int count = pole.Rings.Count;
                board.SetRingCount(p, count);
                for (int r = 0; r < count; r++)
                {
                    var ringData = pole.Rings[r];
                    board.SetRingColor(p, r, ringData.Color);
                    board.SetRingType(p, r, ringData.Type);
                    board.SetRingAdditional(p, r, ringData.AdditionalData);
                    totalRings++;
                }
                if (count > 0)
                {
                    var top = pole.TopRing;
                    board.SetTopRingFrozen(p, top.Type == RingType.Frozen);
                }
            }

            if (totalRings == 0)
            {
                NexusLog.Warn("HintCommand", nameof(BuildBoardStateFromModel), "",
                    "Board built from model has zero rings — solver will return unsolvable.");
            }

            return (board, maxCapacity, GameplayHelpers.BuildPortalTargets(m.Poles));
        }

        private static async ValueTask<Move> TryFindFirstMoveAsync(BoardState initial, int maxCapacity, int[] portalTargets, CancellationToken ct, BombTickMode tickMode)
        {
            var result = await LevelSolver.SolveAsync(initial, maxCapacity, portalTargets: portalTargets, cancellationToken: ct, bombTickMode: tickMode);
            if (!result.IsSolvable || result.MoveCount <= 0 || result.Moves == null)
            {
                return new Move(-1, -1, default);
            }
            var first = result.Moves[0];
            return new Move(first.FromPoleId, first.ToPoleId, first.Ring);
        }

        /// <summary>
        /// Zero-GC callback struct for rewarded ad completion.
        /// Prevents memory leak by avoiding lambda closure capture of command instance.
        /// Follows Nexus 0-GC allocation pattern for async callbacks.
        ///
        /// FIX-E1: The original struct held a reference to `HintCommand Command` which was
        /// never used inside OnRewardResult — it was dead code. Holding a reference to the
        /// command from inside a struct that is passed as a delegate to the Ad SDK creates
        /// a potential retention cycle: the SDK holds the delegate, the delegate holds the
        /// struct (boxed on the heap), the struct holds the Command, and the Command holds
        /// injected services. On some ad SDK implementations the callback is never nulled
        /// after firing, meaning the entire DI graph is kept alive until the next ad request.
        /// Fix: remove the unused Command field entirely.
        ///
        /// IMPORTANT: Ad SDK callbacks may arrive on a background thread.
        /// We use FireThreadSafe to marshal signal dispatch to the main Unity thread.
        /// Economy spend is skipped for ad-rewarded hints (the ad IS the payment).
        /// </summary>
        private struct HintRewardCallback
        {
            public Move FirstMove;
            public ISignalBus SignalBus;
            // FIX-E1: Command field removed — was never used, caused unnecessary object retention.

            public void OnRewardResult(bool success)
            {
                if (success)
                {
                    // Ad SDK callbacks may arrive on a background thread.
                    // FireThreadSafe marshals to the main Unity thread.
                    // No economy spend needed — the ad itself is the payment.
                    SignalBus?.FireThreadSafe(new HintResolvedSignal(FirstMove.From, FirstMove.To, true));
                }
                else
                {
                    SignalBus?.FireThreadSafe(HintResolvedSignal.Empty);
                }
            }
        }
    }
}
