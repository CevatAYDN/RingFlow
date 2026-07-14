using UnityEditor;
using UnityEngine;
using System.IO;
using RingFlow.Gameplay.Localization;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(LocalizationConfigSO))]
    public sealed class LocalizationConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (LocalizationConfigSO)target;

            EditorGUILayout.LabelField("Yerelleştirme Yapılandırması (Localization Config)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            // ── CSV Link ──
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Yerelleştirme Tablosu (CSV)", EditorStyles.boldLabel);
                string csvPath = "Assets/Resources/Localization.csv";
                bool csvExists = File.Exists(csvPath);

                if (csvExists)
                {
                    EditorGUILayout.HelpBox($"Localization.csv dosyası mevcut: {csvPath}", MessageType.Info);
                    if (GUILayout.Button("Localization.csv Dosyasını Aç / Düzenle", GUILayout.Height(26)))
                    {
                        var csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
                        if (csvAsset != null)
                        {
                            AssetDatabase.OpenAsset(csvAsset);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"Kritik: Localization.csv dosyası eksik! Yol: {csvPath}", MessageType.Error);
                }
            }

            EditorGUILayout.Space(8f);

            // ── Language Entries ──
            serializedObject.Update();
            var langsProp = serializedObject.FindProperty("Languages");

            if (langsProp != null && langsProp.isArray)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"Desteklenen Diller ({langsProp.arraySize})", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f);

                    for (int i = 0; i < langsProp.arraySize; i++)
                    {
                        var lang = langsProp.GetArrayElementAtIndex(i);
                        var codeProp = lang.FindPropertyRelative("Code");
                        var nameProp = lang.FindPropertyRelative("DisplayName");
                        var rtlProp = lang.FindPropertyRelative("IsRTL");

                        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                        {
                            EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20f));
                            codeProp.stringValue = EditorGUILayout.TextField(codeProp.stringValue, GUILayout.Width(40f));
                            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.MinWidth(120f));
                            rtlProp.boolValue = EditorGUILayout.ToggleLeft("Sağdan Sola (RTL)", rtlProp.boolValue, GUILayout.Width(130f));

                            if (GUILayout.Button("-", GUILayout.Width(24f)))
                            {
                                langsProp.DeleteArrayElementAtIndex(i);
                                break;
                            }
                        }
                    }

                    if (GUILayout.Button("+ Yeni Dil Ekle", GUILayout.Height(24)))
                    {
                        langsProp.InsertArrayElementAtIndex(langsProp.arraySize);
                        var newElem = langsProp.GetArrayElementAtIndex(langsProp.arraySize - 1);
                        newElem.FindPropertyRelative("Code").stringValue = "new";
                        newElem.FindPropertyRelative("DisplayName").stringValue = "Yeni Dil";
                        newElem.FindPropertyRelative("IsRTL").boolValue = false;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
