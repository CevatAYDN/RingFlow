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

            NexusLog.Warn("LevelLostCommand", nameof(Execute), "",
                $"Level LOST. Reason: '{signal.Reason}'. HasChallengeFailed={_model?.HasChallengeFailed.Value}. Transitioning to LoseState.");

            if (_fsm == null)
            {
                NexusLog.Error("LevelLostCommand", nameof(Execute), "",
                    "IGameStateMachine not injected — cannot transition to LoseState. Player is stuck.");
                return;
            }

            // LOG-1: ChangeStateAsync returns a Task; discard with _ = to suppress CS4014 but
            // add error handling so a failed transition is visible in the log instead of silent.
            var task = _fsm.ChangeStateAsync<LoseState>(signal);
            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    NexusLog.Error("LevelLostCommand", nameof(Execute), "",
                        $"Transition to LoseState faulted: {t.Exception?.GetBaseException()?.Message}");
            }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
