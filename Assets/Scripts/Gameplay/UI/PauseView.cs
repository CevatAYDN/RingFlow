using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(PauseMediator))]
    public class PauseView : View, IAuthoredView
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private TextMeshProUGUI _progressLabel;
        [SerializeField] private CanvasGroup _cardGroup;
        public Button ResumeButton => _resumeButton;
        public Button RestartButton => _restartButton;
        public Button QuitButton => _quitButton;
        public Button SettingsButton => _settingsButton;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI SubtitleText => _subtitleText;
        public TextMeshProUGUI ProgressLabel => _progressLabel;
        public CanvasGroup CardGroup => _cardGroup;

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
                if (upper.Contains("RESUME")) { _resumeBtn = btn.gameObject; _resumeButton = btn; }
                else if (upper.Contains("RESTART")) { _restartBtn = btn.gameObject; _restartButton = btn; }
                else if (upper.Contains("QUIT") || upper.Contains("MAIN MENU") || upper.Contains("MENU")) { _quitBtn = btn.gameObject; _quitButton = btn; }
                else if (upper.Contains("SETTINGS")) { _settingsBtn = btn.gameObject; _settingsButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt.fontSize >= 30 || txt.name.ToUpperInvariant().Contains("TITLE")) _titleText = txt;
                else if (txt.name.ToUpperInvariant().Contains("SUB")) _subtitleText = txt;
                else if (txt.name.ToUpperInvariant().Contains("PROGRESS")) _progressLabel = txt;
            }

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }
    }
}
