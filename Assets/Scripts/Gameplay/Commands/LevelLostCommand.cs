using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    // PERMANENT FIX: IAsyncCommand<T> instead of ICommand<T>.
    // ICommand<T>.Execute() returns synchronously; the pool immediately clears all
    // [Inject] fields. Any async void helper then loses its _fsm reference before
    // the first await resumes (pool reset races the continuation).
    // IAsyncCommand<T>.ExecuteAsync() keeps the instance alive until the returned
    // ValueTask completes, so _fsm is valid for the full await chain.
    public class LevelLostCommand : IAsyncCommand<LevelLostSignal>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private GameplayModel _model;

        public async ValueTask ExecuteAsync(LevelLostSignal signal, CancellationToken ct)
        {
            if (_model != null)
            {
                _model.HasChallengeFailed.Value = true;
            }

            NexusLog.Warn("LevelLostCommand", nameof(ExecuteAsync), "",
                $"Level LOST. Reason: '{signal.Reason}'. HasChallengeFailed={_model?.HasChallengeFailed.Value}. Transitioning to LoseState.");

            if (_fsm == null)
            {
                NexusLog.Error("LevelLostCommand", nameof(ExecuteAsync), "",
                    "IGameStateMachine not injected — cannot transition to LoseState. Player is stuck.");
                return;
            }

            try
            {
                await _fsm.ChangeStateAsync<LoseState>(signal);
            }
            catch (System.Exception ex)
            {
                NexusLog.Error("LevelLostCommand", nameof(ExecuteAsync), "",
                    $"Transition to LoseState threw: {ex.GetType().Name}: {ex.Message}. " +
                    "Player may be stuck on gameplay screen.");
            }
        }
    }
}
