using UnityEngine;
using UnityEngine.UI;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Gameplay.Services
{
    /// <summary>
    /// GDD §UI — DI-injectable contract for shared UI creation helpers.
    /// Replaces the static GameUIResources class to comply with AGENTS.md:
    /// "Never use static mutable state" and "All dependencies must be injected."
    /// GameUIResources remains as a backward-compatible thin wrapper that delegates
    /// to the bound service; direct callers should migrate to IGameUIResourcesService.
    /// </summary>
    public interface IGameUIResourcesService
    {
        // ── Color tokens ────────────────────────────────────────────
        Color PrimaryColor { get; }
        Color PrimaryLight { get; }
        Color PrimaryPressed { get; }
        Color AccentColor { get; }
        Color AccentLight { get; }
        Color BgColor { get; }
        Color BgDark { get; }
        Color SurfaceColor { get; }
        Color SurfaceDark { get; }
        Color PanelColor { get; }
        Color PanelDark { get; }
        Color TextColor { get; }
        Color TextOnPrimary { get; }
        Color TextOnDark { get; }
        Color MutedText { get; }
        Color MutedTextDark { get; }
        Color DangerColor { get; }
        Color DangerLight { get; }
        Color SuccessColor { get; }
        Color SuccessLight { get; }
        Color WarningColor { get; }
        Color InfoColor { get; }
        Color OverlayLight { get; }
        Color OverlayMedium { get; }
        Color OverlayHeavy { get; }
        Color StarEarned { get; }
        Color StarEmpty { get; }
        Color CoinColor { get; }
        Color DiamondColor { get; }
        Color XpColor { get; }
        Color DisabledText { get; }

        // ── Durations & Sizes ───────────────────────────────────────
        float ScreenFadeDuration { get; }
        float PopupScaleDuration { get; }
        float ButtonHoverScale { get; }
        float ButtonPressScale { get; }
        float SlideDuration { get; }
        float StaggerDelay { get; }
        float ButtonHeight { get; }
        float ButtonWidth { get; }
        int ButtonFontSize { get; }
        float SmallButtonHeight { get; }
        float IconButtonSize { get; }
        float CornerRadius { get; }
        float ElementSpacing { get; }
        float SectionSpacing { get; }
        float CardPadding { get; }

        // ── Cache & Creators ────────────────────────────────────────
        /// <summary>Access to the bound sprite library (may be null if SpriteLibrary not assigned on UIThemeConfigSO).</summary>
        UISpriteLibrarySO SpriteLibrary { get; }
        Font GetFont();
        Sprite GetRoundedSprite();
        GameObject CreatePanel(string name, Transform parent);
        GameObject CreateSafeAreaPanel(string name, Transform parent);
        GameObject CreateCard(string name, Transform parent, Color? bgColor = null);
        GameObject CreateButton(string label, Transform parent, float width, float height);
        GameObject CreateIconButton(string iconText, Transform parent, float size = 48f);
        GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color color);
        GameObject CreateDisplayText(string content, Transform parent, int fontSize, Color color);
        GameObject CreateToggle(Transform parent, float anchorX1, float anchorY1, float anchorX2, float anchorY2, bool initialValue);
        GameObject CreateOverlay(Transform parent, Color color);

        // ── Style Appliers ──────────────────────────────────────────
        void ApplyPrimaryStyle(GameObject btn);
        void ApplyAccentStyle(GameObject btn);
        void ApplySuccessStyle(GameObject btn);
        void ApplyOutlineStyle(GameObject btn);
        void ApplySecondaryStyle(GameObject btn);
        void ApplyDarkStyle(GameObject btn);
        void ApplyDangerStyle(GameObject btn);
        void ApplyTextButtonStyle(GameObject btn);
        void ApplyIconStyle(Button button);

        // ── Localization ─────────────────────────────────────────────
        void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc);
        void LocalizeText(GameObject go, string key, ILocalizationService loc);
        void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax);

        // ── Accessibility & Animations ──────────────────────────────
        void SetReducedMotion(bool reduceMotion);
        void AnimatePopupEntry(GameObject go, float duration = 0.3f);
        void AnimatePopupExit(GameObject go, float duration = 0.2f, System.Action onComplete = null);
        void AnimateScreenEntry(GameObject go, float duration = 0.35f);
        void AnimateScreenExit(GameObject go, float duration = 0.25f, System.Action onComplete = null);
        void AnimateRewardPunch(GameObject go, float scale = 1.3f, float duration = 0.4f);
        void AnimateStaggeredEntry(GameObject[] elements, float delay = 0.07f, float duration = 0.3f);
        void AddButtonEffects(GameObject go);
        void AddButtonEffects(Button button);
        Sprite GetSprite(string name);
    }
}
