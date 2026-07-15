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
        private string _worldSearchFilter = string.Empty;
        private Vector2 _scrollPos;

        public override void OnInspectorGUI()
        {
            var db = (GameConfigDatabaseSO)target;
            float responsiveLabelWidth = RingFlowEditorUtils.GetResponsiveLabelWidth(80f, 140f, 0.16f);

            EditorGUILayout.LabelField("RingFlow Veritabanı Editörü (Database)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            // ── GDD Consistency Checks ──
            if (db.TotalLevels != 2000 || db.Worlds.Count != 40 || db.LevelsPerWorld != 50)
            {
                RingFlowEditorUtils.BeginSectionBox("⚠ GDD Tutarsızlığı Saptandı", "Veritabanı parametreleri GDD'deki 2000 seviye ve 40 dünya hedefiyle eşleşmiyor.");
                if (db.TotalLevels != 2000)
                    EditorGUILayout.LabelField($"• Toplam Seviye {db.TotalLevels} ayarlanmış (GDD beklentisi: 2000).", EditorStyles.miniLabel);
                if (db.Worlds.Count != 40)
                    EditorGUILayout.LabelField($"• Dünya Sayısı {db.Worlds.Count} ayarlanmış (GDD beklentisi: 40).", EditorStyles.miniLabel);
                if (db.LevelsPerWorld != 50)
                    EditorGUILayout.LabelField($"• Dünya Başına Seviye {db.LevelsPerWorld} ayarlanmış (GDD beklentisi: 50).", EditorStyles.miniLabel);
                RingFlowEditorUtils.EndSectionBox();
                EditorGUILayout.Space(4f);
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUI.BeginChangeCheck();

            // ── Genel Ayarlar ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbGeneral, "Genel Seviye Ayarları", () =>
            {
                RingFlowEditorUtils.BeginSectionBox("Genel Seviye Ayarları", "Oyun genelindeki seviye miktarları ve temel kurallar.");
                db.TotalLevels = EditorGUILayout.IntField("Toplam Seviye Sayısı", db.TotalLevels);
                db.LevelsPerThemeStep = EditorGUILayout.IntField("Tema Başına Seviye Adımı", db.LevelsPerThemeStep);
                db.MinimumEmptyPoles = EditorGUILayout.IntField("Minimum Boş Direk Sayısı", db.MinimumEmptyPoles);
                RingFlowEditorUtils.EndSectionBox();
            });

            EditorGUILayout.Space(8f);

            // ── Zorluk Dereceleri (Bands) ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbBands, "Zorluk Dereceleri (Difficulty Bands)", () =>
            {
                if (db.DifficultyBands == null) db.DifficultyBands = new System.Collections.Generic.List<DifficultyBandData>();

                for (int i = 0; i < db.DifficultyBands.Count; i++)
                {
                    var band = db.DifficultyBands[i];
                    RingFlowEditorUtils.BeginSectionBox(band.Band.ToString(), "Zorluk bandında geçerli parametreler ve izin verilen özel halka mekanikleri.");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        band.MaxLevel = EditorGUILayout.IntField("Maks. Seviye", band.MaxLevel, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(110f, 160f, 0.14f)));
                        band.MinEmptyPoles = EditorGUILayout.IntField("Min. Boş Direk", band.MinEmptyPoles, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(100f, 150f, 0.13f)));
                        band.MaxCapacity = EditorGUILayout.IntField("Maks. Kapasite", band.MaxCapacity, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(100f, 150f, 0.13f)));
                    }

                    EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField("İzin Verilen Halka/Mekanik Tipleri:");

                    if (band.AllowedMechanics == null)
                        band.AllowedMechanics = new System.Collections.Generic.List<WorldMechanicType>();

                    var mechNames = System.Enum.GetNames(typeof(WorldMechanicType));
                    var mechValues = (WorldMechanicType[])System.Enum.GetValues(typeof(WorldMechanicType));

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
                                bool newAllowed = EditorGUILayout.ToggleLeft(mechNames[idx], allowed, GUILayout.Width(responsiveLabelWidth));
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
                    RingFlowEditorUtils.EndSectionBox();

                    db.DifficultyBands[i] = band;

                    if (i < db.DifficultyBands.Count - 1)
                        EditorGUILayout.Space(2f);
                }
            });

            EditorGUILayout.Space(8f);

            // ── Renk İlerleme Eğrisi ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbColorCurve, "Renk İlerleme Eğrisi (Color Curve)", () =>
            {
                if (db.ColorCurve == null)
                    db.ColorCurve = new System.Collections.Generic.List<ColorCurvePoint>();

                RingFlowEditorUtils.BeginSectionBox("Renk Eşik Eğrisi", "Seviye ilerledikçe havuzdaki maksimum aktif renk miktarını kontrol eden eğri basamakları.");

                for (int i = 0; i < db.ColorCurve.Count; i++)
                {
                    var pt = db.ColorCurve[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Nokta {i + 1}", GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(50f, 70f, 0.07f)));
                        EditorGUILayout.LabelField("Seviye ≥", GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(50f, 80f, 0.07f)));
                        pt.LevelThreshold = EditorGUILayout.IntField(pt.LevelThreshold, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(50f, 90f, 0.08f)));
                        EditorGUILayout.LabelField("Renkler", GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(50f, 70f, 0.07f)));
                        pt.ColorCount = EditorGUILayout.IntSlider(pt.ColorCount, 2, 10);
                        db.ColorCurve[i] = pt;
                    }
                }

                // ── Visual Color Curve Preview Bar ──
                EditorGUILayout.Space(4f);
                Rect barRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20f));
                Color barBg = new Color(0.12f, 0.14f, 0.16f, 0.5f);
                EditorGUI.DrawRect(barRect, barBg);
                RingFlowEditorUtils.DrawRectBorder(barRect, new Color(0.35f, 0.35f, 0.38f, 0.5f), 1);

                for (int i = 0; i < db.ColorCurve.Count; i++)
                {
                    var pt = db.ColorCurve[i];
                    var rect = GUILayoutUtility.GetRect(0f, 16f, GUILayout.ExpandWidth(true));
                    float normalVal = (float)(pt.ColorCount - 2) / 8f; // range 2 to 10
                    Color col = Color.HSVToRGB(normalVal * 0.7f, 0.8f, 0.8f);
                    EditorGUI.DrawRect(rect, col);
                    var textStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
                    EditorGUI.LabelField(rect, $"L{pt.LevelThreshold}: {pt.ColorCount}C", textStyle);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(6f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Yeni Nokta Ekle"))
                        db.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = 2000, ColorCount = 10 });
                    if (db.ColorCurve.Count > 1 && GUILayout.Button("- Son Noktayı Sil"))
                        db.ColorCurve.RemoveAt(db.ColorCurve.Count - 1);
                }
                RingFlowEditorUtils.EndSectionBox();
            });

            EditorGUILayout.Space(8f);

            // ── Dünyalar ve Temalar ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbWorlds, "Dünyalar ve Tema Ayarları (Worlds)", () =>
            {
                if (db.Worlds == null)
                    db.Worlds = new System.Collections.Generic.List<WorldConfigData>();

                // Search Filter
                using (new EditorGUILayout.HorizontalScope())
                {
                    _worldSearchFilter = EditorGUILayout.TextField("Dünya Ara (İsim / No)", _worldSearchFilter);
                    if (!string.IsNullOrEmpty(_worldSearchFilter) && GUILayout.Button("Temizle", GUILayout.Width(60f)))
                    {
                        _worldSearchFilter = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                if (!string.IsNullOrEmpty(_worldSearchFilter))
                {
                    RingFlowEditorUtils.BeginSectionBox("Dünya Arama Sonuçları", "Arama kriterlerine uyan dünyalar listelenmektedir.");
                    for (int i = 0; i < db.Worlds.Count; i++)
                    {
                        if (db.Worlds[i].Theme.ToLower().Contains(_worldSearchFilter.ToLower()) || 
                            (i + 1).ToString() == _worldSearchFilter)
                        {
                            if (GUILayout.Button($"Dünya {i + 1}: {db.Worlds[i].Theme}", EditorStyles.miniButton))
                            {
                                _selectedWorldIndex = i;
                                _worldSearchFilter = string.Empty;
                                GUI.FocusControl(null);
                                break;
                            }
                        }
                    }
                    RingFlowEditorUtils.EndSectionBox();
                    EditorGUILayout.Space(2f);
                }

                string[] worldNames = new string[db.Worlds.Count];
                for (int i = 0; i < db.Worlds.Count; i++)
                    worldNames[i] = $"Dünya {i + 1}: {db.Worlds[i].Theme}";

                _selectedWorldIndex = EditorGUILayout.Popup("Dünya Seç", _selectedWorldIndex, worldNames);

                if (_selectedWorldIndex >= 0 && _selectedWorldIndex < db.Worlds.Count)
                {
                    var w = db.Worlds[_selectedWorldIndex];
                    int startLevel = _selectedWorldIndex * db.LevelsPerWorld + 1;
                    int endLevel = startLevel + db.LevelsPerWorld - 1;

                    RingFlowEditorUtils.BeginSectionBox($"Dünya {_selectedWorldIndex + 1} Detayları: {w.Theme}", $"Seviye Aralığı: {startLevel} – {endLevel}");

                    var band = db.GetBandForLevel(startLevel);
                    int intensity = db.GetMechanicIntensityForLevel(startLevel);
                    EditorGUILayout.LabelField($"Zorluk Bandı: {band} | Yoğunluk: {intensity}", EditorStyles.miniLabel);

                    w.Theme = EditorGUILayout.TextField("Dünya Tema Adı", w.Theme);
                    w.UnlockedByWorldIndex = EditorGUILayout.IntField("Kilit Açma Dünya Endeksi", w.UnlockedByWorldIndex);
                    w.IsEventWorld = EditorGUILayout.Toggle("Boss (Etkinlik) Dünyası mı", w.IsEventWorld);
                    w.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup("Özel Mekanik", w.MechanicType);

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

                    RingFlowEditorUtils.EndSectionBox();
                    db.Worlds[_selectedWorldIndex] = w;
                }
            });

            EditorGUILayout.Space(8f);

            // ── Denge ve Ödül Ayarları ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbBalance, "Denge ve Ödül Ayarları (Balance Config)", () =>
            {
                RingFlowEditorUtils.BeginSectionBox("Denge ve Ödül Ayarları", "Altın, XP ödülleri, yıldız eşikleri, günlük ödüller ve reklam sıklığı.");

                EditorGUILayout.LabelField("Geri Alma (Undo) & İpucu (Hint)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.FreeUndosPerSession = EditorGUILayout.IntField("Ücretsiz Hak (Oturum)", db.BalanceConfig.FreeUndosPerSession);
                db.BalanceConfig.UndoCoinCost = EditorGUILayout.IntField("Geri Alma Altın Bedeli", db.BalanceConfig.UndoCoinCost);
                db.BalanceConfig.HintCoinCost = EditorGUILayout.IntField("İpucu Altın Bedeli", db.BalanceConfig.HintCoinCost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Seviye Tamamlama Ödülleri", EditorStyles.miniBoldLabel);
                db.BalanceConfig.NormalCoinReward = EditorGUILayout.IntField("Normal Bölüm Altın Ödülü", db.BalanceConfig.NormalCoinReward);
                db.BalanceConfig.BossCoinReward = EditorGUILayout.IntField("Boss Bölüm Altın Ödülü", db.BalanceConfig.BossCoinReward);
                db.BalanceConfig.NormalXpReward = EditorGUILayout.IntField("Normal Bölüm XP Ödülü", db.BalanceConfig.NormalXpReward);
                db.BalanceConfig.BossXpReward = EditorGUILayout.IntField("Boss Bölüm XP Ödülü", db.BalanceConfig.BossXpReward);
                db.BalanceConfig.LevelUpCoinReward = EditorGUILayout.IntField("Seviye Atlama Altın Ödülü", db.BalanceConfig.LevelUpCoinReward);
                db.BalanceConfig.LevelUpBonusDivisor = EditorGUILayout.IntField("Seviye Atlama Bonus Böleni", db.BalanceConfig.LevelUpBonusDivisor);
                db.BalanceConfig.LevelUpBonusMultiplier = EditorGUILayout.IntField("Seviye Atlama Bonus Çarpanı", db.BalanceConfig.LevelUpBonusMultiplier);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Yıldız Eşikleri (Adım Oranı %)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.ThreeStarTargetRatioPercent = EditorGUILayout.IntSlider("3-Yıldız Hedef Hamle %", db.BalanceConfig.ThreeStarTargetRatioPercent, 50, 200);
                db.BalanceConfig.TwoStarTargetRatioPercent = EditorGUILayout.IntSlider("2-Yıldız Hedef Hamle %", db.BalanceConfig.TwoStarTargetRatioPercent, 50, 300);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Sandık Değerleri (XP)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.ChestXpBronze = EditorGUILayout.IntField("Bronze Sandık XP", db.BalanceConfig.ChestXpBronze);
                db.BalanceConfig.ChestXpSilver = EditorGUILayout.IntField("Gümüş Sandık XP", db.BalanceConfig.ChestXpSilver);
                db.BalanceConfig.ChestXpGold = EditorGUILayout.IntField("Altın Sandık XP", db.BalanceConfig.ChestXpGold);
                db.BalanceConfig.ChestXpDiamond = EditorGUILayout.IntField("Elmas Sandık XP", db.BalanceConfig.ChestXpDiamond);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Sandık Kazanma İhtimalleri (Kümülatif)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.SilverChestChance = EditorGUILayout.Slider("Gümüş Sandık Şansı %", db.BalanceConfig.SilverChestChance, 0f, 1f);
                db.BalanceConfig.GoldChestChance = EditorGUILayout.Slider("Altın Sandık Şansı %", db.BalanceConfig.GoldChestChance, 0f, 1f);
                db.BalanceConfig.DiamondChestChance = EditorGUILayout.Slider("Elmas Sandık Şansı %", db.BalanceConfig.DiamondChestChance, 0f, 1f);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Oyuncu XP Eşikleri", EditorStyles.miniBoldLabel);
                db.BalanceConfig.XpThresholdLevel1 = EditorGUILayout.IntField("XP Eşiği (Seviye 1)", db.BalanceConfig.XpThresholdLevel1);
                db.BalanceConfig.XpThresholdLevel2 = EditorGUILayout.IntField("XP Eşiği (Seviye 2)", db.BalanceConfig.XpThresholdLevel2);
                db.BalanceConfig.XpThresholdLevel3 = EditorGUILayout.IntField("XP Eşiği (Seviye 3)", db.BalanceConfig.XpThresholdLevel3);
                db.BalanceConfig.XpThresholdDefault = EditorGUILayout.IntField("XP Eşiği (Varsayılan)", db.BalanceConfig.XpThresholdDefault);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Günlük Ödüller", EditorStyles.miniBoldLabel);
                db.BalanceConfig.MinClaimIntervalMinutes = EditorGUILayout.IntField("Min. Bekleme (dakika)", db.BalanceConfig.MinClaimIntervalMinutes);
                if (db.BalanceConfig.DailyRewards == null)
                    db.BalanceConfig.DailyRewards = new System.Collections.Generic.List<DailyRewardEntry>();
                for (int i = 0; i < db.BalanceConfig.DailyRewards.Count; i++)
                {
                    var reward = db.BalanceConfig.DailyRewards[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        reward.CurrencyId = EditorGUILayout.TextField("Para Birimi", reward.CurrencyId);
                        reward.Amount = EditorGUILayout.IntField("Miktar", reward.Amount);
                        if (GUILayout.Button("-", GUILayout.Width(24f)))
                        {
                            db.BalanceConfig.DailyRewards.RemoveAt(i);
                            break;
                        }
                    }
                    db.BalanceConfig.DailyRewards[i] = reward;
                }
                if (GUILayout.Button("+ Günlük Ödül Ekle"))
                    db.BalanceConfig.DailyRewards.Add(new DailyRewardEntry { CurrencyId = "Coins", Amount = 100 });

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Reklam Sıklığı", EditorStyles.miniBoldLabel);
                db.BalanceConfig.InterstitialAdInterval = EditorGUILayout.IntField("Geçiş Reklamı Sıklığı (Bölüm)", db.BalanceConfig.InterstitialAdInterval);

                RingFlowEditorUtils.EndSectionBox();
            });

            EditorGUILayout.Space(8f);

            // ── Seviye Üretim Ayarları (LevelGenConfig) ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbLevelGen, "Seviye Üretim Ayarları (LevelGenConfig)", () =>
            {
                if (db.LevelGen.SolverLimitBuckets == null)
                    db.LevelGen.SolverLimitBuckets = new System.Collections.Generic.List<SolverLimitBucket>();
                if (db.LevelGen.MechanicPriorityOrder == null)
                    db.LevelGen.MechanicPriorityOrder = new System.Collections.Generic.List<int>();

                RingFlowEditorUtils.BeginSectionBox("Seviye Üretim Parametreleri", "Algoritma karıştırma sıklığı, çözücü durum sınırları ve mekanik öncelikleri.");

                EditorGUILayout.LabelField("Üretim / Karıştırma", EditorStyles.miniBoldLabel);
                db.LevelGen.MaxScrambleAttempts = EditorGUILayout.IntField("Maks. Karıştırma Denemesi", db.LevelGen.MaxScrambleAttempts);
                db.LevelGen.ScrambleTargetBase = EditorGUILayout.IntField("Karıştırma Hedef Tabanı", db.LevelGen.ScrambleTargetBase);
                db.LevelGen.ScrambleTargetRandomRange = EditorGUILayout.IntField("Karıştırma Rastgele Aralık", db.LevelGen.ScrambleTargetRandomRange);
                db.LevelGen.MaxGenerationSeeds = EditorGUILayout.IntField("Maks. Üretim Seed", db.LevelGen.MaxGenerationSeeds);
                db.LevelGen.MaxCandidates = EditorGUILayout.IntField("Maks. Aday", db.LevelGen.MaxCandidates);
                db.LevelGen.BaseGenerationSeedMultiplier = EditorGUILayout.IntField("Seed Çarpanı", db.LevelGen.BaseGenerationSeedMultiplier);
                db.LevelGen.EmptyPolesCompactAttempts = EditorGUILayout.IntField("Boş Direk Sıkııştırma Denemesi", db.LevelGen.EmptyPolesCompactAttempts);
                db.LevelGen.ScrambleMinEmptyPolesFloor = EditorGUILayout.IntField("Min Boş Direk Tabanı", db.LevelGen.ScrambleMinEmptyPolesFloor);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Mekanik / Çözücü", EditorStyles.miniBoldLabel);
                db.LevelGen.BombCountdown = EditorGUILayout.IntField("Bomba Geri Sayım", db.LevelGen.BombCountdown);
                db.LevelGen.BombTickMode = (BombTickMode)EditorGUILayout.EnumPopup("Bomba Tick Modu", db.LevelGen.BombTickMode);
                db.LevelGen.MaxMechanicTypesPerLevel = EditorGUILayout.IntField("Seviye Başına Maks. Mekanik", db.LevelGen.MaxMechanicTypesPerLevel);
                db.LevelGen.MinSolverMoves = EditorGUILayout.IntField("Min Çözücü Hamlesi", db.LevelGen.MinSolverMoves);
                db.LevelGen.DefaultMaxMovesLimit = EditorGUILayout.IntField("Varsayılan Maks. Hamle Sınırı", db.LevelGen.DefaultMaxMovesLimit);
                db.LevelGen.MaxSolverStatesLimit = EditorGUILayout.IntField("Maks. Çözücü Durum Sınırı", db.LevelGen.MaxSolverStatesLimit);
                db.LevelGen.PoleCountClamp = EditorGUILayout.IntField("Direk Sayısı Sınırı", db.LevelGen.PoleCountClamp);
                db.LevelGen.PoleScaleCapacityDenominator = EditorGUILayout.IntField("Kapasite Böleni", db.LevelGen.PoleScaleCapacityDenominator);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Hedef Skor", EditorStyles.miniBoldLabel);
                db.LevelGen.TargetScoreBase = EditorGUILayout.FloatField("Hedef Skor Tabanı", db.LevelGen.TargetScoreBase);
                db.LevelGen.TargetScoreLevelDenominator = EditorGUILayout.FloatField("Seviye Böleni", db.LevelGen.TargetScoreLevelDenominator);
                db.LevelGen.TargetScoreMultiplier = EditorGUILayout.FloatField("Hedef Skor Çarpanı", db.LevelGen.TargetScoreMultiplier);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Çözücü Durum Kovaları (Solver Limit Buckets)", EditorStyles.miniBoldLabel);
                for (int i = 0; i < db.LevelGen.SolverLimitBuckets.Count; i++)
                {
                    var bucket = db.LevelGen.SolverLimitBuckets[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bucket.MaxColorCount = EditorGUILayout.IntField("Maks. Renk", bucket.MaxColorCount);
                        bucket.StateLimit = EditorGUILayout.IntField("Durum Sınırı", bucket.StateLimit);
                        if (GUILayout.Button("-", GUILayout.Width(24f)))
                        {
                            db.LevelGen.SolverLimitBuckets.RemoveAt(i);
                            break;
                        }
                    }
                    db.LevelGen.SolverLimitBuckets[i] = bucket;
                }
                if (GUILayout.Button("+ Kova Ekle"))
                    db.LevelGen.SolverLimitBuckets.Add(new SolverLimitBucket { MaxColorCount = 99, StateLimit = 8000 });

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Mekanik Öncelik Sırası (RingType Değerleri)", EditorStyles.miniBoldLabel);
                for (int i = 0; i < db.LevelGen.MechanicPriorityOrder.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        db.LevelGen.MechanicPriorityOrder[i] = EditorGUILayout.IntField($"Öncelik {i + 1}", db.LevelGen.MechanicPriorityOrder[i]);
                        if (GUILayout.Button("-", GUILayout.Width(24f)))
                        {
                            db.LevelGen.MechanicPriorityOrder.RemoveAt(i);
                            break;
                        }
                    }
                }
                if (GUILayout.Button("+ Öncelik Ekle"))
                    db.LevelGen.MechanicPriorityOrder.Add(0);

                RingFlowEditorUtils.EndSectionBox();
            });

            EditorGUILayout.Space(12f);

            // ── Hızlı Seviye Sorgulama ve Önizleme ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbLookup, "Hızlı Seviye Sorgulama ve Tema Önizleme", () =>
            {
                RingFlowEditorUtils.BeginSectionBox("Hızlı Seviye Sorgulama ve Tema Önizleme", "Seçilen seviyenin tüm tema, zorluk ve mekanik bilgilerini sorgulayın.");

                _lookupLevelIndex = EditorGUILayout.IntField("Sorgulanacak Seviye", _lookupLevelIndex);
                if (_lookupLevelIndex < 1 || _lookupLevelIndex > db.TotalLevels)
                {
                    EditorGUILayout.HelpBox(
                        $"Seviye {_lookupLevelIndex} geçersiz. Lütfen 1 ile {db.TotalLevels} arasında bir seviye giriniz.",
                        MessageType.Error);
                    RingFlowEditorUtils.EndSectionBox();
                    return;
                }

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

                RingFlowEditorUtils.EndSectionBox();
            });

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
