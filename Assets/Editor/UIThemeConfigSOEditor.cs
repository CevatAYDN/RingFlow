using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Inspector editor for <see cref="UIThemeConfigSO"/> so the entire UI theme
    /// (colors, layout tokens, button presets) is editable from the dashboard.
    /// </summary>
    [CustomEditor(typeof(UIThemeConfigSO))]
    public sealed class UIThemeConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (UIThemeConfigSO)target;

            EditorGUILayout.LabelField("Arayüz Tema Yapılandırması (UI Theme)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            // ── Visual Palette Preview ──
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                DrawColorBox(config.PrimaryColor, "Primary");
                DrawColorBox(config.AccentColor, "Accent");
                DrawColorBox(config.BgColor, "BG");
                DrawColorBox(config.SurfaceColor, "Surface");
                DrawColorBox(config.PanelColor, "Panel");
                DrawColorBox(config.TextColor, "Text");
            }
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();

            RingFlowEditorUtils.SectionTitle("Renkler");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                config.PrimaryColor = ColorField("Birincil Renk", config.PrimaryColor);
                config.PrimaryPressed = ColorField("Birincil Basılı", config.PrimaryPressed);
                config.AccentColor = ColorField("Vurgu Rengi", config.AccentColor);
                config.BgColor = ColorField("Arka Plan Rengi", config.BgColor);
                config.SurfaceColor = ColorField("Yüzey Rengi", config.SurfaceColor);
                config.PanelColor = ColorField("Panel Rengi", config.PanelColor);
                config.TextColor = ColorField("Metin Rengi", config.TextColor);
                config.MutedText = ColorField("Sönük Metin", config.MutedText);
                config.DangerColor = ColorField("Tehlike Rengi", config.DangerColor);
                config.SuccessColor = ColorField("Başarı Rengi", config.SuccessColor);
            }
            EditorGUILayout.Space(6f);

            RingFlowEditorUtils.SectionTitle("Yerleşim Tokenları");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                config.PanelElevation = EditorGUILayout.FloatField("Panel Yükseltmesi", config.PanelElevation);
                config.ButtonHeight = EditorGUILayout.FloatField("Buton Yüksekliği", config.ButtonHeight);
                config.ButtonWidth = EditorGUILayout.FloatField("Buton Genişliği", config.ButtonWidth);
                config.ButtonFontSize = EditorGUILayout.IntField("Buton Yazı Boyutu", config.ButtonFontSize);
            }
            EditorGUILayout.Space(6f);

            RingFlowEditorUtils.SectionTitle("Buton Renk Ön Ayarları");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                config.PrimaryButtonColors = DrawButtonColors("Birincil Buton", config.PrimaryButtonColors);
                config.OutlineButtonColors = DrawButtonColors("Çerçeve Buton", config.OutlineButtonColors);
                config.DangerButtonColors = DrawButtonColors("Tehlike Buton", config.DangerButtonColors);
                config.TextButtonColors = DrawButtonColors("Metin Buton", config.TextButtonColors);
                config.IconButtonColors = DrawButtonColors("Simge Buton", config.IconButtonColors);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }
        }

        private static Color ColorField(string label, Color value)
        {
            return EditorGUILayout.ColorField(label, value);
        }

        private static ButtonColorConfig DrawButtonColors(string title, ButtonColorConfig button)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                button.NormalColor = EditorGUILayout.ColorField("Normal", button.NormalColor);
                button.HighlightedColor = EditorGUILayout.ColorField("Vurgulu", button.HighlightedColor);
                button.PressedColor = EditorGUILayout.ColorField("Basılı", button.PressedColor);
                button.SelectedColor = EditorGUILayout.ColorField("Seçili", button.SelectedColor);
                button.DisabledColor = EditorGUILayout.ColorField("Pasif", button.DisabledColor);
            }
            EditorGUILayout.Space(4f);
            return button;
        }

        private static void DrawColorBox(Color color, string label)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(55f)))
            {
                var rect = GUILayoutUtility.GetRect(50f, 20f);
                EditorGUI.DrawRect(rect, color);
                EditorGUILayout.LabelField(label, RingFlowEditorUtils.CenteredMiniLabel, GUILayout.Width(50f));
            }
        }
    }
}
