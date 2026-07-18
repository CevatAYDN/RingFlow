using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Premium UI creation system — shared styling, animations, accessibility, and layout
    /// helpers used by all screen Views. Every visual constant flows from UIThemeConfigSO.
    /// </summary>
    public static class GameUIResources
    {
        private static UIThemeConfigSO _theme;
        private static UIThemeConfigSO Theme
        {
            get
            {
                if (_theme == null)
                    throw new System.InvalidOperationException("[GameUIResources] UIThemeConfigSO is not bound! Call GameUIResources.Bind() first.");
                return _theme;
            }
        }

        private static bool _reduceMotion;

        public static void Bind(UIThemeConfigSO theme) => _theme = theme;

        public static void SetReducedMotion(bool reduceMotion)
        {
            _reduceMotion = reduceMotion;
        }

        // ── Color tokens ──────────────────────────────────────────────────
        public static Color PrimaryColor    => Theme.PrimaryColor;
        public static Color PrimaryLight    => Theme.PrimaryLight;
        public static Color PrimaryPressed  => Theme.PrimaryPressed;
        public static Color AccentColor     => Theme.AccentColor;
        public static Color AccentLight     => Theme.AccentLight;
        public static Color BgColor         => Theme.BgColor;
        public static Color BgDark          => Theme.BgDark;
        public static Color SurfaceColor    => Theme.SurfaceColor;
        public static Color SurfaceDark     => Theme.SurfaceDark;
        public static Color PanelColor      => Theme.PanelColor;
        public static Color PanelDark       => Theme.PanelDark;
        public static Color TextColor       => Theme.TextColor;
        public static Color TextOnPrimary   => Theme.TextOnPrimary;
        public static Color TextOnDark      => Theme.TextOnDark;
        public static Color MutedText       => Theme.MutedText;
        public static Color MutedTextDark   => Theme.MutedTextDark;
        public static Color DangerColor     => Theme.DangerColor;
        public static Color DangerLight     => Theme.DangerLight;
        public static Color SuccessColor    => Theme.SuccessColor;
        public static Color SuccessLight    => Theme.SuccessLight;
        public static Color WarningColor    => Theme.WarningColor;
        public static Color InfoColor       => Theme.InfoColor;
        public static Color OverlayLight    => Theme.OverlayLight;
        public static Color OverlayMedium   => Theme.OverlayMedium;
        public static Color OverlayHeavy    => Theme.OverlayHeavy;
        public static Color StarEarned      => Theme.StarEarned;
        public static Color StarEmpty       => Theme.StarEmpty;
        public static Color CoinColor       => Theme.CoinColor;
        public static Color DiamondColor    => Theme.DiamondColor;
        public static Color XpColor         => Theme.XpColor;
        public static Color DisabledText    => Theme.DisabledText;

        public static float ScreenFadeDuration   => Theme.ScreenFadeDuration;
        public static float PopupScaleDuration   => Theme.PopupScaleDuration;
        public static float ButtonHoverScale     => Theme.ButtonHoverScale;
        public static float ButtonPressScale     => Theme.ButtonPressScale;
        public static float SlideDuration         => Theme.SlideDuration;
        public static float StaggerDelay          => Theme.StaggerDelay;
        public static float ButtonHeight          => Theme.ButtonHeight;
        public static float ButtonWidth           => Theme.ButtonWidth;
        public static int   ButtonFontSize         => Theme.ButtonFontSize;
        public static float SmallButtonHeight     => Theme.SmallButtonHeight;
        public static float IconButtonSize        => Theme.IconButtonSize;
        public static float CornerRadius          => Theme.CornerRadius;
        public static float ElementSpacing        => Theme.ElementSpacing;
        public static float SectionSpacing        => Theme.SectionSpacing;
        public static float CardPadding           => Theme.CardPadding;

        // ── Cached assets ────────────────────────────────────────────────
        private static Font s_font;
        private static Sprite s_roundedSprite;
        private static Texture2D s_roundedTexture;

        public static Font GetFont()
        {
            if (s_font == null)
            {
                s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (s_font == null)
                    s_font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return s_font;
        }

        /// <summary>
        /// Shared sliced sprite for buttons/panels using a generated white texture.
        /// Supports Image.Type.Sliced with 4px borders.
        /// </summary>
        public static Sprite GetRoundedSprite()
        {
            if (s_roundedSprite != null) return s_roundedSprite;

            const int size = 16;
            s_roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RingFlow_GeneratedUISprite",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            s_roundedTexture.SetPixels32(pixels);
            s_roundedTexture.Apply(false, true);

            s_roundedSprite = Sprite.Create(
                s_roundedTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(4f, 4f, 4f, 4f));
            s_roundedSprite.name = "RingFlow_GeneratedUISprite";
            s_roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_roundedSprite;
        }

        // ── Panel creators ────────────────────────────────────────────────

        public static GameObject CreatePanel(string name, Transform parent)
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

        public static GameObject CreateSafeAreaPanel(string name, Transform parent)
        {
            var go = CreatePanel(name, parent);
            go.AddComponent<SafeAreaHandler>();
            return go;
        }

        public static GameObject CreateCard(string name, Transform parent, Color? bgColor = null)
        {
            var go = CreatePanel(name, parent);
            var img = go.GetComponent<Image>();
            img.color = bgColor ?? SurfaceColor;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -4f);
            return go;
        }

        // ── Button creators ───────────────────────────────────────────────

        private static void ApplyButtonColors(Button button, ButtonColorConfig colorCfg)
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

        public static GameObject CreateButton(string label, Transform parent, float width, float height)
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
            ApplyButtonColors(button, Theme.PrimaryButtonColors);
            go.AddComponent<UIButtonEffects>();

            // Shadow
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -3f);

            // Label
            var textGo = CreateChildText("Label", go.transform, Theme.ButtonFontSize, TextAnchor.MiddleCenter, TextOnPrimary);
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

        public static GameObject CreateIconButton(string iconText, Transform parent, float size = 48f)
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
            ApplyButtonColors(button, Theme.IconButtonColors);
            go.AddComponent<UIButtonEffects>();

            var textGo = CreateChildText("Icon", go.transform, Mathf.RoundToInt(size * 0.5f), TextAnchor.MiddleCenter, TextColor);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textGo.GetComponent<Text>().text = iconText;

            return go;
        }

        // ── Text creators ─────────────────────────────────────────────────

        public static GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color color)
        {
            var go = CreateChildText("Text", parent, fontSize, align, color);
            var text = go.GetComponent<Text>();
            text.text = content;
            return go;
        }

        private static GameObject CreateChildText(string name, Transform parent, int fontSize, TextAnchor align, Color color)
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

        public static GameObject CreateDisplayText(string content, Transform parent, int fontSize, Color color)
        {
            var go = CreateText(content, parent, fontSize, TextAnchor.MiddleCenter, color);
            var text = go.GetComponent<Text>();
            text.fontStyle = FontStyle.Bold;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);
            return go;
        }

        // ── Toggle creator ────────────────────────────────────────────────

        public static GameObject CreateToggle(Transform parent, float anchorX1, float anchorY1, float anchorX2, float anchorY2, bool initialValue)
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

        // ── Overlay creator ───────────────────────────────────────────────

        public static GameObject CreateOverlay(Transform parent, Color color)
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

        // ── Style appliers ────────────────────────────────────────────────

        private static void SetButtonColors(Button button, ButtonColorConfig cfg)
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

        public static void ApplyPrimaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = PrimaryColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.PrimaryButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public static void ApplyAccentStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = AccentColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.AccentButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public static void ApplySuccessStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = SuccessColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.SuccessButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public static void ApplyOutlineStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = SurfaceColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.OutlineButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplySecondaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = PanelColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.OutlineButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplyDarkStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = SurfaceDark; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.DarkButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnDark;
        }

        public static void ApplyDangerStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) { image.color = DangerColor; image.sprite = GetRoundedSprite(); image.type = Image.Type.Sliced; }
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.DangerButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextOnPrimary;
        }

        public static void ApplyTextButtonStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) image.color = Color.clear;
            var button = btn.GetComponent<Button>();
            if (button != null) SetButtonColors(button, Theme.TextButtonColors);
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplyIconStyle(Button button)
        {
            if (button != null) SetButtonColors(button, Theme.IconButtonColors);
        }

        // ── Localization helpers ─────────────────────────────────────────

        public static void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc)
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

        public static void LocalizeText(GameObject go, string key, ILocalizationService loc)
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

        // ── Anchor helper ────────────────────────────────────────────────

        public static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Animation helpers ────────────────────────────────────────────

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>Animate popup entry with scale bounce + fade.</summary>
        public static void AnimatePopupEntry(GameObject go, float duration = 0.3f)
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

        /// <summary>Animate popup exit with scale down + fade.</summary>
        public static void AnimatePopupExit(GameObject go, float duration = 0.2f, System.Action onComplete = null)
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

        /// <summary>Animate screen entry with fade in.</summary>
        public static void AnimateScreenEntry(GameObject go, float duration = 0.35f)
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

        /// <summary>Animate screen exit with fade out.</summary>
        public static void AnimateScreenExit(GameObject go, float duration = 0.25f, System.Action onComplete = null)
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

        /// <summary>Scale punch effect for rewards.</summary>
        public static void AnimateRewardPunch(GameObject go, float scale = 1.3f, float duration = 0.4f)
        {
            DOTween.Kill(go.transform);
            go.transform.localScale = Vector3.one;
            go.transform.DOPunchScale(Vector3.one * (scale - 1f), duration, 5, 0.5f).SetAutoKill(true);
        }

        /// <summary>Staggered element entry animation.</summary>
        public static void AnimateStaggeredEntry(GameObject[] elements, float delay = 0.07f, float duration = 0.3f)
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

        public static void AddButtonEffects(GameObject go)
        {
            if (go != null && go.GetComponent<Button>() != null && go.GetComponent<UIButtonEffects>() == null)
            {
                go.AddComponent<UIButtonEffects>();
            }
        }

        public static void AddButtonEffects(Button button)
        {
            if (button != null && button.GetComponent<UIButtonEffects>() == null)
            {
                button.gameObject.AddComponent<UIButtonEffects>();
            }
        }
    }
}
