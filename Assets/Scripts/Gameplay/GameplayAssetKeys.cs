namespace RingFlow.Gameplay
{
    /// <summary>
    /// Single source of truth for every asset key, persistence key, and shared
    /// gameplay tuning constant used across the project. The dashboard
    /// (EditorPaths facade) and all editor sections, runtime systems, and tests
    /// reference these keys so a rename or retune touches exactly one line
    /// instead of dozens spread across the codebase.
    ///
    /// Every literal string and tuning number must live here. Do not re-declare
    /// a key or constant anywhere else - alias it from this class instead.
    /// </summary>
    public static class GameplayAssetKeys
    {
        // ── Resources.Load keys ───────────────────────────────────────────
        // NOTE: Files reside in Assets/Resources/Configs/, so keys must include
        // the "Configs/" subfolder prefix.  Exception: "Localization" is the CSV
        // file at Assets/Resources/Localization.csv (root Resources, no prefix).
        public const string GameConfigDatabase = "Configs/GameConfigDatabase";
        public const string RingColorPalette = "Configs/RingColorPalette";
        public const string GameFeelConfig = "Configs/GameFeelConfig";
        public const string UIThemeConfig = "Configs/UIThemeConfig";
        public const string AudioConfig = "Configs/AudioConfig";
        public const string Localization = "Localization";

        // ── New data-driven config SO keys (Bölüm 1.2, 1.3, 2.1, 2.2) ──
        public const string StoreCatalog = "Configs/StoreCatalog";
        public const string LocalizationConfig = "Configs/LocalizationConfig";
        public const string RingMechanicData = "Configs/RingMechanicData";
        public const string ThemeSkinDatabase = "Configs/ThemeSkinDatabase";

        public const string UiScreenPrefix = "UI/";

        // ── PlayerPrefs keys (single source for ALL persistence keys) ──────
        // Runtime systems, mediators, and the editor all read/write through
        // these so a key rename happens in exactly one place.
        public static class PlayerPrefs
        {
            // Economy / progression
            public const string Coins = "RF_Coins";
            public const string Diamonds = "RF_Diamonds";
            public const string Xp = "RF_Xp";
            public const string CurrentLevel = "RF_CurrentLevel";
            public const string MaxUnlocked = "RF_MaxUnlocked";
            public const string Worlds = "RF_Worlds";
            public const string Themes = "RF_Themes";
            public const string ChestBronze = "RF_Chest_Bronze";
            public const string ChestSilver = "RF_Chest_Silver";
            public const string ChestGold = "RF_Chest_Gold";
            public const string ChestDiamond = "RF_Chest_Diamond";
            public const string PlayerLevel = "RF_PlayerLevel";
            public const string DailyDayIndex = "RF_DailyDayIndex";
            public const string DailyLastClaimUtc = "RF_DailyLastClaimUtc";
            public const string UndoUsedFree = "RF_UndoUsedFree";
            public const string Achievements = "RF_Achievements";
            public const string RemoveAds = "RF_RemoveAds";
            public const string HintCount = "RF_HintCount";

            // Save system
            public const string FirstLaunchTime = "RF_FirstLaunchTime";
            public const string SaveSchemaVersion = "RF_SaveSchemaVersion";
            public const string SaveChecksum = "RF_SaveChecksum";

            // Settings
            public const string Music = "RF_Set_Music";
            public const string Sfx = "RF_Set_Sfx";
            public const string Haptic = "RF_Set_Haptic";
            public const string ReduceMotion = "RF_Set_ReduceMotion";
            public const string SlowMode = "RF_Set_SlowMode";
            public const string BigButtons = "RF_Set_BigButtons";
            public const string ColorBlind = "RF_Set_ColorBlind";
            public const string Language = "RF_Set_Language";

            // Legal / GDPR
            public const string GdprAccepted = "RF_GdprAccepted";
        }

        // ── Gameplay tuning constants (single source for all magic numbers) ─
        // These are HARD LIMITS and ENGINE DEFAULTS only — not gameplay balance values.
        // Gameplay balance always comes from GameConfigDatabaseSO (ScriptableObject).
        // C5: Any site that uses Tuning.* as a runtime fallback instead of reading from
        // the authoritative SO should throw InvalidOperationException in dev builds to
        // surface misconfiguration early.
        public static class Tuning
        {
            /// <summary>Ring pole count hard clamp — must match GameConfigDatabase.LevelGen.PoleCountClamp.</summary>
            public const int MaxPoleCount = 12;

            /// <summary>Max rings per pole hard default. Must match GameConfigDatabase ring capacity.</summary>
            public const int MaxCapacity = 4;

            /// <summary>
            /// Bomb countdown HARD FALLBACK. Production code must read from LevelGenConfig.BombCountdown.
            /// Use ThrowIfMissingConfig() in callers that reach this value at runtime.
            /// </summary>
            public const int BombCountdown = 5;

            /// <summary>
            /// Fallback when DifficultyBands is not configured. Surfaces missing config in dev builds.
            /// </summary>
            public const int DefaultMinEmptyPoles = 1;

            /// <summary>
            /// Fallback when DifficultyBands is not configured. Surfaces missing config in dev builds.
            /// </summary>
            public const int DefaultMechanicIntensity = 1;

            /// <summary>Default tween capacity for DOTween (used before SO loading in bootstrap only).</summary>
            public const int TweenCapacityDefault = 1500;

            /// <summary>Default sequence capacity for DOTween (used before SO loading in bootstrap only).</summary>
            public const int SequenceCapacityDefault = 200;

            /// <summary>Fallback world count when SO is not configured.</summary>
            public const int DefaultWorldCount = 40;

            /// <summary>Threshold for sentinel mechanic activation (technical debt: must come from SO).</summary>
            public const int SentinelMinRings = 999;

            /// <summary>Highest valid color index (hard clamp, not balance).</summary>
            public const int ColorIndexMax = 10;

            /// <summary>
            /// Color index fallback when palette has fewer entries.
            /// C5: callers must validate palette size before reaching this; never rely on it silently.
            /// </summary>
            public const int ColorIndexFallback = 3;

            /// <summary>
            /// C5: Asserts in development builds that the caller should never reach a Tuning.* fallback
            /// for a value that must come from an authoritative ScriptableObject.
            /// In release builds this is a no-op to avoid hard crashes.
            /// </summary>
            [System.Diagnostics.Conditional("UNITY_EDITOR")]
            [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
            public static void AssertNotFallback(string callerName, string fieldName)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                throw new System.InvalidOperationException(
                    $"[GameplayAssetKeys.Tuning] '{callerName}' reached the fallback value for '{fieldName}'. " +
                    $"This field must be read from the authoritative ScriptableObject (GameConfigDatabaseSO). " +
                    $"Bind the SO via DI and ensure it is loaded before this code runs.");
#endif
            }
        }
    }
}
