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
        private bool _autoSave = true;
        private bool _generateInProgress;
        private int _batchLevelCount = 100;

        [System.NonSerialized] private LevelData _generatedLevel;
        private string _solveStatus = "Seviye yüklenmedi / üretilmedi.";
        private readonly List<string> _solutionSteps = new();
        private Vector2 _solutionScroll;

        public LevelData GeneratedLevel => _generatedLevel;

        public override string DisplayName => "Seviye Üretici & Yapay Zeka Çözücü";
        public override string PrefKey => EditorPrefsKeys.FoldGenerator;

        public void OnEnable()
        {
            _levelIndex = EditorPrefs.GetInt(EditorPrefsKeys.LevelIndex, 1);
            _seed = EditorPrefs.GetInt(EditorPrefsKeys.Seed, 100);
            _poleCount = EditorPrefs.GetInt(EditorPrefsKeys.Poles, 4);
            _colorCount = EditorPrefs.GetInt(EditorPrefsKeys.Colors, 3);
            _maxCapacity = EditorPrefs.GetInt(EditorPrefsKeys.MaxCap, 4);
            _batchLevelCount = EditorPrefs.GetInt("RF_BatchLevelCount", 100);
        }

        public void OnDisable()
        {
            EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
            EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
            EditorPrefs.SetInt(EditorPrefsKeys.Poles, _poleCount);
            EditorPrefs.SetInt(EditorPrefsKeys.Colors, _colorCount);
            EditorPrefs.SetInt(EditorPrefsKeys.MaxCap, _maxCapacity);
            EditorPrefs.SetInt("RF_BatchLevelCount", _batchLevelCount);
        }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _levelIndex  = EditorGUILayout.IntSlider("Seviye Endeksi",   _levelIndex,  1, WorldConfigSO.TotalLevels);
                _seed        = EditorGUILayout.IntField  ("Rastgele Tohum (Seed)",   _seed);
                _poleCount   = EditorGUILayout.IntSlider("Direk Sayısı",   _poleCount,   3, 12);
                _colorCount  = EditorGUILayout.IntSlider("Renk Sayısı",  _colorCount,  2, 10);
                _maxCapacity = EditorGUILayout.IntSlider("Maks. Halka Kapasitesi",  _maxCapacity, 3, 5);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
                    EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
                    EditorPrefs.SetInt(EditorPrefsKeys.Poles, _poleCount);
                    EditorPrefs.SetInt(EditorPrefsKeys.Colors, _colorCount);
                    EditorPrefs.SetInt(EditorPrefsKeys.MaxCap, _maxCapacity);
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("GDD Zorluk Eğrisini Uygula", GUILayout.Height(ButtonHeight)))
                        ApplyGddCurveParams();

                    if (GUILayout.Button("Tek Seviye Üret", GUILayout.Height(ButtonHeight)))
                        Generate();
                }

                _autoSave = EditorGUILayout.Toggle("Otomatik Asset Olarak Kaydet", _autoSave);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Toplu Seviye Üretim Ayarları", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    _batchLevelCount = EditorGUILayout.IntField("Üretilecek Seviye Adedi", _batchLevelCount);
                    _batchLevelCount = Mathf.Clamp(_batchLevelCount, 1, WorldConfigSO.TotalLevels);

                    if (GUILayout.Button($"{_batchLevelCount} Seviyeyi Toplu Üret", GUILayout.Height(ButtonHeight)))
                        GenerateAllLevels();
                }

                if (_generatedLevel != null && !_autoSave &&
                    GUILayout.Button("Mevcut Seviyeyi Asset Olarak Kaydet", GUILayout.Height(ButtonHeight)))
                    SaveLevelAsset(_generatedLevel);

                if (_generateInProgress)
                    EditorGUILayout.HelpBox("Tüm seviyeler üretiliyor... İlerleme için Konsolu kontrol edin.", MessageType.Info);

                DrawGenerationResults();
            }
        }

        private void DrawGenerationResults()
        {
            if (_generatedLevel == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Üretilen Seviye Bilgileri:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Seviye: {_generatedLevel.LevelIndex} | Direkler: {_generatedLevel.Poles.Count}");
            EditorGUILayout.LabelField($"Hedef Hamle Sayısı: {_generatedLevel.TargetMoves} (Yapay Zeka Doğruladı)");

            DrawLevelVisual(_generatedLevel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Yapay Zeka Çözüm Yolu:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Durum: {_solveStatus}", EditorStyles.wordWrappedLabel);

            if (_solutionSteps.Count == 0) return;
            _solutionScroll = EditorGUILayout.BeginScrollView(_solutionScroll, GUILayout.Height(120));
            for (int i = 0; i < _solutionSteps.Count; i++)
                EditorGUILayout.LabelField($"{i + 1}. {_solutionSteps[i]}");
            EditorGUILayout.EndScrollView();
        }

        public static void DrawLevelVisual(LevelData levelData)
        {
            if (levelData == null || levelData.Poles == null || levelData.Poles.Count == 0)
            {
                EditorGUILayout.HelpBox("Gösterilecek seviye verisi yok.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Seviye Önizleme Görseli:", EditorStyles.boldLabel);

            float poleWidth = 60f;
            float ringHeight = 18f;
            float poleGap = 8f;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                for (int p = 0; p < levelData.Poles.Count; p++)
                {
                    var pole = levelData.Poles[p];
                    int poleMaxCap = pole.MaxCapacity;

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(poleWidth)))
                    {
                        EditorGUILayout.LabelField($"Direk {p}", RingFlowEditorUtils.CenteredMiniLabel, GUILayout.Width(poleWidth));

                        float height = poleMaxCap * (ringHeight + 2f) + 12f;
                        Rect rect = GUILayoutUtility.GetRect(poleWidth, height);

                        Color colBg = pole.IsLocked ? new Color(0.18f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.16f, 0.18f, 1f);
                        Color borderCol = pole.IsLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.35f, 0.35f, 0.38f);

                        EditorGUI.DrawRect(rect, colBg);
                        RingFlowEditorUtils.DrawRectBorder(rect, borderCol, 1);

                        for (int r = 0; r < pole.Rings.Count; r++)
                        {
                            var ring = pole.Rings[r];
                            float ringY = rect.yMax - 6f - (r + 1) * (ringHeight + 2f);
                            Rect ringRect = new Rect(rect.x + 4f, ringY, poleWidth - 8f, ringHeight);

                            Color ringColor = RingPalette.Get(ring.Color);
                            EditorGUI.DrawRect(ringRect, ringColor);
                            RingFlowEditorUtils.DrawRectBorder(ringRect, Color.black, 1);

                            string ringLabel = RingFlowEditorUtils.GetRingShortLabel(ring.Type);
                            if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                                ringLabel += ring.AdditionalData;

                            var textStyle = new GUIStyle(RingFlowEditorUtils.CenteredMiniLabel)
                            {
                                fontStyle = FontStyle.Bold,
                                normal = { textColor = RingFlowEditorUtils.GetContrastColor(ringColor) }
                            };
                            GUI.Label(ringRect, ringLabel, textStyle);
                        }

                        if (pole.IsLocked)
                        {
                            Rect lockRect = new Rect(rect.x + 3f, rect.y + 4f, poleWidth - 6f, 13f);
                            EditorGUI.DrawRect(lockRect, new Color(0.8f, 0.1f, 0.1f, 0.9f));
                            var lockStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                                { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                            GUI.Label(lockRect, "KİLİTLİ", lockStyle);
                        }
                    }

                    if (p < levelData.Poles.Count - 1)
                        GUILayout.Space(poleGap);
                }
            }
            EditorGUILayout.Space(5f);
        }

        private void ApplyGddCurveParams()
        {
            _poleCount   = DifficultyCurve.PoleCountForLevel(_levelIndex);
            _colorCount  = DifficultyCurve.ColorCountForLevel(_levelIndex);
            _maxCapacity = DifficultyCurve.MaxCapacityForLevel(_levelIndex);
            if (_poleCount < _colorCount + 1) _poleCount = _colorCount + 1;
            if (_poleCount > 12) _poleCount = 12;

            EditorPrefs.SetInt(EditorPrefsKeys.Poles, _poleCount);
            EditorPrefs.SetInt(EditorPrefsKeys.Colors, _colorCount);
            EditorPrefs.SetInt(EditorPrefsKeys.MaxCap, _maxCapacity);

            NexusLog.Info("RingFlowEditor", nameof(ApplyGddCurveParams), _levelIndex.ToString(),
                $"GDD eğrisi uygulandı: Direkler={_poleCount}, Renkler={_colorCount}, Kapasite={_maxCapacity}");
        }

        public void GenerateFromDashboard() { Generate(); }

        public void GenerateFromDashboardAll() { GenerateAllLevels(); }

        private void Generate()
        {
            if (_levelIndex < 1 || _levelIndex > WorldConfigSO.TotalLevels)
            {
                NexusLog.Warn("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                    "Seviye endeksi sınırların dışında, düzeltiliyor.");
                _levelIndex = Mathf.Clamp(_levelIndex, 1, WorldConfigSO.TotalLevels);
            }
            if (_poleCount < _colorCount + 1) _poleCount = _colorCount + 1;
            if (_poleCount > 12) _poleCount = 12;

            NexusLog.Info("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                $"Seviye {_levelIndex} üretiliyor: Seed={_seed}, Direkler={_poleCount}, Renkler={_colorCount}, Kapasite={_maxCapacity}");

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
            if (solveResult.IsSolvable && solveResult.MoveCount > 0)
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

            string folderPath = "Assets/Resources/Levels";
            EnsureLevelsFolder(folderPath);

            int okCount = 0;
            int failed = 0;
            int total = _batchLevelCount;

            int originalPoleCount = _poleCount;
            int originalColorCount = _colorCount;
            int originalMaxCapacity = _maxCapacity;

            try
            {
                for (int level = 1; level <= total; level++)
                {
                    int poles = DifficultyCurve.PoleCountForLevel(level);
                    int colors = DifficultyCurve.ColorCountForLevel(level);
                    int maxCap = DifficultyCurve.MaxCapacityForLevel(level);
                    if (poles < colors + 1) poles = colors + 1;
                    if (poles > 12) poles = 12;

                    var levelData = LevelGenerator.GenerateLevel(level, 100 + level, poles, colors, maxCap);
                    if (levelData == null)
                    {
                        failed++;
                        NexusLog.Warn("RingFlowEditor", nameof(GenerateAllLevels), level.ToString(),
                            $"Level {level} üretilemedi (tohumlar tükendi). Atlanıyor.");
                    }
                    else
                    {
                        SaveLevelAssetSilent(levelData, folderPath);
                        okCount++;
                    }

                    if (level % 10 == 0 || level == total)
                    {
                        EditorUtility.DisplayProgressBar("Toplu Seviye Üretiliyor",
                            $"Seviye işleniyor: {level}/{total} ({okCount} başarılı, {failed} başarısız)...",
                            (float)level / total);
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

            string folderPath = "Assets/Resources/Levels";
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
                var poleClone = new PoleData(p.MaxCapacity) { IsLocked = p.IsLocked, Rings = new List<RingData>() };
                foreach (var r in p.Rings)
                    poleClone.Rings.Add(new RingData(r.Color, r.Type, r.AdditionalData));
                clone.Poles.Add(poleClone);
            }

            return clone;
        }
    }
}
