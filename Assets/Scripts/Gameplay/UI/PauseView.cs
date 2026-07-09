using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(PauseMediator))]
    public class PauseView : View
    {
        public Button ResumeButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text ProgressLabel { get; private set; }
        private GameObject _resumeBtn, _quitBtn;

        private void Awake()
        {
            if (transform.childCount > 0) return;

            // ── Dimmed background overlay ──
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0, 0, 0, 0.75f);
            }

            // ── Centered card ──
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.20f, 0.25f, 0.80f, 0.75f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            // ── Title ──
            var titleGo = GameUIResources.CreateText("PAUSED", transform, 48, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.2f, 0.58f, 0.8f, 0.70f);

            // ── Resume button (primary) ──
            _resumeBtn = GameUIResources.CreateButton("RESUME", transform, 280, 64);
            GameUIResources.SetAnchors(_resumeBtn.GetComponent<RectTransform>(), 0.30f, 0.40f, 0.70f, 0.50f);
            ResumeButton = _resumeBtn.GetComponent<Button>();

            // ── Quit button (danger) ──
            _quitBtn = GameUIResources.CreateButton("QUIT TO MENU", transform, 280, 56);
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.30f, 0.30f, 0.70f, 0.38f);
            GameUIResources.ApplyDangerStyle(_quitBtn);
            QuitButton = _quitBtn.GetComponent<Button>();
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(TitleText.gameObject, "game_paused", loc);
            GameUIResources.LocalizeButtonText(_resumeBtn, "game_resume", loc);
            GameUIResources.LocalizeButtonText(_quitBtn, "game_quit_to_menu", loc);
        }
    }
}
