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
            var savedLevel = Resources.Load<LevelDataSO>($"Levels/Level_{currentLevel}");
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

                levelData = LevelGenerator.GenerateLevel(
                    currentLevel, currentLevel * 12345, poleCount, colorCount, maxCapacity);
            }

            if (levelData != null)
            {
                for (int i = 0; i < levelData.Poles.Count; i++)
                {
                    var pData = levelData.Poles[i];
                    var poleState = new PoleState
                    {
                        Id = i,
                        MaxCapacity = pData.MaxCapacity,
                        IsLocked = pData.IsLocked
                    };

                    for (int r = 0; r < pData.Rings.Count; r++)
                    {
                        poleState.AddRing(pData.Rings[r]);
                    }

                    _model.Poles.Add(poleState);
                }

                _model.TargetMovesCount.Value = levelData.TargetMoves;
            }
            else
            {
                NexusLog.Error("InitLevelCommand", "Execute", currentLevel.ToString(),
                    "LevelGenerator returned null — fallback to hardcoded 3-pole tutorial. Likely cause: solver hit search limits or seed exhausted.");

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
    }
}