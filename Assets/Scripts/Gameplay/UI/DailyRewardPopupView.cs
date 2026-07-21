using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(DailyRewardPopupMediator))]
    public class DailyRewardPopupView : View, IAuthoredView
    {
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _dayText;
        [SerializeField] private TextMeshProUGUI _rewardText;
        [SerializeField] private TextMeshProUGUI _streakText;
        [SerializeField] private CanvasGroup _cardGroup;
        [SerializeField] private Image _rewardIcon;
        public Button ClaimButton => _claimButton;
        public Button CloseButton => _closeButton;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI DayText => _dayText;
        public TextMeshProUGUI RewardText => _rewardText;
        public TextMeshProUGUI StreakText => _streakText;
        public CanvasGroup CardGroup => _cardGroup;
        public Image RewardIcon => _rewardIcon;

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
                if (btn.name.ToUpperInvariant().Contains("CLAIM")) { _claimBtn = btn.gameObject; _claimButton = btn; }
                else if (btn.name.ToUpperInvariant().Contains("CLOSE")) { _closeBtn = btn.gameObject; _closeButton = btn; }
            }
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) _titleText = txt;
                else if (upper.Contains("DAY")) _dayText = txt;
                else if (upper.Contains("REWARD")) _rewardText = txt;
                else if (upper.Contains("STREAK")) _streakText = txt;
            }

            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.name.ToUpperInvariant().Contains("REWARD") ||
                    img.name.ToUpperInvariant().Contains("ICON"))
                {
                    _rewardIcon = img;
                    break;
                }
            }

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }

        private void ApplyBaseStyling()
        {
            if (_claimBtn != null) GameUIResources.ApplyAccentStyle(_claimBtn);
            if (_closeBtn != null) GameUIResources.ApplyTextButtonStyle(_closeBtn);
        }
    }
}
