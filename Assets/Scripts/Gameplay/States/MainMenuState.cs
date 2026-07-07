using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class MainMenuState : IGameState
    {
        [Inject] private IAudioService _audio;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            // GDD §12: Mix menu %70
            if (_audio != null)
            {
                _audio.BgmVolume = 0.70f;
            }
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
