using UnityEngine;

namespace RingFlow.Gameplay
{
    [System.Serializable]
    public struct ButtonColorConfig
    {
        public Color NormalColor;
        public Color HighlightedColor;
        public Color PressedColor;
        public Color SelectedColor;
        public Color DisabledColor;
    }

    [CreateAssetMenu(fileName = "UIThemeConfig", menuName = "RingFlow/UI Theme Config", order = 52)]
    public class UIThemeConfigSO : ScriptableObject
    {
        [Header("Colors")]
        public Color PrimaryColor    = new Color(0.22f, 0.51f, 0.91f); // #387AE8
        public Color PrimaryPressed  = new Color(0.16f, 0.40f, 0.75f);
        public Color AccentColor     = new Color(1.00f, 0.76f, 0.03f); // #FFC208
        public Color BgColor         = new Color(0.96f, 0.98f, 1.0f);  // Light white-blue background
        public Color SurfaceColor    = new Color(0.90f, 0.93f, 0.97f); // Light blue-grey surface
        public Color PanelColor      = new Color(0.88f, 0.92f, 0.96f); // Soft light blue-grey panel
        public Color TextColor       = new Color(0.15f, 0.20f, 0.28f); // Clean dark grey/blue text
        public Color MutedText       = new Color(0.40f, 0.45f, 0.52f); // Slate-grey muted text
        public Color DangerColor     = new Color(0.78f, 0.20f, 0.20f);
        public Color SuccessColor    = new Color(0.27f, 0.74f, 0.40f);

        [Header("Layout Tokens")]
        public float PanelElevation = 4f;
        public float ButtonHeight = 56f;
        public float ButtonWidth = 240f;
        public int ButtonFontSize = 22;

        [Header("Button Color Presets")]
        public ButtonColorConfig PrimaryButtonColors = new()
        {
            NormalColor = new Color(0.22f, 0.51f, 0.91f),
            HighlightedColor = new Color(0.30f, 0.62f, 1.00f),
            PressedColor = new Color(0.16f, 0.40f, 0.75f),
            SelectedColor = new Color(0.30f, 0.62f, 1.00f),
            DisabledColor = new Color(0.30f, 0.30f, 0.35f)
        };

        public ButtonColorConfig OutlineButtonColors = new()
        {
            NormalColor = new Color(0.90f, 0.93f, 0.97f),
            HighlightedColor = new Color(0.80f, 0.84f, 0.90f),
            PressedColor = new Color(0.70f, 0.74f, 0.80f),
            SelectedColor = new Color(0.80f, 0.84f, 0.90f),
            DisabledColor = new Color(0.85f, 0.87f, 0.90f)
        };

        public ButtonColorConfig DangerButtonColors = new()
        {
            NormalColor = new Color(0.78f, 0.20f, 0.20f),
            HighlightedColor = new Color(0.88f, 0.32f, 0.32f),
            PressedColor = new Color(0.60f, 0.15f, 0.15f),
            SelectedColor = new Color(0.88f, 0.32f, 0.32f),
            DisabledColor = new Color(0.30f, 0.30f, 0.35f)
        };

        public ButtonColorConfig TextButtonColors = new()
        {
            NormalColor = Color.clear,
            HighlightedColor = new Color(0.15f, 0.20f, 0.28f, 0.08f),
            PressedColor = new Color(0.15f, 0.20f, 0.28f, 0.15f),
            SelectedColor = Color.clear,
            DisabledColor = Color.clear
        };

        public ButtonColorConfig IconButtonColors = new()
        {
            NormalColor = new Color(0.88f, 0.92f, 0.96f),
            HighlightedColor = new Color(0.80f, 0.84f, 0.90f),
            PressedColor = new Color(0.70f, 0.74f, 0.80f),
            SelectedColor = new Color(0.80f, 0.84f, 0.90f),
            DisabledColor = new Color(0.85f, 0.87f, 0.90f)
        };
    }
}
