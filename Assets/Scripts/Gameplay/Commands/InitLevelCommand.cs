using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private ISignalBus _signalBus;
        [Inject] private Services.IAssetService _assetService;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private IAnalyticsService _analyticsService;

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
            LevelDataSO savedLevel = null;
            string levelKey = $"Levels/Level_{currentLevel}";
            if (_assetService == null)
            {
                throw new System.InvalidOperationException("[InitLevelCommand] IAssetService not injected!");
            }
            
            var task = _assetService.LoadAsync<LevelDataSO>(levelKey);
            task.Wait();
            savedLevel = task.Result;

            // GDD curve params — always computed so retry logic can reuse them
            if (_dbConfig == null)
            {
                throw new System.InvalidOperationException("[InitLevelCommand] GameConfigDatabaseSO not injected!");
            }
            var db = _dbConfig;
            int poleCount = db.GetPoleCountForLevel(currentLevel);
            int colorCount = db.GetColorCountForLevel(currentLevel);
            int maxCapacity = db.GetMaxCapacityForLevel(currentLevel);
            int poleClamp = db.LevelGen.PoleCountClamp > 0 ? db.LevelGen.PoleCountClamp : 12;
            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > poleClamp)
            {
                NexusLog.Warn("InitLevelCommand", "Execute", currentLevel.ToString(),
                    $"Computed pole count exceeded {poleClamp}; clamping from {poleCount}.");
                poleCount = poleClamp;
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

                int baseSeedMultiplier = db.LevelGen.BaseGenerationSeedMultiplier > 0
                    ? db.LevelGen.BaseGenerationSeedMultiplier : 12345;
                levelData = LevelGenerator.GenerateLevel(
                    db, currentLevel, currentLevel * baseSeedMultiplier, poleCount, colorCount, maxCapacity);
            }

            if (levelData != null)
            {
                PopulatePoles(levelData);
            }
            else
            {
                var retryMultipliers = db.LevelGen.RetrySeedMultipliers != null && db.LevelGen.RetrySeedMultipliers.Count > 0
                    ? db.LevelGen.RetrySeedMultipliers
                    : new List<int> { 27779, 31415, 16180 };
                foreach (var retryMultiplier in retryMultipliers)
                {
                    levelData = LevelGenerator.GenerateLevel(
                        db, currentLevel, currentLevel * retryMultiplier, poleCount, colorCount, maxCapacity);
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
                    // Fail-loud: hiçbir fallback üretim yok. DB-driven kurallar çözülebilir
                    // seviye üretemediyse bu bir konfigürasyon hatasıdır.
                    NexusLog.Error("InitLevelCommand", "Execute", currentLevel.ToString(),
                        "All seed attempts exhausted — no fallback level generated. " +
                        "Check GameConfigDatabaseSO configuration for this level range.");
                }
            }

            NexusLog.Info("InitLevelCommand", "Execute", currentLevel.ToString(),
                $"Initialized level {currentLevel} with {_model.Poles.Count} poles. Target moves: {_model.TargetMovesCount.Value}.");

            int worldIndex = _dbConfig.GetWorldForLevel(currentLevel);

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

            if (_analyticsService != null)
            {
                _analyticsService.LevelStart(currentLevel, worldIndex);
            }

            _signalBus?.Fire(new LevelLoadedSignal(currentLevel));
        }

        private void PopulatePoles(LevelData levelData)
        {
            for (int i = 0; i < levelData.Poles.Count; i++)
            {
                var pData = levelData.Poles[i];
                var poleState = new PoleState { Id = i, IsLocked = pData.IsLocked };
                poleState.SetCapacity(pData.RingCapacity);
                if (pData.PortalTargetId >= 0)
                    poleState.PortalPartnerId = pData.PortalTargetId;
                for (int r = 0; r < pData.Rings.Count; r++)
                    poleState.AddRing(pData.Rings[r].Clone());
                _model.Poles.Add(poleState);
            }
            _model.TargetMovesCount.Value = levelData.TargetMoves;
        }
    }
}