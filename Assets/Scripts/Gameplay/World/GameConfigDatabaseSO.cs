using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    public enum WorldMechanicType
    {
        None = 0,
        Mystery = 1,
        Frozen = 2,
        LockedPole = 3,
        Stone = 4,
        Glass = 5,
        Rainbow = 6,
        Bomb = 7,
        Chain = 8,
        Magnet = 9,
        Paint = 10,
        Ghost = 11,
        RandomPool1 = 12, // 1 random type from advanced pool
        RandomPool2 = 13, // 2 random types from advanced pool
        RandomPool3 = 14  // 3 random types from advanced pool
    }

    [System.Serializable]
    public struct WorldConfigData
    {
        public int WorldIndex;
        public string Theme;
        public int UnlockedByWorldIndex;
        public bool IsEventWorld;
        public WorldMechanicType MechanicType;
    }

    [System.Serializable]
    public struct GameBalanceConfig
    {
        [Header("Undo")]
        public int FreeUndosPerSession;
        public int UndoCoinCost;

        [Header("Level Completion Rewards")]
        public int NormalCoinReward;
        public int BossCoinReward;
        public int NormalXpReward;
        public int BossXpReward;
        public int LevelUpCoinReward;

        [Header("Star Thresholds")]
        public int ThreeStarTargetRatioPercent; // e.g. 100 → ≤target, 130 → ≤target*1.3
        public int TwoStarTargetRatioPercent;

        [Header("Chest Drop Rates")]
        public float SilverChestChance;
        public float GoldChestChance;
        public float DiamondChestChance;

        [Header("Ad Intervals")]
        public int InterstitialAdInterval;
    }

    [System.Serializable]
    public struct DifficultyBandData
    {
        public DifficultyBand Band;
        public int MaxLevel;
        public int MinEmptyPoles;
        public int MaxCapacity;
    }

    [System.Serializable]
    public struct ColorCurvePoint
    {
        public int LevelThreshold;
        public int ColorCount;
    }

    [CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "RingFlow/Game Config Database")]
    public class GameConfigDatabaseSO : ScriptableObject
    {
        public int TotalLevels = 2000;
        public List<DifficultyBandData> DifficultyBands = new();
        public List<ColorCurvePoint> ColorCurve = new();
        public List<WorldConfigData> Worlds = new();
        public GameBalanceConfig BalanceConfig = new();

        private void OnEnable()
        {
            if (BalanceConfig.ThreeStarTargetRatioPercent == 0)
            {
                BalanceConfig = new GameBalanceConfig
                {
                    FreeUndosPerSession = 5,
                    UndoCoinCost = 5,
                    NormalCoinReward = 50,
                    BossCoinReward = 500,
                    NormalXpReward = 10,
                    BossXpReward = 50,
                    LevelUpCoinReward = 100,
                    ThreeStarTargetRatioPercent = 100,
                    TwoStarTargetRatioPercent = 130,
                    SilverChestChance = 0.40f,
                    GoldChestChance = 0.20f,
                    DiamondChestChance = 0.05f,
                    InterstitialAdInterval = 3
                };
            }
        }

        private static GameConfigDatabaseSO _instance;
        public static GameConfigDatabaseSO Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GameConfigDatabaseSO>();
                        _instance.InitializeDefaults();
                    }
                }
                return _instance;
            }
        }

        public void InitializeDefaults()
        {
            TotalLevels = 2000;

            BalanceConfig = new GameBalanceConfig
            {
                FreeUndosPerSession = 5,
                UndoCoinCost = 5,
                NormalCoinReward = 50,
                BossCoinReward = 500,
                NormalXpReward = 10,
                BossXpReward = 50,
                LevelUpCoinReward = 100,
                ThreeStarTargetRatioPercent = 100,
                TwoStarTargetRatioPercent = 130,
                SilverChestChance = 0.40f,
                GoldChestChance = 0.10f,
                DiamondChestChance = 0.01f,
                InterstitialAdInterval = 3
            };

            // Default Difficulty Bands (GDD §5)
            DifficultyBands = new List<DifficultyBandData>
            {
                new() { Band = DifficultyBand.Tutorial, MaxLevel = 20, MinEmptyPoles = 2, MaxCapacity = 4 },
                new() { Band = DifficultyBand.Easy, MaxLevel = 100, MinEmptyPoles = 2, MaxCapacity = 4 },
                new() { Band = DifficultyBand.Medium, MaxLevel = 350, MinEmptyPoles = 1, MaxCapacity = 4 },
                new() { Band = DifficultyBand.Hard, MaxLevel = 600, MinEmptyPoles = 1, MaxCapacity = 4 },
                new() { Band = DifficultyBand.Expert, MaxLevel = 1000, MinEmptyPoles = 1, MaxCapacity = 4 },
                new() { Band = DifficultyBand.Master, MaxLevel = 1500, MinEmptyPoles = 1, MaxCapacity = 4 },
                new() { Band = DifficultyBand.Legend, MaxLevel = 2000, MinEmptyPoles = 1, MaxCapacity = 4 }
            };

            // Default Color Curve Points (GDD §5)
            // GDD explicitly stops the ramp at 9 colours and uses the last bucket (10 colours)
            // only from level 1200+. The previous default transitioned to 10 colours at level 801,
            // which compounded with the wider empty-pole drop to make Expert bands feel 1.5x harder.
            ColorCurve = new List<ColorCurvePoint>
            {
                new() { LevelThreshold = 1, ColorCount = 3 },
                new() { LevelThreshold = 20, ColorCount = 4 },
                new() { LevelThreshold = 80, ColorCount = 5 },
                new() { LevelThreshold = 150, ColorCount = 6 },
                new() { LevelThreshold = 300, ColorCount = 7 },
                new() { LevelThreshold = 500, ColorCount = 8 },
                new() { LevelThreshold = 800, ColorCount = 9 },
                new() { LevelThreshold = 1200, ColorCount = 10 }
            };

            // Default 40 Worlds configuration
            Worlds = new List<WorldConfigData>();
            for (int i = 0; i < 40; i++)
            {
                var w = new WorldConfigData
                {
                    WorldIndex = i,
                    Theme = i == 0 ? "Grass Valley" : $"World {i + 1}",
                    UnlockedByWorldIndex = i == 0 ? 0 : i - 1,
                    IsEventWorld = (i + 1) % 5 == 0 // Boss world every 5 worlds
                };

                // Mechanics mapping based on world index (GDD: 12 worlds for 12 mechanics)
                if (i == 0) w.MechanicType = WorldMechanicType.None;
                else if (i == 1) w.MechanicType = WorldMechanicType.Mystery;
                else if (i == 2) w.MechanicType = WorldMechanicType.Frozen;
                else if (i == 3) w.MechanicType = WorldMechanicType.LockedPole;
                else if (i == 4) w.MechanicType = WorldMechanicType.Stone;
                else if (i == 5) w.MechanicType = WorldMechanicType.Glass;
                else if (i == 6) w.MechanicType = WorldMechanicType.Rainbow;
                else if (i == 7) w.MechanicType = WorldMechanicType.Bomb;
                else if (i == 8) w.MechanicType = WorldMechanicType.Chain;
                else if (i == 9) w.MechanicType = WorldMechanicType.Magnet;
                else if (i == 10) w.MechanicType = WorldMechanicType.Paint;
                else if (i == 11) w.MechanicType = WorldMechanicType.Ghost;
                else
                {
                    if (i >= 25) w.MechanicType = WorldMechanicType.RandomPool3;
                    else if (i >= 18) w.MechanicType = WorldMechanicType.RandomPool2;
                    else w.MechanicType = WorldMechanicType.RandomPool1;
                }

                Worlds.Add(w);
            }
        }

        // Helper search methods
        public DifficultyBand GetBandForLevel(int level)
        {
            if (level <= 3) return DifficultyBand.Tutorial;
            if (DifficultyBands == null || DifficultyBands.Count == 0) return DifficultyBand.Tutorial;
            foreach (var b in DifficultyBands)
            {
                if (level <= b.MaxLevel) return b.Band;
            }
            return DifficultyBands[^1].Band;
        }

        public int GetColorCountForLevel(int level)
        {
            if (level == 1 || level == 2) return 2;
            if (level == 3) return 3;

            if (ColorCurve == null || ColorCurve.Count == 0) return 3;
            int count = ColorCurve[0].ColorCount;
            foreach (var pt in ColorCurve)
            {
                if (level >= pt.LevelThreshold)
                    count = pt.ColorCount;
            }
            return count;
        }

        public int GetPoleCountForLevel(int level)
        {
            if (level == 1 || level == 2) return 3;
            if (level == 3) return 4;

            return GetColorCountForLevel(level) + GetMinEmptyPolesForLevel(level);
        }

        public int GetMinEmptyPolesForLevel(int level)
        {
            if (level == 1 || level == 2) return 1;
            if (level == 3) return 1;

            if (DifficultyBands == null || DifficultyBands.Count == 0) return 1;
            var band = GetBandForLevel(level);
            foreach (var b in DifficultyBands)
            {
                if (b.Band == band) return b.MinEmptyPoles;
            }
            return 1;
        }

        public int GetMaxCapacityForLevel(int level)
        {
            if (level == 1 || level == 2) return 3;
            if (level == 3) return 4;

            if (DifficultyBands == null || DifficultyBands.Count == 0) return 4;
            var band = GetBandForLevel(level);
            foreach (var b in DifficultyBands)
            {
                if (b.Band == band) return b.MaxCapacity;
            }
            return 4;
        }

        public int GetWorldForLevel(int level)
        {
            const int levelsPerWorld = 50;
            int world = (level - 1) / levelsPerWorld;
            return Mathf.Clamp(world, 0, Worlds.Count - 1);
        }

        public WorldMechanicType GetMechanicForWorld(int worldIndex)
        {
            if (Worlds == null || worldIndex < 0 || worldIndex >= Worlds.Count) return WorldMechanicType.None;
            return Worlds[worldIndex].MechanicType;
        }
    }
}
