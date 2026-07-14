using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Shared UI creation helpers — fonts, colors, layout templates.
    /// Keeps visual constants in one place so all screens look consistent.
    /// </summary>
    public static class GameUIResources
    {
        private static UIThemeConfigSO _theme;

        public static void Bind(UIThemeConfigSO theme)
        {
            _theme = theme;
        }

        private static UIThemeConfigSO Theme
        {
            get
            {
                if (_theme == null)
                {
                    throw new System.InvalidOperationException("[GameUIResources] UIThemeConfigSO is not bound!");
                }
                return _theme;
            }
        }

        // ── Color tokens (Material Design-inspired dark theme) ────────────
        public static Color PrimaryColor    => Theme.PrimaryColor;
        public static Color PrimaryPressed  => Theme.PrimaryPressed;
        public static Color AccentColor     => Theme.AccentColor;
        public static Color BgColor         => Theme.BgColor;
        public static Color SurfaceColor    => Theme.SurfaceColor;
        public static Color PanelColor      => Theme.PanelColor;
        public static Color TextColor       => Theme.TextColor;
        public static Color MutedText       => Theme.MutedText;
        public static Color DangerColor     => Theme.DangerColor;
        public static Color SuccessColor    => Theme.SuccessColor;

        public static float PanelElevation => Theme.PanelElevation;
        public static float ButtonHeight => Theme.ButtonHeight;
        public static float ButtonWidth => Theme.ButtonWidth;

        private static Font s_font;
        public static Font GetFont()
        {
            if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_font;
        }

        private static Sprite s_roundedSprite;
        /// <summary>
        /// Unity's built-in 4px-radius sliced sprite. Used with <see cref="Image.Type.Sliced"/>
        /// to give buttons/panels soft rounded corners for a more premium look.
        /// </summary>
        public static Sprite GetRoundedSprite()
        {
            if (s_roundedSprite == null)
                s_roundedSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            return s_roundedSprite;
        }

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

        private static void ApplyButtonColors(Button button, ButtonColorConfig colorCfg)
        {
            var colors = button.colors;
            colors.normalColor = colorCfg.NormalColor;
            colors.highlightedColor = colorCfg.HighlightedColor;
            colors.pressedColor = colorCfg.PressedColor;
            colors.selectedColor = colorCfg.SelectedColor;
            colors.disabledColor = colorCfg.DisabledColor;
            button.colors = colors;
        }

        public static GameObject CreateButton(string label, Transform parent, float width, float height)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Shadow));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            var image = go.GetComponent<Image>();
            image.color = PrimaryColor;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;

            // Soft drop shadow gives buttons depth for a more premium feel.
            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);

            var button = go.GetComponent<Button>();
            ApplyButtonColors(button, Theme.PrimaryButtonColors);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = Theme.ButtonFontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.fontStyle = FontStyle.Bold;

            return go;
        }

        public static GameObject CreateText(string content, Transform parent, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
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
            text.text = content;
            return go;
        }

        public static void ApplyOutlineStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = SurfaceColor;
            var button = btn.GetComponent<Button>();
            ApplyButtonColors(button, Theme.OutlineButtonColors);

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplyIconStyle(Button button)
        {
            ApplyButtonColors(button, Theme.IconButtonColors);
        }

        public static void ApplyPrimaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = PrimaryColor;
            var button = btn.GetComponent<Button>();
            ApplyButtonColors(button, Theme.PrimaryButtonColors);

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = Color.white;
        }

        public static void ApplyDangerStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = DangerColor;
            var button = btn.GetComponent<Button>();
            ApplyButtonColors(button, Theme.DangerButtonColors);

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = Color.white;
        }

        public static void ApplySecondaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = SurfaceColor;
            var button = btn.GetComponent<Button>();
            ApplyButtonColors(button, Theme.OutlineButtonColors);

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplyTextButtonStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) image.color = Color.clear;
            var button = btn.GetComponent<Button>();
            ApplyButtonColors(button, Theme.TextButtonColors);

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        // ── Localization helpers ─────────────────────────────────────────
        public static void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc)
        {
            if (loc == null || btn == null) return;
            var text = btn.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = loc.GetString(key, text.text);
                if (loc.IsRTL && text.alignment == TextAnchor.MiddleLeft) text.alignment = TextAnchor.MiddleRight;
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

        // ── Anchors helper ───────────────────────────────────────────────
        public static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
