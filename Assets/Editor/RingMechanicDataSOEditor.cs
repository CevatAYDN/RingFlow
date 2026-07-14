using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(RingMechanicDataSO))]
    public sealed class RingMechanicDataSOEditor : UnityEditor.Editor
    {
        private GameConfigDatabaseSO _cachedDb;

        public override void OnInspectorGUI()
        {
            var data = (RingMechanicDataSO)target;

            EditorGUILayout.LabelField("Halka Mekanik Veritabanı (Ring Mechanic Data)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            if (_cachedDb == null)
            {
                _cachedDb = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            }

            if (_cachedDb == null)
            {
                EditorGUILayout.HelpBox("Dynamic metadata resolution requires GameConfigDatabase.asset in Resources!", MessageType.Warning);
            }

            serializedObject.Update();
            var mechanicsProp = serializedObject.FindProperty("Mechanics");

            if (mechanicsProp != null && mechanicsProp.isArray)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Mekanik Tipleri ve Özellikleri", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("İsim ve kilit bilgileri tek kaynak kuralı gereği GameConfigDatabaseSO'dan dinamik olarak çekilir.", MessageType.Info);
                    EditorGUILayout.Space(2f);

                    for (int i = 0; i < mechanicsProp.arraySize; i++)
                    {
                        var entry = mechanicsProp.GetArrayElementAtIndex(i);
                        var typeProp = entry.FindPropertyRelative("Type");
                        var iconProp = entry.FindPropertyRelative("Icon");
                        var movementProp = entry.FindPropertyRelative("IsMovementRestricting");
                        var affectedTypesProp = entry.FindPropertyRelative("AffectedRingTypes");

                        WorldMechanicType type = (WorldMechanicType)typeProp.enumValueIndex;
                        string displayNameKey = data.GetDisplayNameKey(type, _cachedDb);
                        int firstWorld = data.GetFirstAppearanceWorldIndex(type, _cachedDb);

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                // Draw icon preview
                                var iconTex = iconProp.objectReferenceValue as Sprite;
                                var prevBg = GUI.backgroundColor;
                                GUILayout.Box(iconTex != null ? iconTex.texture : Texture2D.whiteTexture, GUILayout.Width(36f), GUILayout.Height(36f));

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    EditorGUILayout.LabelField($"{type} Mekaniği", EditorStyles.boldLabel);
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField($"Anahtar: {displayNameKey}", EditorStyles.miniLabel, GUILayout.Width(180f));
                                        EditorGUILayout.LabelField($"İlk Dünya: Dünya {firstWorld + 1}", EditorStyles.miniLabel);
                                    }
                                }
                            }

                            EditorGUILayout.Space(2f);
                            EditorGUILayout.PropertyField(iconProp, new GUIContent("Simge Varlığı (Icon Sprite)"));
                            movementProp.boolValue = EditorGUILayout.Toggle("Hareketi Sınırlar mı", movementProp.boolValue);
                            EditorGUILayout.PropertyField(affectedTypesProp, new GUIContent("Etkilenen Halka Tipleri"), true);
                        }
                        EditorGUILayout.Space(4f);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("+ Yeni Mekanik Tanımı Ekle"))
                        {
                            mechanicsProp.InsertArrayElementAtIndex(mechanicsProp.arraySize);
                        }
                        if (mechanicsProp.arraySize > 0 && GUILayout.Button("- Son Tanımı Sil"))
                        {
                            mechanicsProp.DeleteArrayElementAtIndex(mechanicsProp.arraySize - 1);
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
