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
        public const string GameConfigDatabase = "GameConfigDatabase";
        public const string RingColorPalette = "RingColorPalette";
        public const string GameFeelConfig = "GameFeelConfig";
        public const string UIThemeConfig = "UIThemeConfig";
        public const string AudioConfig = "AudioConfig";
        public const string Localization = "Localization";
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

            // Legal / GDPR
            public const string GdprAccepted = "RF_GdprAccepted";
        }

        // ── Gameplay tuning constants (single source for all magic numbers) ─
        // Values that were previously hard-coded inside gameplay logic now live
        // here so balance and limits are auditable and consistent everywhere.
        public static class Tuning
        {
            /// <summary>Hard cap on the number of poles a board supports.</summary>
            public const int MaxPoleCount = 12;

            /// <summary>Default ring capacity per pole when not overridden by the database.</summary>
            public const int MaxCapacity = 4;

            /// <summary>Default countdown steps for a bomb ring when not overridden by the database.</summary>
            public const int BombCountdown = 5;

            /// <summary>Sentinel used to mark "no minimum ring count constraint" during generation.</summary>
            public const int SentinelMinRings = 999;

            /// <summary>Upper bound for a generated color index before it wraps to a fallback.</summary>
            public const int ColorIndexMax = 10;

            /// <summary>Fallback color index used when the generated index exceeds <see cref="ColorIndexMax"/>.</summary>
            public const int ColorIndexFallback = 3;

            /// <summary>Default DOTween tween capacity used by the lifecycle bootstrap.</summary>
            public const int TweenCapacityDefault = 1500;

            /// <summary>Default DOTween sequence capacity used by the lifecycle bootstrap.</summary>
            public const int SequenceCapacityDefault = 200;

            /// <summary>Default total world count when the database does not supply one.</summary>
            public const int DefaultWorldCount = 40;

            /// <summary>Default minimum empty poles when the database does not supply one.</summary>
            public const int DefaultMinEmptyPoles = 1;

            /// <summary>Default mechanic intensity when the database does not supply one.</summary>
            public const int DefaultMechanicIntensity = 1;
        }
    }
}
