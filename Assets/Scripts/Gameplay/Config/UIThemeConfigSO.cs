using UnityEngine;

namespace RingFlow.Gameplay
{
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
    }
}
