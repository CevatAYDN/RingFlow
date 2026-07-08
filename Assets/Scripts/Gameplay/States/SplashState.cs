using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;

namespace RingFlow.Gameplay
{
    public class SplashState : IGameState
    {
        [Inject] private ISignalBus _signalBus;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Splash));
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) { }
    }
}
