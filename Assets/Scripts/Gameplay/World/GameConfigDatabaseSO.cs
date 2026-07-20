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

    public enum DailyRewardResetMode
    {
        CalendarDayUtc,
        FixedIntervalMinutes
    }

    public enum BombTickMode
    {
        AllBombsPerMove,
        SourceAndTargetPolesOnly,
        MovedBombOnly
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
        public DailyRewardResetMode ResetMode;
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
        public BombTickMode BombTickMode;
        public int BombCountdown; // Ticks before bomb detonates (stored as 4-bit, max 15)
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
        public int TransitionLevelCount; // Band geçişlerinde yoğunluğu kademeli artırmak için geçiş seviye sayısı (Transition Sieve)
        public List<int> RetrySeedMultipliers;
        public List<int> MechanicPriorityOrder; // RingType enum values in priority order
    }

    [System.Serializable]
    public struct ChallengeModeConfig
    {
        public bool Enabled;
        public int LevelInterval;
        public int MoveLimit;
        public int TimeLimitSeconds;
    }

    [CreateAssetMenu(fileName = "GameConfigDatabase", menuName = "RingFlow/Game Config Database")]
    public class GameConfigDatabaseSO : ScriptableObject
    {
        public int TotalLevels = 0;
        public int LevelsPerWorld = 50;
        public int TotalWorlds = 0;
        public int LevelsPerThemeStep = 5;
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
            BombTickMode = BombTickMode.AllBombsPerMove,
            BombCountdown = 3,
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
            TransitionLevelCount = 10,
            SolverLimitBuckets = new()
            {
                new() { MaxColorCount = 3, StateLimit = 30000 },
                new() { MaxColorCount = 5, StateLimit = 50000 },
                new() { MaxColorCount = 7, StateLimit = 80000 },
                new() { MaxColorCount = 9, StateLimit = 100000 },
                new() { MaxColorCount = 99, StateLimit = 100000 }
            },
            RetrySeedMultipliers = new() { 27779, 31415, 16180, 73939, 49297, 65537 },
            MechanicPriorityOrder = new() { 3, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11, 12 }
        };
        public ChallengeModeConfig ChallengeMode = new()
        {
            Enabled = false,
            LevelInterval = 0,
            MoveLimit = 0,
            TimeLimitSeconds = 0
        };

        private void OnEnable()
        {
            // Data-driven: asset üzerinde kayıtlı değerler kullanılır.
            // OnEnable'da varsayılan değer ataması yok — InitializeDefaults()
            // yalnızca test/editor ortamında manuel çağrılır.
        }

        /// <summary>
        /// Sets all database fields to GDD-compliant defaults.
        /// Scales automatically to the current <see cref="TotalLevels"/> value:
        ///   • If TotalLevels == 0 (fresh asset), defaults to 2000 (full GDD scope).
        ///   • If TotalLevels is already set (e.g. 100 for MVP), the difficulty bands,
        ///     color curve, and worlds are generated proportionally so they cover the
        ///     actual level range without leaving unreachable gaps.
        ///
        /// DATA-1: Previously this always wrote TotalLevels=2000 and generated 40 worlds
        /// regardless of what the user had set, making it impossible to work with a
        /// smaller (e.g. 100-level MVP) configuration. Now it respects the existing
        /// TotalLevels OR defaults to 2000 for a blank asset.
        /// </summary>
        public void InitializeDefaults()
        {
            // Preserve any existing TotalLevels the user configured; only default to
            // 2000 when the asset is completely blank (TotalLevels == 0).
            if (TotalLevels <= 0) TotalLevels = 2000;

            // Compute worlds/levelsPerWorld from TotalLevels instead of hardcoding 40x50.
            // GDD §51 targets 40 worlds × 50 levels. For smaller configs we reduce worlds
            // while keeping 50 levels per world if possible, or shrink LevelsPerWorld to 10.
            const int preferredLevelsPerWorld = 50;
            const int minLevelsPerWorld = 10;

            if (TotalLevels >= preferredLevelsPerWorld)
            {
                LevelsPerWorld = preferredLevelsPerWorld;
                TotalWorlds = Mathf.Max(1, TotalLevels / preferredLevelsPerWorld);
                // Snap TotalLevels to a clean multiple so worlds are evenly divided.
                TotalLevels = TotalWorlds * LevelsPerWorld;
            }
            else
            {
                LevelsPerWorld = Mathf.Max(minLevelsPerWorld, TotalLevels);
                TotalWorlds = 1;
            }

            // Boss automatically detected: last level of each world via IsBossLevel().
            // No separate BossLevelModulo field needed — GDD §46.

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
                ResetMode = DailyRewardResetMode.CalendarDayUtc,
                MinClaimIntervalMinutes = 5,
                DailyRewards = new List<DailyRewardEntry>
                {
                    new() { CurrencyId = CurrencyIds.Coins, Amount = 100 },
                    new() { CurrencyId = CurrencyIds.Coins, Amount = 150 },
                    new() { CurrencyId = CurrencyIds.Coins, Amount = 200 },
                    new() { CurrencyId = CurrencyIds.Hint, Amount = 1 },
                    new() { CurrencyId = CurrencyIds.Coins, Amount = 300 },
                    new() { CurrencyId = CurrencyIds.Theme, Amount = 1 },
                    new() { CurrencyId = CurrencyIds.Diamonds, Amount = 25 }
                },
                InterstitialAdInterval = 3
            };

            // GDD §3: DifficultyBands scaled proportionally to TotalLevels.
            // Capacity curve: Tutorial=3 (gentle intro), Easy-Hard=4, Expert+=5.
            // Empty poles decrease as difficulty rises (fewer buffers = tighter play).
            int t = TotalLevels;
            DifficultyBands = new List<DifficultyBandData>
            {
                new()
                {
                    Band = DifficultyBand.Tutorial,
                    MaxLevel = Mathf.Max(5, Mathf.RoundToInt(t * 0.10f)),
                    MinEmptyPoles = 2, MaxCapacity = 3, MechanicIntensity = 1,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery }
                },
                new()
                {
                    Band = DifficultyBand.Easy,
                    MaxLevel = Mathf.RoundToInt(t * 0.25f),
                    MinEmptyPoles = 2, MaxCapacity = 4, MechanicIntensity = 1,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery, WorldMechanicType.Frozen }
                },
                new()
                {
                    Band = DifficultyBand.Medium,
                    MaxLevel = Mathf.RoundToInt(t * 0.45f),
                    MinEmptyPoles = 1, MaxCapacity = 4, MechanicIntensity = 2,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery, WorldMechanicType.Frozen,
                          WorldMechanicType.LockedPole, WorldMechanicType.Stone }
                },
                new()
                {
                    Band = DifficultyBand.Hard,
                    MaxLevel = Mathf.RoundToInt(t * 0.60f),
                    MinEmptyPoles = 1, MaxCapacity = 4, MechanicIntensity = 2,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery, WorldMechanicType.Frozen,
                          WorldMechanicType.LockedPole, WorldMechanicType.Stone,
                          WorldMechanicType.Glass, WorldMechanicType.Rainbow }
                },
                new()
                {
                    Band = DifficultyBand.Expert,
                    MaxLevel = Mathf.RoundToInt(t * 0.75f),
                    MinEmptyPoles = 1, MaxCapacity = 5, MechanicIntensity = 3,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery, WorldMechanicType.Frozen,
                          WorldMechanicType.LockedPole, WorldMechanicType.Stone,
                          WorldMechanicType.Glass, WorldMechanicType.Rainbow,
                          WorldMechanicType.Bomb, WorldMechanicType.Chain, WorldMechanicType.Portal }
                },
                new()
                {
                    Band = DifficultyBand.Master,
                    MaxLevel = Mathf.RoundToInt(t * 0.90f),
                    MinEmptyPoles = 1, MaxCapacity = 5, MechanicIntensity = 3,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery, WorldMechanicType.Frozen,
                          WorldMechanicType.LockedPole, WorldMechanicType.Stone,
                          WorldMechanicType.Glass, WorldMechanicType.Rainbow,
                          WorldMechanicType.Bomb, WorldMechanicType.Chain,
                          WorldMechanicType.Magnet, WorldMechanicType.Portal }
                },
                new()
                {
                    Band = DifficultyBand.Legend,
                    MaxLevel = t,
                    MinEmptyPoles = 1, MaxCapacity = 5, MechanicIntensity = 4,
                    AllowedMechanics = new List<WorldMechanicType>
                        { WorldMechanicType.None, WorldMechanicType.Mystery, WorldMechanicType.Frozen,
                          WorldMechanicType.LockedPole, WorldMechanicType.Stone,
                          WorldMechanicType.Glass, WorldMechanicType.Rainbow,
                          WorldMechanicType.Bomb, WorldMechanicType.Chain,
                          WorldMechanicType.Magnet, WorldMechanicType.Paint,
                          WorldMechanicType.Portal,
                          WorldMechanicType.RandomPool1, WorldMechanicType.RandomPool2,
                          WorldMechanicType.RandomPool3 }
                }
            };

            // DATA-1: ColorCurve is now scaled to TotalLevels.
            // 8 breakpoints distribute the 3→10 color range across the full level span.
            // Each breakpoint is expressed as a fraction of TotalLevels so at 100 levels
            // the curve reaches 10 colors at level ~50 (challenging endgame),
            // while at 2000 levels it ramps more gradually (relaxed early game).
            ColorCurve = new List<ColorCurvePoint>
            {
                new() { LevelThreshold = 1,                              ColorCount = 3  },
                new() { LevelThreshold = Mathf.Max(2,  t / 20),         ColorCount = 4  },
                new() { LevelThreshold = Mathf.Max(5,  t / 10),         ColorCount = 5  },
                new() { LevelThreshold = Mathf.Max(10, t * 20 / 100),   ColorCount = 6  },
                new() { LevelThreshold = Mathf.Max(20, t * 35 / 100),   ColorCount = 7  },
                new() { LevelThreshold = Mathf.Max(30, t * 50 / 100),   ColorCount = 8  },
                new() { LevelThreshold = Mathf.Max(50, t * 65 / 100),   ColorCount = 9  },
                new() { LevelThreshold = Mathf.Max(70, t * 80 / 100),   ColorCount = 10 },
            };

            // DATA-1: Worlds list generated from computed TotalWorlds (adaptive).
            Worlds = new List<WorldConfigData>();
            for (int i = 0; i < TotalWorlds; i++)
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
                else if (i == 12) w.MechanicType = WorldMechanicType.Portal;
                else if (i >= 26) w.MechanicType = WorldMechanicType.RandomPool3;
                else if (i >= 19) w.MechanicType = WorldMechanicType.RandomPool2;
                else w.MechanicType = WorldMechanicType.RandomPool1;

                Worlds.Add(w);
            }

            // Generate progressive themes across all TotalLevels (LevelsPerThemeStep per step).
            LevelThemes = new List<LevelThemeData>();
            int numThemes = Mathf.Max(1, TotalLevels / Mathf.Max(1, LevelsPerThemeStep));
            for (int ti = 0; ti < numThemes; ti++)
            {
                int startLevel = ti * LevelsPerThemeStep + 1;
                int endLevel = Mathf.Min(startLevel + LevelsPerThemeStep - 1, TotalLevels);

                int colorCount = ComputeColorCountForLevel(startLevel, ColorCurve);

                var forcedMechanics = new List<WorldMechanicType>();
                if (ti == 0)
                {
                    forcedMechanics.Add(WorldMechanicType.None);
                }
                else if (ti <= 11)
                {
                    // Steps 1..11 sequentially introduce all 11 special mechanics
                    forcedMechanics.Add((WorldMechanicType)ti);
                }
                else
                {
                    // Cycles through mechanics and random pools
                    int cycle = (ti - 12) % 15;
                    if (cycle < 11)
                        forcedMechanics.Add((WorldMechanicType)(cycle + 1));
                    else if (cycle == 11)
                        forcedMechanics.Add(WorldMechanicType.RandomPool1);
                    else if (cycle == 12)
                        forcedMechanics.Add(WorldMechanicType.RandomPool2);
                    else
                        forcedMechanics.Add(WorldMechanicType.RandomPool3);
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

        /// <summary>
        /// Seviye endeksinden kademeli, monotonik artan renk sayısını hesaplar
        /// (revizyon notları §2–§5). Erken seviyelerde belirgin ilerleme hissi verir;
        /// ilerledikçe renk çeşitliliği hiçbir zaman azalmaz, nihai tavan 10 renktir.
        /// <see cref="ColorCurve"/> listesini kullanarak SSOT ihlalini ortadan kaldırır.
        /// </summary>
        public static int ComputeColorCountForLevel(int level, List<ColorCurvePoint> colorCurve = null)
        {
            if (colorCurve != null && colorCurve.Count > 0)
            {
                int count = colorCurve[0].ColorCount;
                foreach (var pt in colorCurve)
                {
                    if (level >= pt.LevelThreshold)
                        count = pt.ColorCount;
                }
                return count;
            }

            // Fallback — yalnızca ColorCurve henüz başlatılmamışsa kullanılır (InitializeDefaults öncesi).
            if (level <= 5) return 3;
            if (level <= 15) return 4;
            if (level <= 30) return 5;
            if (level <= 60) return 6;
            if (level <= 120) return 7;
            if (level <= 250) return 8;
            if (level <= 500) return 9;
            return 10;
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
            // GDD: return colorCount + MinEmptyPoles directly from the difficulty band data.
            // Master/Legend bands define MinEmptyPoles=0 — this is intentional GDD data.
            // The generator's scramble-phase safety guard lives in InitLevelCommand
            // (if (poleCount < colorCount + 1) poleCount = colorCount + 1;) and is
            // independent of this method. Do NOT add Math.Max here.
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

        public bool IsChallengeLevel(int level)
        {
            return ChallengeMode.Enabled
                   && ChallengeMode.LevelInterval > 0
                   && level > 0
                   && level % ChallengeMode.LevelInterval == 0;
        }

        public int GetChallengeMoveLimitForLevel(int level)
        {
            return IsChallengeLevel(level) ? ChallengeMode.MoveLimit : 0;
        }

        public int GetChallengeTimeLimitSecondsForLevel(int level)
        {
            return IsChallengeLevel(level) ? ChallengeMode.TimeLimitSeconds : 0;
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

        public int GetLevelCountForWorld(int world)
        {
            if (world < 0 || (Worlds != null && world >= Worlds.Count))
                return 0;
            int remaining = TotalLevels - (world * LevelsPerWorld);
            return System.Math.Max(0, System.Math.Min(remaining, LevelsPerWorld));
        }

        public int GetXpRequiredForLevel(int level)
        {
            var cfg = BalanceConfig;
            return level switch
            {
                1 => cfg.XpThresholdLevel1,
                2 => cfg.XpThresholdLevel2,
                3 => cfg.XpThresholdLevel3,
                _ => cfg.XpThresholdDefault
            };
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

        #region DB Validation Helpers

        /// <summary>
        /// Validates that DifficultyBands are ordered correctly (no overlap, no gap,
        /// last band covers TotalLevels) and returns a list of violation messages.
        /// Returns an empty list when the configuration is consistent.
        ///
        /// Called by the DatabaseSection editor and by unit tests (DataDrivenConfigTests).
        /// Never throws — accumulates all issues so the caller can display them together.
        /// </summary>
        public List<string> ValidateDifficultyBands()
        {
            var issues = new List<string>();

            if (DifficultyBands == null || DifficultyBands.Count == 0)
            {
                issues.Add("DifficultyBands list is empty — call InitializeDefaults() first.");
                return issues;
            }

            int prev = 0;
            for (int i = 0; i < DifficultyBands.Count; i++)
            {
                var band = DifficultyBands[i];

                // Each band's MaxLevel must be strictly greater than the previous band's MaxLevel.
                if (band.MaxLevel <= prev)
                    issues.Add($"DifficultyBands[{i}] ({band.Band}): MaxLevel={band.MaxLevel} " +
                               $"is not strictly greater than previous MaxLevel={prev}. " +
                               "Bands must not overlap or have zero-width ranges.");

                // MaxCapacity sanity
                if (band.MaxCapacity < 1)
                    issues.Add($"DifficultyBands[{i}] ({band.Band}): MaxCapacity={band.MaxCapacity} < 1.");

                // MinEmptyPoles sanity
                if (band.MinEmptyPoles < 0)
                    issues.Add($"DifficultyBands[{i}] ({band.Band}): MinEmptyPoles={band.MinEmptyPoles} < 0.");

                // MechanicIntensity sanity
                if (band.MechanicIntensity < 1)
                    issues.Add($"DifficultyBands[{i}] ({band.Band}): MechanicIntensity={band.MechanicIntensity} < 1.");

                prev = band.MaxLevel;
            }

            // Last band must cover TotalLevels
            int lastMax = DifficultyBands[^1].MaxLevel;
            if (TotalLevels > 0 && lastMax < TotalLevels)
                issues.Add($"Last DifficultyBand ({DifficultyBands[^1].Band}) MaxLevel={lastMax} " +
                           $"< TotalLevels={TotalLevels}. Levels {lastMax + 1}–{TotalLevels} have no band.");

            return issues;
        }

        /// <summary>
        /// Validates that ColorCurve thresholds are strictly monotonically increasing
        /// and that color counts do not decrease (enforcing the GDD "never fewer colors"
        /// guarantee). Returns a list of violation messages; empty = OK.
        /// </summary>
        public List<string> ValidateColorCurve()
        {
            var issues = new List<string>();

            if (ColorCurve == null || ColorCurve.Count == 0)
            {
                issues.Add("ColorCurve list is empty — call InitializeDefaults() first.");
                return issues;
            }

            for (int i = 1; i < ColorCurve.Count; i++)
            {
                var prev = ColorCurve[i - 1];
                var curr = ColorCurve[i];

                // Thresholds must be strictly ascending
                if (curr.LevelThreshold <= prev.LevelThreshold)
                    issues.Add($"ColorCurve[{i}]: LevelThreshold={curr.LevelThreshold} " +
                               $"is not strictly greater than ColorCurve[{i - 1}].LevelThreshold={prev.LevelThreshold}. " +
                               "Thresholds must be in ascending order.");

                // Color count must not decrease (GDD §54 — progressive difficulty)
                if (curr.ColorCount < prev.ColorCount)
                    issues.Add($"ColorCurve[{i}]: ColorCount={curr.ColorCount} < " +
                               $"ColorCurve[{i - 1}].ColorCount={prev.ColorCount}. " +
                               "Color count must be non-decreasing (GDD §54).");
            }

            // First threshold must be 1 (covers level 1)
            if (ColorCurve[0].LevelThreshold != 1)
                issues.Add($"ColorCurve[0].LevelThreshold={ColorCurve[0].LevelThreshold} should be 1 " +
                           "so level 1 always has a defined color count.");

            return issues;
        }

        /// <summary>
        /// Runs all DB validators and returns every issue found.
        /// Combines ValidateDifficultyBands() + ValidateColorCurve()
        /// plus cross-field checks (Worlds vs MechanicUnlocks, etc.).
        /// Returns empty list when the asset is fully consistent.
        /// </summary>
        public List<string> ValidateAsset()
        {
            var issues = new List<string>();

            issues.AddRange(ValidateDifficultyBands());
            issues.AddRange(ValidateColorCurve());

            // TotalLevels must be positive
            if (TotalLevels <= 0)
                issues.Add("TotalLevels must be > 0.");

            // LevelsPerWorld must be positive
            if (LevelsPerWorld <= 0)
                issues.Add("LevelsPerWorld must be > 0.");

            // TotalLevels must be evenly divisible by LevelsPerWorld (unless worlds==1)
            if (TotalWorlds > 1 && TotalLevels != TotalWorlds * LevelsPerWorld)
                issues.Add($"TotalLevels ({TotalLevels}) ≠ TotalWorlds ({TotalWorlds}) × LevelsPerWorld ({LevelsPerWorld}) = {TotalWorlds * LevelsPerWorld}. " +
                           "Levels are not evenly distributed across worlds.");

            // Worlds list must match TotalWorlds
            if (Worlds == null)
                issues.Add("Worlds list is null.");
            else if (Worlds.Count != TotalWorlds)
                issues.Add($"Worlds.Count ({Worlds.Count}) ≠ TotalWorlds ({TotalWorlds}). " +
                           "Each world needs a WorldConfigData entry.");

            // LevelThemes must cover the full level range
            if (LevelThemes == null || LevelThemes.Count == 0)
                issues.Add("LevelThemes list is empty — levels will have no theme assignment.");
            else
            {
                int lastEnd = 0;
                for (int i = 0; i < LevelThemes.Count; i++)
                {
                    var t = LevelThemes[i];
                    if (t.StartLevel <= lastEnd)
                        issues.Add($"LevelThemes[{i}]: StartLevel={t.StartLevel} ≤ previous EndLevel={lastEnd}. Themes must not overlap.");
                    if (t.StartLevel > t.EndLevel)
                        issues.Add($"LevelThemes[{i}]: StartLevel={t.StartLevel} > EndLevel={t.EndLevel}. Invalid range.");
                    lastEnd = t.EndLevel;
                }
                if (lastEnd < TotalLevels)
                    issues.Add($"LevelThemes last EndLevel={lastEnd} < TotalLevels={TotalLevels}. " +
                               $"Levels {lastEnd + 1}–{TotalLevels} have no theme.");
            }

            // MechanicUnlocks must have entries
            if (MechanicUnlocks == null || MechanicUnlocks.Count == 0)
                issues.Add("MechanicUnlocks list is empty.");

            // BalanceConfig base checks
            if (BalanceConfig.NormalCoinReward <= 0)
                issues.Add("BalanceConfig.NormalCoinReward must be > 0.");
            if (BalanceConfig.FreeUndosPerSession < 0)
                issues.Add("BalanceConfig.FreeUndosPerSession must be >= 0.");

            // LevelGen basic sanity
            if (LevelGen.MaxSolverStatesLimit <= 0)
                issues.Add("LevelGen.MaxSolverStatesLimit must be > 0.");

            return issues;
        }

        #endregion

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

    // FIX-L1: DifficultyBand.Insane and DifficultyBand.Boss are defined in the enum but
    // never referenced in DifficultyBands list (InitializeDefaults), GetBandForLevel, or
    // any level generation path. They are dead entries that confuse readers into thinking
    // there is an "Insane" difficulty curve or a "Boss" band configuration.
    //
    // GDD §46 defines Boss Levels as every-50th level determined by IsBossLevel() which
    // uses the world boundary calculation — not a DifficultyBand. Boss difficulty is
    // handled by the Boss-specific reward multipliers in GameBalanceConfig (BossCoinReward,
    // BossXpReward), not by a separate band.
    //
    // Resolution: keep the enum values for forward-compatibility (removing them would be
    // a breaking change for any serialized AssetDatabase references) but document them
    // as unused/reserved so future developers don't attempt to wire them.
    public enum DifficultyBand
    {
        Tutorial,
        Easy,
        Medium,
        Hard,
        Expert,
        Master,
        Legend
    }
}
