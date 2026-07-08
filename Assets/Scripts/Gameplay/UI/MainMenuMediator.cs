using System;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class MainMenuMediator : Mediator<MainMenuView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private DailyRewardService _dailyReward;
        [Inject] private ILocalizationService _loc;

        private Action<int, int> _coinsHandler;
        private Action<int, int> _diamondsHandler;

        protected override void OnBind()
        {
            View.Localize(_loc);
            View.ContinueButton.onClick.AddListener(ContinueGame);
            View.PlayButton.onClick.AddListener(QuickPlay);
            View.LevelSelectButton.onClick.AddListener(GoToLevelSelect);
            View.SettingsButton.onClick.AddListener(OpenSettings);
            View.DailyRewardButton.onClick.AddListener(OpenDailyReward);

            bool canClaim = _dailyReward != null && _dailyReward.CanClaimNow();
            View.SetDailyRewardAvailable(canClaim);

            if (_progress != null)
            {
                _coinsHandler = (_, n) => View?.UpdateCoins(n);
                _diamondsHandler = (_, n) => View?.UpdateDiamonds(n);
                _progress.Coins.OnChanged(_coinsHandler);
                View.UpdateCoins(_progress.Coins.Value);
                _progress.Diamonds.OnChanged(_diamondsHandler);
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

            if (_progress != null)
            {
                if (_coinsHandler != null) _progress.Coins.RemoveOnChanged(_coinsHandler);
                if (_diamondsHandler != null) _progress.Diamonds.RemoveOnChanged(_diamondsHandler);
            }
            _coinsHandler = null;
            _diamondsHandler = null;
        }
    }
}
