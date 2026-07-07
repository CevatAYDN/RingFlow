using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class PlayingState : IGameState
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private IAudioService _audio;
        [Inject] private IProgressionService _progression;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            // GDD §12: oyun %40 (Boss seviyesiyse %80)
            if (_audio != null)
            {
                int currentLevel = _progression?.CurrentLevel.Value ?? 1;
                bool isBoss = WorldConfigSO.IsBossLevel(currentLevel);
                _audio.BgmVolume = isBoss ? 0.80f : 0.40f;
            }

            // Start level initialization
            int targetLevel = args is int levelIndex ? levelIndex : (_progression?.CurrentLevel.Value ?? 1);
            _signalBus?.Fire(new InitLevelSignal(targetLevel));

            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
