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
    /// </summary>
    internal static class EditorPaths
    {
        // ── Asset paths (derived from the canonical keys where applicable) ──
        public const string ScenePath             = "Assets/Scenes/RingFlow.unity";
        public const string ContextDataPath       = "Assets/Settings/GameplayContextData.asset";
        public const string GameConfigDbPath      = "Assets/Resources/" + GameplayAssetKeys.GameConfigDatabase + ".asset";
        public const string GameFeelConfigPath    = "Assets/Resources/" + GameplayAssetKeys.GameFeelConfig + ".asset";
        public const string TorusPrefabPath       = "Assets/Resources/Torus.obj";
        public const string LevelsFolder          = "Assets/Resources/Levels";
        public const string UiScreensFolder       = "Assets/Resources/UI";

        // ── Resource keys (dashboard-managed facade over GameplayAssetKeys) ──
        public const string GameConfigDatabaseKey = GameplayAssetKeys.GameConfigDatabase;
        public const string RingColorPaletteKey   = GameplayAssetKeys.RingColorPalette;
        public const string GameFeelConfigKey     = GameplayAssetKeys.GameFeelConfig;
        public const string UIThemeConfigKey      = GameplayAssetKeys.UIThemeConfig;
        public const string LocalizationKey       = GameplayAssetKeys.Localization;
        public const string UiScreenPrefix        = GameplayAssetKeys.UiScreenPrefix;

        // ── Persistence keys (raw strings consolidated here) ──
        public const string PlayerPrefsStorageSeed = "NT_StorageSeed";
        public const string SecureDataFolderName   = "SecureData";

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
    }
}
