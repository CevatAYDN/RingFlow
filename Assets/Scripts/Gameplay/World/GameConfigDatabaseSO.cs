using UnityEngine;
using System.Collections.Generic;
using Nexus.Core.Services;

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
        Portal = 15, // Portal pole pairs — ring placed on portal teleports to linked partner
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
    public struct DailyRewardEntry
    {
        public string CurrencyId;
        public int Amount;
    }

    [System.Serializable]
    public struct GameBalanceConfig
    {
        [Header("Undo")]
        public int FreeUndosPerSession;
        public int UndoCoinCost;

        [Header("Hint")]
        public int HintCoinCost;

        [Header("Level Completion Rewards")]
        public int NormalCoinReward;
        public int BossCoinReward;
        public int NormalXpReward;
        public int BossXpReward;
        public int LevelUpCoinReward;
        public int LevelUpBonusDivisor;
        public int LevelUpBonusMultiplier;

        [Header("Star Thresholds")]
        public int ThreeStarTargetRatioPercent; // e.g. 100 → ≤target, 130 → ≤target*1.3
        public int TwoStarTargetRatioPercent;

        [Header("Chest Values & Drop Rates")]
        public int ChestXpBronze;
        public int ChestXpSilver;
        public int ChestXpGold;
        public int ChestXpDiamond;
        public float SilverChestChance;
        public float GoldChestChance;
        public float DiamondChestChance;

        [Header("Player XP Thresholds")]
        public int XpThresholdLevel1;
        public int XpThresholdLevel2;
        public int XpThresholdLevel3;
        public int XpThresholdDefault;

        [Header("Daily Rewards")]
        public int MinClaimIntervalMinutes;
        public List<DailyRewardEntry> DailyRewards;

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
        public int MechanicIntensity;
        public List<WorldMechanicType> AllowedMechanics;
    }

    [System.Serializable]
    public struct ColorCurvePoint
    {
        public int LevelThreshold;
        public int ColorCount;
    }

    [System.Serializable]
    public struct LevelThemeData
    {
        public int StartLevel;
        public int EndLevel;
        public int ColorCount;
        public List<WorldMechanicType> ForcedMechanics;
    }

    [System.Serializable]
    public struct SolverLimitBucket
    {
        public int MaxColorCount;
        public int StateLimit;
    }

    [System.Serializable]
    public struct MechanicUnlockEntry
    {
        public WorldMechanicType MechanicType;
        public int FirstAppearanceWorldIndex;
        public string DisplayNameKey;
    }

    [System.Serializable]
    public struct LevelGenConfig
    {
        public int MaxScrambleAttempts;
        public int ScrambleTargetBase;
        public int ScrambleTargetRandomRange;
        public int MaxGenerationSeeds;
        public int MaxCandidates;
        public int BombCountdown;
        public int MaxMechanicTypesPerLevel;
        public int MinSolverMoves;
        public int DefaultMaxMovesLimit;
        public int MaxSolverStatesLimit;
        public int PoleCountClamp;
        public int BaseGenerationSeedMultiplier;
        public int EmptyPolesCompactAttempts;
        public int PoleScaleCapacityDenominator;
        public float TargetScoreBase;
        public float TargetScoreLevelDenominator;
        public float TargetScoreMultiplier;
        public int ScrambleMinEmptyPolesFloor;
        public List<SolverLimitBucket> SolverLimitBuckets;
        public List<int> RetrySeedMultipliers;
        public List<int> MechanicPriorityOrder; // RingType enum values in priority order
    }

    [CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "RingFlow/Game Config Database")]
    public class GameConfigDatabaseSO : ScriptableObject
    {
        public int TotalLevels = 0;
        public int LevelsPerWorld = 25;
        public int TotalWorlds = 0;
        public int BossLevelModulo = 25;
        public int LevelsPerThemeStep = 5;
        public int MinimumEmptyPoles = 0;
        public List<DifficultyBandData> DifficultyBands = new();
        public List<ColorCurvePoint> ColorCurve = new();
        public List<LevelThemeData> LevelThemes = new();
        public List<WorldConfigData> Worlds = new();
        public List<MechanicUnlockEntry> MechanicUnlocks = new();
        public GameBalanceConfig BalanceConfig = new();
        public LevelGenConfig LevelGen = new()
        {
            MaxScrambleAttempts = 1500,
            ScrambleTargetBase = 150,
            ScrambleTargetRandomRange = 80,
            MaxGenerationSeeds = 50,
            MaxCandidates = 5,
            BombCountdown = 5,
            MaxMechanicTypesPerLevel = 4,
            MinSolverMoves = 2,
            DefaultMaxMovesLimit = 200,
            MaxSolverStatesLimit = 100000,
            PoleCountClamp = 12,
            BaseGenerationSeedMultiplier = 12345,
            EmptyPolesCompactAttempts = 10,
            PoleScaleCapacityDenominator = 4,
            TargetScoreBase = 15f,
            TargetScoreLevelDenominator = 2000f,
            TargetScoreMultiplier = 125f,
            ScrambleMinEmptyPolesFloor = 1,
            SolverLimitBuckets = new()
            {
                new() { MaxColorCount = 3, StateLimit = 20000 },
                new() { MaxColorCount = 5, StateLimit = 30000 },
                new() { MaxColorCount = 7, StateLimit = 20000 },
                new() { MaxColorCount = 9, StateLimit = 12000 },
                new() { MaxColorCount = 99, StateLimit = 8000 }
            },
            RetrySeedMultipliers = new() { 27779, 31415, 16180 },
            MechanicPriorityOrder = new() { 3, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 15 }
        };

        private void OnEnable()
        {
            // Data-driven: asset üzerinde kayıtlı değerler kullanılır.
            // OnEnable'da varsayılan değer ataması yok — InitializeDefaults()
            // yalnızca test/editor ortamında manuel çağrılır.
        }

        public void InitializeDefaults()
        {
            TotalLevels = 2000;

            MechanicUnlocks = new List<MechanicUnlockEntry>
            {
                new() { MechanicType = WorldMechanicType.None,      FirstAppearanceWorldIndex = 0,  DisplayNameKey = "mechanic.none" },
                new() { MechanicType = WorldMechanicType.Mystery,   FirstAppearanceWorldIndex = 1,  DisplayNameKey = "mechanic.mystery" },
                new() { MechanicType = WorldMechanicType.Frozen,    FirstAppearanceWorldIndex = 2,  DisplayNameKey = "mechanic.frozen" },
                new() { MechanicType = WorldMechanicType.LockedPole,FirstAppearanceWorldIndex = 3,  DisplayNameKey = "mechanic.locked_pole" },
                new() { MechanicType = WorldMechanicType.Stone,     FirstAppearanceWorldIndex = 4,  DisplayNameKey = "mechanic.stone" },
                new() { MechanicType = WorldMechanicType.Glass,     FirstAppearanceWorldIndex = 5,  DisplayNameKey = "mechanic.glass" },
                new() { MechanicType = WorldMechanicType.Rainbow,   FirstAppearanceWorldIndex = 6,  DisplayNameKey = "mechanic.rainbow" },
                new() { MechanicType = WorldMechanicType.Bomb,      FirstAppearanceWorldIndex = 7,  DisplayNameKey = "mechanic.bomb" },
                new() { MechanicType = WorldMechanicType.Chain,     FirstAppearanceWorldIndex = 8,  DisplayNameKey = "mechanic.chain" },
                new() { MechanicType = WorldMechanicType.Magnet,    FirstAppearanceWorldIndex = 9,  DisplayNameKey = "mechanic.magnet" },
                new() { MechanicType = WorldMechanicType.Paint,     FirstAppearanceWorldIndex = 10, DisplayNameKey = "mechanic.paint" },
                new() { MechanicType = WorldMechanicType.Ghost,     FirstAppearanceWorldIndex = 11, DisplayNameKey = "mechanic.ghost" },
                new() { MechanicType = WorldMechanicType.Portal,    FirstAppearanceWorldIndex = 12, DisplayNameKey = "mechanic.portal" }
            };

            BalanceConfig = new GameBalanceConfig
            {
                FreeUndosPerSession = 5,
                UndoCoinCost = 5,
                HintCoinCost = 50,
                NormalCoinReward = 50,
                BossCoinReward = 500,
                NormalXpReward = 10,
                BossXpReward = 50,
                LevelUpCoinReward = 100,
                LevelUpBonusDivisor = 11,
                LevelUpBonusMultiplier = 10,
                ThreeStarTargetRatioPercent = 100,
                TwoStarTargetRatioPercent = 130,
                ChestXpBronze = 100,
                ChestXpSilver = 250,
                ChestXpGold = 500,
                ChestXpDiamond = 1000,
                SilverChestChance = 0.40f,
                GoldChestChance = 0.10f,
                DiamondChestChance = 0.01f,
                XpThresholdLevel1 = 100,
                XpThresholdLevel2 = 250,
                XpThresholdLevel3 = 500,
                XpThresholdDefault = 1000,
                MinClaimIntervalMinutes = 5,
                DailyRewards = new List<DailyRewardEntry>
                {
                    new() { CurrencyId = "Coins", Amount = 100 },
                    new() { CurrencyId = "Coins", Amount = 150 },
                    new() { CurrencyId = "Coins", Amount = 200 },
                    new() { CurrencyId = "Hint", Amount = 1 },
                    new() { CurrencyId = "Coins", Amount = 300 },
                    new() { CurrencyId = "Theme", Amount = 1 },
                    new() { CurrencyId = "Diamonds", Amount = 25 }
                },
                InterstitialAdInterval = 3
            };

            // Default Difficulty Bands (GDD §5)
            DifficultyBands = new List<DifficultyBandData>
            {
                new()
                {
                    Band = DifficultyBand.Tutorial,
                    MaxLevel = 20,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 1,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery
                    }
                },
                new()
                {
                    Band = DifficultyBand.Easy,
                    MaxLevel = 100,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 1,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen
                    }
                },
                new()
                {
                    Band = DifficultyBand.Medium,
                    MaxLevel = 350,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 2,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone
                    }
                },
                new()
                {
                    Band = DifficultyBand.Hard,
                    MaxLevel = 600,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 2,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow
                    }
                },
                new()
                {
                    Band = DifficultyBand.Expert,
                    MaxLevel = 1000,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 3,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow,
                        WorldMechanicType.Bomb,
                        WorldMechanicType.Chain,
                        WorldMechanicType.Portal
                    }
                },
                new()
                {
                    Band = DifficultyBand.Master,
                    MaxLevel = 1500,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 3,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow,
                        WorldMechanicType.Bomb,
                        WorldMechanicType.Chain,
                        WorldMechanicType.Magnet,
                        WorldMechanicType.Portal
                    }
                },
                new()
                {
                    Band = DifficultyBand.Legend,
                    MaxLevel = 2000,
                    MinEmptyPoles = 1,
                    MaxCapacity = 4,
                    MechanicIntensity = 4,
                    AllowedMechanics = new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow,
                        WorldMechanicType.Bomb,
                        WorldMechanicType.Chain,
                        WorldMechanicType.Magnet,
                        WorldMechanicType.Paint,
                        WorldMechanicType.Ghost,
                        WorldMechanicType.Portal,
                        WorldMechanicType.RandomPool1,
                        WorldMechanicType.RandomPool2,
                        WorldMechanicType.RandomPool3
                    }
                }
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
                    IsEventWorld = (i + 1) % 5 == 0
                };

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
                else if (i == 12) w.MechanicType = WorldMechanicType.Portal;
                else if (i >= 26) w.MechanicType = WorldMechanicType.RandomPool3;
                else if (i >= 19) w.MechanicType = WorldMechanicType.RandomPool2;
                else w.MechanicType = WorldMechanicType.RandomPool1;

                Worlds.Add(w);
            }

            // Generate progressive themes for all 2000 levels (5 levels per theme step)
            LevelThemes = new List<LevelThemeData>();
            int numThemes = TotalLevels / LevelsPerThemeStep;
            for (int t = 0; t < numThemes; t++)
            {
                int startLevel = t * LevelsPerThemeStep + 1;
                int endLevel = Mathf.Min(startLevel + LevelsPerThemeStep - 1, TotalLevels);

                int colorCount;
                if (startLevel <= 50)
                {
                    // Revizyon notları §2/§3/§4/§5: İlk 50 seviye için kademeli renk/zorluk
                    // eğrisi. Her 5 seviyelik adımda (step) renk sayısı yumuşak biçimde artar
                    // (3 → 7), böylece oyuncu belirgin bir ilerleme hissi yaşar.
                    int step = (startLevel - 1) / LevelsPerThemeStep; // 0..9
                    int[] earlyColorRamp = { 3, 4, 4, 5, 5, 6, 6, 6, 7, 7 };
                    colorCount = earlyColorRamp[step];
                }
                else if (startLevel >= 1200) colorCount = 10;
                else if (startLevel >= 800) colorCount = 9;
                else if (startLevel >= 500) colorCount = 8;
                else if (startLevel >= 300) colorCount = 7;
                else if (startLevel >= 150) colorCount = 6;
                else if (startLevel >= 80) colorCount = 5;
                else if (startLevel >= 20) colorCount = 4;
                else colorCount = 3;

                var forcedMechanics = new List<WorldMechanicType>();
                if (t == 0)
                {
                    forcedMechanics.Add(WorldMechanicType.None);
                }
                else if (t <= 11)
                {
                    // Steps 1..11 sequentially introduce all 11 special mechanics
                    forcedMechanics.Add((WorldMechanicType)t);
                }
                else
                {
                    // Cycles through mechanics and random pools
                    int cycle = (t - 12) % 15;
                    if (cycle < 11)
                    {
                        forcedMechanics.Add((WorldMechanicType)(cycle + 1));
                    }
                    else if (cycle == 11)
                    {
                        forcedMechanics.Add(WorldMechanicType.RandomPool1);
                    }
                    else if (cycle == 12)
                    {
                        forcedMechanics.Add(WorldMechanicType.RandomPool2);
                    }
                    else
                    {
                        forcedMechanics.Add(WorldMechanicType.RandomPool3);
                    }
                }

                LevelThemes.Add(new LevelThemeData
                {
                    StartLevel = startLevel,
                    EndLevel = endLevel,
                    ColorCount = colorCount,
                    ForcedMechanics = forcedMechanics
                });
            }
        }

        // Helper search methods
        public DifficultyBand GetBandForLevel(int level)
        {
            if (DifficultyBands == null || DifficultyBands.Count == 0)
            {
                throw new System.InvalidOperationException(
                    "[GameConfigDatabaseSO] DifficultyBands DB'de tanımlı değil. Lütfen DifficultyBands listesini doldurun.");
            }

            foreach (var b in DifficultyBands)
            {
                if (level <= b.MaxLevel) return b.Band;
            }
            return DifficultyBands[^1].Band;
        }

        public int GetColorCountForLevel(int level)
        {
            if (LevelThemes != null && LevelThemes.Count > 0)
            {
                for (int i = 0; i < LevelThemes.Count; i++)
                {
                    var theme = LevelThemes[i];
                    if (level >= theme.StartLevel && level <= theme.EndLevel)
                    {
                        return theme.ColorCount;
                    }
                }
            }

            if (ColorCurve == null || ColorCurve.Count == 0)
            {
                throw new System.InvalidOperationException(
                    $"[GameConfigDatabaseSO] ColorCurve DB'de tanımlı değil. Level {level} için renk sayısı belirlenemiyor. " +
                    "Lütfen ColorCurve listesini doldurun.");
            }

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
            return GetColorCountForLevel(level) + GetMinEmptyPolesForLevel(level);
        }

        public int GetMinEmptyPolesForLevel(int level)
        {
            if (DifficultyBands == null || DifficultyBands.Count == 0)
            {
                throw new System.InvalidOperationException(
                    $"[GameConfigDatabaseSO] DifficultyBands DB'de tanımlı değil. Level {level} için boş direk sayısı belirlenemiyor. " +
                    "Lütfen DifficultyBands listesini doldurun.");
            }

            var band = GetBandForLevel(level);
            foreach (var b in DifficultyBands)
            {
                if (b.Band == band) return b.MinEmptyPoles;
            }

            throw new System.InvalidOperationException(
                $"[GameConfigDatabaseSO] DifficultyBand '{band}' için MinEmptyPoles DB'de tanımlı değil. " +
                "Lütfen DifficultyBands verisini güncelleyin.");
        }

        public int GetMaxCapacityForLevel(int level)
        {
            if (DifficultyBands == null || DifficultyBands.Count == 0)
            {
                throw new System.InvalidOperationException(
                    $"[GameConfigDatabaseSO] DifficultyBands DB'de tanımlı değil. Level {level} için kapasite belirlenemiyor. " +
                    "Lütfen DifficultyBands listesini doldurun.");
            }

            var band = GetBandForLevel(level);
            foreach (var b in DifficultyBands)
            {
                if (b.Band == band) return b.MaxCapacity;
            }

            throw new System.InvalidOperationException(
                $"[GameConfigDatabaseSO] DifficultyBand '{band}' için MaxCapacity DB'de tanımlı değil. " +
                "Lütfen DifficultyBands verisini güncelleyin.");
        }

        public List<WorldMechanicType> GetAllowedMechanicsForLevel(int level)
        {
            var band = GetBandForLevel(level);
            if (DifficultyBands != null && DifficultyBands.Count > 0)
            {
                foreach (var b in DifficultyBands)
                {
                    if (b.Band == band)
                    {
                        if (b.AllowedMechanics != null && b.AllowedMechanics.Count > 0)
                        {
                            return b.AllowedMechanics;
                        }
                        break;
                    }
                }
            }

            throw new System.InvalidOperationException(
                $"[GameConfigDatabaseSO] DifficultyBand '{band}' için AllowedMechanics DB'de tanımlı değil. " +
                "Lütfen DifficultyBands verisini güncelleyin.");
        }

        public int GetMechanicIntensityForLevel(int level)
        {
            var band = GetBandForLevel(level);
            if (DifficultyBands != null && DifficultyBands.Count > 0)
            {
                foreach (var b in DifficultyBands)
                {
                    if (b.Band == band)
                    {
                        if (b.MechanicIntensity > 0)
                            return b.MechanicIntensity;
                        break;
                    }
                }
            }

            throw new System.InvalidOperationException(
                $"[GameConfigDatabaseSO] DifficultyBand '{band}' için MechanicIntensity DB'de tanımlı değil veya 0. " +
                "Lütfen DifficultyBands verisine bu alanı ekleyin.");
        }

        public int GetWorldForLevel(int level)
        {
            if (Worlds == null || Worlds.Count == 0)
                throw new System.InvalidOperationException(
                    "[GameConfigDatabaseSO] Worlds list DB'de tanımlı değil. Lütfen Worlds listesini doldurun.");
            if (LevelsPerWorld <= 0)
                throw new System.InvalidOperationException(
                    "[GameConfigDatabaseSO] LevelsPerWorld 0 veya negatif. Lütfen geçerli bir değer girin.");

            int world = (level - 1) / LevelsPerWorld;
            if (world < 0 || world >= Worlds.Count)
                throw new System.ArgumentOutOfRangeException(nameof(level),
                    $"Level {level}, world index {world} üretiyor ancak Worlds.Count = {Worlds.Count}. " +
                    "Lütfen TotalLevels ve Worlds.Count değerlerini kontrol edin.");
            return world;
        }

        public LevelThemeData GetLevelThemeForLevel(int level)
        {
            if (LevelThemes != null && LevelThemes.Count > 0)
            {
                for (int i = 0; i < LevelThemes.Count; i++)
                {
                    var theme = LevelThemes[i];
                    if (level >= theme.StartLevel && level <= theme.EndLevel)
                    {
                        return theme;
                    }
                }
            }

            throw new System.InvalidOperationException(
                $"[GameConfigDatabaseSO] LevelThemes DB'de tanımlı değil veya level {level} için eşleşen tema bulunamadı. " +
                "Lütfen LevelThemes listesini doldurun.");
        }

        public WorldMechanicType GetMechanicForWorld(int worldIndex)
        {
            if (Worlds == null || Worlds.Count == 0)
                throw new System.InvalidOperationException(
                    "[GameConfigDatabaseSO] Worlds list DB'de tanımlı değil. Lütfen Worlds listesini doldurun.");
            if (worldIndex < 0 || worldIndex >= Worlds.Count)
                throw new System.ArgumentOutOfRangeException(nameof(worldIndex),
                    $"World index {worldIndex} geçersiz. Worlds.Count = {Worlds.Count}. " +
                    "Lütfen Worlds listesini kontrol edin.");
            return Worlds[worldIndex].MechanicType;
        }

        #region Static Progression Helpers (shared)
        public static int ToAbsoluteLevel(int worldIndex, int levelInWorld, GameConfigDatabaseSO dbConfig)
        {
            int count = 0;
            for (int w = 0; w < worldIndex; w++)
                count += dbConfig.LevelsPerWorld;
            return count + levelInWorld;
        }

        public static int LevelInWorldFromAbsoluteLevel(int absoluteLevel, GameConfigDatabaseSO dbConfig, out int worldIndex)
        {
            int acc = 0;
            for (int w = 0; w < dbConfig.Worlds.Count; w++)
            {
                int c = dbConfig.LevelsPerWorld;
                if (absoluteLevel < acc + c)
                {
                    worldIndex = w;
                    return absoluteLevel - acc;
                }
                acc += c;
            }
            worldIndex = dbConfig.Worlds.Count - 1;
            return absoluteLevel - acc;
        }

        public static bool IsBossLevel(GameConfigDatabaseSO dbConfig, int absoluteLevel)
        {
            int acc = 0;
            for (int w = 0; w < dbConfig.Worlds.Count; w++)
            {
                int c = dbConfig.LevelsPerWorld;
                if (absoluteLevel < acc + c)
                    return absoluteLevel == acc + c - 1;
                acc += c;
            }
            return false;
        }
        #endregion
    }

    public enum DifficultyBand
    {
        Tutorial,
        Easy,
        Medium,
        Hard,
        Expert,
        Master,
        Legend,
        Insane,
        Boss,
    }
}
