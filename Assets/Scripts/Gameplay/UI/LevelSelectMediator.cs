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
        [Inject] private GameConfigDatabaseSO _dbConfig;

        protected override void OnBind()
        {
            _diag?.Checkpoint("LevelSelectMediator.OnBind");
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            if (View == null) return;

            View.Localize(_loc);

            int maxUnlocked = _progression?.MaxUnlockedLevel.Value ?? 1;
            int totalLevelsInWorld = _dbConfig?.LevelsPerWorld ?? 8;
            int currentWorld = _progression != null
                ? (_dbConfig?.GetWorldForLevel(_progression.CurrentLevel.Value) ?? 1)
                : 1;

            // Update progress display
            int levelsInWorld = _dbConfig?.GetLevelCountForWorld(currentWorld) ?? totalLevelsInWorld;
            View.SetProgress(maxUnlocked, levelsInWorld);

            if (View.WorldLabel != null)
            {
                string worldFormat = _loc?.GetString("format_world", "WORLD {0}") ?? "WORLD {0}";
                View.WorldLabel.text = string.Format(worldFormat, currentWorld);
            }

            for (int i = 0; i < View.LevelButtons.Count; i++)
            {
                int levelIndex = i + 1;
                var button = View.LevelButtons[i];
                bool unlocked = levelIndex <= maxUnlocked;
                int stars = 0;

                View.UpdateLevelButton(i, unlocked, stars);

                int capturedLevel = levelIndex;
                button.onClick.AddListener(() =>
                {
                    SignalBus.Fire(new LevelSelectedSignal(capturedLevel));
                });
            }

            if (View.BackButton != null)
            {
                View.BackButton.onClick.AddListener(() =>
                {
                    SignalBus.Fire(new QuitToMenuRequestedSignal());
                });
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
