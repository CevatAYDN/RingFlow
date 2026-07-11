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
            if (NeedsSelfBuild())
            {
                BuildUI();
            }
            else
            {
                BindReferencesFromChildren();
            }
        }

        private bool NeedsSelfBuild()
        {
            if (transform.childCount == 0) return true;
            BindReferencesFromChildren();
            return ClaimButton == null || CloseButton == null;
        }

        private void BuildUI()
        {
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0, 0, 0, 0.75f);
                overlay.raycastTarget = true;
            }

            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.10f, 0.20f, 0.90f, 0.80f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;
            card.GetComponent<Image>().raycastTarget = true;

            var titleGo = GameUIResources.CreateText("DAILY REWARD", card.transform, 36, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            TitleText.name = "Title";
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.05f, 0.72f, 0.95f, 0.85f);

            var dayGo = GameUIResources.CreateText("Day 1", card.transform, 64, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            DayText = dayGo.GetComponent<Text>();
            DayText.fontStyle = FontStyle.Bold;
            DayText.name = "Day";
            GameUIResources.SetAnchors(dayGo.GetComponent<RectTransform>(), 0.20f, 0.42f, 0.80f, 0.62f);

            var rewardGo = GameUIResources.CreateText("+50 Coins", card.transform, 24, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            RewardText = rewardGo.GetComponent<Text>();
            RewardText.name = "Reward";
            GameUIResources.SetAnchors(rewardGo.GetComponent<RectTransform>(), 0.20f, 0.32f, 0.80f, 0.42f);

            var claimBtnGo = GameUIResources.CreateButton("CLAIM", card.transform, 300, 64);
            ClaimButton = claimBtnGo.GetComponent<Button>();
            ClaimButton.name = "Claim";
            _claimBtn = claimBtnGo;
            GameUIResources.SetAnchors(claimBtnGo.GetComponent<RectTransform>(), 0.15f, 0.14f, 0.85f, 0.26f);

            var closeBtnGo = GameUIResources.CreateButton("CLOSE", card.transform, 120, 40);
            CloseButton = closeBtnGo.GetComponent<Button>();
            CloseButton.name = "Close";
            _closeBtn = closeBtnGo;
            GameUIResources.ApplySecondaryStyle(closeBtnGo);
            var closeText = closeBtnGo.GetComponentInChildren<Text>();
            if (closeText != null) closeText.fontSize = 16;
            GameUIResources.SetAnchors(closeBtnGo.GetComponent<RectTransform>(), 0.40f, 0.04f, 0.60f, 0.12f);
        }

        public void Localize(ILocalizationService loc)
        {
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "daily_reward_title", loc);
            GameUIResources.LocalizeButtonText(_claimBtn, "daily_reward_claim", loc);
            GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
        }

        public void ShowReward(int dayIndex, string rewardText)
        {
            if (DayText != null) DayText.text = $"Day {dayIndex + 1}";
            if (RewardText != null) RewardText.text = rewardText;
            if (ClaimButton != null) ClaimButton.interactable = true;
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.ToUpper().Contains("CLAIM")) { _claimBtn = btn.gameObject; ClaimButton = btn; }
                else if (btn.name.ToUpper().Contains("CLOSE")) { _closeBtn = btn.gameObject; CloseButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.transform.parent != transform && txt.transform.parent?.parent != transform) continue;

                if (txt.fontSize == 36 || txt.text.ToUpper().Contains("REWARD") || txt.name.ToUpper().Contains("TITLE"))
                {
                    TitleText = txt;
                }
                else if (txt.fontSize == 64 || txt.text.ToUpper().Contains("DAY") || txt.name.ToUpper().Contains("DAY"))
                {
                    DayText = txt;
                }
                else if (txt.fontSize == 24 || txt.text.ToUpper().Contains("COIN") || txt.name.ToUpper().Contains("REWARD"))
                {
                    RewardText = txt;
                }
            }
        }
    }
}
