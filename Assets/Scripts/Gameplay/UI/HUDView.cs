using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View
    {
        public Text MovesText { get; private set; }
        public Text LevelText { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Button UndoButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button PauseButton { get; private set; }
        public Button HintButton { get; private set; }

        protected virtual void Awake()
        {
            var topBar = GameUIResources.CreatePanel("TopBar", transform);
            GameUIResources.SetAnchors(topBar.GetComponent<RectTransform>(), 0f, 0.88f, 1f, 1f);
            topBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);

            var lvlGo = GameUIResources.CreateText("Level 1", transform, 18, TextAnchor.MiddleLeft, GameUIResources.MutedText);
            GameUIResources.SetAnchors(lvlGo.GetComponent<RectTransform>(), 0.04f, 0.94f, 0.22f, 0.99f);
            LevelText = lvlGo.GetComponent<Text>();

            var movesGo = GameUIResources.CreateText("Moves: 0", transform, 26, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            GameUIResources.SetAnchors(movesGo.GetComponent<RectTransform>(), 0.22f, 0.92f, 0.62f, 0.99f);
            movesGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            MovesText = movesGo.GetComponent<Text>();

            var pauseBtn = GameUIResources.CreateButton("II", transform, 60, 60);
            GameUIResources.SetAnchors(pauseBtn.GetComponent<RectTransform>(), 0.88f, 0.90f, 0.98f, 0.98f);
            pauseBtn.GetComponent<Image>().color = GameUIResources.PanelColor;
            ApplyIconStyle(pauseBtn.GetComponent<Button>());
            PauseButton = pauseBtn.GetComponent<Button>();

            var bottomBar = GameUIResources.CreatePanel("BottomBar", transform);
            GameUIResources.SetAnchors(bottomBar.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.14f);
            bottomBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);

            var coinPanel = GameUIResources.CreatePanel("CoinPanel", bottomBar.transform);
            GameUIResources.SetAnchors(coinPanel.GetComponent<RectTransform>(), 0.04f, 0.30f, 0.45f, 0.85f);
            var coinImg = coinPanel.GetComponent<Image>();
            coinImg.color = new Color(0.95f, 0.78f, 0.20f, 0.85f);

            var coinText = GameUIResources.CreateText("0", coinPanel.transform, 18, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            coinText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(coinText.GetComponent<RectTransform>(), 0.18f, 0f, 1f, 1f);
            CoinsText = coinText.GetComponent<Text>();

            var diaPanel = GameUIResources.CreatePanel("DiaPanel", bottomBar.transform);
            GameUIResources.SetAnchors(diaPanel.GetComponent<RectTransform>(), 0.55f, 0.30f, 0.96f, 0.85f);
            var diaImg = diaPanel.GetComponent<Image>();
            diaImg.color = new Color(0.30f, 0.70f, 0.95f, 0.85f);

            var diaText = GameUIResources.CreateText("0", diaPanel.transform, 18, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            diaText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(diaText.GetComponent<RectTransform>(), 0.18f, 0f, 1f, 1f);
            DiamondsText = diaText.GetComponent<Text>();

            var undoBtn = GameUIResources.CreateButton("UNDO", transform, 140, 44);
            GameUIResources.SetAnchors(undoBtn.GetComponent<RectTransform>(), 0.04f, 0.025f, 0.32f, 0.10f);
            ApplyOutlineStyle(undoBtn);
            UndoButton = undoBtn.GetComponent<Button>();

            var restartBtn = GameUIResources.CreateButton("RESTART", transform, 160, 44);
            GameUIResources.SetAnchors(restartBtn.GetComponent<RectTransform>(), 0.36f, 0.025f, 0.64f, 0.10f);
            ApplyOutlineStyle(restartBtn);
            RestartButton = restartBtn.GetComponent<Button>();

            var hintBtn = GameUIResources.CreateButton("HINT", transform, 140, 44);
            GameUIResources.SetAnchors(hintBtn.GetComponent<RectTransform>(), 0.68f, 0.025f, 0.96f, 0.10f);
            ApplyOutlineStyle(hintBtn);
            HintButton = hintBtn.GetComponent<Button>();
        }

        public void UpdateMoves(int moves)
        {
            if (MovesText != null) MovesText.text = $"Moves: {moves}";
        }

        public void UpdateLevel(int level)
        {
            if (LevelText != null) LevelText.text = $"Level {level}";
        }

        public void UpdateCoins(int coins)
        {
            if (CoinsText != null) CoinsText.text = coins.ToString();
        }

        public void UpdateDiamonds(int diamonds)
        {
            if (DiamondsText != null) DiamondsText.text = diamonds.ToString();
        }

        private static void ApplyOutlineStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = GameUIResources.SurfaceColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = GameUIResources.SurfaceColor;
            colors.highlightedColor = new Color(0.20f, 0.22f, 0.28f);
            colors.pressedColor = new Color(0.10f, 0.11f, 0.14f);
            button.colors = colors;
        }

        private static void ApplyIconStyle(Button button)
        {
            var colors = button.colors;
            colors.normalColor = GameUIResources.PanelColor;
            colors.highlightedColor = new Color(0.20f, 0.22f, 0.28f);
            colors.pressedColor = new Color(0.10f, 0.11f, 0.14f);
            button.colors = colors;
        }
    }
}
