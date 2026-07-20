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
        public Button RestartButton { get; private set; }
        public Button QuitButton { get; private set; }
        public TextMeshProUGUI TitleText { get; private set; }
        public TextMeshProUGUI MessageText { get; private set; }
        public TextMeshProUGUI LevelText { get; private set; }
        public TextMeshProUGUI ProgressText { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

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
                if (upper.Contains("RESTART") || upper.Contains("TRY")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (upper.Contains("MAIN MENU") || upper.Contains("QUIT")) { _quitBtn = btn.gameObject; QuitButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("GAME OVER")) TitleText = txt;
                else if (upper.Contains("MESSAGE") || upper.Contains("FAILED")) MessageText = txt;
                else if (upper.Contains("LEVEL")) LevelText = txt;
                else if (upper.Contains("PROGRESS")) ProgressText = txt;
            }
        }
    }
}
