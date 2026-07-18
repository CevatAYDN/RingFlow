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

            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0.03f, 0.05f, 0.08f, 0.78f);
            }

            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.14f, 0.20f, 0.86f, 0.78f);
            card.GetComponent<Image>().color = GameUIResources.SurfaceColor;

            var accent = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            GameUIResources.SetAnchors(accent.GetComponent<RectTransform>(), 0.14f, 0.72f, 0.86f, 0.74f);
            accent.GetComponent<Image>().color = GameUIResources.AccentColor;

            var titleGo = GameUIResources.CreateText("PAUSED", card.transform, 54, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.15f, 0.56f, 0.85f, 0.70f);

            var subtitle = GameUIResources.CreateText("Take a breath. Your puzzle state is محفوظ.", card.transform, 18, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            GameUIResources.SetAnchors(subtitle.GetComponent<RectTransform>(), 0.15f, 0.49f, 0.85f, 0.57f);

            _resumeBtn = GameUIResources.CreateButton("RESUME", card.transform, 300, 66);
            GameUIResources.SetAnchors(_resumeBtn.GetComponent<RectTransform>(), 0.26f, 0.34f, 0.74f, 0.44f);
            ResumeButton = _resumeBtn.GetComponent<Button>();

            _quitBtn = GameUIResources.CreateButton("QUIT TO MENU", card.transform, 300, 58);
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.26f, 0.22f, 0.74f, 0.30f);
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
