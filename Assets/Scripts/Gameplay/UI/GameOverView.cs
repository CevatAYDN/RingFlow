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
            BindReferencesFromChildren();
        }

        public void Localize(ILocalizationService loc)
        {
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_over_title", loc);
            if (MessageText != null) GameUIResources.LocalizeText(MessageText.gameObject, "game_over_message", loc);
            GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("RESTART")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (upper.Contains("MAIN MENU")) { _quitBtn = btn.gameObject; QuitButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("GAME OVER")) TitleText = txt;
                else if (upper.Contains("MESSAGE") || upper.Contains("FAILED")) MessageText = txt;
            }
        }
    }
}
