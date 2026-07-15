using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay.Views;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(ThemeSkinDatabaseSO))]
    public sealed class ThemeSkinDatabaseSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var db = (ThemeSkinDatabaseSO)target;

            EditorGUILayout.LabelField("Tema ve Görünüm Veritabanı (Theme Skin Database)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            serializedObject.Update();
            var entriesProp = serializedObject.FindProperty("Entries");

            if (entriesProp != null && entriesProp.isArray)
            {
                RingFlowEditorUtils.BeginSectionBox("Dünya Temaları", $"Kayıtlı Dünya Tema ve Görünüm Tanımları ({entriesProp.arraySize})");

                for (int i = 0; i < entriesProp.arraySize; i++)
                {
                    var entry = entriesProp.GetArrayElementAtIndex(i);
                    var worldProp = entry.FindPropertyRelative("WorldIndex");
                    var keyProp = entry.FindPropertyRelative("ThemeNameKey");
                    var bgColProp = entry.FindPropertyRelative("BackgroundColor");
                    var poleColProp = entry.FindPropertyRelative("PoleColor");
                    var floorColProp = entry.FindPropertyRelative("FloorColor");
                    var poleMatProp = entry.FindPropertyRelative("PoleMaterial");
                    var floorMatProp = entry.FindPropertyRelative("FloorMaterial");
                    var bgSpriteProp = entry.FindPropertyRelative("BgSprite");

                    Rect rowRect = EditorGUILayout.BeginVertical();
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Tema {i + 1}: Dünya {worldProp.intValue + 1}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Kaldır", EditorStyles.miniButton, GUILayout.Width(60f)))
                        {
                            entriesProp.DeleteArrayElementAtIndex(i);
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            break;
                        }
                    }

                    EditorGUILayout.Space(2f);
                    worldProp.intValue = EditorGUILayout.IntField("Dünya Endeksi", worldProp.intValue);
                    keyProp.stringValue = EditorGUILayout.TextField("Tema Adı (Anahtar)", keyProp.stringValue);

                    EditorGUILayout.Space(2f);
                    bgColProp.colorValue = EditorGUILayout.ColorField("Arka Plan Rengi", bgColProp.colorValue);
                    poleColProp.colorValue = EditorGUILayout.ColorField("Direk Rengi", poleColProp.colorValue);
                    floorColProp.colorValue = EditorGUILayout.ColorField("Zemin Rengi", floorColProp.colorValue);

                    EditorGUILayout.Space(2f);
                    EditorGUILayout.PropertyField(poleMatProp, new GUIContent("Direk Materyali"));
                    EditorGUILayout.PropertyField(floorMatProp, new GUIContent("Zemin Materyali"));
                    EditorGUILayout.PropertyField(bgSpriteProp, new GUIContent("Arka Plan Resmi (Bg Sprite)"));

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(6f);
                }

                if (GUILayout.Button("+ Yeni Tema Görünümü Ekle", GUILayout.Height(26)))
                {
                    entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
                    var newElem = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
                    newElem.FindPropertyRelative("WorldIndex").intValue = entriesProp.arraySize - 1;
                    newElem.FindPropertyRelative("ThemeNameKey").stringValue = $"theme.world_{entriesProp.arraySize}";
                    newElem.FindPropertyRelative("BackgroundColor").colorValue = new Color(0.12f, 0.14f, 0.17f);
                    newElem.FindPropertyRelative("PoleColor").colorValue = Color.white;
                    newElem.FindPropertyRelative("FloorColor").colorValue = new Color(0.3f, 0.3f, 0.35f);
                }

                RingFlowEditorUtils.EndSectionBox();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
