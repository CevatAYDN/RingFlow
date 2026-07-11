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
                if (pole.IsEmpty)
                {
                    _model.CompletedPoles.Remove(p);
                    continue;
                }

                nonEmptyPoleCount++;

                bool poleSolved = LevelSolver.IsSolved(pole, pole.MaxCapacity);
                if (poleSolved)
                {
                    if (!_model.CompletedPoles.Contains(p))
                    {
                        _model.CompletedPoles.Add(p);
                        _signalBus.Fire(new PoleCompletedSignal(p));
                    }
                }
                else
                {
                    _model.CompletedPoles.Remove(p);
                    won = false;
                }
            }

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