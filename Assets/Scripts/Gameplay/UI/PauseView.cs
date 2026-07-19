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

            // 1. MAIN CARD (White SurfaceColor with shadow)
            var cardGo = GameUIResources.CreateCard("Card", transform);
            cardGo.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.12f, 0.22f, 0.88f, 0.78f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();
            if (CardGroup == null) CardGroup = cardGo.AddComponent<CanvasGroup>();

            // Title "DURAKLATILDI"
            var titleGo = GameUIResources.CreateText("PAUSED", cardGo.transform, 30, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.05f, 0.80f, 0.95f, 0.92f);

            // Subtitle
            var subGo = GameUIResources.CreateText("Take a moment", cardGo.transform, 16, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            subGo.name = "Subtitle";
            SubtitleText = subGo.GetComponent<Text>();
            GameUIResources.SetAnchors(subGo.GetComponent<RectTransform>(), 0.05f, 0.70f, 0.95f, 0.78f);

            // Progress info
            var progGo = GameUIResources.CreateText("", cardGo.transform, 14, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            progGo.name = "ProgressLabel";
            ProgressLabel = progGo.GetComponent<Text>();
            GameUIResources.SetAnchors(progGo.GetComponent<RectTransform>(), 0.05f, 0.62f, 0.95f, 0.68f);

            // 2. MAIN RESUME BUTTON (Coral)
            _resumeBtn = GameUIResources.CreateButton("DEVAM ET", cardGo.transform, 240f, 52f);
            _resumeBtn.name = "Btn_RESUME";
            GameUIResources.ApplyPrimaryStyle(_resumeBtn);
            ResumeButton = _resumeBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_resumeBtn.GetComponent<RectTransform>(), 0.12f, 0.44f, 0.88f, 0.54f);
            
            var playIconGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            playIconGo.transform.SetParent(_resumeBtn.transform, false);
            var playIcon = playIconGo.GetComponent<Image>();
            playIcon.sprite = GameUIResources.GetSprite("play_icon");
            playIcon.preserveAspect = true;
            GameUIResources.SetAnchors(playIconGo.GetComponent<RectTransform>(), 0.05f, 0.15f, 0.25f, 0.85f);

            // 3. RESTART (Teal) & QUIT (White/Outline) SIDE-BY-SIDE BUTTONS
            // Restart
            _restartBtn = GameUIResources.CreateButton("YENİDEN", cardGo.transform, 110f, 44f);
            _restartBtn.name = "Btn_RESTART";
            GameUIResources.ApplyAccentStyle(_restartBtn); // Teal/AccentStyle
            RestartButton = _restartBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_restartBtn.GetComponent<RectTransform>(), 0.12f, 0.30f, 0.48f, 0.39f);

            // Quit
            _quitBtn = GameUIResources.CreateButton("MENÜ", cardGo.transform, 110f, 44f);
            _quitBtn.name = "Btn_QUIT";
            GameUIResources.ApplyOutlineStyle(_quitBtn);
            QuitButton = _quitBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_quitBtn.GetComponent<RectTransform>(), 0.52f, 0.30f, 0.88f, 0.39f);

            // 4. BOTTOM SOUND/SETTINGS PANEL (Sunny background with sound & gear icons)
            var bottomPanel = GameUIResources.CreatePanel("SoundPanel", cardGo.transform);
            bottomPanel.GetComponent<Image>().color = GameUIResources.AccentColor; // Yellow/Sunny
            GameUIResources.SetAnchors(bottomPanel.GetComponent<RectTransform>(), 0.12f, 0.12f, 0.88f, 0.22f);

            // Audio aesthetic icons
            var soundIconGo = new GameObject("SoundIconImage", typeof(RectTransform), typeof(Image));
            soundIconGo.transform.SetParent(bottomPanel.transform, false);
            var soundIcon = soundIconGo.GetComponent<Image>();
            soundIcon.sprite = GameUIResources.GetSprite("sound_on");
            soundIcon.preserveAspect = true;
            GameUIResources.SetAnchors(soundIconGo.GetComponent<RectTransform>(), 0.10f, 0.15f, 0.30f, 0.85f);

            var musicIconGo = new GameObject("MusicIconImage", typeof(RectTransform), typeof(Image));
            musicIconGo.transform.SetParent(bottomPanel.transform, false);
            var musicIcon = musicIconGo.GetComponent<Image>();
            musicIcon.sprite = GameUIResources.GetSprite("music_on");
            musicIcon.preserveAspect = true;
            GameUIResources.SetAnchors(musicIconGo.GetComponent<RectTransform>(), 0.40f, 0.15f, 0.60f, 0.85f);

            // Settings button (Gear) inside yellow panel
            _settingsBtn = GameUIResources.CreateIconButton("", bottomPanel.transform, 32f);
            _settingsBtn.name = "Btn_SETTINGS";
            SettingsButton = _settingsBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_settingsBtn.GetComponent<RectTransform>(), 0.70f, 0.15f, 0.90f, 0.85f);
            _settingsBtn.GetComponent<Image>().color = Color.clear;
            
            var settingsIconGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            settingsIconGo.transform.SetParent(_settingsBtn.transform, false);
            var settingsIcon = settingsIconGo.GetComponent<Image>();
            settingsIcon.sprite = GameUIResources.GetSprite("settings");
            settingsIcon.preserveAspect = true;
            GameUIResources.SetAnchors(settingsIconGo.GetComponent<RectTransform>(), 0.10f, 0.10f, 0.90f, 0.90f);
        }

        private ILocalizationService _locService;

        public void SetProgress(int level, int moves)
        {
            if (ProgressLabel == null) return;
            string levelFormat = _locService?.GetString("format_level", "Level {0}") ?? "Level {0}";
            string movesFormat = _locService?.GetString("format_moves", "Moves: {0}") ?? "Moves: {0}";
            ProgressLabel.text = $"{string.Format(levelFormat, level)}  ·  {string.Format(movesFormat, moves)}";
        }

        public void Localize(ILocalizationService loc)
        {
            _locService = loc;
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_paused", loc);
            if (SubtitleText != null) GameUIResources.LocalizeText(SubtitleText.gameObject, "game_tagline", loc);
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
                // Need to handle both button itself and container name for custom hierarchy
                else if (upper.Contains("RESTART")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (upper.Contains("QUIT") || upper.Contains("MAIN MENU") || upper.Contains("MENÜ")) { _quitBtn = btn.gameObject; QuitButton = btn; }
                else if (upper.Contains("SETTINGS")) { _settingsBtn = btn.gameObject; SettingsButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.fontSize >= 30 || txt.name.ToUpperInvariant().Contains("TITLE")) TitleText = txt;
                else if (txt.name.ToUpperInvariant().Contains("SUB")) SubtitleText = txt;
                else if (txt.name.ToUpperInvariant().Contains("PROGRESS")) ProgressLabel = txt;
            }
        }
    }
}
