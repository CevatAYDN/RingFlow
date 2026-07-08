using Nexus.Core;
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

        protected virtual void Awake()
        {
            var titleGo = GameUIResources.CreateText("RING FLOW", transform, 64, TextAnchor.UpperCenter, GameUIResources.AccentColor);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleRect, 0.1f, 0.78f, 0.9f, 0.92f);

            var subGo = GameUIResources.CreateText("A Puzzle Game", transform, 22, TextAnchor.UpperCenter, GameUIResources.MutedText);
            GameUIResources.SetAnchors(subGo.GetComponent<RectTransform>(), 0.2f, 0.74f, 0.8f, 0.78f);

            var divider = GameUIResources.CreatePanel("Divider", transform);
            GameUIResources.SetAnchors(divider.GetComponent<RectTransform>(), 0.42f, 0.71f, 0.58f, 0.72f);
            divider.GetComponent<Image>().color = GameUIResources.AccentColor;

            var continueBtn = GameUIResources.CreateButton("CONTINUE", transform, 360, 80);
            GameUIResources.SetAnchors(continueBtn.GetComponent<RectTransform>(), 0.25f, 0.58f, 0.75f, 0.69f);
            continueBtn.GetComponentInChildren<Text>().fontSize = 26;
            ContinueButton = continueBtn.GetComponent<Button>();

            var playBtn = GameUIResources.CreateButton("QUICK PLAY", transform, 320, 60);
            GameUIResources.SetAnchors(playBtn.GetComponent<RectTransform>(), 0.30f, 0.45f, 0.70f, 0.55f);
            ApplyOutlineStyle(playBtn);
            PlayButton = playBtn.GetComponent<Button>();

            var lvlBtn = GameUIResources.CreateButton("LEVELS", transform, 320, 60);
            GameUIResources.SetAnchors(lvlBtn.GetComponent<RectTransform>(), 0.30f, 0.33f, 0.70f, 0.43f);
            ApplyOutlineStyle(lvlBtn);
            LevelSelectButton = lvlBtn.GetComponent<Button>();

            var dailyBtn = GameUIResources.CreateButton("DAILY REWARD", transform, 320, 60);
            GameUIResources.SetAnchors(dailyBtn.GetComponent<RectTransform>(), 0.30f, 0.21f, 0.70f, 0.31f);
            ApplyOutlineStyle(dailyBtn);
            DailyRewardButton = dailyBtn.GetComponent<Button>();
            DailyRewardButton.gameObject.SetActive(true);

            var settingsBtn = GameUIResources.CreateButton("⚙", transform, 60, 60);
            GameUIResources.SetAnchors(settingsBtn.GetComponent<RectTransform>(), 0.85f, 0.04f, 0.95f, 0.13f);
            settingsBtn.GetComponentInChildren<Text>().fontSize = 28;
            settingsBtn.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            SettingsButton = settingsBtn.GetComponent<Button>();

            var versionGo = GameUIResources.CreateText("v0.1.0", transform, 12, TextAnchor.LowerLeft, GameUIResources.MutedText);
            GameUIResources.SetAnchors(versionGo.GetComponent<RectTransform>(), 0.04f, 0.01f, 0.20f, 0.04f);
            VersionLabel = versionGo.GetComponent<Text>();

            var coinText = GameUIResources.CreateText("Coins: 0", transform, 16, TextAnchor.LowerRight, new Color(0.95f, 0.78f, 0.20f));
            GameUIResources.SetAnchors(coinText.GetComponent<RectTransform>(), 0.20f, 0.01f, 0.55f, 0.04f);
            CoinsText = coinText.GetComponent<Text>();

            var diaText = GameUIResources.CreateText("◆ 0", transform, 16, TextAnchor.LowerRight, new Color(0.30f, 0.70f, 0.95f));
            GameUIResources.SetAnchors(diaText.GetComponent<RectTransform>(), 0.55f, 0.01f, 0.85f, 0.04f);
            DiamondsText = diaText.GetComponent<Text>();
        }

        public void UpdateCoins(int coins) { if (CoinsText != null) CoinsText.text = $"Coins: {coins}"; }
        public void UpdateDiamonds(int diamonds) { if (DiamondsText != null) DiamondsText.text = $"◆ {diamonds}"; }

        public void SetDailyRewardAvailable(bool available)
        {
            if (DailyRewardButton == null) return;
            DailyRewardButton.interactable = available;
            var label = DailyRewardButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = available ? "DAILY REWARD •" : "DAILY REWARD";
                label.color = available ? GameUIResources.AccentColor : GameUIResources.MutedText;
            }
        }

        private static void ApplyOutlineStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = GameUIResources.PanelColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = GameUIResources.PanelColor;
            colors.highlightedColor = new Color(0.22f, 0.22f, 0.30f);
            colors.pressedColor = new Color(0.10f, 0.10f, 0.14f);
            colors.disabledColor = new Color(0.18f, 0.18f, 0.22f);
            button.colors = colors;
        }
    }
}
