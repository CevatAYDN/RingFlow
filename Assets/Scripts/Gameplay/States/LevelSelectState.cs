using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;

namespace RingFlow.Gameplay
{
    public class LevelSelectState : IGameState
    {
        [Inject] private ISignalBus _signalBus;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.LevelSelect));
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
