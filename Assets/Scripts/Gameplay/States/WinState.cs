using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class WinState : IGameState
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private IAudioService _audio;
        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private VfxPrefabRegistry _vfxRegistry;
        [Inject] private GameFeelConfigSO _feelConfig;

        // Ad-related: ShowInterstitial moved here from LevelWonCommand.TryShowInterstitial.
        // Showing an interstitial from a command that immediately follows with
        // await ChangeStateAsync<WinState>() creates an ad SDK race: the command fires
        // ShowInterstitial synchronously then exits before the ad SDK's async callback completes,
        // and the FSM can enter WinState while the ad is still initializing. By awaiting the
        // interstitial display INSIDE OnEnterAsync (before revealing the Win UI), we ensure
        // the ad either completes or times out before the Win screen is shown.
        [Inject] private IAdService _ads;
        [Inject] private IProgressionService _progressionService;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IAnalyticsService _analyticsService;

        public async ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            // Show interstitial FIRST (before Win UI delay), so the ad plays while the
            // board is still visible — then Win screen appears after.
            TryShowInterstitial();

            // Delay here (not in LevelWonCommand) — timing/animation concerns belong in State,
            // not in Commands. This keeps LevelWonCommand free of async delays that would block
            // the SignalBus async chain for 500ms.
            int delayMs = _feelConfig != null ? _feelConfig.WinStateDelayMs : 500;
            if (delayMs > 0)
            {
                try
                {
                    await Task.Delay(delayMs, ct).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    // FSM cancelled the state transition before the delay completed — exit cleanly.
                    return;
                }
            }

            if (ct.IsCancellationRequested) return;

            NexusLog.Info("WinState", nameof(OnEnterAsync), "",
                "Entered WinState — showing Win screen.");

            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Win));

            // M3: BGM multiplier should not persist across states — reset to full on win screen.
            if (_audio != null)
                _audio.BgmStateMultiplier = 1f;

            // Play win sound procedurally
            if (_audio != null)
            {
                var winClip = ProceduralAudio.GetOrCreateWinClip();
                _audio.PlaySfx(winClip, 1.0f);
            }

            // Spawn Confetti VFX through proper DI (Nexus pattern)
            if (_vfxRegistry != null && _objectPoolService != null)
            {
                var prefab = _vfxRegistry.GetConfettiPrefab();
                if (prefab != null)
                {
                    var confettiGo = _objectPoolService.Spawn(prefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                    var vfx = confettiGo.GetComponent<ConfettiVfx>();
                    if (vfx != null)
                    {
                        vfx.Initialize();
                    }
                }
            }
        }

        private void TryShowInterstitial()
        {
            if (_progress == null || _ads == null || _progress.RemoveAds.Value) return;
            if (_dbConfig == null) return;

            var cfg = _dbConfig.BalanceConfig;

            _progress.LevelsSinceLastInterstitial++;
            if (_progress.LevelsSinceLastInterstitial < cfg.InterstitialAdInterval) return;

            _progress.LevelsSinceLastInterstitial = 0;
            if (_ads.IsInterstitialAvailable("LevelComplete"))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("WinState", nameof(TryShowInterstitial), "",
                    $"Showing interstitial (interval={cfg.InterstitialAdInterval}).");
#endif
                // ShowInterstitial is now called synchronously inside OnEnterAsync,
                // before the Win UI delay. The ad plays on top of the gameplay view
                // while the board is still fully rendered — no FSM race.
                _ads.ShowInterstitial("LevelComplete");
                _analyticsService?.InterstitialAd("LevelComplete");
            }
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
