using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class HintCommand : ICommand<HintRequestedSignal>
    {
        public const long HintCostCoins = 50;

        [Inject] private GameplayModel _model;
        [Inject] private IEconomyService _economy;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IPlayerPrefsService _prefs;

        public void Execute(HintRequestedSignal signal)
        {
            // Hint is only meaningful while the game is in progress.
            if (_model.IsGameWon.Value || _model.Poles.Count == 0)
            {
                _signalBus.Fire(HintResolvedSignal.Empty);
                return;
            }

            // Check spend ability — if user can't afford, return empty (UI offers ad option).
            if (!_economy.CanAfford("Coins", HintCostCoins))
            {
                _signalBus.Fire(HintResolvedSignal.Empty);
                return;
            }

            // Snapshot live state → BoardState → run solver with cap = completed-moves + slack.
            // We only need the FIRST move, not the full path.
            var current = BuildBoardStateFromModel();
            var firstMove = TryFindFirstMove(current, _model.MovesCount.Value, 50);

            if (firstMove.From == -1 || firstMove.To == -1)
            {
                // Defensive: solver says no progress is possible with the current cap.
                _signalBus.Fire(HintResolvedSignal.Empty);
                return;
            }

            _economy.Spend("Coins", HintCostCoins, "Hint");
            _signalBus.Fire(new HintResolvedSignal(firstMove.From, firstMove.To, true));
        }

        public static BoardState BuildBoardStateFromModelFromPoles(GameplayModel m)
        {
            return BuildBoardStateFromModelFromPolesStatic(m);
        }

        private static BoardState BuildBoardStateFromModelFromPolesStatic(GameplayModel m)
        {
            var board = new BoardState { PoleCount = m.Poles.Count };
            for (int p = 0; p < m.Poles.Count && p < 10; p++)
            {
                var pole = m.Poles[p];
                board.SetPoleLocked(p, pole.IsLocked);
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    var ring = pole.Rings[r];
                    board.AddRingSimple(p, ring);
                }
                if (pole.Rings.Count > 0)
                {
                    // Top frozen logical flag (best-effort; only Frozen type matters today)
                    var top = pole.TopRing;
                    board.SetTopRingFrozen(p, top.Type == RingType.Frozen);
                }
            }
            return board;
        }

        private BoardState BuildBoardStateFromModel()
        {
            return BuildBoardStateFromModelFromPolesStatic(_model);
        }

        private static Move TryFindFirstMove(BoardState initial, int movesSoFar, int computeBudget)
        {
            // Solve within an enlarged move cap (your moves so far + slack). The first move of the optimal path
            // is what we surface.
            var result = LevelSolver.Solve(initial, Math.Max(4, computeBudget));
            if (!result.IsSolvable || result.MoveCount <= 0 || result.Moves == null) return new Move(-1, -1);
            var first = result.Moves[0];
            return new Move(first.FromPoleId, first.ToPoleId);
        }
    }
}

