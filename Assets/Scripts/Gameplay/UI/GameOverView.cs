using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(GameOverMediator))]
    public class GameOverView : View, IAuthoredView
    {
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private CanvasGroup _cardGroup;
        public Button RestartButton => _restartButton;
        public Button QuitButton => _quitButton;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI MessageText => _messageText;
        public TextMeshProUGUI LevelText => _levelText;
        public TextMeshProUGUI ProgressText => _progressText;
        public CanvasGroup CardGroup => _cardGroup;

        private GameObject _restartBtn, _quitBtn;

        private ILocalizationService _loc;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void SetLevel(int level, string reason = "")
        {
            if (LevelText != null)
            {
                string format = _loc?.GetString("format_level", "Level {0}") ?? "Level {0}";
                LevelText.text = string.Format(format, level);
            }
            if (ProgressText != null)
            {
                ProgressText.text = string.IsNullOrEmpty(reason)
                    ? (_loc?.GetString("game_over_encourage", "Don't give up!") ?? "Don't give up!")
                    : reason;
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            _loc = loc;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_over_title", loc);
            if (MessageText != null) GameUIResources.LocalizeText(MessageText.gameObject, "game_over_message", loc);
            if (_restartBtn != null) GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            if (_quitBtn != null) GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("RESTART") || upper.Contains("TRY")) { _restartBtn = btn.gameObject; _restartButton = btn; }
                else if (upper.Contains("MAIN MENU") || upper.Contains("QUIT")) { _quitBtn = btn.gameObject; _quitButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("GAME OVER")) _titleText = txt;
                else if (upper.Contains("MESSAGE") || upper.Contains("FAILED")) _messageText = txt;
                else if (upper.Contains("LEVEL")) _levelText = txt;
                else if (upper.Contains("PROGRESS")) _progressText = txt;
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
