using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    /// <summary>
    /// Seviye üretici bölümü — yalnızca GDD veritabanı (GameConfigDatabaseSO)
    /// üzerinden data-driven üretim yapar. Manuel tweak / fallback yok.
    /// UI yalnızca "Seviye Endeksi" ve "Seed" ister; geri kalan tüm
    /// parametreler DB'den.Commands ile aynı fonksiyonları kullanır.
    /// </summary>
    public sealed class GeneratorSection : EditorSection
    {
        private int _levelIndex = 1;
        private int _seed = 100;
        private bool _autoSave = true;
        private bool _generateInProgress;
        private int _batchStartLevel = 1;
        private int _batchEndLevel = 50;

        [System.NonSerialized] private LevelData _generatedLevel;
        private string _solveStatus = "Seviye yüklenmedi / üretilmedi.";
        private readonly List<string> _solutionSteps = new();
        private Vector2 _solutionScroll;
        private GameConfigDatabaseSO _cachedDatabase;

        public LevelData GeneratedLevel => _generatedLevel;

        public override string DisplayName => "Seviye Üretici & Yapay Zeka Çözücü";
        public override string PrefKey => EditorPrefsKeys.FoldGenerator;

        public void OnEnable()
        {
            _levelIndex = EditorPrefs.GetInt(EditorPrefsKeys.LevelIndex, 1);
            _seed = EditorPrefs.GetInt(EditorPrefsKeys.Seed, 100);
            _batchStartLevel = EditorPrefs.GetInt(EditorPrefsKeys.BatchStartLevel, 1);
            _batchEndLevel = EditorPrefs.GetInt(EditorPrefsKeys.BatchEndLevel, 50);
            _autoSave = EditorPrefs.GetBool(EditorPrefsKeys.AutoSave, true);
        }

        public void OnDisable()
        {
            EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
            EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
            EditorPrefs.SetInt(EditorPrefsKeys.BatchStartLevel, _batchStartLevel);
            EditorPrefs.SetInt(EditorPrefsKeys.BatchEndLevel, _batchEndLevel);
            EditorPrefs.SetBool(EditorPrefsKeys.AutoSave, _autoSave);
        }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            _cachedDatabase = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (_cachedDatabase == null)
            {
                EditorGUILayout.HelpBox("Zorluk Veritabanı (GameConfigDatabase.asset) bulunamadı!", MessageType.Error);
                return;
            }
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField("Zorluk Veritabanı (Database)", _cachedDatabase, typeof(GameConfigDatabaseSO), false);
                if (GUILayout.Button("Düzenle", GUILayout.Width(80f)))
                    Selection.activeObject = _cachedDatabase;
            }
            EditorGUILayout.Space(2f);

            // Tüm parametreler DB'den — yerel olarak okunur, UI'da yalnızca görüntülenir
            int poleCount = _cachedDatabase.GetPoleCountForLevel(_levelIndex);
            int colorCount = _cachedDatabase.GetColorCountForLevel(_levelIndex);
            int maxCapacity = _cachedDatabase.GetMaxCapacityForLevel(_levelIndex);
            int minEmptyPoles = _cachedDatabase.GetMinEmptyPolesForLevel(_levelIndex);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Seviye Parametreleri", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                _levelIndex = EditorGUILayout.IntSlider("Seviye Endeksi", _levelIndex, 1, _cachedDatabase.TotalLevels);
                _seed = EditorGUILayout.IntField("Rastgele Tohum (Seed)", _seed);

                // GDD'den gelen parametreler — salt okunur, manuel tweak yok
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var titleStyle = new GUIStyle(EditorStyles.label)
                        { fontStyle = FontStyle.Bold, normal = { textColor = EditorPaths.EditorColors.Info } };
                    EditorGUILayout.LabelField("GDD Parametreleri:", titleStyle);
                    EditorGUILayout.LabelField($"• Direk Sayısı: {poleCount}");
                    EditorGUILayout.LabelField($"• Renk Sayısı: {colorCount}");
                    EditorGUILayout.LabelField($"• Maks. Kapasite: {maxCapacity}");
                    EditorGUILayout.LabelField($"• Min. Boş Direk: {minEmptyPoles}");
                }

                DrawDifficultyPreview(_levelIndex);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
                    EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
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
                _batchStartLevel = Mathf.Clamp(_batchStartLevel, 1, _cachedDatabase.TotalLevels);
                _batchEndLevel = Mathf.Clamp(_batchEndLevel, _batchStartLevel, _cachedDatabase.TotalLevels);

                int count = _batchEndLevel - _batchStartLevel + 1;
                if (GUILayout.Button($"{_batchStartLevel} - {_batchEndLevel} Arası ({count} Seviye) Toplu Üret", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("Toplu Üretim",
                        $"{_batchStartLevel} ile {_batchEndLevel} arasındaki {count} seviye GDD eğrilerine göre üretilecek. " +
                        "Mevcut seviyelerinizin üzerine yazılabilir. Emin misiniz?", "Evet, Üret", "İptal"))
                    {
                        GenerateAllLevels();
                    }
                }
            }

            if (_generateInProgress)
                EditorGUILayout.HelpBox("Toplu seviye üretimi devam ediyor... Lütfen bekleyin.", MessageType.Info);

            DrawGenerationResults();
        }

        private void DrawDifficultyPreview(int levelIndex)
        {
            var db = _cachedDatabase;
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
            var db = _cachedDatabase;
            int level = _generatedLevel.LevelIndex;
            var band = db.GetBandForLevel(level);
            int intensity = db.GetMechanicIntensityForLevel(level);
            var allowed = db.GetAllowedMechanicsForLevel(level);

            EditorGUILayout.LabelField($"Seviye: {_generatedLevel.LevelIndex} | Direkler: {_generatedLevel.Poles.Count}");
            EditorGUILayout.LabelField($"Band: {band} | Yoğunluk: {intensity}");
            EditorGUILayout.LabelField($"Açık Mekanikler: {string.Join(", ", allowed)}");
            EditorGUILayout.LabelField($"Hedef Hamle Sayısı: {_generatedLevel.TargetMoves} (Yapay Zeka Doğruladı)");

            var portalPairs = new System.Collections.Generic.List<string>();
            for (int p = 0; p < _generatedLevel.Poles.Count; p++)
            {
                int partnerId = _generatedLevel.Poles[p].PortalTargetId;
                if (partnerId >= 0 && partnerId > p)
                {
                    portalPairs.Add($"Direk {p} ↔ Direk {partnerId}");
                }
            }

            if (portalPairs.Count > 0)
            {
                EditorGUILayout.Space(2f);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Portal Çiftleri:", EditorStyles.boldLabel);
                    foreach (var pair in portalPairs)
                        EditorGUILayout.LabelField($"  • {pair}");
                }
            }

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
            var db = _cachedDatabase ?? Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (db == null) return;
            if (_levelIndex < 1 || _levelIndex > db.TotalLevels)
            {
                NexusLog.Warn("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                    "Seviye endeksi sınırların dışında, düzeltiliyor.");
                _levelIndex = Mathf.Clamp(_levelIndex, 1, db.TotalLevels);
            }

            // Tüm parametreler DB'den — yerel değişkenler
            int poleCount = db.GetPoleCountForLevel(_levelIndex);
            int colorCount = db.GetColorCountForLevel(_levelIndex);
            int maxCapacity = db.GetMaxCapacityForLevel(_levelIndex);
            int minEmptyPoles = Mathf.Max(1, db.GetMinEmptyPolesForLevel(_levelIndex));
            if (poleCount > db.LevelGen.PoleCountClamp) poleCount = db.LevelGen.PoleCountClamp;

            NexusLog.Info("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                $"Seviye {_levelIndex} üretiliyor: Seed={_seed}, Direkler={poleCount}, Renkler={colorCount}, Kapasite={maxCapacity}, BoşDirek={minEmptyPoles}");

            _generatedLevel = LevelGenerator.GenerateLevel(db, _levelIndex, _seed, poleCount, colorCount, maxCapacity);

            if (_generatedLevel == null)
            {
                _solveStatus = "Üretim başarısız (50 tohum denendi ve tükendi). DB yapılandırmasını kontrol edin.";
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

            var solveResult = LevelSolver.Solve(board, maxCapacity);
            int emptyPoleCount = CountEmptyPoles(_generatedLevel);

            if (emptyPoleCount > 1)
            {
                _solveStatus = $"Geçersiz: en fazla 1 boş direk olmalı, bulunan: {emptyPoleCount}.";
                _solutionSteps.Clear();
            }
            else if (solveResult.IsSolvable && solveResult.MoveCount > 0)
            {
                _solveStatus = $"Yapay zeka çözümü bulundu: {solveResult.MoveCount} hamle.";
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
            var db = _cachedDatabase ?? Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (db == null) return;

            string folderPath = EditorPaths.LevelsFolder;
            EnsureLevelsFolder(folderPath);

            int okCount = 0;
            int failed = 0;
            int total = _batchEndLevel - _batchStartLevel + 1;

            try
            {
                for (int level = _batchStartLevel; level <= _batchEndLevel; level++)
                {
                    int poles = db.GetPoleCountForLevel(level);
                    int colors = db.GetColorCountForLevel(level);
                    int maxCap = db.GetMaxCapacityForLevel(level);
                    int minEmpty = db.GetMinEmptyPolesForLevel(level);
                    if (poles < colors + minEmpty) poles = colors + minEmpty;
                    if (poles > db.LevelGen.PoleCountClamp) poles = db.LevelGen.PoleCountClamp;

                    var levelData = LevelGenerator.GenerateLevel(db, level, 100 + level, poles, colors, maxCap);
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

                    SaveLevelAssetSilent(levelData, folderPath);
                    okCount++;

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
        }

        private static void EnsureLevelsFolder(string folderPath)
        {
            RingFlowEditorUtils.EnsureAssetFolders(EditorPaths.ResourcesFolder);
            if (!AssetDatabase.IsValidFolder(EditorPaths.LevelsFolder))
                AssetDatabase.CreateFolder(EditorPaths.ResourcesFolder, "Levels");
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
                var poleClone = new PoleData(p.RingCapacity) { IsLocked = p.IsLocked, PortalTargetId = p.PortalTargetId, Rings = new List<RingData>() };
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
    }
}
