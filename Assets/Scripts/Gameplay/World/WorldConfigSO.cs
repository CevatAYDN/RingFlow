using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §5 — Difficulty bands. Maps directly to the table at line 58 of the GDD.
    /// </summary>
    public enum DifficultyBand
    {
        Tutorial = 0,
        Easy,
        Medium,
        Hard,
        Expert,
        Master,
        Legend
    }

    /// <summary>
    /// GDD §5 — Per-world configuration. 40 worlds × 50 levels = 2000 levels.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "RingFlow/World Config")]
    public class WorldConfigSO : ScriptableObject
    {
        [Tooltip("Zero-based world index 0..39")]
        public int WorldIndex;

        [Tooltip("Display name (e.g. Grass Valley). Localized via ILocalizationService.")]
        public string Theme = "Unnamed";

        [Tooltip("World-unlock world index. 0 = always unlocked.")]
        public int UnlockedByWorldIndex = -1;

        [Tooltip("Boss level occurs every 50 levels (always the 50th).")]
        public bool IsEventWorld;

        private void OnValidate()
        {
            if (WorldIndex < 0) WorldIndex = 0;
            if (WorldIndex > 39) WorldIndex = 39;
        }

        public const int LevelsPerWorld = 50;
        public const int TotalWorlds = 40;
        public const int TotalLevels = TotalWorlds * LevelsPerWorld; // 2000

        /// <summary>1-based absolute level index (1..2000). Level 1 = World 0 / Level 1.</summary>
        public static int ToAbsoluteLevel(int worldIndex, int levelInWorld)
        {
            return worldIndex * LevelsPerWorld + levelInWorld + 1;
        }

        public static int WorldFromAbsoluteLevel(int absoluteLevel)
        {
            if (absoluteLevel < 1) absoluteLevel = 1;
            int world = (absoluteLevel - 1) / LevelsPerWorld;
            return Mathf.Clamp(world, 0, TotalWorlds - 1);
        }

        public static int LevelInWorldFromAbsoluteLevel(int absoluteLevel)
        {
            if (absoluteLevel < 1) absoluteLevel = 1;
            int lvl = (absoluteLevel - 1) % LevelsPerWorld;
            return lvl;
        }

        public static bool IsBossLevel(int absoluteLevel)
        {
            // Boss every 50 levels (last in each world) — GDD §5
            int levelInWorld = LevelInWorldFromAbsoluteLevel(absoluteLevel);
            return levelInWorld == LevelsPerWorld - 1;
        }
    }

    /// <summary>
    /// Difficulty curve mapping per GDD §5.
    /// Tutorial(1-20) → Easy(21-100) → Medium(101-350) → Hard(351-600) → Expert(601-1000) → Master(1001-1500) → Legend(1501-2000).
    /// </summary>
    public static class DifficultyCurve
    {
        public static DifficultyBand BandForLevel(int absoluteLevel)
        {
            if (absoluteLevel <= 20)  return DifficultyBand.Tutorial;
            if (absoluteLevel <= 100) return DifficultyBand.Easy;
            if (absoluteLevel <= 350) return DifficultyBand.Medium;
            if (absoluteLevel <= 600) return DifficultyBand.Hard;
            if (absoluteLevel <= 1000) return DifficultyBand.Expert;
            if (absoluteLevel <= 1500) return DifficultyBand.Master;
            return DifficultyBand.Legend;
        }

        /// <summary>GDD §5 color progression: 3(1) → 4(20) → 5(80) → 6(150) → 7(300) → 8(500) → 9(800) → 10(1200+).</summary>
        public static int ColorCountForLevel(int absoluteLevel)
        {
            if (absoluteLevel <= 1)   return 3;
            if (absoluteLevel <= 20)  return 4;
            if (absoluteLevel <= 80)  return 5;
            if (absoluteLevel <= 150) return 6;
            if (absoluteLevel <= 300) return 7;
            if (absoluteLevel <= 500) return 8;
            if (absoluteLevel <= 800) return 9;
            return 10;
        }

        /// <summary>GDD §5 pole count: 4(tutorial) → 10(legend) — caps at 10 due to solver struct limit.</summary>
        public static int PoleCountForLevel(int absoluteLevel)
        {
            // Maps Tutorial(1-20)=4, Easy(21-100)=5, Medium(101-350)=6, Hard(351-600)=7,
            // Expert(601-1000)=8, Master(1001-1500)=9, Legend(1501-2000)=10.
            return 4 + (int)BandForLevel(absoluteLevel);
        }

        /// <summary>Empty-pole rule: asla 0 değil (never 0). Easy 2, Medium 1, Hard 1+ arranged.</summary>
        public static int MinEmptyPolesForLevel(int absoluteLevel)
        {
            var band = BandForLevel(absoluteLevel);
            if (band == DifficultyBand.Tutorial || band == DifficultyBand.Easy) return 2;
            return 1;
        }

        /// <summary>World-spec: 6 event (5–6 cap) only on Event/Challenge modes. Default 4.</summary>
        public static int MaxCapacityForLevel(int absoluteLevel)
        {
            return WorldConfigSO.IsBossLevel(absoluteLevel) ? 4 : 4;
        }
    }
}
