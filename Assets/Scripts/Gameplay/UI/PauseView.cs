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
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

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
            if (SubtitleText != null) GameUIResources.LocalizeText(SubtitleText.gameObject, "pause_subtitle", loc);
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
                else if (upper.Contains("QUIT") || upper.Contains("MAIN MENU") || upper.Contains("MENU")) { _quitBtn = btn.gameObject; QuitButton = btn; }
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
