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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("LevelSelectMediator", nameof(OnBind), "",
                $"Binding level select. maxUnlocked={maxUnlocked}, buttons={View.LevelButtons.Count}.");
#endif
            for (int i = 0; i < View.LevelButtons.Count; i++)
            {
                int levelIndex = i + 1;
                var button = View.LevelButtons[i];

                bool unlocked = levelIndex <= maxUnlocked;
                button.interactable = unlocked;
                ApplyLockedStyle(button, unlocked);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!unlocked)
                    NexusLog.Info("LevelSelectMediator", nameof(OnBind), levelIndex.ToString(),
                        $"Level {levelIndex} locked (maxUnlocked={maxUnlocked}).");
#endif

                int capturedLevel = levelIndex;
                button.onClick.AddListener(() =>
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("LevelSelectMediator", "LevelButton", capturedLevel.ToString(),
                        $"Level {capturedLevel} selected. Firing LevelSelectedSignal.");
#endif
                    SignalBus.Fire(new LevelSelectedSignal(capturedLevel));
                });
            }

            if (View.BackButton != null)
            {
                View.BackButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("LevelSelectMediator", "BackButton", "", "Back pressed. Firing QuitToMenuRequestedSignal.");
#endif
                    SignalBus.Fire(new QuitToMenuRequestedSignal());
                });
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("LevelSelectMediator", nameof(OnBind), "", "BackButton is null — player cannot navigate back.");
            }
#endif
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
