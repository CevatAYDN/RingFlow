using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

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
        Color PrimaryPressed { get; }
        Color AccentColor { get; }
        Color BgColor { get; }
        Color SurfaceColor { get; }
        Color PanelColor { get; }
        Color TextColor { get; }
        Color MutedText { get; }
        Color DangerColor { get; }
        Color SuccessColor { get; }

        float PanelElevation { get; }
        float ButtonHeight { get; }
        float ButtonWidth { get; }

        Font GetFont();
        Sprite GetRoundedSprite();

        GameObject CreatePanel(string name, Transform parent);
        GameObject CreateSafeAreaPanel(string name, Transform parent);
        GameObject CreateButton(string label, Transform parent, float width, float height);
        GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color color);

        void ApplyOutlineStyle(GameObject btn);
        void ApplyIconStyle(Button button);
        void ApplyPrimaryStyle(GameObject btn);
        void ApplyDangerStyle(GameObject btn);
        void ApplySecondaryStyle(GameObject btn);
        void ApplyTextButtonStyle(GameObject btn);

        void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc);
        void LocalizeText(GameObject go, string key, ILocalizationService loc);
        void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax);
    }
}
