using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(GameOverMediator))]
    public class GameOverView : View
    {
        public Button RestartButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text MessageText { get; private set; }

        private GameObject _restartBtn, _quitBtn;

        private void Awake()
        {
            if (transform.childCount > 0) return;

            // Dark overlay
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0, 0, 0, 0.75f);
            }

            // Centered panel card
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.16f, 0.25f, 0.84f, 0.75f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            // Title "GAME OVER"
            var titleGo = GameUIResources.CreateText("GAME OVER", transform, 52, TextAnchor.MiddleCenter, GameUIResources.DangerColor);
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.2f, 0.60f, 0.8f, 0.70f);

            // Message e.g. "A bomb exploded!"
            var msgGo = GameUIResources.CreateText("Level Failed!", transform, 24, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            MessageText = msgGo.GetComponent<Text>();
            GameUIResources.SetAnchors(msgGo.GetComponent<RectTransform>(), 0.2f, 0.48f, 0.8f, 0.58f);

            // RESTART Button
            _restartBtn = GameUIResources.CreateButton("RESTART", transform, 300, 68);
            GameUIResources.SetAnchors(_restartBtn.GetComponent<RectTransform>(), 0.28f, 0.34f, 0.72f, 0.44f);
            RestartButton = _restartBtn.GetComponent<Button>();
            GameUIResources.ApplyPrimaryStyle(_restartBtn);

            // MAIN MENU Button
            _quitBtn = GameUIResources.CreateButton("MAIN MENU", transform, 300, 56);
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.28f, 0.24f, 0.72f, 0.32f);
            QuitButton = _quitBtn.GetComponent<Button>();
            GameUIResources.ApplyOutlineStyle(_quitBtn);
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(TitleText.gameObject, "game_over_title", loc);
            GameUIResources.LocalizeText(MessageText.gameObject, "game_over_message", loc);
            GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
        }
    }
}
