using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine.UI;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay.UI
{
    public class LevelSelectMediator : Mediator<LevelSelectView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private ILocalizationService _loc;
        [Inject] private IGameDiagnostics _diag;
        [Inject] private IViewMediatorTracker _tracker;

        protected override void OnBind()
        {
            _diag?.Checkpoint("LevelSelectMediator.OnBind");
            if (View == null)
            {
                NexusLog.Warn("LevelSelectMediator", nameof(OnBind), "", "LevelSelectView not bound.");
                return;
            }
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            View.Localize(_loc);
            // Lock buttons beyond MaxUnlockedLevel so the player can't select them
            int maxUnlocked = _progression?.MaxUnlockedLevel.Value ?? 1;
            for (int i = 0; i < View.LevelButtons.Count; i++)
            {
                int levelIndex = i + 1;
                var button = View.LevelButtons[i];

                bool unlocked = levelIndex <= maxUnlocked;
                button.interactable = unlocked;
                ApplyLockedStyle(button, unlocked);

                int capturedLevel = levelIndex;
                button.onClick.AddListener(() =>
                {
                    SignalBus.Fire(new LevelSelectedSignal(capturedLevel));
                });
            }

            View.BackButton.onClick.AddListener(() =>
            {
                SignalBus.Fire(new QuitToMenuRequestedSignal());
            });
        }

        private static void ApplyLockedStyle(Button button, bool unlocked)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = unlocked ? label.text : label.text + " (locked)";
                label.color = unlocked ? GameUIResources.TextColor : GameUIResources.MutedText;
            }
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = unlocked ? GameUIResources.PrimaryColor : GameUIResources.PanelColor;
            }
        }

        protected override void OnUnbind()
        {
            _tracker?.TrackViewUnbound(View?.GetType());
            if (View?.LevelButtons != null)
            {
                foreach (var btn in View.LevelButtons)
                    btn?.onClick.RemoveAllListeners();
            }
            View?.BackButton?.onClick.RemoveAllListeners();
        }
    }
}
