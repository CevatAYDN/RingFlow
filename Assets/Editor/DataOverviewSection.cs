using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Birleşik Veri Ekranı — tüm seviye/config verilerini tek bir sekmede
    /// özetler ve her bir config varlığına doğrudan atlar. Düzenleme, var olan
    /// SO custom editörlerine (Selection üzerinden) devredilir; bu bölüm yalnızca
    /// toplu görünüm ve gezinme sağlar.
    /// </summary>
    public sealed class DataOverviewSection : EditorSection
    {
        // Cached state — loaded once per OnGUI instead of once per entry per frame.
        private GameConfigDatabaseSO _cachedDatabase;
        private readonly Object[] _cachedAssets = new Object[AssetEntries.Length];
        private bool _assetsResolved;

        public override string DisplayName => "Birleşik Veri Ekranı";
        public override string PrefKey => EditorPrefsKeys.FoldDataOverview;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            EnsureCachedDatabase();
            EnsureCachedAssets();
            if (_cachedDatabase == null)
            {
                EditorGUILayout.HelpBox("Zorluk Veritabanı (GameConfigDatabase.asset) bulunamadı!", MessageType.Error);
                return;
            }

            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataDifficulty,
                "Zorluk Dereceleri Özeti", () => DrawDifficultySummary(_cachedDatabase));
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataColor,
                "Renk İlerleme Eğrisi", () => DrawColorCurve(_cachedDatabase));
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataWorlds,
                "Dünyalar ve Temalar", () => DrawWorlds(_cachedDatabase));
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataAssets,
                "Yapılandırma Varlıklarına Git", DrawAssetJumps);
        }

        private void EnsureCachedDatabase()
        {
            _cachedDatabase = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
        }

        private void EnsureCachedAssets()
        {
            // Per-frame reload would cost 9 Resources.Load calls every repaint.
            // We resolve once and keep references; if a load failed because an asset
            // was created later in this session, the user can click the missing row
            // (or collapse/reopen the section) — the next EnsureCachedAssets resolves.
            if (_assetsResolved) return;
            for (int i = 0; i < AssetEntries.Length; i++)
                _cachedAssets[i] = AssetEntries[i].Load();
            _assetsResolved = true;
        }

        private static void DrawDifficultySummary(GameConfigDatabaseSO db)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Toplam Seviye: {db.TotalLevels}", EditorStyles.boldLabel);
                for (int i = 0; i < db.DifficultyBands.Count; i++)
                {
                    var band = db.DifficultyBands[i];
                    EditorGUILayout.LabelField(
                        $"{band.Band}: Maks Seviye={band.MaxLevel} | Boş Direk={band.MinEmptyPoles} | Kapasite={band.MaxCapacity} | Mekanik={band.AllowedMechanics?.Count ?? 0}");
                }
            }
        }

        private static void DrawColorCurve(GameConfigDatabaseSO db)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < db.ColorCurve.Count; i++)
                {
                    var pt = db.ColorCurve[i];
                    EditorGUILayout.LabelField($"Seviye ≥ {pt.LevelThreshold}: {pt.ColorCount} renk");
                }
            }
        }

        private static void DrawWorlds(GameConfigDatabaseSO db)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < db.Worlds.Count; i++)
                {
                    var w = db.Worlds[i];
                    EditorGUILayout.LabelField(
                        $"Dünya {i + 1}: {w.Theme} | Mekanik: {w.MechanicType} | {(w.IsEventWorld ? "Boss" : "Normal")}");
                }
            }
        }

        private void DrawAssetJumps()
        {
            for (int i = 0; i < AssetEntries.Length; i++)
            {
                var entry = AssetEntries[i];
                var obj = _cachedAssets[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(entry.Label, GUILayout.MinWidth(220f));
                    if (obj == null)
                    {
                        var prev = GUI.color;
                        GUI.color = EditorPaths.EditorColors.Warning;
                        EditorGUILayout.LabelField("Eksik", GUILayout.Width(60f));
                        GUI.color = prev;
                    }
                    else if (GUILayout.Button("Aç", GUILayout.Width(80f)))
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            }
        }

        private static readonly (string Label, System.Func<UnityEngine.Object> Load)[] AssetEntries =
        {
            ("Oyun Veritabanı (GameConfigDatabase)", () => Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)),
            ("Oyun Hissiyatı (Game Feel)", () => Resources.Load<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)),
            ("Halka Renk Paleti", () => Resources.Load<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)),
            ("Ses (Audio)", () => Resources.Load<AudioConfigSO>(EditorPaths.AudioConfigKey)),
            ("Arayüz Teması (UI Theme)", () => Resources.Load<UIThemeConfigSO>(EditorPaths.UIThemeConfigKey)),
            ("Mağaza Kataloğu (StoreCatalog)", () => Resources.Load<RingFlow.Gameplay.Economy.StoreCatalogSO>(EditorPaths.StoreCatalogKey)),
            ("Yerelleştirme (LocalizationConfig)", () => Resources.Load<RingFlow.Gameplay.Localization.LocalizationConfigSO>(EditorPaths.LocalizationConfigKey)),
            ("Halka Mekanik Verisi (RingMechanicData)", () => Resources.Load<RingFlow.Gameplay.Strategies.RingMechanicDataSO>(EditorPaths.RingMechanicDataKey)),
            ("Tema/Skin Veritabanı (ThemeSkinDatabase)", () => Resources.Load<RingFlow.Gameplay.Views.ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey)),
        };
    }
}
