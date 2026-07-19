using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using RingFlow.Gameplay;

namespace RingFlow.Gameplay.Services
{
    /// <summary>
    /// INexusService implementation of IGameUIResourcesService.
    /// Manages instance-based theme states and creation helpers.
    /// 
    /// Bound in GameplayLifecycle.OnConfigure() via:
    ///   builder.BindService&lt;IGameUIResourcesService, GameUIResourcesService&gt;();
    /// </summary>
    public class GameUIResourcesService : IGameUIResourcesService, INexusService
    {
        private readonly UIThemeConfigSO _theme;
        private bool _reduceMotion;

        // Cached assets
        private Font _font;
        private Sprite _roundedSprite;
        private Texture2D _roundedTexture;

        public GameUIResourcesService(UIThemeConfigSO theme)
        {
            _theme = theme ?? throw new System.ArgumentNullException(nameof(theme),
                "[GameUIResourcesService] UIThemeConfigSO is required.");
        }

        // INexusService lifecycle
        public ValueTask InitializeAsync(CancellationToken ct)
        {
            return default;
        }

        public void OnDispose()
        {
            if (_roundedTexture != null)
            {
                Object.Destroy(_roundedTexture);
                _roundedTexture = null;
            }
            if (_roundedSprite != null)
            {
                Object.Destroy(_roundedSprite);
                _roundedSprite = null;
            }
        }

        // ── Color tokens ────────────────────────────────────────────
        public Color PrimaryColor    => _theme.PrimaryColor;
        public Color PrimaryLight    => _theme.PrimaryLight;
        public Color PrimaryPressed  => _theme.PrimaryPressed;
        public Color AccentColor     => _theme.AccentColor;
        public Color AccentLight     => _theme.AccentLight;
        public Color BgColor         => _theme.BgColor;
        public Color BgDark          => _theme.BgDark;
        public Color SurfaceColor    => _theme.SurfaceColor;
        public Color SurfaceDark     => _theme.SurfaceDark;
        public Color PanelColor      => _theme.PanelColor;
        public Color PanelDark       => _theme.PanelDark;
        public Color TextColor       => _theme.TextColor;
        public Color TextOnPrimary   => _theme.TextOnPrimary;
        public Color TextOnDark      => _theme.TextOnDark;
        public Color MutedText       => _theme.MutedText;
        public Color MutedTextDark   => _theme.MutedTextDark;
        public Color DangerColor     => _theme.DangerColor;
        public Color DangerLight     => _theme.DangerLight;
        public Color SuccessColor    => _theme.SuccessColor;
        public Color SuccessLight    => _theme.SuccessLight;
        public Color WarningColor    => _theme.WarningColor;
        public Color InfoColor       => _theme.InfoColor;
        public Color OverlayLight    => _theme.OverlayLight;
        public Color OverlayMedium   => _theme.OverlayMedium;
        public Color OverlayHeavy    => _theme.OverlayHeavy;
        public Color StarEarned      => _theme.StarEarned;
        public Color StarEmpty       => _theme.StarEmpty;
        public Color CoinColor       => _theme.CoinColor;
        public Color DiamondColor    => _theme.DiamondColor;
        public Color XpColor         => _theme.XpColor;
        public Color DisabledText    => _theme.DisabledText;

        // ── Durations & Sizes ───────────────────────────────────────
        public float ScreenFadeDuration   => _theme.ScreenFadeDuration;
        public float PopupScaleDuration   => _theme.PopupScaleDuration;
        public float ButtonHoverScale     => _theme.ButtonHoverScale;
        public float ButtonPressScale     => _theme.ButtonPressScale;
        public float SlideDuration         => _theme.SlideDuration;
        public float StaggerDelay          => _theme.StaggerDelay;
        public float ButtonHeight          => _theme.ButtonHeight;
        public float ButtonWidth           => _theme.ButtonWidth;
        public int   ButtonFontSize         => _theme.ButtonFontSize;
        public float SmallButtonHeight     => _theme.SmallButtonHeight;
        public float IconButtonSize        => _theme.IconButtonSize;
        public float CornerRadius          => _theme.CornerRadius;
        public float ElementSpacing        => _theme.ElementSpacing;
        public float SectionSpacing        => _theme.SectionSpacing;
        public float CardPadding           => _theme.CardPadding;
        
