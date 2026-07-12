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
}
