using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    public sealed class DatabaseSection : EditorSection
    {
        public override string DisplayName => "Oyun Ayarları Veritabanı";
        public override string PrefKey => EditorPrefsKeys.FoldDatabase;

        private GameConfigDatabaseSO _database;
        private int _selectedWorldIndex;

        private struct BatchValidationResult
        {
            public int LevelIndex;
            public bool Success;
            public int MoveCount;
            public float TimeMs;
            public string Log;
        }

        private Vector2 _scrollPos;
        private int _valStartLevel = 1;
        private int _valEndLevel = 50;
        private bool _fixTargetsEnabled;
        private List<BatchValidationResult> _validationResults = new();

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            if (_database == null)
                _database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(DatabaseAssetPath);

            if (_database == null)
            {
                EditorGUILayout.HelpBox(
                    "Resources klasöründe GameConfigDatabase asset dosyası bulunamadı. " +
                    "Çalışma zamanında varsayılan ayarlar kullanılır, ancak kalıcı değişiklik yapmak için asset dosyasını oluşturmalısınız.",
                    MessageType.Warning);

                if (GUILayout.Button("GameConfigDatabase Dosyası Oluştur", GUILayout.Height(36)))
                    CreateDatabaseAsset();
                return;
            }

            RingFlowEditorUtils.BeginSectionBox("Veritabanı Yapılandırması", "Oyun veritabanındaki zorluk derecelerini, renk eğrilerini ve dünyaları yönetin.");

            EditorGUILayout.ObjectField("Asset Dosyası", _database, typeof(GameConfigDatabaseSO), false);
            EditorGUILayout.Space(2f);

            Undo.RecordObject(_database, "Edit GameConfigDatabase");

            EditorGUI.BeginChangeCheck();

            // RESP-1: Responsive split — compact (narrow) vs side-by-side (wide)
            bool narrow = RingFlowEditorUtils.IsNarrowWidth(680f);

            // Hızlı ön ayar butonları — tek tıkla tüm eğriler ölçeklenir
            EditorGUILayout.LabelField("Hızlı Kapsam Seçimi", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("100 Level\n(MVP)", GUILayout.Height(36)))
                {
                    if (EditorUtility.DisplayDialog("Hızlı Ön Ayar — 100 Level",
                        "TotalLevels=100 olarak ayarlanıp tüm eğriler otomatik ölçeklenecek.\n(DifficultyBands, ColorCurve, Worlds yeniden oluşturulacak)", "Uygula", "İptal"))
                    {
                        Undo.RecordObject(_database, "Preset 100 Level");
                        _database.TotalLevels = 100;
                        _database.InitializeDefaults();
                        EditorUtility.SetDirty(_database);
                        AssetDatabase.SaveAssets();
                    }
                }
                if (GUILayout.Button("500 Level\n(Beta)", GUILayout.Height(36)))
                {
                    if (EditorUtility.DisplayDialog("Hızlı Ön Ayar — 500 Level",
                        "TotalLevels=500 olarak ayarlanıp tüm eğriler otomatik ölçeklenecek.", "Uygula", "İptal"))
                    {
                        Undo.RecordObject(_database, "Preset 500 Level");
                        _database.TotalLevels = 500;
                        _database.InitializeDefaults();
                        EditorUtility.SetDirty(_database);
                        AssetDatabase.SaveAssets();
                    }
                }
                if (GUILayout.Button("2000 Level\n(GDD Tam)", GUILayout.Height(36)))
                {
                    if (EditorUtility.DisplayDialog("Hızlı Ön Ayar — 2000 Level (GDD)",
                        "TotalLevels=2000 olarak ayarlanıp tüm eğriler GDD tam kapsamına göre ölçeklenecek.", "Uygula", "İptal"))
                    {
                        Undo.RecordObject(_database, "Preset 2000 Level");
                        _database.TotalLevels = 2000;
                        _database.InitializeDefaults();
                        EditorUtility.SetDirty(_database);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            EditorGUILayout.Space(6f);

            if (narrow)
            {
                _database.TotalLevels        = EditorGUILayout.IntField("Toplam Seviye Sayısı", _database.TotalLevels);
                _database.LevelsPerThemeStep = EditorGUILayout.IntField("Tema Başına Seviye Adımı", _database.LevelsPerThemeStep);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _database.TotalLevels        = EditorGUILayout.IntField("Toplam Seviye", _database.TotalLevels, GUILayout.Width(260f));
                    _database.LevelsPerThemeStep = EditorGUILayout.IntField("Tema Adımı", _database.LevelsPerThemeStep, GUILayout.Width(200f));
                }
            }

            // Live consistency hint (data-driven, no hardcode)
            int computedTotal = _database.TotalWorlds * _database.LevelsPerWorld;
            if (_database.TotalWorlds > 0 && _database.LevelsPerWorld > 0 && _database.TotalLevels != computedTotal)
            {
                EditorGUILayout.HelpBox(
                    $"TotalLevels ({_database.TotalLevels}) ≠ TotalWorlds ({_database.TotalWorlds}) × LevelsPerWorld ({_database.LevelsPerWorld}) = {computedTotal}. " +
                    "'Hızlı Ön Ayar' butonları veya InitializeDefaults ile senkronize edebilirsiniz.",
                    MessageType.Warning);
            }

            // DifficultyBands overlap / gap validation (FIX-DB1)
            var bandIssues = _database.ValidateDifficultyBands();
            if (bandIssues.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "⚠ DifficultyBands tutarsızlığı:\n" + string.Join("\n", bandIssues),
                    MessageType.Warning);
            }

            // ColorCurve monotonicity validation (FIX-DB2)
            var curveIssues = _database.ValidateColorCurve();
            if (curveIssues.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "⚠ ColorCurve tutarsızlığı:\n" + string.Join("\n", curveIssues),
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10f);

            // --- 1. ZORLUK DERECELERİ (responsive inline editable) ---
            RingFlowEditorUtils.BeginSectionBox("Zorluk Dereceleri Ayarları",
                "Band sınırları TotalLevels'e göre ayarlı. Maks Seviye = TotalLevels'in yüzdesi.");
            if (_database.DifficultyBands == null || _database.DifficultyBands.Count == 0)
            {
                EditorGUILayout.HelpBox("DifficultyBands boş. InitializeDefaults ile varsayılanları oluşturun.", MessageType.Warning);
            }
            else
            {
                // Header row
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Band", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
                    if (!narrow)
                    {
                        EditorGUILayout.LabelField("Maks Seviye", EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                        EditorGUILayout.LabelField("Boş Direk", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
                        EditorGUILayout.LabelField("Kapasite", EditorStyles.miniBoldLabel, GUILayout.Width(80f));
                        EditorGUILayout.LabelField("Yoğunluk", EditorStyles.miniBoldLabel, GUILayout.Width(80f));
                        EditorGUILayout.LabelField("% Toplam", EditorStyles.miniBoldLabel, GUILayout.MinWidth(60f));
                    }
                }

                for (int i = 0; i < _database.DifficultyBands.Count; i++)
                {
                    var bandData = _database.DifficultyBands[i];
                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    EditorGUILayout.LabelField(bandData.Band.ToString(), EditorStyles.boldLabel, GUILayout.Width(70f));

                    if (narrow)
                    {
                        // Compact: stacked fields
                        EditorGUILayout.EndHorizontal();
                        bandData.MaxLevel      = EditorGUILayout.IntField("  Maks Seviye", bandData.MaxLevel);
                        bandData.MinEmptyPoles = EditorGUILayout.IntField("  Boş Direk", bandData.MinEmptyPoles);
                        bandData.MaxCapacity   = EditorGUILayout.IntField("  Kapasite", bandData.MaxCapacity);
                    }
                    else
                    {
                        bandData.MaxLevel      = EditorGUILayout.IntField(bandData.MaxLevel, GUILayout.Width(110f));
                        bandData.MinEmptyPoles = EditorGUILayout.IntField(bandData.MinEmptyPoles, GUILayout.Width(90f));
                        bandData.MaxCapacity   = EditorGUILayout.IntField(bandData.MaxCapacity, GUILayout.Width(80f));
                        bandData.MechanicIntensity = EditorGUILayout.IntSlider(bandData.MechanicIntensity, 1, 5, GUILayout.Width(80f));
                        // DATA-2: Live % indicator — no hardcode denominator, uses TotalLevels
                        float pct = _database.TotalLevels > 0 ? bandData.MaxLevel * 100f / _database.TotalLevels : 0f;
                        var prevColor = GUI.color;
                        GUI.color = pct > 100f ? EditorPaths.EditorColors.Warning : EditorPaths.EditorColors.Info;
                        EditorGUILayout.LabelField($"{pct:F0}%", EditorStyles.miniLabel, GUILayout.MinWidth(60f));
                        GUI.color = prevColor;
                        EditorGUILayout.EndHorizontal();
                    }

                    _database.DifficultyBands[i] = bandData;
                    EditorGUILayout.Space(2f);
                }
            }
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(6f);

            // --- 2. RENK İLERLEME EĞRİSİ (responsive inline editable) ---
            RingFlowEditorUtils.BeginSectionBox("Renk İlerleme Eğrisi",
                "Her eşikten itibaren kullanılacak renk sayısı. Eşikler TotalLevels'e göre ölçeklenir.");

            if (_database.ColorCurve == null || _database.ColorCurve.Count == 0)
            {
                EditorGUILayout.HelpBox("ColorCurve boş. InitializeDefaults ile varsayılanları oluşturun.", MessageType.Warning);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Seviye ≥", EditorStyles.miniBoldLabel, GUILayout.Width(narrow ? 80f : 100f));
                    EditorGUILayout.LabelField("Renk Sayısı", EditorStyles.miniBoldLabel, GUILayout.Width(narrow ? 80f : 100f));
                    if (!narrow) EditorGUILayout.LabelField("Görsel", EditorStyles.miniBoldLabel, GUILayout.MinWidth(80f));
                }

                for (int i = 0; i < _database.ColorCurve.Count; i++)
                {
                    var pt = _database.ColorCurve[i];
                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    pt.LevelThreshold = EditorGUILayout.IntField(pt.LevelThreshold, GUILayout.Width(narrow ? 80f : 100f));
                    pt.ColorCount     = EditorGUILayout.IntSlider(pt.ColorCount, 2, 10, GUILayout.Width(narrow ? 120f : 160f));

                    // DATA-2: Color bar width proportional to ColorCount/10 — no hardcode max
                    if (!narrow)
                    {
                        float barFill = pt.ColorCount / 10f;
                        Rect barOuter = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.MinWidth(80f), GUILayout.Height(16f));
                        EditorGUI.DrawRect(barOuter, new Color(0.15f, 0.15f, 0.2f));
                        Rect barInner = new(barOuter.x, barOuter.y, barOuter.width * barFill, barOuter.height);
                        EditorGUI.DrawRect(barInner, Color.Lerp(EditorPaths.EditorColors.Success, EditorPaths.EditorColors.Warning, barFill));
                        EditorGUI.LabelField(barOuter, $"{pt.ColorCount} renk", EditorStyles.centeredGreyMiniLabel);
                    }

                    _database.ColorCurve[i] = pt;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2f);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                // DATA-2: Default threshold uses TotalLevels, not hardcode 2000
                int defaultThreshold = _database.TotalLevels > 0 ? _database.TotalLevels : 100;
                if (GUILayout.Button("+ Yeni Nokta Ekle", GUILayout.Width(140f)))
                    _database.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = defaultThreshold, ColorCount = 10 });

                if (_database.ColorCurve != null && _database.ColorCurve.Count > 0 &&
                    GUILayout.Button("- Son Noktayı Sil", GUILayout.Width(140f)))
                    _database.ColorCurve.RemoveAt(_database.ColorCurve.Count - 1);
            }
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(6f);

            // --- 3. DÜNYA VE TEMA AYARLARI (inline editable) ---
            RingFlowEditorUtils.BeginSectionBox("Dünyalar ve Tema Ayarları", "Dünyaların tema isimleri ve mekanik konfigürasyonları.");
            int worldCount = _database.Worlds.Count;
            string[] worldNames = new string[worldCount];
            for (int i = 0; i < worldCount; i++)
                worldNames[i] = $"Dünya {i + 1}: {_database.Worlds[i].Theme}";

            _selectedWorldIndex = EditorGUILayout.Popup("Düzenlenecek Dünyayı Seç", _selectedWorldIndex, worldNames);

            EditorGUILayout.Space(5f);

            if (_selectedWorldIndex >= 0 && _selectedWorldIndex < worldCount)
            {
                var wData = _database.Worlds[_selectedWorldIndex];

                wData.Theme = EditorGUILayout.TextField("Tema Görünen Adı", wData.Theme);
                wData.UnlockedByWorldIndex = EditorGUILayout.IntField("Kilit Açan Dünya Endeksi", wData.UnlockedByWorldIndex);
                wData.IsEventWorld = EditorGUILayout.Toggle("Boss (Etkinlik) Dünyası mı", wData.IsEventWorld);
                wData.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup("Özel Mekanik Tipi", wData.MechanicType);

                _database.Worlds[_selectedWorldIndex] = wData;
            }
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(10f);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_database);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Değişiklikleri Kaydet", GUILayout.Height(36)))
                    SaveDatabase();

                if (GUILayout.Button("Varsayılan Ayarlara Sıfırla", GUILayout.Height(36)))
                {
                    if (EditorUtility.DisplayDialog("Veritabanını Sıfırla",
                        "Tüm veritabanı ayarlarını varsayılan GDD kurallarına sıfırlamak istediğinize emin misiniz? Özel ayarlarınızın üzerine yazılacaktır.",
                        "Sıfırla", "İptal"))
                    {
                        _database.InitializeDefaults();
                        EditorUtility.SetDirty(_database);
                        SaveDatabase();
                    }
                }
            }

            // --- 4. TOPLU SEVİYE DOĞRULAYICI ---
            EditorGUILayout.Space(15f);
            RingFlowEditorUtils.BeginSectionBox("Toplu Seviye Doğrulayıcı ve Çözücü Testi", "Seçilen seviye aralığındaki bölümleri tek tek üretir, çözücü ile doğrular, süre ve çözülebilirlik raporu sunar.");

            using (new EditorGUILayout.HorizontalScope())
            {
                _valStartLevel = EditorGUILayout.IntField("Başlangıç Seviyesi", _valStartLevel, GUILayout.Width(180f));
                _valEndLevel = EditorGUILayout.IntField("Bitiş Seviyesi", _valEndLevel, GUILayout.Width(180f));
            }

            _fixTargetsEnabled = EditorGUILayout.ToggleLeft("TargetMoves'u kaydet", _fixTargetsEnabled);

            if (GUILayout.Button("Toplu Doğrulamayı Başlat", GUILayout.Height(30)))
                RunBatchValidation();

            if (_validationResults.Count > 0)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField($"Sonuçlar ({_validationResults.Count})", EditorStyles.boldLabel);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200f));
                var resultLabelStyle = new GUIStyle(EditorStyles.label);
                for (int i = 0; i < _validationResults.Count; i++)
                {
                    var res = _validationResults[i];
                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20f));
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    resultLabelStyle.fontStyle = FontStyle.Bold;
                    resultLabelStyle.normal.textColor = res.Success ? EditorPaths.EditorColors.Success : EditorPaths.EditorColors.Error;
                    EditorGUILayout.LabelField($"Lvl {res.LevelIndex}", resultLabelStyle, GUILayout.Width(60f));
                    EditorGUILayout.LabelField(res.Log);

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(1f);
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Sonuçları Temizle", GUILayout.Width(140f)))
                    _validationResults.Clear();
            }

            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.EndSectionBox();
        }

        private void RunBatchValidation()
        {
            _validationResults.Clear();

            int solvedCount = 0;
            int fixedCount = 0;
            float totalTime = 0;
            int totalMoves = 0;

            if (_database == null)
                _database = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (_database == null)
            {
                NexusLog.Error("DatabaseSection", "ValidateAllLevels", "LoadDatabase", "[DatabaseSection] GameConfigDatabase not loaded!");
                return;
            }

            for (int i = _valStartLevel; i <= _valEndLevel; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                int poleCount = _database.GetPoleCountForLevel(i);
                int colorCount = _database.GetColorCountForLevel(i);
                int maxCap = _database.GetMaxCapacityForLevel(i);
                int minEmptyPoles = _database.GetMinEmptyPolesForLevel(i);

                // EDIT-1: Validate DB consistency before generating — same guard as InitLevelCommand.
                if (poleCount < colorCount + minEmptyPoles)
                {
                    var res2 = new BatchValidationResult
                    {
                        LevelIndex = i,
                        Success = false,
                        Log = $"HATA — pole sayısı ({poleCount}) < renkler ({colorCount}) + boş direk ({minEmptyPoles}). DB ColorCurve veya DifficultyBands güncellenmeli."
                    };
                    _validationResults.Add(res2);
                    NexusLog.Error("DatabaseSection", "RunBatchValidation", i.ToString(),
                        $"[EDIT-1] Level {i}: poleCount={poleCount} < colorCount({colorCount}) + minEmpty({minEmptyPoles}). Skipping generation.");
                    EditorUtility.DisplayProgressBar("Seviye Doğrulanıyor",
                        $"Seviye {i} / {_valEndLevel} doğrulanıyor...",
                        (float)(i - _valStartLevel + 1) / (_valEndLevel - _valStartLevel + 1));
                    continue;
                }

                // LOG-2: Use LevelGenerator.GetDeterministicSeed() instead of hardcoded i*12345.
                // The hardcoded formula was inconsistent with the runtime path (InitLevelCommand
                // uses BaseGenerationSeedMultiplier from DB, not 12345). Using GetDeterministicSeed
                // ensures the editor validation tests exactly the same seed that runtime would use
                // for a cold-start (no pre-existing LevelDataSO asset) generation.
                int seed = LevelGenerator.GetDeterministicSeed(i);
                NexusLog.Info("DatabaseSection", "RunBatchValidation", i.ToString(),
                    $"Validating level {i}: poles={poleCount}, colors={colorCount}, cap={maxCap}, minEmpty={minEmptyPoles}, seed={seed}.");
                var levelData = LevelGenerator.GenerateLevel(_database, i, seed, poleCount, colorCount, maxCap);

                stopwatch.Stop();
                float timeMs = (float)stopwatch.Elapsed.TotalMilliseconds;

                var res = new BatchValidationResult { LevelIndex = i };
                if (levelData == null)
                {
                    res.Success = false;
                    res.Log = "BAŞARISIZ - Jeneratör null döndü (çözülemez tohum / kilit uyuşmazlığı)";
                }
                else
                {
                    var board = new BoardState { PoleCount = levelData.Poles.Count, MaxCapacity = maxCap };
                    for (int p = 0; p < levelData.Poles.Count; p++)
                    {
                        board.SetPoleLocked(p, levelData.Poles[p].IsLocked);
                        board.SetRingCount(p, levelData.Poles[p].Rings.Count);
                        for (int r = 0; r < levelData.Poles[p].Rings.Count; r++)
                        {
                            board.SetRingColor(p, r, levelData.Poles[p].Rings[r].Color);
                            board.SetRingType(p, r, levelData.Poles[p].Rings[r].Type);
                            board.SetRingAdditional(p, r, levelData.Poles[p].Rings[r].AdditionalData);
                        }
                    }

                    int solverLimit = GetSolverLimitFromBuckets(colorCount, _database.LevelGen.SolverLimitBuckets);
                    var solveResult = LevelSolver.Solve(board, maxCap, maxStatesLimit: solverLimit);
                    if (solveResult.IsSolvable)
                    {
                        res.Success = true;
                        res.MoveCount = solveResult.MoveCount;
                        res.TimeMs = timeMs;
                        res.Log = $"BAŞARILI - {res.MoveCount} hamlede çözüldü (Süre: {timeMs:F1}ms)";
                        solvedCount++;
                        totalTime += timeMs;
                        totalMoves += res.MoveCount;

                        if (_fixTargetsEnabled)
                        {
                            var assetPath = $"Assets/Resources/Levels/Level_{i}.asset";
                            var levelSO = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
                            if (levelSO != null)
                            {
                                Undo.RecordObject(levelSO, "TargetMoves Düzelt");
                                levelSO.Data.TargetMoves = solveResult.MoveCount;
                                var portalTargets = new int[levelSO.Data.Poles.Count];
                                for (int p = 0; p < levelSO.Data.Poles.Count; p++)
                                    portalTargets[p] = levelSO.Data.Poles[p].PortalTargetId;
                                LevelGenerator.PopulateGddMetadata(levelSO.Data, _database, board, solveResult.MoveCount, maxCap, portalTargets);
                                EditorUtility.SetDirty(levelSO);
                                fixedCount++;
                            }
                        }
                    }
                    else
                    {
                        res.Success = false;
                        res.Log = "BAŞARISIZ - Çözücü belirtilen sınırda çözüme ulaşamadı";
                    }
                }

                _validationResults.Add(res);
                EditorUtility.DisplayProgressBar("Seviye Doğrulanıyor",
                    $"Seviye {i} / {_valEndLevel} doğrulanıyor...",
                    (float)(i - _valStartLevel + 1) / (_valEndLevel - _valStartLevel + 1));
            }
            EditorUtility.ClearProgressBar();

            string summaryMsg = $"Doğrulanan Seviye: {_validationResults.Count}\n" +
                                $"Başarılı: {solvedCount} / {_validationResults.Count}\n" +
                                $"Düzeltilecek TargetMoves: {fixedCount}\n" +
                                $"Ort. Süre: {(solvedCount > 0 ? (totalTime / solvedCount) : 0):F1}ms\n" +
                                $"Ort. Hamle: {(solvedCount > 0 ? (totalMoves / (float)solvedCount) : 0):F1}";
            EditorUtility.DisplayDialog("Doğrulama Tamamlandı", summaryMsg, "Tamam");
        }

        private static int GetSolverLimitFromBuckets(int colorCount, List<SolverLimitBucket> buckets)
        {
            for (int i = 0; i < buckets.Count; i++)
            {
                if (colorCount <= buckets[i].MaxColorCount)
                    return buckets[i].StateLimit;
            }
            return buckets[^1].StateLimit;
        }

        private const string DatabaseAssetPath = EditorPaths.GameConfigDbPath;

        private void CreateDatabaseAsset()
        {
            var db = ScriptableObject.CreateInstance<GameConfigDatabaseSO>();
            db.InitializeDefaults();

            RingFlowEditorUtils.EnsureAssetFolders("Assets/Resources");
            AssetDatabase.CreateAsset(db, DatabaseAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _database = db;
            EditorUtility.DisplayDialog("Başarılı", $"GameConfigDatabase asset dosyası başarıyla {DatabaseAssetPath} konumunda oluşturuldu!", "Tamam");
        }

        private void SaveDatabase()
        {
            if (_database == null) return;
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Veritabanı Kaydedildi", "Tüm zorluk, renk ve dünya ayarları başarıyla GameConfigDatabase.asset dosyasına kaydedildi!", "Tamam");
        }
    }
}