        public float PanelElevation => _theme.PanelElevation;

        // ── Cache & Creators ────────────────────────────────────────────
        /// <summary>
        /// Provides access to the UISpriteLibrarySO assigned on the UIThemeConfigSO.
        /// Returns null if the theme does not have a SpriteLibrary configured.
        /// </summary>
        public UISpriteLibrarySO SpriteLibrary => _theme.SpriteLibrary;

        public Font GetFont()
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _font;
        }

        public Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null) return _roundedSprite;

            const int size = 16;
            _roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RingFlow_GeneratedUISprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            _roundedTexture.SetPixels32(pixels);
            _roundedTexture.Apply(false, true);

            _roundedSprite = Sprite.Create(
                _roundedTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(4f, 4f, 4f, 4f));
            _roundedSprite.name = "RingFlow_GeneratedUISprite";
            _roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return _roundedSprite;
        }

        public GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = BgColor;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            return go;
        }

        public GameObject CreateSafeAreaPanel(string name, Transform parent)
        {
            var go = CreatePanel(name, parent);
            go.AddComponent<UI.SafeAreaHandler>();
            return go;
        }

        public GameObject CreateCard(string name, Transform parent, Color? bgColor = null)
        {
            var go = CreatePanel(name, parent);
            var img = go.GetComponent<Image>();
            img.color = bgColor ?? SurfaceColor;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -4f);
            return go;
        }

        private void ApplyButtonColors(Button button, ButtonColorConfig colorCfg)
        {
            var colors = button.colors;
            colors.normalColor = colorCfg.NormalColor;
            colors.highlightedColor = colorCfg.HighlightedColor;
            colors.pressedColor = colorCfg.PressedColor;
            colors.selectedColor = colorCfg.SelectedColor;
            colors.disabledColor = colorCfg.DisabledColor;
            colors.fadeDuration = 0.15f;
            button.colors = colors;
        }

        public GameObject CreateButton(string label, Transform parent, float width, float height)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            var image = go.GetComponent<Image>();
            image.color = PrimaryColor;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;

            var button = go.GetComponent<Button>();
            ApplyButtonColors(button, _theme.PrimaryButtonColors);
            go.AddComponent<UI.UIButtonEffects>();

            // Shadow
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -3f);

            // Label
            var textGo = CreateChildText("Label", go.transform, _theme.ButtonFontSize, TextAnchor.MiddleCenter, TextOnPrimary);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 8f);
            textRect.offsetMax = new Vector2(-16f, -8f);
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.supportRichText = true;

            return go;
        }

        public GameObject CreateIconButton(string iconText, Transform parent, float size = 48f)
        {
            var go = new GameObject("IconBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.color = SurfaceColor;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;

            var button = go.GetComponent<Button>();
            ApplyButtonColors(button, _theme.IconButtonColors);
            go.AddComponent<UI.UIButtonEffects>();

            var textGo = CreateChildText("Icon", go.transform, Mathf.RoundToInt(size * 0.5f), TextAnchor.MiddleCenter, TextColor);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textGo.GetComponent<Text>().text = iconText;

            return go;
        }

        public GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color color)
        {
            var go = CreateChildText("Text", parent, fontSize, align, color);
            var text = go.GetComponent<Text>();
            text.text = content;
            return go;
        }

        private GameObject CreateChildText(string name, Transform parent, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            return go;
        }

        public GameObject CreateDisplayText(string content, Transform parent, int fontSize, Color color)
        {
            var go = CreateText(content, parent, fontSize, TextAnchor.MiddleCenter, color);
            var text = go.GetComponent<Text>();
            text.fontStyle = FontStyle.Bold;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);
            return go;
        }

        public GameObject CreateToggle(Transform parent, float anchorX1, float anchorY1, float anchorX2, float anchorY2, bool initialValue)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetAnchors(rect, anchorX1, anchorY1, anchorX2, anchorY2);

            var bg = go.GetComponent<Image>();
            bg.color = PanelColor;
            bg.sprite = GetRoundedSprite();
            bg.type = Image.Type.Sliced;

            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = initialValue;
            toggle.transition = Selectable.Transition.ColorTint;
            var colors = toggle.colors;
            colors.normalColor = new Color(0.60f, 0.62f, 0.68f);
            colors.highlightedColor = new Color(0.55f, 0.58f, 0.65f);
            colors.pressedColor = new Color(0.45f, 0.48f, 0.55f);
            colors.selectedColor = new Color(0.60f, 0.62f, 0.68f);
            colors.disabledColor = new Color(0.40f, 0.40f, 0.45f);
            colors.fadeDuration = 0.1f;
            toggle.colors = colors;

            // Checkmark
            var checkmarkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGo.transform.SetParent(go.transform, false);
            var cmRect = checkmarkGo.GetComponent<RectTransform>();
            cmRect.anchorMin = new Vector2(0.12f, 0.12f);
            cmRect.anchorMax = new Vector2(0.88f, 0.88f);
            cmRect.offsetMin = Vector2.zero;
            cmRect.offsetMax = Vector2.zero;
            var cmImage = checkmarkGo.GetComponent<Image>();
            cmImage.color = AccentColor;
            cmImage.raycastTarget = false;
            toggle.graphic = cmImage;
            toggle.targetGraphic = bg;

            return go;
        }

        public GameObject CreateOverlay(Transform parent, Color color)
        {
            var go = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return go;
        }

        private void SetButtonColors(Button button, ButtonColorConfig cfg)
        {
            var colors = button.colors;
            colors.normalColor = cfg.NormalColor;
            colors.highlightedColor = cfg.HighlightedColor;
            colors.pressedColor = cfg.PressedColor;
            colors.selectedColor = cfg.SelectedColor;
            colors.disabledColor = cfg.DisabledColor;
            colors.fadeDuration = 0.15f;
            button.colors = colors;
        }

        public void ApplyPrimaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = PrimaryColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.PrimaryButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public void ApplyAccentStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = AccentColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.AccentButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public void ApplySuccessStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = SuccessColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.SuccessButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public void ApplyOutlineStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = SurfaceColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.OutlineButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public void ApplySecondaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = PanelColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.OutlineButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public void ApplyDarkStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = SurfaceDark; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.DarkButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnDark;
        }

        public void ApplyDangerStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = DangerColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.DangerButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public void ApplyTextButtonStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) image.color = Color.clear;
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, _theme.TextButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public void ApplyIconStyle(Button button)
        {
            if (button != null) SetButtonColors(button, _theme.IconButtonColors);
        }

        public void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc)
        {
            if (loc == null || btn == null) return;
            var text = btn.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = loc.GetString(key, text.text);
                if (loc.IsRTL && text.alignment == TextAnchor.MiddleLeft)
                    text.alignment = TextAnchor.MiddleRight;
            }
        }

        public void LocalizeText(GameObject go, string key, ILocalizationService loc)
        {
            if (loc == null || go == null) return;
            var text = go.GetComponent<Text>();
            if (text != null)
            {
                text.text = loc.GetString(key, text.text);
                if (loc.IsRTL)
                {
                    if (text.alignment == TextAnchor.MiddleLeft) text.alignment = TextAnchor.MiddleRight;
                    else if (text.alignment == TextAnchor.UpperLeft) text.alignment = TextAnchor.UpperRight;
                    else if (text.alignment == TextAnchor.LowerLeft) text.alignment = TextAnchor.LowerRight;
                }
            }
        }

        public void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Accessibility & Animations ──────────────────────────────
        public void SetReducedMotion(bool reduceMotion)
        {
            _reduceMotion = reduceMotion;
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        public void AnimatePopupEntry(GameObject go, float duration = 0.3f)
        {
            var cg = GetOrAddCanvasGroup(go);
            DOTween.Kill(cg);
            DOTween.Kill(go.transform);

            if (_reduceMotion)
            {
                cg.alpha = 1f;
                go.transform.localScale = Vector3.one;
                go.SetActive(true);
                return;
            }

            cg.alpha = 0f;
            go.transform.localScale = Vector3.one * 0.8f;
            go.SetActive(true);
            DOTween.To(() => cg.alpha, v => cg.alpha = v, 1f, duration * 0.7f).SetEase(DG.Tweening.Ease.OutCubic).SetTarget(cg);
            go.transform.DOScale(1f, duration).SetEase(DG.Tweening.Ease.OutBack).SetAutoKill(true).SetTarget(go.transform);
        }

        public void AnimatePopupExit(GameObject go, float duration = 0.2f, System.Action onComplete = null)
        {
            var cg = GetOrAddCanvasGroup(go);
            DOTween.Kill(cg);
            DOTween.Kill(go.transform);

            if (_reduceMotion)
            {
                go.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            DOTween.To(() => cg.alpha, v => cg.alpha = v, 0f, duration).SetEase(DG.Tweening.Ease.InCubic).SetTarget(cg);
            go.transform.DOScale(0.85f, duration).SetEase(DG.Tweening.Ease.InBack).SetAutoKill(true)
                .OnComplete(() => { go.SetActive(false); onComplete?.Invoke(); }).SetTarget(go.transform);
        }

        public void AnimateScreenEntry(GameObject go, float duration = 0.35f)
        {
            var cg = GetOrAddCanvasGroup(go);
            DOTween.Kill(cg);

            if (_reduceMotion)
            {
                cg.alpha = 1f;
                go.SetActive(true);
                return;
            }

            cg.alpha = 0f;
            go.SetActive(true);
            DOTween.To(() => cg.alpha, v => cg.alpha = v, 1f, duration).SetEase(DG.Tweening.Ease.OutCubic).SetTarget(cg);
        }

        public void AnimateScreenExit(GameObject go, float duration = 0.25f, System.Action onComplete = null)
        {
            var cg = GetOrAddCanvasGroup(go);
            DOTween.Kill(cg);

            if (_reduceMotion)
            {
                go.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            DOTween.To(() => cg.alpha, v => cg.alpha = v, 0f, duration).SetEase(DG.Tweening.Ease.InCubic)
                .OnComplete(() => { go.SetActive(false); onComplete?.Invoke(); }).SetTarget(cg);
        }

        public void AnimateRewardPunch(GameObject go, float scale = 1.3f, float duration = 0.4f)
        {
            DOTween.Kill(go.transform);
            go.transform.localScale = Vector3.one;
            go.transform.DOPunchScale(Vector3.one * (scale - 1f), duration, 5, 0.5f).SetAutoKill(true);
        }

        public void AnimateStaggeredEntry(GameObject[] elements, float delay = 0.07f, float duration = 0.3f)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == null) continue;
                var cg = GetOrAddCanvasGroup(elements[i]);
                DOTween.Kill(cg);
                DOTween.Kill(elements[i].transform);
                cg.alpha = 0f;
                elements[i].transform.localScale = Vector3.one * 0.7f;
                int index = i;
                DOTween.To(() => cg.alpha, v => cg.alpha = v, 1f, duration)
                    .SetDelay(index * delay).SetEase(DG.Tweening.Ease.OutCubic).SetTarget(cg);
                elements[i].transform.DOScale(1f, duration)
                    .SetDelay(index * delay).SetEase(DG.Tweening.Ease.OutBack).SetAutoKill(true).SetTarget(elements[i].transform);
            }
        }

        public void AddButtonEffects(GameObject go)
        {
            if (go != null && go.GetComponent<Button>() != null && go.GetComponent<UI.UIButtonEffects>() == null)
            {
                go.AddComponent<UI.UIButtonEffects>();
            }
        }

        public void AddButtonEffects(Button button)
        {
            if (button != null && button.GetComponent<UI.UIButtonEffects>() == null)
            {
                button.gameObject.AddComponent<UI.UIButtonEffects>();
            }
        }

        /// <summary>
        /// Returns a sprite by name. Resolution order:
        /// 1. UISpriteLibrarySO.GetSprite(name)  — Inspector-assigned, swappable at runtime
        /// 2. Resources.Load&lt;Sprite&gt;("UI/Sprites/{name}")  — file-system fallback
        /// Returns null if neither source has the sprite.
        /// </summary>
        public Sprite GetSprite(string name)
        {
            // 1. Try the SO sprite library first (swappable via Inspector)
            if (_theme.SpriteLibrary != null)
            {
                var fromLibrary = _theme.SpriteLibrary.GetSprite(name);
                if (fromLibrary != null)
                    return fromLibrary;
            }

            // 2. Fallback: load directly from Resources/UI/Sprites/{name}
            return Resources.Load<Sprite>($"UI/Sprites/{name}");
        }
    }
}
