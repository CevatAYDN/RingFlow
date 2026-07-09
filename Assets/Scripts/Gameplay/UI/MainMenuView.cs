using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(MainMenuMediator))]
    public class MainMenuView : View
    {
        public Button ContinueButton { get; private set; }
        public Button PlayButton { get; private set; }
        public Button LevelSelectButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button DailyRewardButton { get; private set; }
        public Text VersionLabel { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Text TitleText { get; private set; }
        public Text TaglineText { get; private set; }
        private GameObject _continueBtn, _playBtn, _lvlBtn, _dailyBtn;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        public void UpdateCoins(int coins) { if (CoinsText != null) CoinsText.text = $"Coins: {coins}"; }
        public void UpdateDiamonds(int diamonds) { if (DiamondsText != null) DiamondsText.text = $"◆ {diamonds}"; }

        private ILocalizationService _locService;

        public void Localize(ILocalizationService loc)
        {
            _locService = loc;
            GameUIResources.LocalizeButtonText(_continueBtn, "menu_continue", loc);
            GameUIResources.LocalizeButtonText(_playBtn, "menu_quick_play", loc);
            GameUIResources.LocalizeButtonText(_lvlBtn, "menu_levels", loc);
            GameUIResources.LocalizeButtonText(_dailyBtn, "menu_daily_reward", loc);
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_title", loc);
            if (TaglineText != null) GameUIResources.LocalizeText(TaglineText.gameObject, "game_tagline", loc);
        }

        public void SetDailyRewardAvailable(bool available)
        {
            if (DailyRewardButton == null) return;
            DailyRewardButton.interactable = available;
            var label = DailyRewardButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                string baseText = _locService != null ? _locService.GetString("menu_daily_reward", "DAILY REWARD") : "DAILY REWARD";
                label.text = available ? $"{baseText} •" : baseText;
                label.color = available ? GameUIResources.AccentColor : GameUIResources.MutedText;
            }
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.Contains("CONTINUE")) { _continueBtn = btn.gameObject; ContinueButton = btn; }
                else if (btn.name.Contains("QUICK PLAY")) { _playBtn = btn.gameObject; PlayButton = btn; }
                else if (btn.name.Contains("LEVELS")) { _lvlBtn = btn.gameObject; LevelSelectButton = btn; }
                else if (btn.name.Contains("DAILY REWARD")) { _dailyBtn = btn.gameObject; DailyRewardButton = btn; }
                else if (btn.name.Contains("⚙")) SettingsButton = btn;
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                // Only consider texts that are direct children of the MainMenuView canvas to avoid matching button labels
                if (txt.transform.parent != transform) continue;

                if (txt.fontSize == 64)
                {
                    TitleText = txt;
                }
                else if (txt.fontSize == 22)
                {
                    TaglineText = txt;
                }
                else if (txt.fontSize == 12)
                {
                    VersionLabel = txt;
                }
                else if (txt.fontSize == 16)
                {
                    // Differentiate coins vs diamonds by text content or color
                    if (txt.text.Contains("Coins") || (txt.color.r > 0.9f && txt.color.b < 0.3f))
                    {
                        CoinsText = txt;
                    }
                    else
                    {
                        DiamondsText = txt;
                    }
                }
            }
        }
    }
}
