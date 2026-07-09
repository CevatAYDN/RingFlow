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
            return GameConfigDatabaseSO.Instance.GetWorldForLevel(absoluteLevel);
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
    /// GDD §5 — Difficulty curve mapping, now driven dynamically by GameConfigDatabaseSO.
    /// GDD §8 — Difficulty Score formülü: poleCount×2.5 + colorCount×3.0 + minMoves×0.8
    ///          + emptyPolePenalty×5.0 + specialCount×4.0 + branchFactor×1.5 − symmetry×2.0
    /// </summary>
    public static class DifficultyCurve
    {
        public static DifficultyBand BandForLevel(int absoluteLevel)
        {
            return GameConfigDatabaseSO.Instance.GetBandForLevel(absoluteLevel);
        }

        public static int ColorCountForLevel(int absoluteLevel)
        {
            return GameConfigDatabaseSO.Instance.GetColorCountForLevel(absoluteLevel);
        }

        public static int PoleCountForLevel(int absoluteLevel)
        {
            return GameConfigDatabaseSO.Instance.GetPoleCountForLevel(absoluteLevel);
        }

        public static int MinEmptyPolesForLevel(int absoluteLevel)
        {
            return GameConfigDatabaseSO.Instance.GetMinEmptyPolesForLevel(absoluteLevel);
        }

        public static int MaxCapacityForLevel(int absoluteLevel)
        {
            return GameConfigDatabaseSO.Instance.GetMaxCapacityForLevel(absoluteLevel);
        }

        // ── GDD §8 Difficulty Score ─────────────────────────────────────

        /// <summary>
        /// Compute a normalized difficulty score (0–1000) for a given level.
        /// Formula: poleCount×2.5 + colorCount×3.0 + minMoves×0.8
        ///          + emptyPolePenalty×5.0 + specialCount×4.0
        ///          + branchFactor×1.5 − symmetry×2.0
        /// </summary>
        /// <param name="absoluteLevel">1-based level index (1..2000).</param>
        /// <param name="minMoves">Expected minimum moves from level data.</param>
        /// <param name="specialCount">Number of special rings (mystery, frozen, etc.) in the level.</param>
        /// <param name="branchFactor">Branching complexity (1.0 = linear, 2.0+ = complex).</param>
        /// <param name="symmetry">Symmetry bonus (0.0 = none, 1.0 = fully symmetric). Higher = easier.</param>
        /// <returns>Difficulty score (0–1000).</returns>
        public static float ComputeDifficultyScore(
            int absoluteLevel,
            int minMoves,
            int specialCount = 0,
            float branchFactor = 1.0f,
            float symmetry = 0.0f)
        {
            int poleCount = PoleCountForLevel(absoluteLevel);
            int colorCount = ColorCountForLevel(absoluteLevel);
            int emptyPoles = MinEmptyPolesForLevel(absoluteLevel);

            float score = 0f;
            score += poleCount * 2.5f;
            score += colorCount * 3.0f;
            score += minMoves * 0.8f;
            score += emptyPoles * 5.0f;
            score += specialCount * 4.0f;
            score += branchFactor * 1.5f;
            score -= symmetry * 2.0f;

            return Mathf.Clamp(score, 0f, 1000f);
        }

        /// <summary>
        /// Returns a human-readable label (Easy / Medium / Hard / Expert / Master) based on score.
        /// </summary>
        public static string DifficultyLabel(float score)
        {
            return score switch
            {
                < 100 => "Tutorial",
                < 250 => "Easy",
                < 450 => "Medium",
                < 650 => "Hard",
                < 850 => "Expert",
                _ => "Master"
            };
        }
    }
}
