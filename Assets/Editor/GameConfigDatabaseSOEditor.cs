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

            // ── Tutarlılık Kontrolü — data-driven, hardcode 2000/40/50 yok ──
            // Kural: TotalLevels == TotalWorlds * LevelsPerWorld ve Worlds.Count == TotalWorlds
            // GDD §51 hedefi 2000 level ama MVP (100 level, 2 dünya) da geçerlidir.
            {
                int expectedTotal = db.TotalWorlds * db.LevelsPerWorld;
                bool totalMismatch = db.TotalLevels != expectedTotal && expectedTotal > 0;
                bool worldCountMismatch = db.Worlds.Count != db.TotalWorlds;

                if (totalMismatch || worldCountMismatch)
                {
                    RingFlowEditorUtils.BeginSectionBox("⚠ Veritabanı Tutarsızlığı",
                        "TotalLevels, TotalWorlds ve LevelsPerWorld değerleri birbiriyle uyuşmuyor.");
                    if (totalMismatch)
                        EditorGUILayout.LabelField(
                            $"• TotalLevels={db.TotalLevels} ≠ TotalWorlds({db.TotalWorlds}) × LevelsPerWorld({db.LevelsPerWorld}) = {expectedTotal}.",
                            EditorStyles.miniLabel);
                    if (worldCountMismatch)
                        EditorGUILayout.LabelField(
                            $"• Worlds.Count={db.Worlds.Count} ≠ TotalWorlds={db.TotalWorlds}.",
                            EditorStyles.miniLabel);
                    EditorGUILayout.HelpBox(
                        "'Varsayılan GDD Ayarlarına Sıfırla' butonu ile tutarlı bir başlangıç yapılandırması oluşturabilirsiniz.",
                        MessageType.Info);
                    RingFlowEditorUtils.EndSectionBox();
                    EditorGUILayout.Space(4f);
                }
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUI.BeginChangeCheck();

            // ── Genel Ayarlar ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbGeneral, "Genel Seviye Ayarları", () =>
            {
                RingFlowEditorUtils.BeginSectionBox("Genel Seviye Ayarları", "Oyun genelindeki seviye miktarları ve temel kurallar.");

                EditorGUILayout.HelpBox(
                    "Bu bölüm oyun genelindeki seviye limitlerini ve temel kuralları tanımlar.\n" +
                    "• Toplam Seviye Sayısı: Oyunda bulunacak toplam bölüm adedi.\n" +
                    "• Tema Adımı: Kaç seviyede bir arka plan veya görsel tema stilinin değişeceği.\n" +
                    "• Minimum Boş Direk: Bulmaca oluşturulurken her seviyede bulunması gereken en az boş direk adedi (Tasarımcının serbestçe halka taşıyabilmesi için gereklidir).",
                    MessageType.Info);

                EditorGUI.BeginChangeCheck();
                int newTotalLevels = EditorGUILayout.IntField(new GUIContent("Toplam Seviye Sayısı", "Oyundaki toplam seviye adedi. Değiştiğinde tüm zorluk ve dünyalar yeniden ölçeklenebilir."), db.TotalLevels);
                if (EditorGUI.EndChangeCheck() && newTotalLevels != db.TotalLevels && newTotalLevels > 0)
                {
                    db.TotalLevels = newTotalLevels;
                    // Otomatik ölçekleme: TotalLevels değişince kullanıcıya tek tıkla
                    // tüm eğrileri yeniden oluşturma seçeneği sun.
                    if (EditorUtility.DisplayDialog("Seviye Sayısı Değişti",
                        $"TotalLevels {newTotalLevels} olarak ayarlandı.\n\n" +
                        "DifficultyBands, ColorCurve ve Worlds listeleri yeni değere göre otomatik ölçeklensin mi?\n\n" +
                        "(Evet = InitializeDefaults ile yeniden oluştur, Hayır = mevcut listeyi koru)",
                        "Evet, Otomatik Ölçekle", "Hayır, Sadece Değeri Kaydet"))
                    {
                        db.InitializeDefaults();
                    }
                    EditorUtility.SetDirty(db);
                }

                db.LevelsPerThemeStep = EditorGUILayout.IntField(new GUIContent("Tema Başına Seviye Adımı", "Görsel veya mekanik temaların kaç seviyede bir güncelleneceğini belirler."), db.LevelsPerThemeStep);
                // Boş direk sayıları her DifficultyBand'da tanımlıdır (GetMinEmptyPolesForLevel).
                // Hızlı ön ayar butonları — hardcode değil, InitializeDefaults kullanır
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Hızlı Ön Ayar", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("100 Level (MVP)", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(db, "Preset 100 Level");
                        db.TotalLevels = 100;
                        db.InitializeDefaults();
                        EditorUtility.SetDirty(db);
                    }
                    if (GUILayout.Button("500 Level", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(db, "Preset 500 Level");
                        db.TotalLevels = 500;
                        db.InitializeDefaults();
                        EditorUtility.SetDirty(db);
                    }
                    if (GUILayout.Button("2000 Level (GDD Tam)", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(db, "Preset 2000 Level");
                        db.TotalLevels = 2000;
                        db.InitializeDefaults();
                        EditorUtility.SetDirty(db);
                    }
                }

                RingFlowEditorUtils.EndSectionBox();
            });

            EditorGUILayout.Space(8f);

            // ── Zorluk Dereceleri (Bands) ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDbBands, "Zorluk Dereceleri (Difficulty Bands)", () =>
            {
                if (db.DifficultyBands == null) db.DifficultyBands = new System.Collections.Generic.List<DifficultyBandData>();

                EditorGUILayout.HelpBox(
                    "Difficulty Bands (Zorluk Dereceleri), oyunun bölümlerini kolaydan zora (Easy, Medium, Hard, Master, vb.) sınıflandırır.\n" +
                    "• Maks. Seviye: Bu zorluk grubunun geçerli olacağı en yüksek seviye numarası.\n" +
                    "• Min. Boş Direk: Bulmaca üretilirken bu band için hedeflenen minimum boş direk sayısı.\n" +
                    "• Maks. Kapasite: Her bir direğin tutabileceği maksimum halka sayısı (Genelde 4 halkadır).\n" +
                    "• İzin Verilen Mekanikler: Bu zorluk derecesinde karşılaşılabilecek özel halka tipleri (Mystery, Frozen, Locked, vb.).",
                    MessageType.Info);

                for (int i = 0; i < db.DifficultyBands.Count; i++)
                {
                    var band = db.DifficultyBands[i];
                    RingFlowEditorUtils.BeginSectionBox(band.Band.ToString(), "Zorluk bandında geçerli parametreler ve izin verilen özel halka mekanikleri.");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        band.MaxLevel = EditorGUILayout.IntField(new GUIContent("Maks. Seviye", "Bu zorluk grubunun geçerli olacağı son seviye."), band.MaxLevel, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(110f, 160f, 0.14f)));
                        band.MinEmptyPoles = EditorGUILayout.IntField(new GUIContent("Min. Boş Direk", "Bu gruptaki seviyelerde bulunacak en az boş direk adedi."), band.MinEmptyPoles, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(100f, 150f, 0.13f)));
                        band.MaxCapacity = EditorGUILayout.IntField(new GUIContent("Maks. Kapasite", "Direklerin halka kapasitesi (Varsayılan: 4)."), band.MaxCapacity, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(100f, 150f, 0.13f)));
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

                EditorGUILayout.HelpBox(
                    "Renk İlerleme Eğrisi, hangi seviyeden itibaren oyuna kaç farklı renk dahil edileceğini (ve dolayısıyla zorluğun nasıl artacağını) belirler.\n" +
                    "• Seviye ≥: Bu kuralın geçerli olacağı en düşük seviye eşiği.\n" +
                    "• Renkler: O seviyeden itibaren kullanılacak toplam renk çeşidi sayısı (En az 2, en fazla 10 renk).",
                    MessageType.Info);

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
                    // FIX-E1: Default threshold = TotalLevels (not hardcode 2000)
                    int newThreshold = db.TotalLevels > 0 ? db.TotalLevels : 100;
                    if (GUILayout.Button("+ Yeni Nokta Ekle"))
                        db.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = newThreshold, ColorCount = 10 });
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

                    EditorGUILayout.HelpBox(
                        "Dünya Ayarları, oyundaki bölge bazlı temaları ve özel mekanikleri yapılandırır.\n" +
                        "• Dünya Tema Adı: Dünyanın görsel arka planı/teması (örneğin Forest, Space, Desert).\n" +
                        "• Kilit Açma Dünya Endeksi: Bu dünyaya geçebilmek için tamamlanması gereken önceki dünya numarası (0-indexed).\n" +
                        "• Boss (Etkinlik) Dünyası: Özel zaman kısıtlamalı veya zorlu bir etkinlik alanı olup olmadığı.\n" +
                        "• Özel Mekanik: Bu dünyadaki seviyelerde üretilecek baskın özel halka mekaniği (Örn: Frozen, Magnet, Paint).",
                        MessageType.Info);

                    var band = db.GetBandForLevel(startLevel);
                    int intensity = db.GetMechanicIntensityForLevel(startLevel);
                    EditorGUILayout.LabelField($"Zorluk Bandı: {band} | Yoğunluk: {intensity}", EditorStyles.miniLabel);

                    w.Theme = EditorGUILayout.TextField(new GUIContent("Dünya Tema Adı", "Dünyanın tematik ismi."), w.Theme);
                    w.UnlockedByWorldIndex = EditorGUILayout.IntField(new GUIContent("Kilit Açma Dünya Endeksi", "Bu dünyayı açmak için tamamlanması gereken dünya index'i."), w.UnlockedByWorldIndex);
                    w.IsEventWorld = EditorGUILayout.Toggle(new GUIContent("Boss (Etkinlik) Dünyası mı", "Eğer boss dünyası ise zorlu kurallar geçerli olur."), w.IsEventWorld);
                    w.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup(new GUIContent("Özel Mekanik", "Bu dünyadaki seviyelerde ağırlıklı olarak kullanılacak mekanik türü."), w.MechanicType);

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

                EditorGUILayout.HelpBox(
                    "Denge ve Ödül Ayarları, oyun ekonomisini, oyuncunun seviye atlama hızını ve reklam sıklığını düzenler.\n" +
                    "• Geri Alma & İpucu Bedelleri: Hamleyi geri almak (Undo) veya ipucu (Hint) istemek için harcanacak altın bedeli.\n" +
                    "• Seviye Ödülleri: Normal ve Boss seviyelerini geçince verilecek altın ve XP (Tecrübe Puanı) değerleri.\n" +
                    "• Yıldız Limitleri (%): Oyuncunun 3 veya 2 yıldız kazanabilmesi için çözücü hedefine göre yapabileceği maksimum hamle tolerans oranı (Örn: %120 yaparsanız, 10 hamlelik bölüme 12 hamleye kadar 3 yıldız verilir).",
                    MessageType.Info);

                EditorGUILayout.LabelField("Geri Alma (Undo) & İpucu (Hint)", EditorStyles.miniBoldLabel);
                db.BalanceConfig.FreeUndosPerSession = EditorGUILayout.IntField(new GUIContent("Ücretsiz Hak (Oturum)", "Her oyun oturumunda oyuncuya verilecek ücretsiz geri alma hakkı."), db.BalanceConfig.FreeUndosPerSession);
                db.BalanceConfig.UndoCoinCost = EditorGUILayout.IntField(new GUIContent("Geri Alma Altın Bedeli", "Geri alma hakkı tükendiğinde bir geri alma işleminin altın maliyeti."), db.BalanceConfig.UndoCoinCost);
                db.BalanceConfig.HintCoinCost = EditorGUILayout.IntField(new GUIContent("İpucu Altın Bedeli", "Bir seviyede ipucu istemenin altın maliyeti."), db.BalanceConfig.HintCoinCost);

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

                EditorGUILayout.HelpBox(
                    "Seviye Üretim Ayarları, bölümler otomatik üretilirken (Generator) arka planda çalışan yapay zekanın (Solver) sınırlarını belirler.\n" +
                    "• Maks. Karıştırma Denemesi: Bir seviyeyi çözülebilir kılana kadar algoritmanın deneyeceği maksimum karıştırma (Scramble) sayısı.\n" +
                    "• Maks. Çözücü Durum Sınırı: Çözücünün Unity editörünü dondurmaması için arama yapabileceği maksimum durum sayısı (Search Space limit).\n" +
                    "• Min Çözücü Hamlesi: Otomatik üretilen bir seviyenin kabul edilmesi için gereken en az optimal hamle limiti (Çok basit seviyeleri filtrelemek için kullanılır).",
                    MessageType.Info);

                EditorGUILayout.LabelField("Üretim / Karıştırma", EditorStyles.miniBoldLabel);
                db.LevelGen.MaxScrambleAttempts = EditorGUILayout.IntField(new GUIContent("Maks. Karıştırma Denemesi", "Algoritmanın çözülebilir seviye üretmek için yapacağı maksimum deneme adedi."), db.LevelGen.MaxScrambleAttempts);
                db.LevelGen.ScrambleTargetBase = EditorGUILayout.IntField(new GUIContent("Karıştırma Hedef Tabanı", "Halkaların karıştırılma yoğunluğu taban değeri."), db.LevelGen.ScrambleTargetBase);
                db.LevelGen.ScrambleTargetRandomRange = EditorGUILayout.IntField(new GUIContent("Karıştırma Rastgele Aralık", "Karıştırma yoğunluğuna eklenecek rastgele dalgalanma aralığı."), db.LevelGen.ScrambleTargetRandomRange);
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
