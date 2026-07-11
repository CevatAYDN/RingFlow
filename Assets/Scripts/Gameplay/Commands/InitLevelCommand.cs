using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private ISignalBus _signalBus;
        [Inject] private Services.IAssetService _assetService;

        public void Execute(InitLevelSignal signal)
        {
            if (_model == null)
            {
                NexusLog.Error("InitLevelCommand", "Execute", signal.LevelIndex.ToString(),
                    "GameplayModel is null — cannot initialize level.");
                return;
            }

            _model.Reset();

            int currentLevel = signal.LevelIndex > 0
                ? signal.LevelIndex
                : _progressionService?.CurrentLevel.Value ?? 1;

            LevelData levelData = null;

            // Route through IAssetService so future Addressables migration is a one-line change.
            // Falls back to Resources.Load if the service is not available (editor/test scenarios).
            LevelDataSO savedLevel = null;
            string levelKey = $"Levels/Level_{currentLevel}";
            if (_assetService != null)
            {
                var task = _assetService.LoadAsync<LevelDataSO>(levelKey);
                task.Wait(); // Synchronous in command context; Addressables replacement can go async later
                savedLevel = task.Result;
            }
            else
            {
                savedLevel = Resources.Load<LevelDataSO>(levelKey);
            }

            // GDD curve params — always computed so retry logic can reuse them
            int poleCount = DifficultyCurve.PoleCountForLevel(currentLevel);
            int colorCount = DifficultyCurve.ColorCountForLevel(currentLevel);
            int maxCapacity = DifficultyCurve.MaxCapacityForLevel(currentLevel);
            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > 12)
            {
                NexusLog.Warn("InitLevelCommand", "Execute", currentLevel.ToString(),
                    $"Computed pole count exceeded 12; clamping from {poleCount}.");
                poleCount = 12;
            }

            if (savedLevel != null && savedLevel.Data != null)
            {
                levelData = savedLevel.Data;
            }
            else
            {
                if (_progressionService == null && signal.LevelIndex <= 0)
                {
                    NexusLog.Warn("InitLevelCommand", "Execute", currentLevel.ToString(),
                        "Progression service not bound and no level index specified — defaulting to level 1.");
                }

                levelData = LevelGenerator.GenerateLevel(
                    currentLevel, currentLevel * 12345, poleCount, colorCount, maxCapacity);
            }

            if (levelData != null)
            {
                PopulatePoles(levelData);
            }
            else
            {
                // P0 fix: retry with alternate seeds before falling back to tutorial.
                var retrySeeds = new[] { currentLevel * 27779, currentLevel * 31415, currentLevel * 16180 };
                foreach (var retrySeed in retrySeeds)
                {
                    levelData = LevelGenerator.GenerateLevel(
                        currentLevel, retrySeed, poleCount, colorCount, maxCapacity);
                    if (levelData != null) break;
                }

                if (levelData != null)
                {
                    NexusLog.Warn("InitLevelCommand", "Execute", currentLevel.ToString(),
                        "Primary seed exhausted; level generated with retry seed.");
                    PopulatePoles(levelData);
                }
                else
                {
                    NexusLog.Error("InitLevelCommand", "Execute", currentLevel.ToString(),
                        "All seed attempts exhausted — emergency fallback to hardcoded 3-pole tutorial level.");
                    BuildFallbackTutorialLevel();
                }
            }

            NexusLog.Info("InitLevelCommand", "Execute", currentLevel.ToString(),
                $"Initialized level {currentLevel} with {_model.Poles.Count} poles. Target moves: {_model.TargetMovesCount.Value}.");

            int worldIndex = WorldConfigSO.WorldFromAbsoluteLevel(currentLevel);

            if (levelData != null)
            {
                int glassCount = 0;
                foreach (var p in levelData.Poles)
                    foreach (var r in p.Rings)
                        if (r.Type == RingType.Glass) glassCount++;
                if (glassCount > 0)
                    NexusLog.Info("InitLevelCommand", "Execute", currentLevel.ToString(),
                        $"Level has {glassCount} Glass ring(s) — treated as Standard (visual-only).");
            }

            AnalyticsEvents.LevelStart(currentLevel, worldIndex);

            _signalBus?.Fire(new LevelLoadedSignal(currentLevel));
        }

        private void PopulatePoles(LevelData levelData)
        {
            for (int i = 0; i < levelData.Poles.Count; i++)
            {
                var pData = levelData.Poles[i];
                var poleState = new PoleState { Id = i, IsLocked = pData.IsLocked };
                poleState.SetCapacity(pData.RingCapacity);
                for (int r = 0; r < pData.Rings.Count; r++)
                    poleState.AddRing(pData.Rings[r].Clone());
                _model.Poles.Add(poleState);
            }
            _model.TargetMovesCount.Value = levelData.TargetMoves;
        }

        private void BuildFallbackTutorialLevel()
        {
            var p0 = new PoleState { Id = 0 };
            p0.AddRing(new RingData(RingColor.Red));
            p0.AddRing(new RingData(RingColor.Blue));
            var p1 = new PoleState { Id = 1 };
            p1.AddRing(new RingData(RingColor.Blue));
            p1.AddRing(new RingData(RingColor.Red));
            var p2 = new PoleState { Id = 2 };
            _model.Poles.Add(p0);
            _model.Poles.Add(p1);
            _model.Poles.Add(p2);
            _model.TargetMovesCount.Value = 2;
        }
    }
}