using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class GeneratorSection : EditorSection
    {
        private int _levelIndex = 1;
        private int _seed = 100;
        private int _poleCount = 4;
        private int _colorCount = 3;
        private int _maxCapacity = 4;
        private int _minEmptyPoles = 1;
        private bool _autoSave = true;
        private bool _generateInProgress;
        private int _batchStartLevel = 1;
        private int _batchEndLevel = 50;

        [System.NonSerialized] private LevelData _generatedLevel;
        private string _solveStatus = "Seviye yüklenmedi / üretilmedi.";
        private readonly List<string> _solutionSteps = new();
        private Vector2 _solutionScroll;

        public LevelData GeneratedLevel => _generatedLevel;

        public override string DisplayName => "Seviye Üretici & Yapay Zeka Çözücü";
        public override string PrefKey => EditorPrefsKeys.FoldGenerator;

        public void OnEnable()
        {
            var db = GameConfigDatabaseSO.Instance;
            _levelIndex = EditorPrefs.GetInt(EditorPrefsKeys.LevelIndex, 1);
            _seed = EditorPrefs.GetInt(EditorPrefsKeys.Seed, 100);
            _poleCount = EditorPrefs.GetInt(EditorPrefsKeys.Poles, 4);
            _colorCount = EditorPrefs.GetInt(EditorPrefsKeys.Colors, 3);
            _maxCapacity = EditorPrefs.GetInt(EditorPrefsKeys.MaxCap, 4);
            _minEmptyPoles = Mathf.Max(1, EditorPrefs.GetInt(EditorPrefsKeys.MinEmptyPoles, 1));
            _batchStartLevel = EditorPrefs.GetInt(EditorPrefsKeys.BatchStartLevel, 1);
            _batchEndLevel = EditorPrefs.GetInt(EditorPrefsKeys.BatchEndLevel, 50);
            _autoSave = EditorPrefs.GetBool(EditorPrefsKeys.AutoSave, true);
        }

        public void OnDisable()
        {
            EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
            EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
            EditorPrefs.SetInt(EditorPrefsKeys.Poles, _poleCount);
            EditorPrefs.SetInt(EditorPrefsKeys.Colors, _colorCount);
            EditorPrefs.SetInt(EditorPrefsKeys.MaxCap, _maxCapacity);
            EditorPrefs.SetInt(EditorPrefsKeys.MinEmptyPoles, _minEmptyPoles);
            EditorPrefs.SetInt(EditorPrefsKeys.BatchStartLevel, _batchStartLevel);
            EditorPrefs.SetInt(EditorPrefsKeys.BatchEndLevel, _batchEndLevel);
            EditorPrefs.SetBool(EditorPrefsKeys.AutoSave, _autoSave);
        }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            var db = GameConfigDatabaseSO.Instance;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField("Zorluk Veritabanı (Database)", db, typeof(GameConfigDatabaseSO), false);
                if (GUILayout.Button("Düzenle", GUILayout.Width(80f)))
                {
                    Selection.activeObject = db;
                }
            }
            EditorGUILayout.Space(2f);

            _poleCount = db.GetPoleCountForLevel(_levelIndex);
            _colorCount = db.GetColorCountForLevel(_levelIndex);
            _maxCapacity = db.GetMaxCapacityForLevel(_levelIndex);
            _minEmptyPoles = db.GetMinEmptyPolesForLevel(_levelIndex);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Seviye Parametreleri", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                _levelIndex  = EditorGUILayout.IntSlider("Seviye Endeksi", _levelIndex, 1, db.TotalLevels);
                _seed        = EditorGUILayout.IntField("Rastgele Tohum (Seed)", _seed);

                // GDD’den gelen parametreler (manuel tweak yok)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var titleStyle = new GUIStyle(EditorStyles.label)
                        { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.2f, 0.7f, 1.0f) } };
                    EditorGUILayout.LabelField("GDD Parametreleri:", titleStyle);
                    EditorGUILayout.LabelField($"• Direk Sayısı: {_poleCount}");
                    EditorGUILayout.LabelField($"• Renk Sayısı: {_colorCount}");
                    EditorGUILayout.LabelField($"• Maks. Kapasite: {_maxCapacity}");
                    EditorGUILayout.LabelField($"• Min. Boş Direk: {_minEmptyPoles}");
                }

                DrawDifficultyPreview(_levelIndex);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
                    EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
                    EditorPrefs.SetInt(EditorPrefsKeys.Poles, _poleCount);
                    EditorPrefs.SetInt(EditorPrefsKeys.Colors, _colorCount);
                    EditorPrefs.SetInt(EditorPrefsKeys.MaxCap, _maxCapacity);
                    EditorPrefs.SetInt(EditorPrefsKeys.MinEmptyPoles, _minEmptyPoles);
                }

                EditorGUILayout.Space(6f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Tek Seviye Üret", GUILayout.Height(30)))
                        Generate();
                }

                _autoSave = EditorGUILayout.Toggle("Üretileni Otomatik Kaydet (Asset)", _autoSave);
            }

            // Toplu Seviye Üretim Ayarları
            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Toplu Seviye Üretim Paneli", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Seçilen seviye aralığındaki tüm bölümleri otomatik olarak GDD parametreleriyle üretir ve kaydeder.", MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _batchStartLevel = EditorGUILayout.IntField("Başlangıç Seviyesi", _batchStartLevel);
                    _batchEndLevel = EditorGUILayout.IntField("Bitiş Seviyesi", _batchEndLevel);
                }
                _batchStartLevel = Mathf.Clamp(_batchStartLevel, 1, db.TotalLevels);
                _batchEndLevel = Mathf.Clamp(_batchEndLevel, _batchStartLevel, db.TotalLevels);

                int count = _batchEndLevel - _batchStartLevel + 1;
                if (GUILayout.Button($"{_batchStartLevel} - {_batchEndLevel} Arası ({count} Seviye) Toplu Üret", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("Toplu Üretim",
                        $"{_batchStartLevel} ile {_batchEndLevel} arasındaki {count} seviye GDD eğrilerine göre üretilecek. " +
                        "Mevcut el yapımı seviyelerinizin üzerine yazılabilir. Emin misiniz?", "Evet, Üret", "İptal"))
                    {
                        GenerateAllLevels();
                    }
                }
            }

            if (_generateInProgress)
            {
                EditorGUILayout.HelpBox("Toplu seviye üretimi devam ediyor... Lütfen bekleyin.", MessageType.Info);
            }

            DrawGenerationResults();
        }

        private void DrawDifficultyPreview(int levelIndex)
        {
            var db = GameConfigDatabaseSO.Instance;
            var band = db.GetBandForLevel(levelIndex);
            int intensity = db.GetMechanicIntensityForLevel(levelIndex);
            var allowed = db.GetAllowedMechanicsForLevel(levelIndex);
            var theme = db.GetLevelThemeForLevel(levelIndex);
            int worldIndex = db.GetWorldForLevel(levelIndex);
            var worldMechanic = db.GetMechanicForWorld(worldIndex);
            int emptyPoles = db.GetMinEmptyPolesForLevel(levelIndex);

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Zorluk Önizleme (GDD Raporu)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Band: {band}");
                EditorGUILayout.LabelField($"Tema: {theme.StartLevel}-{theme.EndLevel} | Renk: {theme.ColorCount}");
                EditorGUILayout.LabelField($"Boş Direk: {emptyPoles} (zorunlu)");
                EditorGUILayout.LabelField($"Dünya {worldIndex + 1}: {(worldMechanic == WorldMechanicType.None ? "Mekanik Yok" : worldMechanic.ToString())}");
                EditorGUILayout.LabelField($"Mekanik Yoğunluğu: {intensity}");
                EditorGUILayout.LabelField($"Aktif Mekanikler: {string.Join(", ", allowed)}");

                if (worldMechanic != WorldMechanicType.None
                    && worldMechanic != WorldMechanicType.RandomPool1
                    && worldMechanic != WorldMechanicType.RandomPool2
                    && worldMechanic != WorldMechanicType.RandomPool3
                    && !allowed.Contains(worldMechanic))
                {
                    EditorGUILayout.HelpBox(
                        $"Dünya {worldIndex + 1} mekanik ataması ile bant izinleri uyuşmuyor. Generator yine dünya mekaniğini enjekte eder, ancak config güncellemesi önerilir.",
                        MessageType.Warning);
                }
            }
        }

        private void DrawGenerationResults()
        {
            if (_generatedLevel == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Üretilen Seviye Bilgileri:", EditorStyles.boldLabel);
            var db = GameConfigDatabaseSO.Instance;
            int level = _generatedLevel.LevelIndex;
            var band = db.GetBandForLevel(level);
            int intensity = db.GetMechanicIntensityForLevel(level);
            var allowed = db.GetAllowedMechanicsForLevel(level);
            
            EditorGUILayout.LabelField($"Seviye: {_generatedLevel.LevelIndex} | Direkler: {_generatedLevel.Poles.Count}");
            EditorGUILayout.LabelField($"Band: {band} | Yoğunluk: {intensity}");
            EditorGUILayout.LabelField($"Açık Mekanikler: {string.Join(", ", allowed)}");
            EditorGUILayout.LabelField($"Hedef Hamle Sayısı: {_generatedLevel.TargetMoves} (Yapay Zeka Doğruladı)");

            LevelVisualRenderer.Draw(_generatedLevel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Yapay Zeka Çözüm Yolu:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Durum: {_solveStatus}", EditorStyles.wordWrappedLabel);

            if (_solutionSteps.Count == 0) return;
            _solutionScroll = EditorGUILayout.BeginScrollView(_solutionScroll, GUILayout.Height(120));
            for (int i = 0; i < _solutionSteps.Count; i++)
                EditorGUILayout.LabelField($"{i + 1}. {_solutionSteps[i]}");
            EditorGUILayout.EndScrollView();
        }


        public void GenerateFromDashboard() { Generate(); }

        public void GenerateFromDashboardAll() { GenerateAllLevels(); }

        private void Generate()
        {
            var db = GameConfigDatabaseSO.Instance;
            if (_levelIndex < 1 || _levelIndex > db.TotalLevels)
            {
                NexusLog.Warn("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                    "Seviye endeksi sınırların dışında, düzeltiliyor.");
                _levelIndex = Mathf.Clamp(_levelIndex, 1, db.TotalLevels);
            }
            _colorCount = Mathf.Max(_colorCount, db.GetColorCountForLevel(_levelIndex));
            _poleCount = Mathf.Max(_colorCount + 1, db.GetPoleCountForLevel(_levelIndex));
            _minEmptyPoles = db.GetMinEmptyPolesForLevel(_levelIndex);
                    _minEmptyPoles = Mathf.Max(1, _minEmptyPoles);
                    if (_poleCount > 12) _poleCount = 12;

            NexusLog.Info("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                $"Seviye {_levelIndex} üretiliyor: Seed={_seed}, Direkler={_poleCount}, Renkler={_colorCount}, Kapasite={_maxCapacity}, BoşDirek={_minEmptyPoles}");

            _generatedLevel = LevelGenerator.GenerateLevel(_levelIndex, _seed, _poleCount, _colorCount, _maxCapacity);

            if (_generatedLevel == null)
            {
                _solveStatus = "Üretim başarısız (50 tohum denendi ve tükendi).";
                _solutionSteps.Clear();
                return;
            }

            var board = new BoardState { PoleCount = _generatedLevel.Poles.Count };
            for (int p = 0; p < _generatedLevel.Poles.Count; p++)
            {
                var pole = _generatedLevel.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    var ring = pole.Rings[r];
                    board.SetRingColor(p, r, ring.Color);
                    board.SetRingType(p, r, ring.Type);
                    board.SetRingAdditional(p, r, ring.AdditionalData);
                }
                board.SetRingCount(p, pole.Rings.Count);
            }

            var solveResult = LevelSolver.Solve(board, _maxCapacity);
            var summary = new ValidationSummary
            {
                Solvable = solveResult.IsSolvable,
                MoveCount = solveResult.MoveCount,
                EmptyPoles = CountEmptyPoles(_generatedLevel),
                MaxEmptyAllowed = 1
            };

            if (summary.EmptyPoles > summary.MaxEmptyAllowed)
            {
                _solveStatus = $"Geçersiz: en fazla {summary.MaxEmptyAllowed} boş direk olmalı, bulunan: {summary.EmptyPoles}.";
                _solutionSteps.Clear();
            }
            else if (summary.Solvable && summary.MoveCount > 0)
            {
                _solveStatus = $"Yapay zeka çözümü bulundu: {summary.MoveCount} hamle.";
                _solutionSteps.Clear();
                foreach (var move in solveResult.Moves)
                    _solutionSteps.Add($"Halkayı Direk {move.FromPoleId}'den Direk {move.ToPoleId}'ye taşı.");
            }
            else
            {
                _solveStatus = "Çözülemez! Tohum doğrulamayı geçemedi.";
                _solutionSteps.Clear();
            }

            if (_autoSave)
                SaveLevelAsset(_generatedLevel);
        }

        private void GenerateAllLevels()
        {
            _generateInProgress = true;
            var db = GameConfigDatabaseSO.Instance;

            string folderPath = EditorPaths.LevelsFolder;
            EnsureLevelsFolder(folderPath);

            int okCount = 0;
            int failed = 0;
            int total = _batchEndLevel - _batchStartLevel + 1;

            int originalPoleCount = _poleCount;
            int originalColorCount = _colorCount;
            int originalMaxCapacity = _maxCapacity;
            int originalMinEmptyPoles = _minEmptyPoles;

            try
            {
                for (int level = _batchStartLevel; level <= _batchEndLevel; level++)
                {
                    int poles = DifficultyCurve.PoleCountForLevel(level);
                    int colors = DifficultyCurve.ColorCountForLevel(level);
                    int maxCap = DifficultyCurve.MaxCapacityForLevel(level);
                    int minEmpty = db.GetMinEmptyPolesForLevel(level);
                    if (poles < colors + minEmpty) poles = colors + minEmpty;
                    if (poles > 12) poles = 12;

                    var levelData = LevelGenerator.GenerateLevel(level, 100 + level, poles, colors, maxCap);
                    if (levelData == null)
                    {
                        failed++;
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Toplu Üretim Başarısız", 
                            $"Seviye {level} üretilemedi (50 tohum denendi ve çözülebilir bir seviye bulunamadı).\n" +
                            "Toplu seviye üretimi durduruldu. Lütfen veritabanındaki boş direk / kilitli direk dengesini veya mekanikleri kontrol edin.", "Tamam");
                        _generateInProgress = false;
                        return;
                    }
                    else
                    {
                        SaveLevelAssetSilent(levelData, folderPath);
                        okCount++;
                    }

                    if (level % 10 == 0 || level == _batchEndLevel)
                    {
                        EditorUtility.DisplayProgressBar("Toplu Seviye Üretiliyor",
                            $"Seviye işleniyor: {level}/{_batchEndLevel} ({okCount} başarılı, {failed} başarısız)...",
                            (float)(level - _batchStartLevel + 1) / total);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            NexusLog.Info("RingFlowEditor", nameof(GenerateAllLevels), "",
                $"Tamamlandı: {okCount}/{total} seviye başarıyla üretildi, {failed} başarısız.");

            _generateInProgress = false;

            EditorUtility.DisplayDialog("Toplu Üretim Tamamlandı",
                $"{okCount}/{total} seviye şuraya kaydedildi: {folderPath}\n{failed} başarısız.", "Tamam");

            _poleCount = originalPoleCount;
            _colorCount = originalColorCount;
            _maxCapacity = originalMaxCapacity;
            _minEmptyPoles = originalMinEmptyPoles;
        }

        private static void EnsureLevelsFolder(string folderPath)
        {
            RingFlowEditorUtils.EnsureAssetFolders("Assets/Resources");
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");
        }

        private static void SaveLevelAssetSilent(LevelData levelData, string folderPath)
        {
            string assetPath = $"{folderPath}/Level_{levelData.LevelIndex}.asset";
            LevelDataSO levelSO = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);

            if (levelSO == null)
            {
                levelSO = ScriptableObject.CreateInstance<LevelDataSO>();
                levelSO.Data = CloneLevelData(levelData);
                AssetDatabase.CreateAsset(levelSO, assetPath);
            }
            else
            {
                levelSO.Data = CloneLevelData(levelData);
                EditorUtility.SetDirty(levelSO);
            }
        }

        private void SaveLevelAsset(LevelData levelData)
        {
            if (levelData == null) return;

            string folderPath = EditorPaths.LevelsFolder;
            EnsureLevelsFolder(folderPath);

            string assetPath = $"{folderPath}/Level_{levelData.LevelIndex}.asset";
            LevelDataSO levelSO = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);

            if (levelSO == null)
            {
                levelSO = ScriptableObject.CreateInstance<LevelDataSO>();
                levelSO.Data = CloneLevelData(levelData);
                AssetDatabase.CreateAsset(levelSO, assetPath);
            }
            else
            {
                levelSO.Data = CloneLevelData(levelData);
                EditorUtility.SetDirty(levelSO);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Başarılı",
                $"Seviye kaydedildi: {assetPath}\nBu seviye artık runtime'da otomatik olarak yüklenecektir!", "Tamam");
        }

        private static LevelData CloneLevelData(LevelData source)
        {
            var clone = new LevelData
            {
                LevelIndex = source.LevelIndex,
                Seed = source.Seed,
                TargetMoves = source.TargetMoves,
                Poles = new List<PoleData>()
            };

            foreach (var p in source.Poles)
            {
                var poleClone = new PoleData(p.RingCapacity) { IsLocked = p.IsLocked, Rings = new List<RingData>() };
                foreach (var r in p.Rings)
                    poleClone.Rings.Add(new RingData(r.Color, r.Type, r.AdditionalData));
                clone.Poles.Add(poleClone);
            }

            return clone;
        }

        private static int CountEmptyPoles(LevelData levelData)
        {
            if (levelData == null || levelData.Poles == null) return 0;
            int empty = 0;
            for (int i = 0; i < levelData.Poles.Count; i++)
            {
                if (levelData.Poles[i].Rings == null || levelData.Poles[i].Rings.Count == 0)
                    empty++;
            }
            return empty;
        }

        [System.Serializable]
        private struct ValidationSummary
        {
            public bool Solvable;
            public int EmptyPoles;
            public int MaxEmptyAllowed;
            public int MoveCount;
        }
    }
}
