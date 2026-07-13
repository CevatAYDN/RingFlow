using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Editor
{
    /// <summary>
    /// Central registry for every hardcoded asset path, magic color, and key
    /// the dashboard touches. Keep this in sync whenever a new editor workflow
    /// needs to reference on-disk data.
    ///
    /// Resource keys are a dashboard-managed facade over <see cref="GameplayAssetKeys"/>,
    /// the single source of truth in the runtime assembly, so a key rename
    /// happens in exactly one place.
    ///
    /// All editor visuals (status colors, panel neutrals, card accents, GUI
    /// sizes) are defined once in <see cref="EditorColors"/> and <see cref="EditorSizes"/>
    /// so the dashboard renders consistently instead of with copy-pasted literals.
    /// </summary>
    internal static class EditorPaths
    {
        // ── Asset paths (derived from the canonical keys where applicable) ──
        public const string ScenePath             = "Assets/Scenes/RingFlow.unity";
        public const string ContextDataPath       = "Assets/Settings/GameplayContextData.asset";
        public const string GameConfigDbPath = "Assets/Resources/" + GameplayAssetKeys.GameConfigDatabase + ".asset";
        public const string GameFeelConfigPath = "Assets/Resources/" + GameplayAssetKeys.GameFeelConfig + ".asset";
        public const string RingColorPalettePath = "Assets/Resources/" + GameplayAssetKeys.RingColorPalette + ".asset";
        public const string AudioConfigPath = "Assets/Resources/" + GameplayAssetKeys.AudioConfig + ".asset";
        public const string UIThemeConfigPath = "Assets/Resources/" + GameplayAssetKeys.UIThemeConfig + ".asset";
        public const string StoreCatalogPath = "Assets/Resources/" + GameplayAssetKeys.StoreCatalog + ".asset";
        public const string LocalizationConfigPath = "Assets/Resources/" + GameplayAssetKeys.LocalizationConfig + ".asset";
        public const string RingMechanicDataPath = "Assets/Resources/" + GameplayAssetKeys.RingMechanicData + ".asset";
        public const string ThemeSkinDatabasePath = "Assets/Resources/" + GameplayAssetKeys.ThemeSkinDatabase + ".asset";
        public const string TorusPrefabPath       = "Assets/Resources/Torus.obj";
        public const string LevelsFolder          = "Assets/Resources/Levels";
        public const string ResourcesFolder        = "Assets/Resources";
        public const string UiScreensFolder       = ResourcesFolder + "/UI";

        // ── Resource keys (dashboard-managed facade over GameplayAssetKeys) ──
        public const string GameConfigDatabaseKey = GameplayAssetKeys.GameConfigDatabase;
        public const string RingColorPaletteKey   = GameplayAssetKeys.RingColorPalette;
        public const string GameFeelConfigKey     = GameplayAssetKeys.GameFeelConfig;
        public const string UIThemeConfigKey = GameplayAssetKeys.UIThemeConfig;
        public const string AudioConfigKey = GameplayAssetKeys.AudioConfig;
        public const string StoreCatalogKey = GameplayAssetKeys.StoreCatalog;
        public const string LocalizationConfigKey = GameplayAssetKeys.LocalizationConfig;
        public const string RingMechanicDataKey = GameplayAssetKeys.RingMechanicData;
        public const string ThemeSkinDatabaseKey = GameplayAssetKeys.ThemeSkinDatabase;
        public const string LocalizationKey = GameplayAssetKeys.Localization;
        public const string UiScreenPrefix        = GameplayAssetKeys.UiScreenPrefix;

        // ── Persistence keys (raw strings consolidated here) ──
        public const string PlayerPrefsStorageSeed = "NT_StorageSeed";
        public const string SecureDataFolderName   = "SecureData";

        // ── Scene object names referenced by editor tools ──
        public const string VisualBoardName = "RingFlow_VisualBoard";

        // ── Theme colors (Status bar / scene / boot) ──
        public static readonly Color CameraBackground = new(0.12f, 0.14f, 0.17f);
        public static readonly Color DirectionalLightColor = new(1f, 0.96f, 0.90f);
        public static readonly Color StatusBarSceneLabel = new(0.65f, 0.65f, 0.65f);
        public static readonly Color StatusBarPaused = new(1f, 0.8f, 0.2f);
        public static readonly Color StatusBarPlaying = new(0.3f, 0.85f, 0.3f);

        // ── Cache durations ──
        public const double ValidationCacheSeconds  = 1.0;
        public const double UIRootCacheSeconds       = 0.5;
        public const double RootCacheSeconds         = 0.5;

        // ── Window sizing ──
        public static readonly Vector2 MinWindowSize = new(720f, 760f);

        // ── Editor visual palette (single source for dashboard colors) ──
        // Semantic status colors: route every success/error/info/warning tint here.
        // Neutral/panel colors: shared chrome for boards, borders, and text.
        // Card accents: the colored tiles on the dashboard home screen.
        // Domain-specific previews (ring palette swatches, portal/3D board
        // materials) are intentionally NOT here - they are authored visuals.
        public static class EditorColors
        {
            public static readonly Color Info    = new(0.2f, 0.7f, 1.0f);
            public static readonly Color Success = new(0.2f, 0.7f, 0.2f);
            public static readonly Color Error   = new(0.9f, 0.3f, 0.3f);
            public static readonly Color Warning = new(1f, 0.85f, 0.3f);

            public static readonly Color PanelDark    = new(0.12f, 0.12f, 0.15f);
            public static readonly Color PanelSlate   = new(0.20f, 0.22f, 0.25f);
            public static readonly Color Border       = new(0.35f, 0.35f, 0.38f);
            public static readonly Color MutedText    = new(0.6f, 0.6f, 0.65f);
            public static readonly Color HeaderAccent = new(0.4f, 0.7f, 1.0f, 0.3f);

            public static readonly Color CardLevels     = new(0.25f, 0.55f, 0.85f);
            public static readonly Color CardInterface  = new(0.7f, 0.35f, 0.8f);
            public static readonly Color CardTools      = new(0.4f, 0.7f, 0.4f);
            public static readonly Color CardQuickGen   = new(0.85f, 0.5f, 0.2f);
            public static readonly Color CardQuickSetup = new(0.85f, 0.3f, 0.3f);
        }

        // ── Standard GUI sizes (single source for layout magic numbers) ──
        public static class EditorSizes
        {
            public const float HeaderHeight   = 40f;
            public const float ToolbarHeight  = 28f;
            public const float SectionSpacing      = 4f;
            public const float SectionGap          = 8f;
            public const float SectionBreak        = 10f;
            public const float ButtonHeight        = 32f;
            public const float SectionHeaderHeight = 22f;
            public const float FieldWidth          = 80f;
            public const float CardHeight          = 90f;
        }
    }
}
