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
        // ── Color tokens (Material Design-inspired dark theme) ────────────
        public static Color PrimaryColor    => new Color(0.22f, 0.51f, 0.91f); // #387AE8
        public static Color PrimaryPressed  => new Color(0.18f, 0.42f, 0.78f);
        public static Color AccentColor     => new Color(1.00f, 0.76f, 0.03f); // #FFC208
        public static Color BgColor         => new Color(0.08f, 0.08f, 0.12f); // #14141F
        public static Color SurfaceColor    => new Color(0.12f, 0.13f, 0.18f);
        public static Color PanelColor      => new Color(0.14f, 0.14f, 0.20f); // #242433
        public static Color TextColor       => Color.white;
        public static Color MutedText       => new Color(0.60f, 0.60f, 0.65f);
        public static Color DangerColor     => new Color(0.78f, 0.20f, 0.20f);
        public static Color SuccessColor    => new Color(0.27f, 0.74f, 0.40f);

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
            text.color = TextColor;
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
            colors.highlightedColor = new Color(0.20f, 0.22f, 0.28f);
            colors.pressedColor = new Color(0.10f, 0.11f, 0.14f);
            colors.disabledColor = new Color(0.18f, 0.18f, 0.22f);
            button.colors = colors;
        }

        public static void ApplyIconStyle(Button button)
        {
            var colors = button.colors;
            colors.normalColor = PanelColor;
            colors.highlightedColor = new Color(0.20f, 0.22f, 0.28f);
            colors.pressedColor = new Color(0.10f, 0.11f, 0.14f);
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
        }

        public static void ApplySecondaryStyle(GameObject btn)
        {
            var image = btn.GetComponent<Image>();
            image.color = SurfaceColor;
            var button = btn.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = SurfaceColor;
            colors.highlightedColor = new Color(0.20f, 0.22f, 0.28f);
            colors.pressedColor = new Color(0.10f, 0.11f, 0.14f);
            button.colors = colors;
        }

        // ── Localization helpers ─────────────────────────────────────────
        public static void LocalizeButtonText(GameObject btn, string key, ILocalizationService loc)
        {
            if (loc == null || btn == null) return;
            var text = btn.GetComponentInChildren<Text>();
            if (text != null) text.text = loc.GetString(key, text.text);
        }

        public static void LocalizeText(GameObject go, string key, ILocalizationService loc)
        {
            if (loc == null || go == null) return;
            var text = go.GetComponent<Text>();
            if (text != null) text.text = loc.GetString(key, text.text);
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
