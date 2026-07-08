using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class MainMenuMediator : Mediator<MainMenuView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private DailyRewardService _dailyReward;

        protected override void OnBind()
        {
            View.ContinueButton.onClick.AddListener(ContinueGame);
            View.PlayButton.onClick.AddListener(QuickPlay);
            View.LevelSelectButton.onClick.AddListener(GoToLevelSelect);
            View.SettingsButton.onClick.AddListener(OpenSettings);
            View.DailyRewardButton.onClick.AddListener(OpenDailyReward);

            bool canClaim = _dailyReward != null && _dailyReward.CanClaimNow();
            View.SetDailyRewardAvailable(canClaim);

            if (_progress != null)
            {
                _progress.Coins.OnChanged((_, n) => View.UpdateCoins(n));
                View.UpdateCoins(_progress.Coins.Value);
                _progress.Diamonds.OnChanged((_, n) => View.UpdateDiamonds(n));
                View.UpdateDiamonds(_progress.Diamonds.Value);
            }
        }

        private void ContinueGame()
        {
            int level = _progression?.CurrentLevel.Value ?? 1;
            SignalBus.Fire(new LevelSelectedSignal(level));
        }

        private void QuickPlay()
        {
            int level = _progression?.CurrentLevel.Value ?? 1;
            SignalBus.Fire(new LevelSelectedSignal(level));
        }

        private void GoToLevelSelect()
        {
            SignalBus.Fire(new PlayRequestedSignal());
        }

        private void OpenSettings()
        {
            SignalBus.Fire(new OpenSettingsSignal());
        }

        private void OpenDailyReward()
        {
            SignalBus.Fire(new OpenDailyRewardSignal());
        }

        protected override void OnUnbind()
        {
            View.ContinueButton.onClick.RemoveAllListeners();
            View.PlayButton.onClick.RemoveAllListeners();
            View.LevelSelectButton.onClick.RemoveAllListeners();
            View.SettingsButton.onClick.RemoveAllListeners();
            View.DailyRewardButton.onClick.RemoveAllListeners();
        }
    }
}
