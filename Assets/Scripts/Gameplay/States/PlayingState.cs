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
        [Inject] private GameplayModel _model;
        [Inject] private IAudioService _audio;
        [Inject] private IProgressionService _progression;
        [Inject] private IGameDiagnostics _diag;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private AudioConfigSO _audioConfig;
        [Inject] private Services.IGameTimeService _time;

        public async ValueTask OnEnterAsync(object args, CancellationToken ct)
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
                        bool isBoss = GameConfigDatabaseSO.IsBossLevel(_dbConfig, currentLevel);
                        // FIX P0.3: use the transient state multiplier so the player's saved
                        // BGM slider stays at whatever they last set in Settings.
                        _audio.BgmStateMultiplier = isBoss ? _audioConfig.Bgm.BossBgmMultiplier : _audioConfig.Bgm.NormalBgmMultiplier;
                    }
                    return;
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

            // GDD §12: oyun %40 (Boss seviyesiyse %80) — data-driven via AudioConfigSO
            if (_audio != null)
            {
                bool isBoss = GameConfigDatabaseSO.IsBossLevel(_dbConfig, targetLevel);
                _audio.BgmStateMultiplier = isBoss ? _audioConfig.Bgm.BossBgmMultiplier : _audioConfig.Bgm.NormalBgmMultiplier;

                int worldIdx = _dbConfig.GetWorldForLevel(targetLevel);
                var bgm = ProceduralAudio.GetOrCreateBgmClip(worldIdx);
                _audio.PlayBgm(bgm, true);
            }

            // Start level initialization
            _diag?.Log("PlayingState", $"Starting level {targetLevel} (resume={isResume}, boss={GameConfigDatabaseSO.IsBossLevel(_dbConfig, targetLevel)}).");
            await _signalBus.FireAsync(new InitLevelSignal(targetLevel));
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime)
        {
            if (_progression == null || _signalBus == null)
                return;

            if (_model == null || !_model.IsChallengeMode.Value || _model.HasChallengeFailed.Value || _model.IsGameWon.Value)
                return;

            int timeLimitSeconds = _model.ChallengeTimeLimitSeconds.Value;
            if (timeLimitSeconds <= 0)
                return;

            long startTicks = _model.LevelStartUtcTicks.Value;
            if (startTicks <= 0)
                return;

            var nowTicks = _time?.UtcNow.Ticks ?? System.DateTime.UtcNow.Ticks;
            var elapsedSeconds = (nowTicks - startTicks) / (double)System.TimeSpan.TicksPerSecond;
            if (elapsedSeconds >= timeLimitSeconds)
            {
                _model.HasChallengeFailed.Value = true;
                // LevelLostCommand is IAsyncCommand — Fire() throws at runtime for async handlers.
                // OnTick is a sync method; FireAsyncAndForget dispatches without blocking the frame.
                _signalBus.FireAsyncAndForget(
                    new LevelLostSignal($"Time limit reached ({elapsedSeconds:0.0}s/{timeLimitSeconds}s)"),
                    ex => NexusLog.Error("PlayingState", "OnTick", "",
                        $"LevelLostSignal (time limit) handler threw: {ex?.GetType().Name}: {ex?.Message}"));
            }
        }
    }
}
