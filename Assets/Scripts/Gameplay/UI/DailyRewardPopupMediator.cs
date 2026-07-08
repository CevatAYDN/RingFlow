using Nexus.Core;
using Nexus.Core.Services;

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
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            View.Localize(_loc);
            if (View.ClaimButton != null) View.ClaimButton.onClick.AddListener(OnClaimClicked);
            if (View.CloseButton != null) View.CloseButton.onClick.AddListener(OnCloseClicked);

            Subscribe<DailyRewardGrantedSignal>(_ => OnCloseClicked());

            if (_dailyReward != null && _progress != null)
            {
                // m7 fix: DayIndexPreview is already "next claim index" (DailyDayIndex + 1).
                // Do NOT subtract 1 — the preview must match what Claim() actually rewards.
                int previewDay = _dailyReward.DayIndexPreview;

                var reward = DailyRewardTable.RewardForDayIndex(previewDay);
                string rewardText = reward.Amount > 0
                    ? $"+{reward.Amount} {reward.CurrencyId}"
                    : reward.CurrencyId;
                View.ShowReward(previewDay, rewardText);

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
