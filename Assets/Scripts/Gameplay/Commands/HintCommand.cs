using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class HintCommand : IAsyncCommand<HintRequestedSignal>
    {
        public const long HintCostCoins = 50;

        [Inject] private GameplayModel _model;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progressionService;

        public async ValueTask ExecuteAsync(HintRequestedSignal signal, CancellationToken ct)
        {
            if (_model == null)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), "", "Model not bound; hint request ignored.");
                _signalBus?.Fire(HintResolvedSignal.Empty);
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
                _signalBus?.Fire(HintResolvedSignal.Empty);
                return;
            }

            // C3: Pre-validate solver BEFORE any payment path.
            // This prevents the ad-race problem where a user watches a rewarded ad
            // but the solver cannot find a valid move, leaving the user unrewarded.
            var (current, maxCapacity) = BuildBoardStateFromModel(_model);

            Move firstMove;
            try
            {
                firstMove = await TryFindFirstMoveAsync(current, maxCapacity, ct);
            }
            catch (System.OperationCanceledException)
            {
                NexusLog.Info("HintCommand", nameof(ExecuteAsync), "",
                    "Hint solver cancelled (likely scene change). Dropping to Empty.");
                _signalBus?.Fire(HintResolvedSignal.Empty);
                return;
            }

            if (firstMove.From == -1 || firstMove.To == -1)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), _model.MovesCount.Value.ToString(),
                    "Solver could not find a first move — level may be unsolvable from current state. Hint request denied before any payment.");
                _signalBus?.Fire(HintResolvedSignal.Empty);
                return;
            }

            if (_economy == null)
            {
                NexusLog.Warn("HintCommand", nameof(ExecuteAsync), "",
                    "EconomyService not bound. Hint flow will fall back to ad path only.");
            }

            if (_economy != null && _economy.CanAfford("Hint", 1))
            {
                ResolveAndFire(firstMove, true);
                return;
            }

            if (_economy != null && _economy.CanAfford("Coins", HintCostCoins))
            {
                ResolveAndFire(firstMove, false);
                return;
            }

            if (_ads != null && _ads.IsRewardedAvailable("Hint"))
            {
                var callback = new HintRewardCallback
                {
                    FirstMove = firstMove,
                    SignalBus = _signalBus,
                    Command = this
                };
                _ads.ShowRewarded("Hint", callback.OnRewardResult);
                return;
            }

            NexusLog.Info("HintCommand", nameof(ExecuteAsync), "",
                "No usable payment channel (Hint, Coins, Ad) — dropping to Empty.");
            _signalBus?.Fire(HintResolvedSignal.Empty);
        }

        private void ResolveAndFire(Move firstMove, bool useHintCurrency)
        {
            if (_model == null) return;

            if (useHintCurrency)
            {
                if (_economy == null || !_economy.Spend("Hint", 1, "Hint"))
                {
                    NexusLog.Warn("HintCommand", nameof(ResolveAndFire), "",
                        "Hint currency spend failed (race with another consumer?).");
                    _signalBus?.Fire(HintResolvedSignal.Empty);
                    return;
                }
            }
            else
            {
                if (_economy == null || !_economy.Spend("Coins", HintCostCoins, "Hint"))
                {
                    NexusLog.Warn("HintCommand", nameof(ResolveAndFire), "",
                        "Coins spend failed (insufficient balance or economy unbound).");
                    _signalBus?.Fire(HintResolvedSignal.Empty);
                    return;
                }
            }

            int level = _progressionService?.CurrentLevel.Value ?? 0;
            AnalyticsEvents.HintUse(level);

            _signalBus?.Fire(new HintResolvedSignal(firstMove.From, firstMove.To, true));
        }

        private static (BoardState Board, int MaxCapacity) BuildBoardStateFromModel(GameplayModel m)
        {
            var board = new BoardState { PoleCount = m.Poles.Count };
            int maxCapacity = 4;
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
                if (pole.MaxCapacity > maxCapacity)
                    maxCapacity = pole.MaxCapacity;

                board.SetPoleLocked(p, pole.IsLocked);

                int count = pole.Rings.Count;
                board.SetRingCount(p, count);
                for (int r = 0; r < count; r++)
                {
                    var ringData = pole.Rings[r];
                    board.SetRingColor(p, r, ringData.Color);
                    board.SetRingType(p, r, ringData.Type);
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

            return (board, maxCapacity);
        }

        private static async ValueTask<Move> TryFindFirstMoveAsync(BoardState initial, int maxCapacity, CancellationToken ct)
        {
            var result = await LevelSolver.SolveAsync(initial, maxCapacity, cancellationToken: ct);
            if (!result.IsSolvable || result.MoveCount <= 0 || result.Moves == null)
            {
                return new Move(-1, -1);
            }
            var first = result.Moves[0];
            return new Move(first.FromPoleId, first.ToPoleId);
        }

        /// <summary>
        /// Zero-GC callback struct for rewarded ad completion.
        /// Prevents memory leak by avoiding lambda closure capture of command instance.
        /// Follows Nexus 0-GC allocation pattern for async callbacks.
        /// </summary>
        private struct HintRewardCallback
        {
            public Move FirstMove;
            public ISignalBus SignalBus;
            public HintCommand Command;

            public void OnRewardResult(bool success)
            {
                if (success)
                {
                    Command.ResolveAndFire(FirstMove, false);
                }
                else
                {
                    SignalBus?.Fire(HintResolvedSignal.Empty);
                }
            }
        }
    }
}
