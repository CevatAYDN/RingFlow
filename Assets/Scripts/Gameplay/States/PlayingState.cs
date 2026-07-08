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
            // Show Gameplay HUD
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Gameplay));

            int targetLevel = args is int levelIndex ? levelIndex : (_progression?.CurrentLevel.Value ?? 1);

            // GDD §12: oyun %40 (Boss seviyesiyse %80)
            if (_audio != null)
            {
                bool isBoss = WorldConfigSO.IsBossLevel(targetLevel);
                _audio.BgmVolume = isBoss ? 0.80f : 0.40f;

                int worldIdx = WorldConfigSO.WorldFromAbsoluteLevel(targetLevel);
                var bgm = ProceduralAudio.GetOrCreateBgmClip(worldIdx);
                _audio.PlayBgm(bgm, true);
            }

            // Start level initialization
            _signalBus?.Fire(new InitLevelSignal(targetLevel));

            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
