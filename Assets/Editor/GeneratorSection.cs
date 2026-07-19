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
        private static GUIStyle s_gddTitleStyle;

        /// <summary>
        /// Optional callback fired after any level asset is written to disk
        /// (single-level save or batch generation complete).
        /// Wire this in RingFlowEditorWindow to invalidate the LevelBrowser cache.
        /// </summary>
        public System.Action OnLevelAssetsChanged;

        public LevelData GeneratedLevel => _generatedLevel;

        public override string DisplayName => "Seviye Üretici & Yapay Zeka Çözücü";
        public override string PrefKey => EditorPrefsKeys.FoldGenerator;

        public void OnEnable()
        {
            _levelIndex = EditorPrefs.GetInt(EditorPrefsKeys.LevelIndex, 1);
            _seed = EditorPrefs.GetInt(EditorPrefsKeys.Seed, 100);
            _batchStartLevel = EditorPrefs.GetInt(EditorPrefsKeys.BatchStartLevel, 1);

            // FIX-E4: Default batchEndLevel from EditorPrefs; if never saved before,
            // use DB's TotalLevels so "batch end" matches the project's actual scope
            // instead of the hardcoded 50 that made batch generation silently stop early.
            int savedBatchEnd = EditorPrefs.GetInt(EditorPrefsKeys.BatchEndLevel, -1);
            if (savedBatchEnd > 0)
            {
                _batchEndLevel = savedBatchEnd;
            }
            else
            {
                // First launch: default to TotalLevels from DB if available; otherwise keep a conservative editor default.
                var db = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)
                    .GetAwaiter().GetResult();
                _batchEndLevel = db != null && db.TotalLevels > 0 ? db.TotalLevels : _batchEndLevel;
            }

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

            if (_cachedDatabase == null)
                _cachedDatabase = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)
                    .GetAwaiter().GetResult();
            if (_cachedDatabase == null)
            {
                // FIX-E1: throw in OnGUI() crashes the entire editor window and prevents
                // the user from fixing the problem. Use DisplayDialog so the window stays
                // open and the user can assign the database asset.
                EditorUtility.DisplayDialog("Veritabanı Bulunamadı",
                    $"GameConfigDatabaseSO '{EditorPaths.GameConfigDatabaseKey}' path'inde bulunamadı.\n" +
                    "Lütfen Resources/Configs/ klasöründe 'GameConfigDatabase' asset'inin mevcut olduğunu doğrulayın.",
                    "Tamam");
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.ObjectField("Zorluk Veritabanı (Database)", _cachedDatabase, typeof(GameConfigDatabaseSO), false);
                if (GUILayout.Button("Düzenle", GUILayout.Width(80f)))
                    Selection.activeObject = _cachedDatabase;
            }
            EditorGUILayout.Space(2f);

            int poleCount = _cachedDatabase.GetPoleCountForLevel(_levelIndex);
            int colorCount = _cachedDatabase.GetColorCountForLevel(_levelIndex);
            int maxCapacity = _cachedDatabase.GetMaxCapacityForLevel(_levelIndex);
            int minEmptyPoles = _cachedDatabase.GetMinEmptyPolesForLevel(_levelIndex);

            RingFlowEditorUtils.BeginSectionBox("Seviye Parametreleri", "Bireysel seviye üretimi için indeks ve seed girin. Diğer parametreler DB'den otomatik gelir.");

            bool narrow = RingFlowEditorUtils.IsNarrowWidth(680f);

            EditorGUI.BeginChangeCheck();
            _levelIndex = EditorGUILayout.IntSlider("Seviye Endeksi", _levelIndex, 1, _cachedDatabase.TotalLevels);

            using (new EditorGUILayout.HorizontalScope())
            {
                _seed = EditorGUILayout.IntField("Seed", _seed, GUILayout.MinWidth(100f));
                // DATA-3: "Deterministik Seed Kullan" butonu — GetDeterministicSeed() DB kuralından gelir
                if (GUILayout.Button("DB Seed", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    _seed = LevelGenerator.GetDeterministicSeed(_levelIndex);
                    EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
                    GUI.FocusControl(null);
                }
                if (GUILayout.Button("Rastgele", EditorStyles.miniButton, GUILayout.Width(70f)))
                {
                    _seed = UnityEngine.Random.Range(1, 999999);
                    EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
                    GUI.FocusControl(null);
                }
            }

            // RESP-2 + DATA-3: DB parametreleri responsive grid olarak gösterilir — hardcode değer yok
            if (s_gddTitleStyle == null)
                s_gddTitleStyle = new GUIStyle(EditorStyles.label)
                    { fontStyle = FontStyle.Bold, normal = { textColor = EditorPaths.EditorColors.Info } };

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("DB Parametreleri (Data-Driven, Salt Okunur):", s_gddTitleStyle);
                if (narrow)
                {
                    EditorGUILayout.LabelField($"Direkler: {poleCount}  |  Renkler: {colorCount}  |  Kapasite: {maxCapacity}  |  Min Boş: {minEmptyPoles}");
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Direk: {poleCount}", GUILayout.Width(90f));
                        EditorGUILayout.LabelField($"Renk: {colorCount}", GUILayout.Width(80f));
                        EditorGUILayout.LabelField($"Kapasite: {maxCapacity}", GUILayout.Width(100f));
                        EditorGUILayout.LabelField($"Min Boş: {minEmptyPoles}", GUILayout.Width(100f));
                        // Live pole-count consistency check (data-driven)
                        if (poleCount > _cachedDatabase.LevelGen.PoleCountClamp)
                        {
                            var prevCol = GUI.color;
                            GUI.color = EditorPaths.EditorColors.Warning;
                            EditorGUILayout.LabelField($"⚠ >{_cachedDatabase.LevelGen.PoleCountClamp} clamp!", EditorStyles.miniBoldLabel);
                            GUI.color = prevCol;
                        }
                    }
                }
            }

            DrawDifficultyPreview(_levelIndex);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
                EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
            }

            EditorGUILayout.Space(4f);

            // RESP-2: Action row — responsive
            if (narrow)
            {
                if (GUILayout.Button("Tek Seviye Üret", GUILayout.Height(30))) Generate();
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Tek Seviye Üret", GUILayout.Height(30), GUILayout.ExpandWidth(true))) Generate();
                    if (GUILayout.Button("DB Seed ile Üret", GUILayout.Height(30), GUILayout.Width(140f)))
                    {
                        _seed = LevelGenerator.GetDeterministicSeed(_levelIndex);
                        Generate();
                    }
                }
            }

            _autoSave = EditorGUILayout.Toggle("Üretileni Otomatik Kaydet", _autoSave);

            RingFlowEditorUtils.EndSectionBox();

            // Toplu Seviye Üretim Ayarları
            RingFlowEditorUtils.BeginSectionBox("Toplu Seviye Üretim Paneli", "Seçilen seviye aralığındaki tüm bölümleri otomatik olarak GDD parametreleriyle üretir.");

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
                    $"{_batchStartLevel} ile {_batchEndLevel} arasındaki {count} seviye GDD eğrilerine göre üretilecek. Mevcut seviyelerinizin üzerine yazılabilir. Emin misiniz?", "Evet, Üret", "İptal"))
                {
                    GenerateAllLevels();
                }
            }

            RingFlowEditorUtils.EndSectionBox();

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

            RingFlowEditorUtils.BeginSectionBox("Üretilen Seviye Bilgileri", "Yapay zeka tarafından doğrulanmış bölüm detayları.");

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

            RingFlowEditorUtils.EndSectionBox();

            LevelVisualRenderer.Draw(_generatedLevel);

            RingFlowEditorUtils.BeginSectionBox("Yapay Zeka Çözüm Yolu", $"Durum: {_solveStatus}");

            if (_solutionSteps.Count > 0)
            {
                _solutionScroll = EditorGUILayout.BeginScrollView(_solutionScroll, GUILayout.Height(120));
                for (int i = 0; i < _solutionSteps.Count; i++)
                    EditorGUILayout.LabelField($"{i + 1}. {_solutionSteps[i]}");
                EditorGUILayout.EndScrollView();
            }

            RingFlowEditorUtils.EndSectionBox();
        }

        public void GenerateFromDashboard() { Generate(); }

        public void GenerateFromDashboardAll() { GenerateAllLevels(); }

        private void Generate()
        {
            var db = _cachedDatabase;
            // FIX-E1: Replace throw with DisplayDialog — throwing inside an editor method
            // crashes the window and prevents the user from correcting the problem.
            if (db == null)
            {
                EditorUtility.DisplayDialog("Veritabanı Eksik",
                    "[GeneratorSection.Generate] GameConfigDatabaseSO is required.", "Tamam");
                return;
            }
            if (_levelIndex < 1 || _levelIndex > db.TotalLevels)
            {
                EditorUtility.DisplayDialog("Geçersiz Seviye İndeksi",
                    $"Level index {_levelIndex} is out of range [1, {db.TotalLevels}]. " +
                    "Update DB TotalLevels or enter a valid index.", "Tamam");
                return;
            }

            int poleCount = db.GetPoleCountForLevel(_levelIndex);
            int colorCount = db.GetColorCountForLevel(_levelIndex);
            int maxCapacity = db.GetMaxCapacityForLevel(_levelIndex);
            int minEmptyPoles = db.GetMinEmptyPolesForLevel(_levelIndex);

            if (poleCount > db.LevelGen.PoleCountClamp)
            {
                EditorUtility.DisplayDialog("Direk Sayısı Sınırı Aşıldı",
                    $"Pole count {poleCount} exceeds PoleCountClamp {db.LevelGen.PoleCountClamp} for level {_levelIndex}. " +
                    "Update DB PoleCountClamp or adjust the level config.", "Tamam");
                return;
            }

            NexusLog.Info("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                $"Seviye {_levelIndex} üretiliyor: Seed={_seed}, Direkler={poleCount}, Renkler={colorCount}, Kapasite={maxCapacity}, BoşDirek={minEmptyPoles}");

            _generatedLevel = LevelGenerator.GenerateLevel(db, _levelIndex, _seed, poleCount, colorCount, maxCapacity);

            if (_generatedLevel == null)
            {
                _solveStatus = "Üretim başarısız (50 tohum denendi ve tükendi). DB yapılandırmasını kontrol edin.";
                _solutionSteps.Clear();
                return;
            }

            var board = BuildBoardStateFromLevelData(_generatedLevel, maxCapacity, out int[] portalTargets);
            var solveResult = LevelSolver.Solve(board, maxCapacity, portalTargets: portalTargets);
            int emptyPoleCount = CountEmptyPoles(_generatedLevel);
            int requiredMinEmpty = db.GetMinEmptyPolesForLevel(_generatedLevel.LevelIndex);

            if (emptyPoleCount < requiredMinEmpty)
            {
                _solveStatus = $"Geçersiz: en az {requiredMinEmpty} boş direk olmalı, bulunan: {emptyPoleCount}.";
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
            var db = _cachedDatabase;
            if (db == null)
                throw new System.InvalidOperationException("[GeneratorSection.GenerateAllLevels] GameConfigDatabaseSO is required.");

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

                    if (poles > db.LevelGen.PoleCountClamp)
                        throw new System.InvalidOperationException(
                            $"Pole count {poles} exceeds PoleCountClamp {db.LevelGen.PoleCountClamp} for level {level}. Update DB or adjust level config.");

                    if (poles < colors + minEmpty)
                        throw new System.InvalidOperationException(
                            $"Pole count {poles} < colors ({colors}) + minEmptyPoles ({minEmpty}) for level {level}. " +
                            "Update DB ColorCurve or DifficultyBands to ensure pole capacity.");

                    var levelData = LevelGenerator.GenerateLevel(db, level, LevelGenerator.GetDeterministicSeed(level), poles, colors, maxCap);
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

            // Notify LevelBrowser that disk state changed after batch generation.
            OnLevelAssetsChanged?.Invoke();

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

            // Notify LevelBrowser and any other listeners that disk state changed.
            OnLevelAssetsChanged?.Invoke();

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
                LevelType = source.LevelType,
                PoleCount = source.PoleCount,
                PoleCapacity = source.PoleCapacity,
                ColorCount = source.ColorCount,
                EmptyPoleCount = source.EmptyPoleCount,
                DifficultyScore = source.DifficultyScore,
                IsTutorial = source.IsTutorial,
                RuleReferences = source.RuleReferences != null ? new List<string>(source.RuleReferences) : new List<string>(),
                IsChallenge = source.IsChallenge,
                ProgressionFlags = source.ProgressionFlags,
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


        private static BoardState BuildBoardStateFromLevelData(LevelData levelData, int maxCapacity, out int[] portalTargets)
        {
            int poleCount = levelData?.Poles?.Count ?? 0;
            portalTargets = new int[poleCount];
            for (int i = 0; i < poleCount; i++) portalTargets[i] = -1;

            if (maxCapacity > BoardState.MaxSupportedCapacity)
                throw new System.InvalidOperationException($"Max capacity {maxCapacity} exceeds BoardState.MaxSupportedCapacity={BoardState.MaxSupportedCapacity}.");

            var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };
            for (int p = 0; p < poleCount; p++)
            {
                var pole = levelData.Poles[p];
                if (pole == null) continue;
                board.SetPoleLocked(p, pole.IsLocked);
                portalTargets[p] = pole.PortalTargetId;

                int count = pole.Rings?.Count ?? 0;
                board.SetRingCount(p, count);
                for (int r = 0; r < count; r++)
                {
                    var ring = pole.Rings[r];
                    board.SetRingColor(p, r, ring.Color);
                    board.SetRingType(p, r, ring.Type);
                    board.SetRingAdditional(p, r, ring.AdditionalData);
                }
                if (count > 0)
                    board.SetTopRingFrozen(p, pole.Rings[count - 1].Type == RingType.Frozen);
            }
            return board;
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
