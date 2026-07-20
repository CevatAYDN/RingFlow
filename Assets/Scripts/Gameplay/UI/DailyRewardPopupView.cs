using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(DailyRewardPopupMediator))]
    public class DailyRewardPopupView : View, IAuthoredView
    {
        public Button ClaimButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text DayText { get; private set; }
        public Text RewardText { get; private set; }
        public Text StreakText { get; private set; }
        public CanvasGroup CardGroup { get; private set; }
        public Image RewardIcon { get; private set; }

        private GameObject _claimBtn, _closeBtn;
        private ILocalizationService _locService;

        private void Awake()
        {
            BindReferencesFromChildren();
            ApplyBaseStyling();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void Localize(ILocalizationService loc)
        {
            _locService = loc;
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "daily_reward_title", loc);
            if (_claimBtn != null) GameUIResources.LocalizeButtonText(_claimBtn, "daily_reward_claim", loc);
            if (_closeBtn != null) GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
        }

        public void ShowReward(int dayIndex, string rewardText, int streak = 0)
        {
            if (DayText != null)
            {
                string dayFormat = _locService?.GetString("daily_reward_day", "Day {0}") ?? "Day {0}";
                DayText.text = string.Format(dayFormat, dayIndex + 1);
            }
            if (RewardText != null) RewardText.text = rewardText;
            if (StreakText != null)
            {
                if (streak > 1)
                {
                    string streakFormat = _locService?.GetString("daily_reward_streak", "{0}-day streak!") ?? "{0}-day streak!";
                    StreakText.text = string.Format(streakFormat, streak);
                }
                else
                {
                    StreakText.text = string.Empty;
                }
            }
            if (ClaimButton != null) ClaimButton.interactable = true;
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                if (btn.name.ToUpperInvariant().Contains("CLAIM")) { _claimBtn = btn.gameObject; ClaimButton = btn; }
                else if (btn.name.ToUpperInvariant().Contains("CLOSE")) { _closeBtn = btn.gameObject; CloseButton = btn; }
            }
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) TitleText = txt;
                else if (upper.Contains("DAY")) DayText = txt;
                else if (upper.Contains("REWARD")) RewardText = txt;
                else if (upper.Contains("STREAK")) StreakText = txt;
            }
        }

        private void ApplyBaseStyling()
        {
            if (_claimBtn != null) GameUIResources.ApplyAccentStyle(_claimBtn);
            if (_closeBtn != null) GameUIResources.ApplyTextButtonStyle(_closeBtn);
        }
    }
}
