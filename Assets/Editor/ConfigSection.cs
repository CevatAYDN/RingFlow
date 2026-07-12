using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

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
        }

        private void DrawRow<T>(string label, string resourceKey, string assetPath) where T : ScriptableObject
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.MinWidth(220f));

                var asset = Resources.Load<T>(resourceKey);
                bool exists = asset != null;

                using (new EditorGUI.DisabledScope(!exists))
                {
                    if (GUILayout.Button("Aç", GUILayout.Width(70f)) && exists)
                    {
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                }

                if (GUILayout.Button(exists ? "Yeniden Oluştur" : "Oluştur", GUILayout.Width(130f)))
                {
                    CreateConfig<T>(assetPath, exists);
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
