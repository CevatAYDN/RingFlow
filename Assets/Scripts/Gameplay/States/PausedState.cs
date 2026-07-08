using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class PausedState : IGameState
    {
        [Inject] private IAudioService _audio;
        [Inject] private ISignalBus _signalBus;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            // Show Pause UI
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Pause));

            // GDD §12: pause %20
            if (_audio != null)
            {
                _audio.BgmVolume = 0.20f;
            }
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
