using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class GameOverState : IGameState
    {
        [Inject] private ISignalBus _signalBus;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            NexusLog.Info("GameOverState", nameof(OnEnterAsync), "", "Game over screen shown.");
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.GameOver));
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
