using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.Services
{
    /// <summary>
    /// INexusService implementation of IGameUIResourcesService.
    /// Delegates all rendering to the existing GameUIResources static class during the
    /// migration period. Once all call-sites are migrated to IGameUIResourcesService,
    /// the static class can be deleted.
    /// 
    /// Bound in GameplayLifecycle.OnConfigure() via:
    ///   builder.BindService&lt;IGameUIResourcesService, GameUIResourcesService&gt;();
    /// </summary>
    public class GameUIResourcesService : IGameUIResourcesService, INexusService
    {
        private readonly UIThemeConfigSO _theme;

        public GameUIResourcesService(UIThemeConfigSO theme)
        {
            _theme = theme ?? throw new System.ArgumentNullException(nameof(theme),
                "[GameUIResourcesService] UIThemeConfigSO is required.");
        }

        // INexusService lifecycle
        public ValueTask InitializeAsync(CancellationToken ct)
        {
            // Ensure the static wrapper is also initialized for backward compatibility
            // with existing call-sites not yet migrated to the service interface.
            GameUIResources.Bind(_theme);
            return default;
        }

        public void OnDispose() { }

        // ── Color tokens ─────────────────────────────────────────────────────
        public Color PrimaryColor   => _theme.PrimaryColor;
        public Color PrimaryPressed => _theme.PrimaryPressed;
        public Color AccentColor    => _theme.AccentColor;
        public Color BgColor        => _theme.BgColor;
        public Color SurfaceColor   => _theme.SurfaceColor;
        public Color PanelColor     => _theme.PanelColor;
        public Color TextColor      => _theme.TextColor;
        public Color MutedText      => _theme.MutedText;
        public Color DangerColor    => _theme.DangerColor;
        public Color SuccessColor   => _theme.SuccessColor;

        public float PanelElevation => _theme.PanelElevation;
        public float ButtonHeight   => _theme.ButtonHeight;
        public float ButtonWidth    => _theme.ButtonWidth;

        public Font   GetFont()          => GameUIResources.GetFont();
        public Sprite GetRoundedSprite() => GameUIResources.GetRoundedSprite();

        public GameObject CreatePanel(string name, Transform parent)                                             => GameUIResources.CreatePanel(name, parent);
        public GameObject CreateSafeAreaPanel(string name, Transform parent)                                     => GameUIResources.CreateSafeAreaPanel(name, parent);
        public GameObject CreateButton(string label, Transform parent, float width, float height)                => GameUIResources.CreateButton(label, parent, width, height);
        public GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color col)=> GameUIResources.CreateText(content, parent, fontSize, align, col);

        public void ApplyOutlineStyle(GameObject btn)    => GameUIResources.ApplyOutlineStyle(btn);
        public void ApplyIconStyle(Button button)        => GameUIResources.ApplyIconStyle(button);
        public void ApplyPrimaryStyle(GameObject btn)    => GameUIResources.ApplyPrimaryStyle(btn);
        public void ApplyDangerStyle(GameObject btn)     => GameUIResources.ApplyDangerStyle(btn);
        public void ApplySecondaryStyle(GameObject btn)  => GameUIResources.ApplySecondaryStyle(btn);
        public void ApplyTextButtonStyle(GameObject btn) => GameUIResources.ApplyTextButtonStyle(btn);

        public void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc) => GameUIResources.LocalizeButtonText(btn, key, loc);
        public void LocalizeText(GameObject go, string key, ILocalizationService loc)         => GameUIResources.LocalizeText(go, key, loc);
        public void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax) => GameUIResources.SetAnchors(rt, xMin, yMin, xMax, yMax);
    }
}
