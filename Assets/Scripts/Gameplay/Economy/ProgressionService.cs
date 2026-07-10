using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public sealed class ProgressionService : Nexus.Core.Services.IProgressionService, INexusService
    {
        private readonly PlayerProgressModel _progress;

        public ProgressionService(PlayerProgressModel progress)
        {
            _progress = progress;
            CurrentLevel = _progress.CurrentLevel;
            MaxUnlockedLevel = _progress.MaxUnlockedLevel;
        }

        public ObservableProperty<int> CurrentLevel { get; }
        public ObservableProperty<int> MaxUnlockedLevel { get; }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() {}

        public void CompleteCurrentLevel()
        {
            SetLevel(CurrentLevel.Value + 1);
        }

        public void SetLevel(int levelIndex)
        {
            if (levelIndex < 1)
            {
                NexusLog.Warn("ProgressionService", nameof(SetLevel), levelIndex.ToString(),
                    $"Requested level below 1 (got {levelIndex}); clamping to 1.");
                levelIndex = 1;
            }

            if (levelIndex > WorldConfigSO.TotalLevels)
            {
                NexusLog.Warn("ProgressionService", nameof(SetLevel), levelIndex.ToString(),
                    $"Requested level above cap {WorldConfigSO.TotalLevels}; clamping to TotalLevels.");
                levelIndex = WorldConfigSO.TotalLevels;
            }

            CurrentLevel.Value = levelIndex;
            if (MaxUnlockedLevel.Value < levelIndex)
            {
                MaxUnlockedLevel.Value = levelIndex;
            }

            NexusLog.Info("ProgressionService", nameof(SetLevel), levelIndex.ToString(),
                $"Level set to {levelIndex}. MaxUnlocked={MaxUnlockedLevel.Value}.");
        }

        public long CalculateUpgradeCost(long baseCost, int level, float multiplier = 1.15f, CurveType curveType = CurveType.Exponential)
        {
            if (level <= 1) return baseCost;

            return curveType switch
            {
                CurveType.Linear => (long)(baseCost * (1 + (level - 1) * (multiplier - 1))),
                CurveType.Exponential => (long)(baseCost * Math.Pow(multiplier, level - 1)),
                CurveType.Polynomial => (long)(baseCost * Math.Pow(level, multiplier)),
                _ => baseCost
            };
        }
    }
}
