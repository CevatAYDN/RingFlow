using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    public sealed class DatabaseSection : EditorSection
    {
        public override string DisplayName => "Oyun Ayarları Veritabanı";
        public override string PrefKey => EditorPrefsKeys.FoldDatabase;

        private GameConfigDatabaseSO _database;
        private int _selectedWorldIndex = 0;

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

            // Load or create database asset
            if (_database == null)
            {
                _database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(DatabaseAssetPath);
            }

            if (_database == null)
            {
                EditorGUILayout.HelpBox(
                    "Resources klasöründe GameConfigDatabase asset dosyası bulunamadı. " +
                    "Çalışma zamanında varsayılan ayarlar kullanılır, ancak kalıcı değişiklik yapmak için asset dosyasını oluşturmalısınız.",
                    MessageType.Warning);

                if (GUILayout.Button("GameConfigDatabase Dosyası Oluştur", GUILayout.Height(36)))
                {
                    CreateDatabaseAsset();
                }
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Veritabanı Asset Özellikleri", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Asset Dosyası", _database, typeof(GameConfigDatabaseSO), false);

                EditorGUILayout.Space(5f);

                EditorGUI.BeginChangeCheck();

                _database.TotalLevels = EditorGUILayout.IntField("Toplam Seviye Sayısı", _database.TotalLevels);
                _database.LevelsPerThemeStep = EditorGUILayout.IntField("Tema Başına Seviye Adımı", _database.LevelsPerThemeStep);
                _database.MinimumEmptyPoles = EditorGUILayout.IntField("Minimum Boş Direk Sayısı", _database.MinimumEmptyPoles);

                EditorGUILayout.Space(10f);
                DrawDifficultyOverview();

                EditorGUILayout.Space(10f);
                // --- 1. ZORLUK DERECELERİ ---
                EditorGUILayout.LabelField("Zorluk Dereceleri Ayarları", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    for (int i = 0; i < _database.DifficultyBands.Count; i++)
                    {
                        var bandData = _database.DifficultyBands[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(bandData.Band.ToString(), GUILayout.Width(80f));
                            
                            int maxLvl = EditorGUILayout.IntField("Maks. Seviye", bandData.MaxLevel, GUILayout.Width(130f));
                            int minEmpty = EditorGUILayout.IntField("Boş Direk", bandData.MinEmptyPoles, GUILayout.Width(110f));
                            int maxCap = EditorGUILayout.IntField("Kapasite", bandData.MaxCapacity, GUILayout.Width(100f));

                            bandData.MaxLevel = maxLvl;
                            bandData.MinEmptyPoles = minEmpty;
                            bandData.MaxCapacity = maxCap;
                            _database.DifficultyBands[i] = bandData;
                        }
                    }
                }

                EditorGUILayout.Space(10f);

                // --- 2. RENK İLERLEME EĞRİSİ ───
                EditorGUILayout.LabelField("Renk İlerleme Eğrisi", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Seviye Eşiği (≥)", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
                        EditorGUILayout.LabelField("Renk Sayısı", EditorStyles.miniBoldLabel, GUILayout.Width(100f));
                    }

                    for (int i = 0; i < _database.ColorCurve.Count; i++)
                    {
                        var pt = _database.ColorCurve[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            int threshold = EditorGUILayout.IntField(pt.LevelThreshold, GUILayout.Width(120f));
                            int colors = EditorGUILayout.IntSlider(pt.ColorCount, 2, 10, GUILayout.Width(200f));

                            pt.LevelThreshold = threshold;
                            pt.ColorCount = colors;
                            _database.ColorCurve[i] = pt;
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("+ Yeni Nokta Ekle", GUILayout.Width(140f)))
                        {
                            _database.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = 2000, ColorCount = 10 });
                        }
                        if (_database.ColorCurve.Count > 0 && GUILayout.Button("- Son Noktayı Sil", GUILayout.Width(140f)))
                        {
                            _database.ColorCurve.RemoveAt(_database.ColorCurve.Count - 1);
                        }
                    }
                }

                EditorGUILayout.Space(10f);

                // --- 3. DÜNYA VE TEMA AYARLARI ---
                EditorGUILayout.LabelField("Dünyalar ve Tema Ayarları", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string[] worldNames = new string[_database.Worlds.Count];
                    for (int i = 0; i < _database.Worlds.Count; i++)
                    {
                        worldNames[i] = $"Dünya {i + 1}: {_database.Worlds[i].Theme}";
                    }

                    _selectedWorldIndex = EditorGUILayout.Popup("Düzenlenecek Dünyayı Seç", _selectedWorldIndex, worldNames);

                    EditorGUILayout.Space(5f);

                    if (_selectedWorldIndex >= 0 && _selectedWorldIndex < _database.Worlds.Count)
                    {
                        var wData = _database.Worlds[_selectedWorldIndex];
                        
                        wData.Theme = EditorGUILayout.TextField("Tema Görünen Adı", wData.Theme);
                        wData.UnlockedByWorldIndex = EditorGUILayout.IntField("Kilit Açan Dünya Endeksi", wData.UnlockedByWorldIndex);
                        wData.IsEventWorld = EditorGUILayout.Toggle("Boss (Etkinlik) Dünyası mı", wData.IsEventWorld);
                        wData.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup("Özel Mekanik Tipi", wData.MechanicType);

                        _database.Worlds[_selectedWorldIndex] = wData;
                    }
                }

                EditorGUILayout.Space(15f);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_database);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Veritabanı Değişikliklerini Kaydet", GUILayout.Height(36)))
                    {
                        SaveDatabase();
                    }
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

                // --- 4. TOPLU SEVİYE DOĞRULAYICI ───
                EditorGUILayout.Space(15f);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Toplu Seviye Doğrulayıcı & Çözücü Testi", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Seçilen seviye aralığındaki bölümleri tek tek üretir, çözücü ile doğrular, süre ve çözülebilirlik raporu sunar.",
                        MessageType.Info);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _valStartLevel = EditorGUILayout.IntField("Başlangıç Seviyesi", _valStartLevel, GUILayout.Width(180f));
                        _valEndLevel = EditorGUILayout.IntField("Bitiş Seviyesi", _valEndLevel, GUILayout.Width(180f));
                    }

                    if (GUILayout.Button("Toplu Doğrulamayı Başlat", GUILayout.Height(30)))
                    {
                        RunBatchValidation();
                    }

                    if (_validationResults.Count > 0)
                    {
                        EditorGUILayout.Space(6f);
                        EditorGUILayout.LabelField($"Sonuçlar ({_validationResults.Count})", EditorStyles.boldLabel);
                        
                        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(200f));
                        for (int i = 0; i < _validationResults.Count; i++)
                        {
                            var res = _validationResults[i];
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                            {
                                var style = new GUIStyle(EditorStyles.label)
                                {
                                    fontStyle = FontStyle.Bold,
                                    normal = { textColor = res.Success ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.9f, 0.3f, 0.3f) }
                                };
                                EditorGUILayout.LabelField($"Lvl {res.LevelIndex}", style, GUILayout.Width(60f));
                                EditorGUILayout.LabelField(res.Log);
                            }
                        }
                        EditorGUILayout.EndScrollView();

                        if (GUILayout.Button("Sonuçları Temizle", GUILayout.Width(140f)))
                        {
                            _validationResults.Clear();
                        }
                    }
                }
            }
        }

        private void RunBatchValidation()
        {
            _validationResults.Clear();
            
            int solvedCount = 0;
            float totalTime = 0;
            int totalMoves = 0;

            for (int i = _valStartLevel; i <= _valEndLevel; i++)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                int poleCount = DifficultyCurve.PoleCountForLevel(i);
                int colorCount = DifficultyCurve.ColorCountForLevel(i);
                int maxCap = DifficultyCurve.MaxCapacityForLevel(i);
                
                int seed = i * 12345;
                var levelData = LevelGenerator.GenerateLevel(i, seed, poleCount, colorCount, maxCap);
                
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

                    int solverLimit = colorCount <= 3 ? 20000 : 15000;
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
            }
            
            string summaryMsg = $"Doğrulanan Seviye: {_validationResults.Count}\n" +
                                $"Başarılı: {solvedCount} / {_validationResults.Count}\n" +
                                $"Ort. Süre: {(solvedCount > 0 ? (totalTime / solvedCount) : 0):F1}ms\n" +
                                $"Ort. Hamle: {(solvedCount > 0 ? (totalMoves / (float)solvedCount) : 0):F1}";
            EditorUtility.DisplayDialog("Doğrulama Tamamlandı", summaryMsg, "Tamam");
        }

        private const string DatabaseAssetPath = "Assets/Resources/GameConfigDatabase.asset";

        private void DrawDifficultyOverview()
        {
            if (_database == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Zorluk Özeti (Önizleme)", EditorStyles.boldLabel);
                for (int i = 0; i < _database.DifficultyBands.Count; i++)
                {
                    var band = _database.DifficultyBands[i];
                    EditorGUILayout.LabelField(
                        $"{band.Band}: Maks Seviye={band.MaxLevel} | Boş Direk={band.MinEmptyPoles} | Kapasite={band.MaxCapacity} | İzinli Mekanikler={band.AllowedMechanics?.Count ?? 0}");
                }
            }
        }

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
