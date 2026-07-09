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
            BindReferencesFromChildren();
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
