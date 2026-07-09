using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(DailyRewardPopupMediator))]
    public class DailyRewardPopupView : View
    {
        public Button ClaimButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text DayText { get; private set; }
        public Text RewardText { get; private set; }
        private GameObject _claimBtn, _closeBtn;

        private void Awake()
        {
            if (transform.childCount > 0) return;

            // ── Dimmed background overlay ─────────────
            var overlay = GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.80f);

            // ── Centered card ─────────────────────────────────────────
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.20f, 0.25f, 0.80f, 0.75f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            // ── Title ────────────────────────────────────────────────
            var title = GameUIResources.CreateText("DAILY REWARD", transform, 36, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = title.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(title.GetComponent<RectTransform>(), 0.2f, 0.66f, 0.8f, 0.74f);

            // ── Day label ────────────────────────────────────────────
            var day = GameUIResources.CreateText("Day 1", transform, 64, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            day.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(day.GetComponent<RectTransform>(), 0.2f, 0.50f, 0.8f, 0.62f);
            DayText = day.GetComponent<Text>();

            // ── Reward line ──────────────────────────────────────────
            var reward = GameUIResources.CreateText("+100 Coins", transform, 24, TextAnchor.MiddleCenter, GameUIResources.SuccessColor);
            GameUIResources.SetAnchors(reward.GetComponent<RectTransform>(), 0.2f, 0.42f, 0.8f, 0.50f);
            RewardText = reward.GetComponent<Text>();

            // ── Claim button (primary) ───────────────────────────────
            _claimBtn = GameUIResources.CreateButton("CLAIM", transform, 280, 64);
            GameUIResources.SetAnchors(_claimBtn.GetComponent<RectTransform>(), 0.30f, 0.32f, 0.70f, 0.40f);
            ClaimButton = _claimBtn.GetComponent<Button>();

            // ── Close button (secondary) ─────────────────────────────
            _closeBtn = GameUIResources.CreateButton("CLOSE", transform, 200, 48);
            GameUIResources.SetAnchors(_closeBtn.GetComponent<RectTransform>(), 0.36f, 0.26f, 0.64f, 0.30f);
            GameUIResources.ApplySecondaryStyle(_closeBtn);
            CloseButton = _closeBtn.GetComponent<Button>();
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(TitleText.gameObject, "daily_reward_title", loc);
            GameUIResources.LocalizeButtonText(_claimBtn, "daily_reward_claim", loc);
            GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
        }

        public void ShowReward(int dayIndex, string rewardText)
        {
            if (DayText != null) DayText.text = $"Day {dayIndex + 1}";
            if (RewardText != null) RewardText.text = rewardText;
            if (ClaimButton != null) ClaimButton.interactable = true;
        }
    }
}
