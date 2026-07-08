using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class SplashState : IGameState
    {
        [Inject] private ISignalBus _signalBus;

        [Inject] private Diagnostics.IGameDiagnostics _diag;

        public async ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _diag?.Checkpoint("SplashState");
            _diag?.Log("FSM", "SplashState.OnEnterAsync started");

            // Wait 2 frames so UIRoot and other subscribers are fully initialized
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();

            if (_signalBus != null)
            {
                _diag?.Log("FSM", "SplashState firing ShowScreenSignal(Splash)");
                _signalBus.Fire(new ShowScreenSignal(ScreenType.Splash));
            }
            else
            {
                NexusLog.Error("SplashState", nameof(OnEnterAsync), "",
                    "ISignalBus unbound; ShowScreenSignal cannot be fired.");
            }
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) { }
    }
}
