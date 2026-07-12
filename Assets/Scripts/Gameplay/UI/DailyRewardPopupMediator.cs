using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Diagnostics;

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
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private DailyRewardService _dailyReward;
        [Inject] private ILocalizationService _loc;
        [Inject] private IGameDiagnostics _diag;
        [Inject] private IViewMediatorTracker _tracker;

        protected override void OnBind()
        {
            _diag?.Checkpoint("DailyRewardPopupMediator.OnBind");
            if (View == null)
            {
                NexusLog.Error("DailyRewardPopupMediator", nameof(OnBind), "", "DailyRewardPopupView not bound.");
                return;
            }
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            View.Localize(_loc);
            if (View.ClaimButton != null) View.ClaimButton.onClick.AddListener(OnClaimClicked);
            if (View.CloseButton != null) View.CloseButton.onClick.AddListener(OnCloseClicked);

            Subscribe<DailyRewardGrantedSignal>(_ => OnCloseClicked());

            if (_dailyReward != null && _progress != null)
            {
                int previewDay = _dailyReward.DayIndexPreview;
                var rewardList = _dbConfig?.BalanceConfig.DailyRewards ?? new System.Collections.Generic.List<DailyRewardEntry>();
                var reward = DailyRewardTable.RewardForDayIndex(rewardList, previewDay);
                string rewardText = reward.Amount > 0
                    ? $"+{reward.Amount} {reward.CurrencyId}"
                    : reward.CurrencyId;
                View.ShowReward(previewDay, rewardText);

                bool canClaim = _dailyReward.CanClaimNow();
                if (View.ClaimButton != null)
                    View.ClaimButton.interactable = canClaim;

                _diag?.Log("DailyRewardPopupMediator", $"Bound. Day={previewDay}, Reward={rewardText}, CanClaim={canClaim}.");
            }
        }

        private void OnClaimClicked()
        {
            if (_dailyReward == null || !_dailyReward.CanClaimNow()) return;
            _diag?.Log("DailyRewardPopupMediator", "Claim clicked.");
            SignalBus.Fire(new DailyRewardClaimSignal());
        }

        private void OnCloseClicked()
        {
            _diag?.Log("DailyRewardPopupMediator", "Close clicked.");
            SignalBus.Fire(new HideScreenSignal(ScreenType.DailyReward));
        }

        protected override void OnUnbind()
        {
            _tracker?.TrackViewUnbound(View?.GetType());
            if (View.ClaimButton != null) View.ClaimButton.onClick.RemoveAllListeners();
            if (View.CloseButton != null) View.CloseButton.onClick.RemoveAllListeners();
        }
    }
}
