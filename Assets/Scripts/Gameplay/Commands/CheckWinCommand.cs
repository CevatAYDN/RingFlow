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
            if (_model.IsGameWon.Value) return;
            if (_model == null || _model.Poles.Count == 0)
            {
                NexusLog.Warn("CheckWinCommand", "Execute", "",
                    "Model or poles empty — cannot evaluate win.");
                return;
            }

            bool won = true;
            int nonEmptyPoleCount = 0;

            foreach (var pole in _model.Poles)
            {
                if (pole.IsEmpty) continue;

                nonEmptyPoleCount++;

                bool poleSolved = LevelSolver.IsSolved(pole, pole.MaxCapacity);
                if (!poleSolved)
                {
                    won = false;
                    break;
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