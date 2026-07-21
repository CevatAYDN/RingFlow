using System;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine.UI;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay.UI
{
    public class LevelSelectMediator : Mediator<LevelSelectView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private ILocalizationService _loc;
        [Inject] private IGameDiagnostics _diag;
        [Inject] private IViewMediatorTracker _tracker;
        [Inject] private GameConfigDatabaseSO _dbConfig;

        private Action<int, int> _maxUnlockedHandler;
        private Action<int, int> _currentLevelHandler;

        protected override void OnBind()
        {
            _diag?.Checkpoint("LevelSelectMediator.OnBind");
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            if (View == null) return;

            View.Localize(_loc);

            RefreshLevelButtons();
            BindProgressionUpdates();

            if (View.BackButton != null)
            {
                View.BackButton.onClick.AddListener(OnBackClicked);
            }

            _diag?.Log("LevelSelectMediator", $"Bound. MaxUnlocked={_progression?.MaxUnlockedLevel.Value}.");
        }

        private void RefreshLevelButtons()
        {
            if (View == null) return;

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
                int stars = _progress?.GetStarsForLevel(levelIndex) ?? 0;

                View.UpdateLevelButton(i, unlocked, stars);

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnLevelSelected(levelIndex));
            }
        }

        private void BindProgressionUpdates()
        {
            if (_progression == null) return;

            _maxUnlockedHandler = (_, _) => RefreshLevelButtons();
            _progression.MaxUnlockedLevel.OnChanged(_maxUnlockedHandler);

            _currentLevelHandler = (_, _) => RefreshLevelButtons();
            _progression.CurrentLevel.OnChanged(_currentLevelHandler);
        }

        private void OnLevelSelected(int level) => SignalBus.Fire(new LevelSelectedSignal(level));
        private void OnBackClicked() => SignalBus.Fire(new QuitToMenuRequestedSignal());

        protected override void OnUnbind()
        {
            _tracker?.TrackViewUnbound(View?.GetType());

            if (_progression != null)
            {
                if (_maxUnlockedHandler != null) _progression.MaxUnlockedLevel.RemoveOnChanged(_maxUnlockedHandler);
                if (_currentLevelHandler != null) _progression.CurrentLevel.RemoveOnChanged(_currentLevelHandler);
            }
            _maxUnlockedHandler = null;
            _currentLevelHandler = null;

            if (View?.LevelButtons != null)
            {
                foreach (var btn in View.LevelButtons)
                    btn?.onClick.RemoveAllListeners();
            }
            View?.BackButton?.onClick.RemoveAllListeners();
        }
    }
}
