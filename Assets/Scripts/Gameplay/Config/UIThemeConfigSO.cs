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

    [System.Serializable]
    public struct TypographyConfig
    {
        public Font Font;
        public int TitleFontSize;
        public int HeaderFontSize;
        public int BodyFontSize;
        public int SmallFontSize;
        public int DisplayFontSize;
        public FontStyle FontStyle;
    }

    [System.Serializable]
    public struct AnimationConfig
    {
        public float ScreenFadeDuration;
        public float PopupScaleDuration;
        public float ButtonHoverScale;
        public float SlideDuration;
        public float StaggerDelay;
        public Ease ScreenEase;
        public Ease PopupEase;
        public Ease ButtonEase;
    }

    [System.Serializable]
    public struct SpacingConfig
    {
        public float ScreenPadding;
        public float CardPadding;
        public float ElementSpacing;
        public float SectionSpacing;
        public float ButtonSpacing;
    }

    public enum Ease
    {
        Linear = 0,
        OutQuad = 1,
        InOutQuad = 2,
        OutCubic = 3,
        InOutCubic = 4,
        OutQuart = 5,
        OutBack = 6,
        OutElastic = 7,
        OutBounce = 8,
        InBack = 9,
    }

    [CreateAssetMenu(fileName = "UIThemeConfig", menuName = "RingFlow/UI Theme Config", order = 52)]
    public class UIThemeConfigSO : ScriptableObject
    {
        [Header("🎨 Colors — Main Palette")]
        public Color PrimaryColor = new Color(0.22f, 0.51f, 0.91f); // #387AE8
        public Color PrimaryLight = new Color(0.45f, 0.70f, 1.00f);
        public Color PrimaryPressed = new Color(0.16f, 0.40f, 0.75f);
        public Color AccentColor = new Color(1.00f, 0.76f, 0.03f); // #FFC208
        public Color AccentLight = new Color(1.00f, 0.88f, 0.35f);

        [Header("🎨 Colors — Background & Surface")]
        public Color BgColor = new Color(0.96f, 0.98f, 1.0f);
        public Color BgDark = new Color(0.06f, 0.08f, 0.12f);
        public Color SurfaceColor = new Color(0.90f, 0.93f, 0.97f);
        public Color SurfaceDark = new Color(0.14f, 0.16f, 0.22f);
        public Color PanelColor = new Color(0.88f, 0.92f, 0.96f);
        public Color PanelDark = new Color(0.10f, 0.11f, 0.15f);

        [Header("🎨 Colors — Text")]
        public Color TextColor = new Color(0.15f, 0.20f, 0.28f);
        public Color TextOnPrimary = Color.white;
        public Color TextOnDark = new Color(0.92f, 0.94f, 0.98f);
        public Color MutedText = new Color(0.40f, 0.45f, 0.52f);
        public Color MutedTextDark = new Color(0.55f, 0.58f, 0.65f);
        public Color DisabledText = new Color(0.60f, 0.62f, 0.68f);

        [Header("🎨 Colors — Feedback")]
        public Color DangerColor = new Color(0.78f, 0.20f, 0.20f);
        public Color DangerLight = new Color(0.95f, 0.40f, 0.40f);
        public Color SuccessColor = new Color(0.27f, 0.74f, 0.40f);
        public Color SuccessLight = new Color(0.50f, 0.90f, 0.60f);
        public Color WarningColor = new Color(1.00f, 0.76f, 0.03f);
        public Color InfoColor = new Color(0.30f, 0.65f, 0.95f);

        [Header("🎨 Colors — Overlay")]
        public Color OverlayLight = new Color(0f, 0f, 0f, 0.45f);
        public Color OverlayMedium = new Color(0f, 0f, 0f, 0.65f);
        public Color OverlayHeavy = new Color(0f, 0f, 0f, 0.80f);

        [Header("🎨 Colors — Special")]
        public Color StarEarned = new Color(1.00f, 0.84f, 0.00f);
        public Color StarEmpty = new Color(0.32f, 0.34f, 0.38f);
        public Color CoinColor = new Color(1.00f, 0.84f, 0.00f);
        public Color DiamondColor = new Color(0.50f, 0.80f, 1.00f);
        public Color XpColor = new Color(0.60f, 0.30f, 1.00f);

        [Header("📐 Layout")]
        public float PanelElevation = 4f;
        public float ButtonHeight = 56f;
        public float ButtonWidth = 240f;
        public float SmallButtonHeight = 40f;
        public float SmallButtonWidth = 120f;
        public float IconButtonSize = 48f;
        public float CornerRadius = 12f;

        [Header("🔤 Typography")]
        public int TitleFontSize = 50;
        public int HeaderFontSize = 34;
        public int BodyFontSize = 22;
        public int SmallFontSize = 16;
        public int DisplayFontSize = 68;
        public int ButtonFontSize = 22;
        public int ToggleFontSize = 18;

        [Header("💫 Animations")]
        public float ScreenFadeDuration = 0.35f;
        public float PopupScaleDuration = 0.30f;
        public float PopupScaleEnter = 0.85f;
        public float ButtonHoverScale = 1.04f;
        public float ButtonPressScale = 0.96f;
        public float SlideDuration = 0.35f;
        public float StaggerDelay = 0.07f;
        public float RewardPunchScale = 1.3f;
        public float RewardPunchDuration = 0.4f;
        public float StarPopDuration = 0.35f;
        public float StarPopDelay = 0.20f;

        [Header("📏 Spacing")]
        public float ScreenPadding = 24f;
        public float CardPadding = 20f;
        public float ElementSpacing = 12f;
        public float SectionSpacing = 24f;
        public float ButtonSpacing = 16f;

        [Header("🔘 Button Color Presets")]
        public ButtonColorConfig PrimaryButtonColors = new()
        {
            NormalColor = new Color(0.22f, 0.51f, 0.91f),
            HighlightedColor = new Color(0.30f, 0.62f, 1.00f),
            PressedColor = new Color(0.16f, 0.40f, 0.75f),
            SelectedColor = new Color(0.30f, 0.62f, 1.00f),
            DisabledColor = new Color(0.30f, 0.30f, 0.35f)
        };

        public ButtonColorConfig AccentButtonColors = new()
        {
            NormalColor = new Color(1.00f, 0.76f, 0.03f),
            HighlightedColor = new Color(1.00f, 0.84f, 0.25f),
            PressedColor = new Color(0.85f, 0.62f, 0.00f),
            SelectedColor = new Color(1.00f, 0.84f, 0.25f),
            DisabledColor = new Color(0.30f, 0.30f, 0.35f)
        };

        public ButtonColorConfig SuccessButtonColors = new()
        {
            NormalColor = new Color(0.27f, 0.74f, 0.40f),
            HighlightedColor = new Color(0.35f, 0.85f, 0.50f),
            PressedColor = new Color(0.20f, 0.60f, 0.30f),
            SelectedColor = new Color(0.35f, 0.85f, 0.50f),
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

        public ButtonColorConfig DarkButtonColors = new()
        {
            NormalColor = new Color(0.14f, 0.16f, 0.22f),
            HighlightedColor = new Color(0.20f, 0.22f, 0.30f),
            PressedColor = new Color(0.10f, 0.12f, 0.18f),
            SelectedColor = new Color(0.20f, 0.22f, 0.30f),
            DisabledColor = new Color(0.08f, 0.08f, 0.10f)
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

        // ── Derived helpers ──

        /// <summary>Resolves Ease enum to DOTween Ease value (if DOTween available).</summary>
        public DG.Tweening.Ease GetDOTweenEase(Ease ease) => ease switch
        {
            Ease.OutQuad => DG.Tweening.Ease.OutQuad,
            Ease.InOutQuad => DG.Tweening.Ease.InOutQuad,
            Ease.OutCubic => DG.Tweening.Ease.OutCubic,
            Ease.InOutCubic => DG.Tweening.Ease.InOutCubic,
            Ease.OutQuart => DG.Tweening.Ease.OutQuart,
            Ease.OutBack => DG.Tweening.Ease.OutBack,
            Ease.OutElastic => DG.Tweening.Ease.OutElastic,
            Ease.OutBounce => DG.Tweening.Ease.OutBounce,
            Ease.InBack => DG.Tweening.Ease.InBack,
            _ => DG.Tweening.Ease.Linear,
        };

        /// <summary>Light theme: BgColor is light, SurfaceColor is light.</summary>
        public bool IsDarkTheme => BgColor.maxColorComponent < 0.5f;

        /// <summary>Text color suitable for the current surface.</summary>
        public Color GetTextOnColor(Color surface) =>
            surface.grayscale > 0.5f ? TextColor : TextOnDark;

        // ── Sprite Library ──────────────────────────────────────────────────

        [Header("🖼 Sprite Library")]
        [Tooltip("Assign a UISpriteLibrarySO asset here to provide named sprites for UI elements. " +
                 "When null, GameUIResources.GetSprite falls back to Resources.Load(\"UI/Sprites/{name}\").")]
        public UISpriteLibrarySO SpriteLibrary;
    }
}
