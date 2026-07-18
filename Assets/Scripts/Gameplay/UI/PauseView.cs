using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(PauseMediator))]
    public class PauseView : View, IAuthoredView
    {
        public Button ResumeButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text SubtitleText { get; private set; }
        public Text ProgressLabel { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private GameObject _resumeBtn, _restartBtn, _quitBtn, _settingsBtn;

        private void Awake()
        {
            if (transform.childCount == 0 || NeedsSelfBuild())
                BuildUI();
            else
                BindReferencesFromChildren();
        }

        private bool NeedsSelfBuild()
        {
            BindReferencesFromChildren();
            return ResumeButton == null || QuitButton == null;
        }

        public void BuildUI()
        {
            // Dark overlay
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = GameUIResources.OverlayHeavy;
                overlay.raycastTarget = true;
            }

            // Card
            var cardGo = GameUIResources.CreateCard("Card", transform, GameUIResources.SurfaceDark);
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.10f, 0.16f, 0.90f, 0.84f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Title
            var titleGo = GameUIResources.CreateText("PAUSED", cardGo.transform, 54, TextAnchor.MiddleCenter, GameUIResources.TextOnDark);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.10f, 0.72f, 0.90f, 0.86f);

            // Subtitle
            var subGo = GameUIResources.CreateText("Take a moment", cardGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            subGo.name = "Subtitle";
            SubtitleText = subGo.GetComponent<Text>();
            GameUIResources.SetAnchors(subGo.GetComponent<RectTransform>(), 0.12f, 0.62f, 0.88f, 0.68f);

            // Progress info
            var progGo = GameUIResources.CreateText("", cardGo.transform, 16, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            progGo.name = "ProgressLabel";
            ProgressLabel = progGo.GetComponent<Text>();
            GameUIResources.SetAnchors(progGo.GetComponent<RectTransform>(), 0.12f, 0.56f, 0.88f, 0.60f);

            float btnW = 280f;
            float btnH = 56f;

            // Resume
            _resumeBtn = GameUIResources.CreateButton("RESUME", cardGo.transform, btnW, btnH);
            _resumeBtn.name = "Btn_RESUME";
            GameUIResources.ApplyPrimaryStyle(_resumeBtn);
            ResumeButton = _resumeBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_resumeBtn.GetComponent<RectTransform>(), 0.22f, 0.42f, 0.78f, 0.50f);

            // Restart
            _restartBtn = GameUIResources.CreateButton("RESTART", cardGo.transform, btnW, btnH * 0.85f);
            _restartBtn.name = "Btn_RESTART";
            GameUIResources.ApplyOutlineStyle(_restartBtn);
            RestartButton = _restartBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_restartBtn.GetComponent<RectTransform>(), 0.22f, 0.32f, 0.78f, 0.39f);

            // Settings
            _settingsBtn = GameUIResources.CreateButton("SETTINGS", cardGo.transform, btnW, btnH * 0.85f);
            _settingsBtn.name = "Btn_SETTINGS";
            GameUIResources.ApplyTextButtonStyle(_settingsBtn);
            SettingsButton = _settingsBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_settingsBtn.GetComponent<RectTransform>(), 0.22f, 0.22f, 0.78f, 0.29f);

            // Quit
            _quitBtn = GameUIResources.CreateButton("QUIT", cardGo.transform, btnW, btnH * 0.85f);
            _quitBtn.name = "Btn_QUIT";
            GameUIResources.ApplyDangerStyle(_quitBtn);
            QuitButton = _quitBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.22f, 0.12f, 0.78f, 0.19f);
        }

        public void SetProgress(int level, int moves)
        {
            if (ProgressLabel != null)
                ProgressLabel.text = $"Level {level} · {moves} moves";
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_paused", loc);
            if (_resumeBtn != null) GameUIResources.LocalizeButtonText(_resumeBtn, "game_resume", loc);
            if (_restartBtn != null) GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            if (_settingsBtn != null) GameUIResources.LocalizeButtonText(_settingsBtn, "settings_title", loc);
            if (_quitBtn != null) GameUIResources.LocalizeButtonText(_quitBtn, "game_quit_to_menu", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("RESUME")) { _resumeBtn = btn.gameObject; ResumeButton = btn; }
                else if (upper.Contains("RESTART")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (upper.Contains("QUIT") || upper.Contains("MAIN MENU")) { _quitBtn = btn.gameObject; QuitButton = btn; }
                else if (upper.Contains("SETTINGS")) { _settingsBtn = btn.gameObject; SettingsButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.fontSize >= 50 || txt.name.ToUpperInvariant().Contains("TITLE")) TitleText = txt;
                else if (txt.name.ToUpperInvariant().Contains("SUB")) SubtitleText = txt;
                else if (txt.name.ToUpperInvariant().Contains("PROGRESS")) ProgressLabel = txt;
            }
        }
    }
}
