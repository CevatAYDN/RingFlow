using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay.UI
{
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
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            if (View == null) return;

            View.Localize(_loc);
            View.ClaimButton?.onClick.AddListener(OnClaimClicked);
            View.CloseButton?.onClick.AddListener(OnCloseClicked);

            Subscribe<DailyRewardGrantedSignal>(_ => OnCloseClicked());

            if (_dailyReward != null && _progress != null)
            {
                int previewDay = _dailyReward.DayIndexPreview;
                var rewardList = _dbConfig?.BalanceConfig.DailyRewards ?? new System.Collections.Generic.List<DailyRewardEntry>();
                var reward = DailyRewardTable.RewardForDayIndex(rewardList, previewDay);
                string rewardText = FormatRewardText(reward);
                int streak = _dailyReward.GetCurrentStreak();
                View.ShowReward(previewDay, rewardText, streak);

                bool canClaim = _dailyReward.CanClaimNow();
                if (View.ClaimButton != null)
                    View.ClaimButton.interactable = canClaim;

                _diag?.Log("DailyRewardPopupMediator", $"Bound. Day={previewDay}, Streak={streak}, CanClaim={canClaim}.");
            }
        }

        private string FormatRewardText(CurrencyAmount reward)
        {
            if (reward.Amount <= 0) return reward.CurrencyId;
            string currencyKey = $"currency_{reward.CurrencyId.ToLowerInvariant()}";
            string currencyName = _loc?.GetString(currencyKey, reward.CurrencyId) ?? reward.CurrencyId;
            string format = _loc?.GetString("reward_amount_format", "+{0} {1}") ?? "+{0} {1}";
            return string.Format(format, reward.Amount, currencyName);
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
            View.ClaimButton?.onClick.RemoveAllListeners();
            View.CloseButton?.onClick.RemoveAllListeners();
        }
    }
}
