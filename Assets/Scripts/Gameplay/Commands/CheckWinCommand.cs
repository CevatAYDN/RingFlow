using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class CheckWinCommand : ICommand<CheckWinSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(CheckWinSignal signal)
        {
            if (_model == null || _model.Poles == null || _model.Poles.Count == 0)
            {
                NexusLog.Warn("CheckWinCommand", "Execute", "",
                    "Model or poles empty — cannot evaluate win.");
                return;
            }

            if (_model.IsGameWon.Value) return;

            bool won = true;
            int nonEmptyPoleCount = 0;

            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];

                // Empty poles are legal buffer space — not "unsolved", not "completed".
                // GDD §12: at least 1 empty pole is required for valid play, empty poles
                // must never block or contribute to win evaluation.
                if (pole.IsEmpty) continue;

                nonEmptyPoleCount++;

                // FIX-A1: IsSolved requires the pole to be full (rings == capacity) AND
                // all rings to share the same color. A partially-filled pole that happens
                // to have same-color rings must NOT be treated as completed — the solver
                // and GDD §21 both require every filled pole to be at full capacity.
                bool poleSolved = LevelSolver.IsSolved(pole, pole.MaxCapacity);
                if (poleSolved)
                {
                    if (!_model.CompletedPoles.Contains(p))
                    {
                        _model.CompletedPoles.Add(p);
                        _signalBus.Fire(new PoleCompletedSignal(p));
                    }
                    // FIX-G3: No Remove() call on a solved pole — a pole that satisfies
                    // IsSolved stays completed. Removing here caused PoleCompletedSignal
                    // to re-fire on the next CheckWin after Undo (visual double-celebration).
                }
                else
                {
                    // Only non-empty, non-solved poles invalidate a win and need cleanup.
                    // FIX-G3: Remove only when the pole is genuinely unsolved.
                    _model.CompletedPoles.Remove(p);
                    won = false;
                }
            }

            // Board with zero non-empty poles cannot be a win state —
            // this guard prevents false-positive wins on fresh/empty boards.
            if (nonEmptyPoleCount == 0)
            {
                won = false;
            }

            if (won)
            {
                _model.IsGameWon.Value = true;
                _signalBus.Fire(new LevelWonSignal());
            }
        }
    }
}