using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Loading state shown during async level setup, asset streaming, or scene transitions.
    /// Fires ShowScreenSignal(Splash) as a loading screen overlay; the actual transition target
    /// is passed via LoadingStateArgs.
    /// </summary>
    public class LoadingState : IGameState
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ISignalBus _signalBus;
        [Inject] private Diagnostics.IGameDiagnostics _diag;

        public async ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _diag?.Checkpoint("LoadingState");
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Splash));

            if (args is LoadingStateArgs loadingArgs)
            {
                await Task.Delay(100, ct); // Minimum loading screen visibility
                await _fsm.ChangeStateAsync(loadingArgs.TargetState, ct, loadingArgs.TargetArgs);
            }
            else
            {
                NexusLog.Warn("LoadingState", nameof(OnEnterAsync), "",
                    "No LoadingStateArgs provided, falling back to MainMenuState.");
                await _fsm.ChangeStateAsync(typeof(MainMenuState), ct);
            }
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }

    public class LoadingStateArgs
    {
        public System.Type TargetState;
        public object TargetArgs;
    }

    /// <summary>
    /// Error state shown when an unrecoverable failure occurs (e.g. level generation exhausted
    /// all seeds, critical service unavailable). Shows a user-friendly message and a retry button.
    /// </summary>
}
