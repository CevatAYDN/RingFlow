using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Singleton accessor for the active palette. Use <see cref="Bind"/> from a bootstrap MonoBehaviour
    /// (or via a ScriptableObject References asset) to swap palettes at runtime.
    /// Provides a sensible default fallback when no asset is bound.
    /// </summary>
    public static class RingPalette
    {
        private static RingColorPaletteSO _palette;
        private static RingColorPaletteSO.ColorBlindMode _colorBlindMode = RingColorPaletteSO.ColorBlindMode.Off;

        public static event System.Action<RingColorPaletteSO.ColorBlindMode> OnColorBlindModeChanged;

        public static void Bind(RingColorPaletteSO palette)
        {
            _palette = palette;
        }

        public static void SetColorBlindMode(RingColorPaletteSO.ColorBlindMode mode)
        {
            if (_colorBlindMode == mode) return;
            _colorBlindMode = mode;
            OnColorBlindModeChanged?.Invoke(mode);
        }

        public static RingColorPaletteSO.ColorBlindMode ColorBlindMode => _colorBlindMode;

        public static Color Get(RingColor color)
        {
            if (_palette != null) return _palette.GetColor(color, _colorBlindMode);
            return GetDefault(color);
        }

        private static Color GetDefault(RingColor color)
        {
            // WCAG-friendly defaults aligned with DeltaE-enforced contrast — fall back when no SO is bound.
            switch (color)
            {
                case RingColor.Red:     return new Color(0.92f, 0.27f, 0.27f);
                case RingColor.Blue:    return new Color(0.20f, 0.55f, 0.93f);
                case RingColor.Green:   return new Color(0.27f, 0.74f, 0.40f);
                case RingColor.Yellow:  return new Color(0.97f, 0.85f, 0.20f);
                case RingColor.Orange:  return new Color(0.97f, 0.58f, 0.18f);
                case RingColor.Purple:  return new Color(0.58f, 0.36f, 0.82f);
                case RingColor.Cyan:    return new Color(0.20f, 0.86f, 0.90f);
                case RingColor.Magenta: return new Color(0.90f, 0.32f, 0.72f);
                default:                return new Color(0.20f, 0.20f, 0.20f);
            }
        }
    }
}
