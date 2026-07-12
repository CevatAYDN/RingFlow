using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(GameConfigDatabaseSO))]
    public class GameConfigDatabaseSOEditor : UnityEditor.Editor
    {
        private int _selectedWorldIndex;
        private int _lookupLevelIndex = 1;
        private Vector2 _scrollPos;

        public override void OnInspectorGUI()
        {
            var db = (GameConfigDatabaseSO)target;

            EditorGUILayout.LabelField("RingFlow Veritabanı Editörü (Database)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUI.BeginChangeCheck();

            // ── Genel Ayarlar ──
            EditorGUILayout.LabelField("Genel Seviye Ayarları", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                db.TotalLevels = EditorGUILayout.IntField("Toplam Seviye Sayısı", db.TotalLevels);
                db.LevelsPerThemeStep = EditorGUILayout.IntField("Tema Başına Seviye Adımı", db.LevelsPerThemeStep);
                db.MinimumEmptyPoles = EditorGUILayout.IntField("Minimum Boş Direk Sayısı", db.MinimumEmptyPoles);
            }

            EditorGUILayout.Space(8f);

            // ── Zorluk Dereceleri (Bands) ──
            EditorGUILayout.LabelField("Zorluk Dereceleri (Difficulty Bands)", EditorStyles.boldLabel);
            if (db.DifficultyBands == null) db.DifficultyBands = new System.Collections.Generic.List<DifficultyBandData>();

            for (int i = 0; i < db.DifficultyBands.Count; i++)
            {
                var band = db.DifficultyBands[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(band.Band.ToString(), EditorStyles.boldLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        band.MaxLevel = EditorGUILayout.IntField("Maks. Seviye", band.MaxLevel);
                        band.MinEmptyPoles = EditorGUILayout.IntField("Min. Boş Direk", band.MinEmptyPoles);
                        band.MaxCapacity = EditorGUILayout.IntField("Maks. Kapasite", band.MaxCapacity);
                    }

                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField("İzin Verilen Halka/Mekanik Tipleri:");
                    
                    if (band.AllowedMechanics == null)
                        band.AllowedMechanics = new System.Collections.Generic.List<WorldMechanicType>();

                    var mechNames = System.Enum.GetNames(typeof(WorldMechanicType));
                    var mechValues = (WorldMechanicType[])System.Enum.GetValues(typeof(WorldMechanicType));

                    // 3-Sütunlu Izgara (Grid) Düzeni - Taşmayı ve kesilmeyi önler
                    int columns = 3;
                    for (int m = 0; m < mechNames.Length; m += columns)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            for (int col = 0; col < columns; col++)
                            {
                                int idx = m + col;
                                if (idx >= mechNames.Length) break;

                                bool allowed = band.AllowedMechanics.Contains(mechValues[idx]);
                                bool newAllowed = EditorGUILayout.ToggleLeft(mechNames[idx], allowed, GUILayout.Width(130f));
                                if (newAllowed != allowed)
                                {
                                    if (newAllowed)
                                        band.AllowedMechanics.Add(mechValues[idx]);
                                    else
                                        band.AllowedMechanics.Remove(mechValues[idx]);
                                }
                            }
                        }
                    }

                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField($"Aktif Olanlar: {string.Join(", ", band.AllowedMechanics)}", EditorStyles.miniLabel);
                }

                db.DifficultyBands[i] = band;

                if (i < db.DifficultyBands.Count - 1)
                    EditorGUILayout.Space(2f);
            }

            EditorGUILayout.Space(8f);

            // ── Renk İlerleme Eğrisi ──
            EditorGUILayout.LabelField("Renk İlerleme Eğrisi (Color Curve)", EditorStyles.boldLabel);
            if (db.ColorCurve == null)
                db.ColorCurve = new System.Collections.Generic.List<ColorCurvePoint>();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < db.ColorCurve.Count; i++)
                {
                    var pt = db.ColorCurve[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Nokta {i + 1}", GUILayout.Width(60f));
                        EditorGUILayout.LabelField("Seviye ≥", GUILayout.Width(55f));
                        pt.LevelThreshold = EditorGUILayout.IntField(pt.LevelThreshold, GUILayout.Width(50f));
                        EditorGUILayout.LabelField("Renkler", GUILayout.Width(50f));
                        pt.ColorCount = EditorGUILayout.IntSlider(pt.ColorCount, 2, 10);
                        db.ColorCurve[i] = pt;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Yeni Nokta Ekle"))
                        db.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = 2000, ColorCount = 10 });
                    if (db.ColorCurve.Count > 1 && GUILayout.Button("- Son Noktayı Sil"))
                        db.ColorCurve.RemoveAt(db.ColorCurve.Count - 1);
                }
            }

            EditorGUILayout.Space(8f);

            // ── Dünyalar ve Temalar ──
            EditorGUILayout.LabelField("Dünyalar ve Tema Ayarları (Worlds)", EditorStyles.boldLabel);
            if (db.Worlds == null)
                db.Worlds = new System.Collections.Generic.List<WorldConfigData>();

            string[] worldNames = new string[db.Worlds.Count];
            for (int i = 0; i < db.Worlds.Count; i++)
                worldNames[i] = $"Dünya {i + 1}: {db.Worlds[i].Theme}";

            _selectedWorldIndex = EditorGUILayout.Popup("Dünya Seç", _selectedWorldIndex, worldNames);

            if (_selectedWorldIndex >= 0 && _selectedWorldIndex < db.Worlds.Count)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var w = db.Worlds[_selectedWorldIndex];
                    int startLevel = _selectedWorldIndex * db.LevelsPerWorld + 1;
                    int endLevel = startLevel + db.LevelsPerWorld - 1;

                    EditorGUILayout.LabelField($"Seviye Aralığı: {startLevel} – {endLevel}", EditorStyles.miniLabel);

                    var band = db.GetBandForLevel(startLevel);
                    int intensity = db.GetMechanicIntensityForLevel(startLevel);
                    EditorGUILayout.LabelField($"Zorluk Bandı: {band} | Yoğunluk: {intensity}", EditorStyles.miniLabel);

                    w.Theme = EditorGUILayout.TextField("Dünya Tema Adı", w.Theme);
                    w.UnlockedByWorldIndex = EditorGUILayout.IntField("Kilit Açma Dünya Endeksi", w.UnlockedByWorldIndex);
                    w.IsEventWorld = EditorGUILayout.Toggle("Boss (Etkinlik) Dünyası mı", w.IsEventWorld);
                    w.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup("Özel Mekanik", w.MechanicType);

                    // Uyuşmazlık uyarısı
                    var allowed = db.GetAllowedMechanicsForLevel(startLevel);
                    if (w.MechanicType != WorldMechanicType.None &&
                        w.MechanicType != WorldMechanicType.RandomPool1 &&
                        w.MechanicType != WorldMechanicType.RandomPool2 &&
                        w.MechanicType != WorldMechanicType.RandomPool3 &&
                        !allowed.Contains(w.MechanicType))
                    {
                        EditorGUILayout.HelpBox(
                            $"Uyuşmazlık Uyarısı: Seçilen özel mekanik ({w.MechanicType}), bu seviyedeki zorluk bandının ({band}) izin verilen mekanikler listesinde yok. " +
                            $"Mekanik seviyede yine de üretilir (dünya mekanikleri band kuralını ezer), ancak tutarlılık için bandın izin verilen listesine eklemeyi düşünebilirsiniz.",
                            MessageType.Warning);
                    }

                    db.Worlds[_selectedWorldIndex] = w;
                }
            }

            EditorGUILayout.Space(8f);

            // ── Denge ve Ödül Ayarları ──
            EditorGUILayout.LabelField("Denge ve Ödül Ayarları (Balance Config)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Geri Alma (Undo)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.FreeUndosPerSession = EditorGUILayout.IntField("Ücretsiz Hak (Oturum)", db.BalanceConfig.FreeUndosPerSession);
                db.BalanceConfig.UndoCoinCost = EditorGUILayout.IntField("Geri Alma Altın Bedeli", db.BalanceConfig.UndoCoinCost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Seviye Tamamlama Ödülleri", EditorStyles.miniBoldLabel);
                db.BalanceConfig.NormalCoinReward = EditorGUILayout.IntField("Normal Bölüm Altın Ödülü", db.BalanceConfig.NormalCoinReward);
                db.BalanceConfig.BossCoinReward = EditorGUILayout.IntField("Boss Bölüm Altın Ödülü", db.BalanceConfig.BossCoinReward);
                db.BalanceConfig.NormalXpReward = EditorGUILayout.IntField("Normal Bölüm XP Ödülü", db.BalanceConfig.NormalXpReward);
                db.BalanceConfig.BossXpReward = EditorGUILayout.IntField("Boss Bölüm XP Ödülü", db.BalanceConfig.BossXpReward);
                db.BalanceConfig.LevelUpCoinReward = EditorGUILayout.IntField("Seviye Atlama Altın Ödülü", db.BalanceConfig.LevelUpCoinReward);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Yıldız Eşikleri (Adım Oranı %)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.ThreeStarTargetRatioPercent = EditorGUILayout.IntSlider("3-Yıldız Hedef Hamle %", db.BalanceConfig.ThreeStarTargetRatioPercent, 50, 200);
                db.BalanceConfig.TwoStarTargetRatioPercent = EditorGUILayout.IntSlider("2-Yıldız Hedef Hamle %", db.BalanceConfig.TwoStarTargetRatioPercent, 50, 300);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Sandık Kazanma İhtimalleri (Kümülatif)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.SilverChestChance = EditorGUILayout.Slider("Gümüş Sandık Şansı %", db.BalanceConfig.SilverChestChance, 0f, 1f);
                db.BalanceConfig.GoldChestChance = EditorGUILayout.Slider("Altın Sandık Şansı %", db.BalanceConfig.GoldChestChance, 0f, 1f);
                db.BalanceConfig.DiamondChestChance = EditorGUILayout.Slider("Elmas Sandık Şansı %", db.BalanceConfig.DiamondChestChance, 0f, 1f);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Reklam Sıklığı", EditorStyles.miniBoldLabel);
                db.BalanceConfig.InterstitialAdInterval = EditorGUILayout.IntField("Geçiş Reklamı Sıklığı (Bölüm)", db.BalanceConfig.InterstitialAdInterval);
            }

            // ── Hızlı Seviye Sorgulama ve Önizleme ──
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Hızlı Seviye Sorgulama ve Tema Önizleme", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _lookupLevelIndex = EditorGUILayout.IntField("Sorgulanacak Seviye", _lookupLevelIndex);
                _lookupLevelIndex = Mathf.Clamp(_lookupLevelIndex, 1, db.TotalLevels);

                var band = db.GetBandForLevel(_lookupLevelIndex);
                int colorCount = db.GetColorCountForLevel(_lookupLevelIndex);
                int poleCount = db.GetPoleCountForLevel(_lookupLevelIndex);
                int maxCap = db.GetMaxCapacityForLevel(_lookupLevelIndex);
                int emptyPoles = db.GetMinEmptyPolesForLevel(_lookupLevelIndex);
                var theme = db.GetLevelThemeForLevel(_lookupLevelIndex);
                int worldIdx = db.GetWorldForLevel(_lookupLevelIndex);
                var worldMechanic = db.GetMechanicForWorld(worldIdx);

                string forcedMechanicsString = (theme.ForcedMechanics == null || theme.ForcedMechanics.Count == 0) ? 
                    "Yok" : string.Join(", ", theme.ForcedMechanics);

                EditorGUILayout.HelpBox(
                    $"[Seviye {_lookupLevelIndex} Raporu]\n" +
                    $"• Zorluk Derecesi: {band}\n" +
                    $"• Bulunduğu Dünya: Dünya {worldIdx + 1} ({db.Worlds[worldIdx].Theme})\n" +
                    $"• Dünya Ana Mekaniği: {worldMechanic}\n" +
                    $"• Renk Sayısı: {colorCount} | Direk Sayısı: {poleCount} (Boş Direkler: {emptyPoles})\n" +
                    $"• Maksimum Halka Kapasitesi: {maxCap}\n" +
                    $"• Aktif Tema Adımı: Seviye {theme.StartLevel} - {theme.EndLevel} (Zorunlu Mekanikler: {forcedMechanicsString})",
                    MessageType.Info);
            }

            EditorGUILayout.Space(12f);

            // ── Sıfırlama Butonu ──
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Varsayılan GDD Ayarlarına Sıfırla", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Varsayılan Ayarlara Sıfırla",
                        "Tüm veritabanı ayarlarını varsayılan GDD kurallarına sıfırlamak istiyor musunuz? Bu işlem geri alınamaz.",
                        "Sıfırla", "İptal"))
                    {
                        db.InitializeDefaults();
                        EditorUtility.SetDirty(db);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(db);
            }
        }
    }
}
