using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay
{
    public class PlayingState : IGameState
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private IAudioService _audio;
        [Inject] private IProgressionService _progression;
        [Inject] private IGameDiagnostics _diag;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _diag?.Checkpoint("PlayingState.OnEnterAsync");
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Gameplay));

            bool isResume = false;
            int targetLevel = -1;

            if (args is PlayingStateArgs playingArgs)
            {
                isResume = playingArgs.IsResume;
                if (isResume)
                {
                    NexusLog.Info("PlayingState", nameof(OnEnterAsync), "",
                        "Resuming from pause — restoring BGM, skipping InitLevel.");
                    if (_audio != null)
                    {
                        int currentLevel = _progression?.CurrentLevel.Value ?? 1;
                        bool isBoss = WorldConfigSO.IsBossLevel(currentLevel);
                        // FIX P0.3: use the transient state multiplier so the player's saved
                        // BGM slider stays at whatever they last set in Settings.
                        _audio.BgmStateMultiplier = isBoss ? 0.80f : 0.40f;
                    }
                    return default;
                }
                targetLevel = playingArgs.LevelIndex;
            }
            else if (args is int levelIndex)
            {
                targetLevel = levelIndex;
            }

            if (targetLevel <= 0)
            {
                targetLevel = _progression?.CurrentLevel.Value ?? 1;
            }

            // GDD §12: oyun %40 (Boss seviyesiyse %80)
            if (_audio != null)
            {
                bool isBoss = WorldConfigSO.IsBossLevel(targetLevel);
                // FIX P0.3: push the transient state multiplier; the user's saved BgmVolume is
                // preserved untouched across level transitions.
                _audio.BgmStateMultiplier = isBoss ? 0.80f : 0.40f;

                int worldIdx = WorldConfigSO.WorldFromAbsoluteLevel(targetLevel);
                var bgm = ProceduralAudio.GetOrCreateBgmClip(worldIdx);
                _audio.PlayBgm(bgm, true);
            }

            // Start level initialization
            _diag?.Log("PlayingState", $"Starting level {targetLevel} (resume={isResume}, boss={WorldConfigSO.IsBossLevel(targetLevel)}).");
            _signalBus?.Fire(new InitLevelSignal(targetLevel));

            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
