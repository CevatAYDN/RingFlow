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

        // ── Layout tokens ────────────────────────────────────────────────
        public const float PanelElevation   = 4f;
        public const float ButtonHeight     = 56f;
        public const float ButtonWidth      = 240f;

        // ── Font (cached) ────────────────────────────────────────────────
        private static Font s_font;
        public static Font GetFont()
        {
            if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_font;
        }

        // ── Panel ────────────────────────────────────────────────────────
        public static GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = BgColor;
            return go;
        }

        // ── Safe-Area-aware panel ────────────────────────────────────────
        public static GameObject CreateSafeAreaPanel(string name, Transform parent)
        {
            var go = CreatePanel(name, parent);
            go.AddComponent<SafeAreaHandler>();
            return go;
        }

        // ── Button (with proper Selectable feedback) ─────────────────────
        public static GameObject CreateButton(string label, Transform parent, float width, float height)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(width, height);
            go.GetComponent<Image>().color = PrimaryColor;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = PrimaryColor;
            colors.highlightedColor = new Color(0.30f, 0.62f, 1.00f);
            colors.pressedColor = PrimaryPressed;
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.30f, 0.30f, 0.35f);
            button.colors = colors;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; // Primary button has white text for contrast
            text.text = label;
            text.fontStyle = FontStyle.Bold;

            return go;
        }

        // ── Text ─────────────────────────────────────────────────────────
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

        // ── Button style presets ─────────────────────────────────────────
        public static void ApplyOutlineStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = SurfaceColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = SurfaceColor;
            colors.highlightedColor = new Color(0.80f, 0.84f, 0.90f);
            colors.pressedColor = new Color(0.70f, 0.74f, 0.80f);
            colors.disabledColor = new Color(0.85f, 0.87f, 0.90f);
            button.colors = colors;

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplyIconStyle(Button button)
        {
            var colors = button.colors;
            colors.normalColor = PanelColor;
            colors.highlightedColor = new Color(0.80f, 0.84f, 0.90f);
            colors.pressedColor = new Color(0.70f, 0.74f, 0.80f);
            button.colors = colors;
        }

        public static void ApplyPrimaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = PrimaryColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = PrimaryColor;
            colors.highlightedColor = new Color(0.30f, 0.62f, 1.00f);
            colors.pressedColor = PrimaryPressed;
            button.colors = colors;

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = Color.white;
        }

        public static void ApplyDangerStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = DangerColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = DangerColor;
            colors.highlightedColor = new Color(0.88f, 0.32f, 0.32f);
            colors.pressedColor = new Color(0.60f, 0.15f, 0.15f);
            button.colors = colors;

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = Color.white;
        }

        public static void ApplySecondaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = SurfaceColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = SurfaceColor;
            colors.highlightedColor = new Color(0.80f, 0.84f, 0.90f);
            colors.pressedColor = new Color(0.70f, 0.74f, 0.80f);
            button.colors = colors;

            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.color = TextColor;
        }

        public static void ApplyTextButtonStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            if (image != null) image.color = Color.clear;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = new Color(0.15f, 0.20f, 0.28f, 0.08f);
            colors.pressedColor = new Color(0.15f, 0.20f, 0.28f, 0.15f);
            button.colors = colors;

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
