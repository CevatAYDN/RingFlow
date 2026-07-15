using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class LevelLostCommand : ICommand<LevelLostSignal>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private GameplayModel _model;

        public void Execute(LevelLostSignal signal)
        {
            if (_model != null)
            {
                _model.HasChallengeFailed.Value = true;
            }

            NexusLog.Info("LevelLostCommand", nameof(Execute), "",
                $"Level lost. Reason: {signal.Reason}");
            _ = _fsm?.ChangeStateAsync<LoseState>(signal);
        }
    }
}
