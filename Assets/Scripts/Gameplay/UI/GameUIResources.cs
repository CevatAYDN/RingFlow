using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Premium UI creation system — thin static proxy wrapper that delegates
    /// to the DI-injected IGameUIResourcesService.
    /// This allows existing View and Mediator code to run without compile-time breakage
    /// while decoupling unit tests and runtime from static mutable state.
    /// </summary>
    public static class GameUIResources
    {
        private static IGameUIResourcesService Service
        {
            get
            {
                var context = Nexus.Core.NexusRuntime.CurrentContext;
                if (context != null)
                {
                    var service = context.TryResolve<IGameUIResourcesService>();
                    if (service != null)
                        return service;
                }

                throw new System.InvalidOperationException(
                    "[GameUIResources] IGameUIResourcesService not available. " +
                    "Ensure it is bound in GameplayLifecycle.OnConfigure() via " +
                    "builder.BindService&lt;IGameUIResourcesService, GameUIResourcesService&gt;().");
            }
        }

        // Backward compatibility binder (no-op kept for compilation)
        public static void Bind(UIThemeConfigSO theme) { }

        public static void SetReducedMotion(bool reduceMotion) => Service.SetReducedMotion(reduceMotion);

        // ── Color tokens ──────────────────────────────────────────
        public static Color PrimaryColor    => Service.PrimaryColor;
        public static Color PrimaryLight    => Service.PrimaryLight;
        public static Color PrimaryPressed  => Service.PrimaryPressed;
        public static Color AccentColor     => Service.AccentColor;
        public static Color AccentLight     => Service.AccentLight;
        public static Color BgColor         => Service.BgColor;
        public static Color BgDark          => Service.BgDark;
        public static Color SurfaceColor    => Service.SurfaceColor;
        public static Color SurfaceDark     => Service.SurfaceDark;
        public static Color PanelColor      => Service.PanelColor;
        public static Color PanelDark       => Service.PanelDark;
        public static Color TextColor       => Service.TextColor;
        public static Color TextOnPrimary   => Service.TextOnPrimary;
        public static Color TextOnDark      => Service.TextOnDark;
        public static Color MutedText       => Service.MutedText;
        public static Color MutedTextDark   => Service.MutedTextDark;
        public static Color DangerColor     => Service.DangerColor;
        public static Color DangerLight     => Service.DangerLight;
        public static Color SuccessColor    => Service.SuccessColor;
        public static Color SuccessLight    => Service.SuccessLight;
        public static Color WarningColor    => Service.WarningColor;
        public static Color InfoColor       => Service.InfoColor;
        public static Color OverlayLight    => Service.OverlayLight;
        public static Color OverlayMedium   => Service.OverlayMedium;
        public static Color OverlayHeavy    => Service.OverlayHeavy;
        public static Color StarEarned      => Service.StarEarned;
        public static Color StarEmpty       => Service.StarEmpty;
        public static Color CoinColor       => Service.CoinColor;
        public static Color DiamondColor    => Service.DiamondColor;
        public static Color XpColor         => Service.XpColor;
        public static Color DisabledText    => Service.DisabledText;

        public static float ScreenFadeDuration   => Service.ScreenFadeDuration;
        public static float PopupScaleDuration   => Service.PopupScaleDuration;
        public static float ButtonHoverScale     => Service.ButtonHoverScale;
        public static float ButtonPressScale     => Service.ButtonPressScale;
        public static float SlideDuration         => Service.SlideDuration;
        public static float StaggerDelay          => Service.StaggerDelay;
        public static float ButtonHeight          => Service.ButtonHeight;
        public static float ButtonWidth           => Service.ButtonWidth;
        public static int   ButtonFontSize         => Service.ButtonFontSize;
        public static float SmallButtonHeight     => Service.SmallButtonHeight;
        public static float IconButtonSize        => Service.IconButtonSize;
        public static float CornerRadius          => Service.CornerRadius;
        public static float ElementSpacing        => Service.ElementSpacing;
        public static float SectionSpacing        => Service.SectionSpacing;
        public static float CardPadding           => Service.CardPadding;

        public static Font GetFont() => Service.GetFont();
        public static Sprite GetRoundedSprite() => Service.GetRoundedSprite();

        /// <summary>
        /// Direct access to the UISpriteLibrarySO. Useful when you need to assign
        /// sprites directly to Image components in View code without string lookup.
        /// Example: myImage.sprite = GameUIResources.SpriteLibrary?.IconCoin;
        /// </summary>
        public static UISpriteLibrarySO SpriteLibrary => Service.SpriteLibrary;

        // ── Panel creators ────────────────────────────────────────────────
        public static GameObject CreatePanel(string name, Transform parent) => Service.CreatePanel(name, parent);
        public static GameObject CreateSafeAreaPanel(string name, Transform parent) => Service.CreateSafeAreaPanel(name, parent);
        public static GameObject CreateCard(string name, Transform parent, Color? bgColor = null) => Service.CreateCard(name, parent, bgColor);

        // ── Button creators ───────────────────────────────────────────────
        public static GameObject CreateButton(string label, Transform parent, float width, float height) => Service.CreateButton(label, parent, width, height);
        public static GameObject CreateIconButton(string iconText, Transform parent, float size = 48f) => Service.CreateIconButton(iconText, parent, size);

        // ── Text creators ─────────────────────────────────────────────────
        public static GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color color) => Service.CreateText(content, parent, fontSize, align, color);
        public static GameObject CreateDisplayText(string content, Transform parent, int fontSize, Color color) => Service.CreateDisplayText(content, parent, fontSize, color);

        // ── Toggle creator ────────────────────────────────────────────────
        public static GameObject CreateToggle(Transform parent, float anchorX1, float anchorY1, float anchorX2, float anchorY2, bool initialValue) => Service.CreateToggle(parent, anchorX1, anchorY1, anchorX2, anchorY2, initialValue);

        // ── Overlay creator ───────────────────────────────────────────────
        public static GameObject CreateOverlay(Transform parent, Color color) => Service.CreateOverlay(parent, color);

        // ── Style appliers ────────────────────────────────────────────────
        public static void ApplyPrimaryStyle(GameObject btn) => Service.ApplyPrimaryStyle(btn);
        public static void ApplyAccentStyle(GameObject btn) => Service.ApplyAccentStyle(btn);
        public static void ApplySuccessStyle(GameObject btn) => Service.ApplySuccessStyle(btn);
        public static void ApplyOutlineStyle(GameObject btn) => Service.ApplyOutlineStyle(btn);
        public static void ApplySecondaryStyle(GameObject btn) => Service.ApplySecondaryStyle(btn);
        public static void ApplyDarkStyle(GameObject btn) => Service.ApplyDarkStyle(btn);
        public static void ApplyDangerStyle(GameObject btn) => Service.ApplyDangerStyle(btn);
        public static void ApplyTextButtonStyle(GameObject btn) => Service.ApplyTextButtonStyle(btn);
        public static void ApplyIconStyle(Button button) => Service.ApplyIconStyle(button);

        // ── Localization helpers ─────────────────────────────────────────
        public static void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc) => Service.LocalizeButtonText(btn, key, loc);
        public static void LocalizeText(GameObject go, string key, ILocalizationService loc) => Service.LocalizeText(go, key, loc);

        // ── Anchor helper ────────────────────────────────────────────────
        public static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax) => Service.SetAnchors(rt, xMin, yMin, xMax, yMax);

        // ── Animation helpers ────────────────────────────────────────────
        public static void AnimatePopupEntry(GameObject go, float duration = 0.3f) => Service.AnimatePopupEntry(go, duration);
        public static void AnimatePopupExit(GameObject go, float duration = 0.2f, System.Action onComplete = null) => Service.AnimatePopupExit(go, duration, onComplete);
        public static void AnimateScreenEntry(GameObject go, float duration = 0.35f) => Service.AnimateScreenEntry(go, duration);
        public static void AnimateScreenExit(GameObject go, float duration = 0.25f, System.Action onComplete = null) => Service.AnimateScreenExit(go, duration, onComplete);
        public static void AnimateRewardPunch(GameObject go, float scale = 1.3f, float duration = 0.4f) => Service.AnimateRewardPunch(go, scale, duration);
        public static void AnimateStaggeredEntry(GameObject[] elements, float delay = 0.07f, float duration = 0.3f) => Service.AnimateStaggeredEntry(elements, delay, duration);
        public static void AddButtonEffects(GameObject go) => Service.AddButtonEffects(go);
        public static void AddButtonEffects(Button button) => Service.AddButtonEffects(button);
        public static Sprite GetSprite(string name) => Service.GetSprite(name);
    }
}
