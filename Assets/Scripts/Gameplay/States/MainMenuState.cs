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
        [Inject] private ISignalBus _signalBus;
        [Inject] private DailyRewardService _dailyReward;
        [Inject] private IAdService _adService;
        [Inject] private PlayerProgressModel _progress;

        [Inject] private Diagnostics.IGameDiagnostics _diag;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _diag?.Checkpoint("MainMenuState");
            _diag?.Log("FSM", "MainMenuState.OnEnterAsync started");
            if (_diag != null)
            {
                var elapsed = _diag.GetElapsedSinceCheckpoint("BootState");
                _diag.Log("FSM", $"Time since BootState: {elapsed.TotalMilliseconds}ms");
            }
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.MainMenu));

            if (_audio != null)
            {
                // FIX P0.3: GDD §12 says the menu ducks BGM to 70%. Set the *state multiplier*,
                // not BgmVolume — leaving BgmVolume untouched preserves the user's slider value.
                _audio.BgmStateMultiplier = 0.70f;
            }

            if (_dailyReward != null && _dailyReward.CanClaimNow())
            {
                _signalBus?.Fire(new ShowScreenSignal(ScreenType.DailyReward));
            }

            if (_adService != null && (_progress == null || !_progress.RemoveAds.Value))
            {
                _adService.ShowBanner("MainMenu", "bottom");
            }

            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct)
        {
            if (_adService != null)
            {
                _adService.HideBanner();
            }
            return default;
        }
        public void OnTick(float deltaTime) {}
    }
}
