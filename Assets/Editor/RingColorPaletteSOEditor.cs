using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(RingColorPaletteSO))]
    public class RingColorPaletteSOEditor : UnityEditor.Editor
    {
        private RingColorPaletteSO.ColorBlindMode _previewMode;

        public override void OnInspectorGUI()
        {
            var palette = (RingColorPaletteSO)target;

            EditorGUILayout.LabelField("Ring Color Palette", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            // ── Colorblind Mode Preview ──
            RingFlowEditorUtils.BeginSectionBox("Erişilebilirlik Önizleme Modu (Accessibility)", "Renk körlüğü modlarında halkaların nasıl görüneceğini test edin.");
            
            _previewMode = (RingColorPaletteSO.ColorBlindMode)EditorGUILayout.EnumPopup("Önizleme Modu", _previewMode);

            bool narrow = RingFlowEditorUtils.IsNarrowWidth(520f);
            if (narrow)
            {
                if (GUILayout.Button("Kapalı")) _previewMode = RingColorPaletteSO.ColorBlindMode.Off;
                if (GUILayout.Button("Protanopia")) _previewMode = RingColorPaletteSO.ColorBlindMode.Protanopia;
                if (GUILayout.Button("Deuteranopia")) _previewMode = RingColorPaletteSO.ColorBlindMode.Deuteranopia;
                if (GUILayout.Button("Tritanopia")) _previewMode = RingColorPaletteSO.ColorBlindMode.Tritanopia;
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Kapalı")) _previewMode = RingColorPaletteSO.ColorBlindMode.Off;
                    if (GUILayout.Button("Protanopia")) _previewMode = RingColorPaletteSO.ColorBlindMode.Protanopia;
                    if (GUILayout.Button("Deuteranopia")) _previewMode = RingColorPaletteSO.ColorBlindMode.Deuteranopia;
                    if (GUILayout.Button("Tritanopia")) _previewMode = RingColorPaletteSO.ColorBlindMode.Tritanopia;
                }
            }
            
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(8f);

            // ── Color Entries ──
            EditorGUILayout.LabelField("Renk Tanımlamaları (Color Entries)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            serializedObject.Update();
            var entriesProp = serializedObject.FindProperty("_entries");

            if (entriesProp != null && entriesProp.isArray)
            {
                // Header
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    float labelWidth = RingFlowEditorUtils.GetResponsiveWidth(70f, 100f, 0.1f);
                    EditorGUILayout.LabelField("Renk", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField("Normal", GUILayout.Width(labelWidth * 0.8f));
                    EditorGUILayout.LabelField("Protanopia", GUILayout.Width(labelWidth * 0.8f));
                    EditorGUILayout.LabelField("Deuteranopia", GUILayout.Width(labelWidth * 0.9f));
                    EditorGUILayout.LabelField("Tritanopia", GUILayout.Width(labelWidth * 0.8f));
                }

                for (int i = 0; i < entriesProp.arraySize; i++)
                {
                    var entry = entriesProp.GetArrayElementAtIndex(i);
                    var colorProp = entry.FindPropertyRelative("Color");
                    var normalProp = entry.FindPropertyRelative("Normal");
                    var protanProp = entry.FindPropertyRelative("Protanopia");
                    var deuterProp = entry.FindPropertyRelative("Deuteranopia");
                    var tritanProp = entry.FindPropertyRelative("Tritanopia");

                    var bgColor = i % 2 == 0 ? new Color(0.22f, 0.22f, 0.24f) : new Color(0.18f, 0.18f, 0.20f);
                    var rowRect = EditorGUILayout.BeginHorizontal();
                    EditorGUI.DrawRect(rowRect, bgColor);

                    // Color label with preview swatch
                    Color activeColor = _previewMode switch
                    {
                        RingColorPaletteSO.ColorBlindMode.Protanopia => protanProp.colorValue,
                        RingColorPaletteSO.ColorBlindMode.Deuteranopia => deuterProp.colorValue,
                        RingColorPaletteSO.ColorBlindMode.Tritanopia => tritanProp.colorValue,
                        _ => normalProp.colorValue
                    };

                    var swatchRect = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f));
                    EditorGUI.DrawRect(swatchRect, activeColor);

                    string colorName = colorProp.enumValueIndex >= 0 && colorProp.enumValueIndex < colorProp.enumDisplayNames.Length
                        ? colorProp.enumDisplayNames[colorProp.enumValueIndex]
                        : "???";
                    EditorGUILayout.LabelField(colorName, GUILayout.Width(70f));

                    EditorGUILayout.PropertyField(normalProp, GUIContent.none, GUILayout.Width(70f));
                    EditorGUILayout.PropertyField(protanProp, GUIContent.none, GUILayout.Width(70f));
                    EditorGUILayout.PropertyField(deuterProp, GUIContent.none, GUILayout.Width(70f));
                    EditorGUILayout.PropertyField(tritanProp, GUIContent.none, GUILayout.Width(70f));

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(4f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Yeni Renk Ekle"))
                    {
                        entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
                    }
                    if (entriesProp.arraySize > 0 && GUILayout.Button("- Son Rengi Sil"))
                    {
                        entriesProp.DeleteArrayElementAtIndex(entriesProp.arraySize - 1);
                    }
                }
            }

            EditorGUILayout.Space(8f);

            // ── Quick Actions ──
            RingFlowEditorUtils.BeginSectionBox("Hızlı İşlemler (Quick Actions)", "Varsayılan renk paletini otomatik üretin veya renk körlüğü modlarını doldurun.");
            
            if (GUILayout.Button("Varsayılan Paleti Otomatik Üret", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Otomatik Üret",
                    "Bu işlem tüm renk girişlerini silerek varsayılan GDD renk paletini yükler. Devam etmek istiyor musunuz?",
                    "Evet, Üret", "İptal"))
                {
                    PopulateDefaultPalette(palette, entriesProp);
                }
            }

            if (GUILayout.Button("Normal Renkleri Körlük Modlarına Kopyala", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Renkleri Kopyala",
                    "Bu işlem normal mod renklerini diğer tüm körlük modlarına kopyalar. Devam etmek istiyor musunuz?",
                    "Evet, Kopyala", "İptal"))
                {
                    FillCbModesFromNormal(entriesProp);
                }
            }
            
            RingFlowEditorUtils.EndSectionBox();

            serializedObject.ApplyModifiedProperties();
        }

        private static void PopulateDefaultPalette(RingColorPaletteSO palette, SerializedProperty entriesProp)
        {
            var colors = (RingColor[])System.Enum.GetValues(typeof(RingColor));

            // Default color mapping (bright, distinguishable palette)
            var defaults = new (Color normal, Color protan, Color deuter, Color tritan)[]
            {
                (new Color(0.90f, 0.00f, 0.00f), new Color(0.80f, 0.55f, 0.00f), new Color(0.80f, 0.55f, 0.00f), new Color(0.40f, 0.65f, 0.80f)), // Red
                (new Color(0.00f, 0.45f, 0.90f), new Color(0.00f, 0.45f, 0.80f), new Color(0.00f, 0.40f, 0.75f), new Color(0.00f, 0.60f, 0.55f)), // Blue
                (new Color(0.00f, 0.75f, 0.25f), new Color(0.60f, 0.60f, 0.00f), new Color(0.55f, 0.60f, 0.00f), new Color(0.00f, 0.50f, 0.50f)), // Green
                (new Color(1.00f, 0.85f, 0.00f), new Color(0.95f, 0.90f, 0.10f), new Color(0.90f, 0.85f, 0.10f), new Color(0.50f, 0.40f, 0.60f)), // Yellow
                (new Color(0.75f, 0.00f, 0.75f), new Color(0.00f, 0.40f, 0.80f), new Color(0.00f, 0.35f, 0.75f), new Color(0.60f, 0.30f, 0.50f)), // Purple
                (new Color(1.00f, 0.50f, 0.00f), new Color(1.00f, 0.60f, 0.00f), new Color(0.95f, 0.55f, 0.00f), new Color(0.70f, 0.40f, 0.30f)), // Orange
                (new Color(0.00f, 0.70f, 0.70f), new Color(0.00f, 0.50f, 0.60f), new Color(0.00f, 0.45f, 0.55f), new Color(0.30f, 0.55f, 0.55f)), // Cyan
                (new Color(1.00f, 0.40f, 0.70f), new Color(0.60f, 0.55f, 0.60f), new Color(0.55f, 0.50f, 0.55f), new Color(0.75f, 0.35f, 0.55f)), // Pink
                (new Color(0.55f, 0.35f, 0.00f), new Color(0.50f, 0.50f, 0.10f), new Color(0.45f, 0.45f, 0.10f), new Color(0.40f, 0.30f, 0.20f)), // Brown
                (new Color(0.50f, 0.50f, 0.50f), new Color(0.45f, 0.45f, 0.45f), new Color(0.40f, 0.40f, 0.40f), new Color(0.55f, 0.45f, 0.45f)), // Grey
                (new Color(0.00f, 0.00f, 0.00f), new Color(0.10f, 0.10f, 0.10f), new Color(0.10f, 0.10f, 0.10f), new Color(0.10f, 0.10f, 0.10f)), // None/Black
            };

            entriesProp.ClearArray();

            int limit = System.Math.Min(colors.Length, defaults.Length);
            for (int i = 0; i < limit; i++)
            {
                entriesProp.InsertArrayElementAtIndex(i);
                var entry = entriesProp.GetArrayElementAtIndex(i);

                entry.FindPropertyRelative("Color").enumValueIndex = (int)colors[i];
                entry.FindPropertyRelative("Normal").colorValue = defaults[i].normal;
                entry.FindPropertyRelative("Protanopia").colorValue = defaults[i].protan;
                entry.FindPropertyRelative("Deuteranopia").colorValue = defaults[i].deuter;
                entry.FindPropertyRelative("Tritanopia").colorValue = defaults[i].tritan;
            }

            EditorUtility.SetDirty(palette);
        }

        private static void FillCbModesFromNormal(SerializedProperty entriesProp)
        {
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var entry = entriesProp.GetArrayElementAtIndex(i);
                var normalColor = entry.FindPropertyRelative("Normal").colorValue;
                entry.FindPropertyRelative("Protanopia").colorValue = normalColor;
                entry.FindPropertyRelative("Deuteranopia").colorValue = normalColor;
                entry.FindPropertyRelative("Tritanopia").colorValue = normalColor;
            }
        }
    }
}
