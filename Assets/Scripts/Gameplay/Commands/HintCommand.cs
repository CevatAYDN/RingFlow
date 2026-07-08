using System;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class HintCommand : ICommand<HintRequestedSignal>
    {
        public const long HintCostCoins = 50;

        [Inject] private GameplayModel _model;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;

        public void Execute(HintRequestedSignal signal)
        {
            if (_model == null || _model.IsGameWon.Value || _model.Poles.Count == 0)
            {
                _signalBus?.Fire(HintResolvedSignal.Empty);
                return;
            }

            if (_economy != null && _economy.CanAfford("Coins", HintCostCoins))
            {
                ResolveAndFire();
                return;
            }

            if (_ads != null && _ads.IsRewardedAvailable("Hint"))
            {
                _ads.ShowRewarded("Hint", success =>
                {
                    if (success) ResolveAndFire();
                    else _signalBus?.Fire(HintResolvedSignal.Empty);
                });
                return;
            }

            _signalBus?.Fire(HintResolvedSignal.Empty);
        }

        private void ResolveAndFire()
        {
            if (_model == null) return;
            var current = BuildBoardStateFromModel(_model);
            var firstMove = TryFindFirstMove(current, _model.MovesCount.Value, 50);

            if (firstMove.From == -1 || firstMove.To == -1)
            {
                _signalBus?.Fire(HintResolvedSignal.Empty);
                return;
            }

            _economy?.Spend("Coins", HintCostCoins, "Hint");
            _signalBus?.Fire(new HintResolvedSignal(firstMove.From, firstMove.To, true));
        }

        private static BoardState BuildBoardStateFromModel(GameplayModel m)
        {
            var board = new BoardState { PoleCount = m.Poles.Count };
            for (int p = 0; p < m.Poles.Count && p < 12; p++)
            {
                var pole = m.Poles[p];
                board.SetPoleLocked(p, pole.IsLocked);
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    board.AddRingSimple(p, pole.Rings[r]);
                }
                if (pole.Rings.Count > 0)
                {
                    var top = pole.TopRing;
                    board.SetTopRingFrozen(p, top.Type == RingType.Frozen);
                }
            }
            return board;
        }

        private static Move TryFindFirstMove(BoardState initial, int movesSoFar, int computeBudget)
        {
            var result = LevelSolver.Solve(initial, Math.Max(4, computeBudget));
            if (!result.IsSolvable || result.MoveCount <= 0 || result.Moves == null)
            {
                return new Move(-1, -1);
            }
            var first = result.Moves[0];
            return new Move(first.FromPoleId, first.ToPoleId);
        }
    }
}
