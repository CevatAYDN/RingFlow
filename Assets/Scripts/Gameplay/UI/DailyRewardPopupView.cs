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
            if (NeedsSelfBuild()) { BuildUI(); ApplyBaseStyling(); }
            else { BindReferencesFromChildren(); ApplyBaseStyling(); }
        }

        private bool NeedsSelfBuild()
        {
            if (transform.childCount == 0) return true;
            BindReferencesFromChildren();
            return ClaimButton == null || CloseButton == null;
        }

        public void BuildUI()
        {
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = GameUIResources.OverlayMedium;
                overlay.raycastTarget = true;
            }

            var cardGo = GameUIResources.CreateCard("Card", transform, GameUIResources.SurfaceDark);
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.08f, 0.14f, 0.92f, 0.86f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Accent bar
            var accent = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(cardGo.transform, false);
            GameUIResources.SetAnchors(accent.GetComponent<RectTransform>(), 0.08f, 0.80f, 0.92f, 0.82f);
            accent.GetComponent<Image>().color = GameUIResources.AccentColor;

            var titleGo = GameUIResources.CreateDisplayText("DAILY REWARD", cardGo.transform, 36, GameUIResources.AccentColor);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.05f, 0.68f, 0.95f, 0.78f);

            // Day circle
            var dayBg = new GameObject("DayCircle", typeof(RectTransform), typeof(Image));
            dayBg.transform.SetParent(cardGo.transform, false);
            GameUIResources.SetAnchors(dayBg.GetComponent<RectTransform>(), 0.35f, 0.44f, 0.65f, 0.66f);
            dayBg.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.20f);
            dayBg.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            dayBg.GetComponent<Image>().type = Image.Type.Sliced;

            var dayGo = GameUIResources.CreateDisplayText("Day 1", cardGo.transform, 52, GameUIResources.TextOnDark);
            dayGo.name = "Day";
            DayText = dayGo.GetComponent<Text>();
            GameUIResources.SetAnchors(dayGo.GetComponent<RectTransform>(), 0.20f, 0.46f, 0.80f, 0.64f);

            var streakGo = GameUIResources.CreateText("", cardGo.transform, 14, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            streakGo.name = "Streak";
            StreakText = streakGo.GetComponent<Text>();
            GameUIResources.SetAnchors(streakGo.GetComponent<RectTransform>(), 0.15f, 0.38f, 0.85f, 0.42f);

            var rewardGo = GameUIResources.CreateText("", cardGo.transform, 22, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            rewardGo.name = "Reward";
            RewardText = rewardGo.GetComponent<Text>();
            GameUIResources.SetAnchors(rewardGo.GetComponent<RectTransform>(), 0.12f, 0.30f, 0.88f, 0.36f);

            _claimBtn = GameUIResources.CreateButton("CLAIM", cardGo.transform, 300, 60);
            _claimBtn.name = "Btn_CLAIM";
            GameUIResources.ApplyAccentStyle(_claimBtn);
            ClaimButton = _claimBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_claimBtn.GetComponent<RectTransform>(), 0.18f, 0.16f, 0.82f, 0.26f);

            _closeBtn = GameUIResources.CreateButton("CLOSE", cardGo.transform, 120, 38);
            _closeBtn.name = "Btn_CLOSE";
            GameUIResources.ApplyTextButtonStyle(_closeBtn);
            CloseButton = _closeBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_closeBtn.GetComponent<RectTransform>(), 0.40f, 0.04f, 0.60f, 0.10f);
        }

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
