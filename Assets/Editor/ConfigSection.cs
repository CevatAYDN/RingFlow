using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Economy;
using RingFlow.Gameplay.Localization;
using RingFlow.Gameplay.Strategies;
using RingFlow.Gameplay.Views;

namespace RingFlow.Editor
{
    /// <summary>
    /// Lists every configuration ScriptableObject (the single source of truth) and
    /// lets the user open or (re)create each one directly from the dashboard.
    /// </summary>
    public sealed class ConfigSection : EditorSection
    {
        public override string DisplayName => "Yapılandırma Varlıkları (Config Assets)";
        public override string PrefKey => EditorPrefsKeys.FoldConfigAssets;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            EditorGUILayout.HelpBox(
                "Tüm oyun yapılandırma varlıklarına (tek veri kaynağı) buradan ulaşın. " +
                "Varlık yoksa 'Oluştur', varsa 'Aç' ile düzenleyiciye gidin.",
                MessageType.Info);

            DrawRow<GameConfigDatabaseSO>("Oyun Veritabanı (GameConfigDatabase)", EditorPaths.GameConfigDatabaseKey, EditorPaths.GameConfigDbPath);
            DrawRow<GameFeelConfigSO>("Oyun Hissiyatı (Game Feel)", EditorPaths.GameFeelConfigKey, EditorPaths.GameFeelConfigPath);
            DrawRow<RingColorPaletteSO>("Halka Renk Paleti", EditorPaths.RingColorPaletteKey, EditorPaths.RingColorPalettePath);
            DrawRow<AudioConfigSO>("Ses Yapılandırması (Audio)", EditorPaths.AudioConfigKey, EditorPaths.AudioConfigPath);
            DrawRow<UIThemeConfigSO>("Arayüz Teması (UI Theme)", EditorPaths.UIThemeConfigKey, EditorPaths.UIThemeConfigPath);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Data-Driven Varlıklar", EditorStyles.boldLabel);

            DrawRow<StoreCatalogSO>("Mağaza Kataloğu (StoreCatalog)", EditorPaths.StoreCatalogKey, EditorPaths.StoreCatalogPath);
            DrawRow<LocalizationConfigSO>("Yerelleştirme (LocalizationConfig)", EditorPaths.LocalizationConfigKey, EditorPaths.LocalizationConfigPath);
            DrawRow<RingMechanicDataSO>("Halka Mekanik Verisi (RingMechanicData)", EditorPaths.RingMechanicDataKey, EditorPaths.RingMechanicDataPath);
            DrawRow<ThemeSkinDatabaseSO>("Tema/Skin Veritabanı (ThemeSkinDatabase)", EditorPaths.ThemeSkinDatabaseKey, EditorPaths.ThemeSkinDatabasePath);
        }

        private void DrawRow<T>(string label, string resourceKey, string assetPath) where T : ScriptableObject
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                var asset = Resources.Load<T>(resourceKey);
                bool exists = asset != null;
                bool positionWidthIsNarrow = RingFlowEditorUtils.IsNarrowWidth(460f);

                if (positionWidthIsNarrow)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        using (new EditorGUI.DisabledScope(!exists))
                        {
                            if (GUILayout.Button("Aç", GUILayout.MinWidth(70f)) && exists)
                            {
                                Selection.activeObject = asset;
                                EditorGUIUtility.PingObject(asset);
                            }
                        }

                        if (GUILayout.Button(exists ? "Yeniden Oluştur" : "Oluştur", GUILayout.MinWidth(130f)))
                        {
                            CreateConfig<T>(assetPath, exists);
                        }
                    }
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(!exists))
                        {
                            if (GUILayout.Button("Aç", GUILayout.MinWidth(70f)) && exists)
                            {
                                Selection.activeObject = asset;
                                EditorGUIUtility.PingObject(asset);
                            }
                        }

                        if (GUILayout.Button(exists ? "Yeniden Oluştur" : "Oluştur", GUILayout.MinWidth(130f)))
                        {
                            CreateConfig<T>(assetPath, exists);
                        }
                    }
                }
            }
        }

        private static void CreateConfig<T>(string assetPath, bool exists) where T : ScriptableObject
        {
            if (exists && !EditorUtility.DisplayDialog("Varlık Mevcut",
                $"{assetPath} zaten var. Üzerine yazılsın mı?", "Evet", "İptal"))
            {
                return;
            }

            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
        }
    }
}
