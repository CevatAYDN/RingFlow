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
    /// GDD §5 — Per-world configuration.
    /// World/level counts come from GameConfigDatabaseSO.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "RingFlow/World Config")]
    public class WorldConfigSO : ScriptableObject
    {
        [Tooltip("Zero-based world index")]
        public int WorldIndex;

        [Tooltip("Display name (e.g. Grass Valley). Localized via ILocalizationService.")]
        public string Theme = "Unnamed";

        [Tooltip("World-unlock world index. 0 = always unlocked.")]
        public int UnlockedByWorldIndex = -1;

        [Tooltip("Boss level occurs every N levels (the Nth in each world).")]
        public bool IsEventWorld;

        /// <summary>1-based absolute level index. Level 1 = World 0 / Level 1.</summary>
        public static int ToAbsoluteLevel(GameConfigDatabaseSO db, int worldIndex, int levelInWorld)
        {
            return worldIndex * db.LevelsPerWorld + levelInWorld + 1;
        }

        public static int LevelInWorldFromAbsoluteLevel(GameConfigDatabaseSO db, int absoluteLevel)
        {
            if (absoluteLevel < 1) absoluteLevel = 1;
            return (absoluteLevel - 1) % db.LevelsPerWorld;
        }

        public static bool IsBossLevel(GameConfigDatabaseSO db, int absoluteLevel)
        {
            int levelInWorld = LevelInWorldFromAbsoluteLevel(db, absoluteLevel);
            return levelInWorld == db.LevelsPerWorld - 1;
        }
    }
}
