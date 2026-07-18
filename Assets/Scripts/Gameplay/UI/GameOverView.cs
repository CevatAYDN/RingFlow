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
            ApplyBaseStyling();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("GameOverView", nameof(Awake), "",
                $"GameOverView bound. RestartButton={RestartButton != null}, QuitButton={QuitButton != null}, " +
                $"TitleText={TitleText != null}, MessageText={MessageText != null}.");
#endif
        }

        private void ApplyBaseStyling()
        {
            if (_restartBtn != null) GameUIResources.ApplyPrimaryStyle(_restartBtn);
            if (_quitBtn != null) GameUIResources.ApplySecondaryStyle(_quitBtn);
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("GameOverView", nameof(Localize), "", "Localize called with null ILocalizationService.");
#endif
                return;
            }
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_over_title", loc);
            if (MessageText != null) GameUIResources.LocalizeText(MessageText.gameObject, "game_over_message", loc);
            GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            GameUIResources.LocalizeButtonText(_quitBtn, "menu_main_menu", loc);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (TitleText == null || MessageText == null || _restartBtn == null || _quitBtn == null)
                NexusLog.Warn("GameOverView", nameof(Localize), "",
                    $"Missing refs during localization. TitleText={TitleText != null}, " +
                    $"MessageText={MessageText != null}, restartBtn={_restartBtn != null}, quitBtn={_quitBtn != null}.");
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (RestartButton == null)
                NexusLog.Warn("GameOverView", nameof(BindReferencesFromChildren), "",
                    "RestartButton not found in children — game over screen cannot restart.");
            if (QuitButton == null)
                NexusLog.Warn("GameOverView", nameof(BindReferencesFromChildren), "",
                    "QuitButton not found in children — game over screen cannot quit.");
#endif
        }
    }
}
