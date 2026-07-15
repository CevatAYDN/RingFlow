using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
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

            _database.TotalLevels = EditorGUILayout.IntField("Toplam Seviye Sayısı", _database.TotalLevels);
            _database.LevelsPerThemeStep = EditorGUILayout.IntField("Tema Başına Seviye Adımı", _database.LevelsPerThemeStep);
            _database.MinimumEmptyPoles = EditorGUILayout.IntField("Minimum Boş Direk Sayısı", _database.MinimumEmptyPoles);

            EditorGUILayout.Space(10f);

            // --- 1. ZORLUK DERECELERİ (inline editable) ---
            RingFlowEditorUtils.BeginSectionBox("Zorluk Dereceleri Ayarları", "Zorluk seviyelerine göre direk kapasitesi ve boş direk sayısı sınırları.");
            for (int i = 0; i < _database.DifficultyBands.Count; i++)
            {
                var bandData = _database.DifficultyBands[i];
                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                EditorGUI.DrawRect(rowRect, rowBg);

                EditorGUILayout.LabelField(bandData.Band.ToString(), EditorStyles.boldLabel, GUILayout.Width(80f));

                int maxLvl = EditorGUILayout.IntField("Maks Seviye", bandData.MaxLevel, GUILayout.Width(130f));
                int minEmpty = EditorGUILayout.IntField("Boş Direk", bandData.MinEmptyPoles, GUILayout.Width(110f));
                int maxCap = EditorGUILayout.IntField("Kapasite", bandData.MaxCapacity, GUILayout.Width(100f));

                bandData.MaxLevel = maxLvl;
                bandData.MinEmptyPoles = minEmpty;
                bandData.MaxCapacity = maxCap;
                _database.DifficultyBands[i] = bandData;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2f);
            }
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(6f);

            // --- 2. RENK İLERLEME EĞRİSİ (inline editable) ---
            RingFlowEditorUtils.BeginSectionBox("Renk İlerleme Eğrisi", "Bölüm seviyelerine göre kullanılacak halka renk miktarları.");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Seviye Eşiği (≥)", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
                EditorGUILayout.LabelField("Renk Sayısı", EditorStyles.miniBoldLabel, GUILayout.Width(100f));
            }

            for (int i = 0; i < _database.ColorCurve.Count; i++)
            {
                var pt = _database.ColorCurve[i];
                Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                EditorGUI.DrawRect(rowRect, rowBg);

                int threshold = EditorGUILayout.IntField(pt.LevelThreshold, GUILayout.Width(120f));
                int colors = EditorGUILayout.IntSlider(pt.ColorCount, 2, 10, GUILayout.Width(200f));
                pt.LevelThreshold = threshold;
                pt.ColorCount = colors;
                _database.ColorCurve[i] = pt;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Yeni Nokta Ekle", GUILayout.Width(140f)))
                    _database.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = 2000, ColorCount = 10 });

                if (_database.ColorCurve.Count > 0 && GUILayout.Button("- Son Noktayı Sil", GUILayout.Width(140f)))
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

                int seed = i * 12345;
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