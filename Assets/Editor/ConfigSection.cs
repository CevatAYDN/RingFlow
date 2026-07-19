using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
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

            RingFlowEditorUtils.BeginSectionBox("Yapılandırma Varlıkları (Config Assets)", "Tüm oyun yapılandırma varlıklarına (tek veri kaynağı) buradan ulaşın.");

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Yapılandırma Adı", EditorStyles.miniBoldLabel, GUILayout.Width(280f));
                EditorGUILayout.LabelField("Durum", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
                EditorGUILayout.LabelField("İşlemler", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
            }

            DrawRow<GameConfigDatabaseSO>("Oyun Veritabanı (GameConfigDatabase)", EditorPaths.GameConfigDatabaseKey, EditorPaths.GameConfigDbPath, 0);
            DrawRow<GameFeelConfigSO>("Oyun Hissiyatı (Game Feel)", EditorPaths.GameFeelConfigKey, EditorPaths.GameFeelConfigPath, 1);
            DrawRow<RingColorPaletteSO>("Halka Renk Paleti", EditorPaths.RingColorPaletteKey, EditorPaths.RingColorPalettePath, 2);
            DrawRow<AudioConfigSO>("Ses Yapılandırması (Audio)", EditorPaths.AudioConfigKey, EditorPaths.AudioConfigPath, 3);
            DrawRow<UIThemeConfigSO>("Arayüz Teması (UI Theme)", EditorPaths.UIThemeConfigKey, EditorPaths.UIThemeConfigPath, 4);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("DATA-DRIVEN VARLIKLAR", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            DrawRow<StoreCatalogSO>("Mağaza Kataloğu (StoreCatalog)", EditorPaths.StoreCatalogKey, EditorPaths.StoreCatalogPath, 5);
            DrawRow<LocalizationConfigSO>("Yerelleştirme (LocalizationConfig)", EditorPaths.LocalizationConfigKey, EditorPaths.LocalizationConfigPath, 6);
            DrawRow<RingMechanicDataSO>("Halka Mekanik Verisi (RingMechanicData)", EditorPaths.RingMechanicDataKey, EditorPaths.RingMechanicDataPath, 7);
            DrawRow<ThemeSkinDatabaseSO>("Tema/Skin Veritabanı (ThemeSkinDatabase)", EditorPaths.ThemeSkinDatabaseKey, EditorPaths.ThemeSkinDatabasePath, 8);
            DrawRow<ScreenRegistrySO>("Ekran Kayıt Defteri (ScreenRegistry)", EditorPaths.ScreenRegistryKey, EditorPaths.ScreenRegistryPath, 9);

            RingFlowEditorUtils.EndSectionBox();
        }

        private void DrawRow<T>(string label, string resourceKey, string assetPath, int rowIndex) where T : ScriptableObject
        {
            var asset = new RingFlow.Gameplay.Services.ResourcesAssetService()
                .LoadAsync<T>(resourceKey)
                .GetAwaiter().GetResult();
            bool exists = asset != null;

            Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
            
            Color bgColor = rowIndex % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
            EditorGUI.DrawRect(rect, bgColor);

            EditorGUILayout.LabelField(label, GUILayout.Width(280f));

            var statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = exists ? EditorPaths.EditorColors.Success : EditorPaths.EditorColors.Error }
            };
            EditorGUILayout.LabelField(exists ? "VAR" : "EKSİK", statusStyle, GUILayout.Width(60f));

            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUI.DisabledScope(!exists))
                {
                    if (GUILayout.Button("Aç", GUILayout.Width(50f), GUILayout.Height(16f)) && exists)
                    {
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                }

                if (GUILayout.Button(exists ? "Yeniden Oluştur" : "Oluştur", GUILayout.Width(110f), GUILayout.Height(16f)))
                {
                    CreateConfig<T>(assetPath, exists);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);
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
