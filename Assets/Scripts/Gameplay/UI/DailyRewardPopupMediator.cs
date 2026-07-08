using Nexus.Core;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Read-only mediator for the Daily Reward popup. The claim + economy
    /// mutation is delegated to <see cref="Commands.DailyRewardClaimCommand"/>
    /// so all daily-reward triggers (UI button, scheduled, push) share one
    /// code path.
    /// </summary>
    public class DailyRewardPopupMediator : Mediator<DailyRewardPopupView>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private DailyRewardService _dailyReward;

        protected override void OnBind()
        {
            if (View.ClaimButton != null) View.ClaimButton.onClick.AddListener(OnClaimClicked);
            if (View.CloseButton != null) View.CloseButton.onClick.AddListener(OnCloseClicked);

            Subscribe<DailyRewardGrantedSignal>(_ => OnCloseClicked());

            if (_dailyReward != null && _progress != null)
            {
                int previewDay = _dailyReward.DayIndexPreview;
                int tableIndex = previewDay - 1;
                if (tableIndex < 0) tableIndex = 0;

                var reward = DailyRewardTable.RewardForDayIndex(tableIndex);
                string rewardText = reward.Amount > 0
                    ? $"+{reward.Amount} {reward.CurrencyId}"
                    : reward.CurrencyId;
                View.ShowReward(tableIndex, rewardText);

                if (View.ClaimButton != null)
                {
                    View.ClaimButton.interactable = _dailyReward.CanClaimNow();
                }
            }
        }

        private void OnClaimClicked()
        {
            if (_dailyReward == null || !_dailyReward.CanClaimNow()) return;
            SignalBus.Fire(new DailyRewardClaimSignal());
        }

        private void OnCloseClicked()
        {
            SignalBus.Fire(new HideScreenSignal(ScreenType.DailyReward));
        }

        protected override void OnUnbind()
        {
            if (View.ClaimButton != null) View.ClaimButton.onClick.RemoveAllListeners();
            if (View.CloseButton != null) View.CloseButton.onClick.RemoveAllListeners();
        }
    }
}
