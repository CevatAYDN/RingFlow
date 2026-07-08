using Nexus.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(PauseMediator))]
    public class PauseView : View
    {
        public Button ResumeButton { get; private set; }
        public Button QuitButton { get; private set; }

        protected virtual void Awake()
        {
            // ── Dimmed background overlay ──
            var overlay = GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.75f);

            // ── Centered card ──
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.20f, 0.25f, 0.80f, 0.75f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            // ── Title ──
            var titleGo = GameUIResources.CreateText("PAUSED", transform, 48, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleRect, 0.2f, 0.58f, 0.8f, 0.70f);

            // ── Resume button (primary) ──
            var resumeBtn = GameUIResources.CreateButton("RESUME", transform, 280, 64);
            GameUIResources.SetAnchors(resumeBtn.GetComponent<RectTransform>(), 0.30f, 0.40f, 0.70f, 0.50f);
            ResumeButton = resumeBtn.GetComponent<Button>();

            // ── Quit button (danger) ──
            var quitBtn = GameUIResources.CreateButton("QUIT TO MENU", transform, 280, 56);
            GameUIResources.SetAnchors(quitBtn.GetComponent<RectTransform>(), 0.30f, 0.30f, 0.70f, 0.38f);
            ApplyDangerStyle(quitBtn);
            QuitButton = quitBtn.GetComponent<Button>();
        }

        private static void ApplyDangerStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = GameUIResources.DangerColor;

            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = GameUIResources.DangerColor;
            colors.highlightedColor = new Color(0.88f, 0.32f, 0.32f);
            colors.pressedColor = new Color(0.60f, 0.15f, 0.15f);
            button.colors = colors;
        }
    }
}
