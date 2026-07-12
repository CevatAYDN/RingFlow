namespace RingFlow.Gameplay
{
    /// <summary>
    /// Single source of truth for every Resources.Load key used across the
    /// project. The dashboard (EditorPaths facade) and all editor sections,
    /// runtime systems, and tests load assets through these keys so a rename
    /// touches exactly one line instead of dozens.
    /// </summary>
    public static class GameplayAssetKeys
    {
        public const string GameConfigDatabase = "GameConfigDatabase";
        public const string RingColorPalette = "RingColorPalette";
        public const string GameFeelConfig = "GameFeelConfig";
        public const string UIThemeConfig = "UIThemeConfig";
        public const string Localization = "Localization";
        public const string UiScreenPrefix = "UI/";
    }
}
