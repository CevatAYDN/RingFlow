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

        [System.NonSerialized] private LevelData _generatedLevel;
        private string _solveStatus = "No level loaded / generated.";
        private readonly System.Collections.Generic.List<string> _solutionSteps = new();
        private Vector2 _solutionScroll;

        public LevelData GeneratedLevel => _generatedLevel;

        public override string DisplayName => "Level Generator & AI Solver";
        public override string PrefKey => EditorPrefsKeys.FoldGenerator;

        public void OnEnable()
        {
            _levelIndex = EditorPrefs.GetInt(EditorPrefsKeys.LevelIndex, 1);
            _seed = EditorPrefs.GetInt(EditorPrefsKeys.Seed, 100);
            _poleCount = EditorPrefs.GetInt(EditorPrefsKeys.Poles, 4);
            _colorCount = EditorPrefs.GetInt(EditorPrefsKeys.Colors, 3);
            _maxCapacity = EditorPrefs.GetInt(EditorPrefsKeys.MaxCap, 4);
        }

        public void OnDisable()
        {
            EditorPrefs.SetInt(EditorPrefsKeys.LevelIndex, _levelIndex);
            EditorPrefs.SetInt(EditorPrefsKeys.Seed, _seed);
            EditorPrefs.SetInt(EditorPrefsKeys.Poles, _poleCount);
            EditorPrefs.SetInt(EditorPrefsKeys.Colors, _colorCount);
            EditorPrefs.SetInt(EditorPrefsKeys.MaxCap, _maxCapacity);
        }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _levelIndex  = EditorGUILayout.IntSlider("Level Index",   _levelIndex,  1, WorldConfigSO.TotalLevels);
                _seed        = EditorGUILayout.IntField  ("Random Seed",   _seed);
                _poleCount   = EditorGUILayout.IntSlider("Poles Count",   _poleCount,   3, 12);
                _colorCount  = EditorGUILayout.IntSlider("Colors Count",  _colorCount,  2, 10);
                _maxCapacity = EditorGUILayout.IntSlider("Max Ring Cap",  _maxCapacity, 3, 5);
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
                    if (GUILayout.Button("Apply GDD Curve", GUILayout.Height(ButtonHeight)))
                    {
                        ApplyGddCurveParams();
                    }
                    if (GUILayout.Button("Generate Level", GUILayout.Height(ButtonHeight)))
                    {
                        Generate();
                    }
                }

                if (_generatedLevel != null)
                {
                    EditorGUILayout.Space(2f);
                    if (GUILayout.Button("Save Generated Level as Asset", GUILayout.Height(ButtonHeight)))
                    {
                        SaveLevelAsset(_generatedLevel);
                    }
                }

                DrawGenerationResults();
            }
        }

        private void DrawGenerationResults()
        {
            if (_generatedLevel == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generated Level Info:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Level: {_generatedLevel.LevelIndex} | Poles: {_generatedLevel.Poles.Count}");
            EditorGUILayout.LabelField($"Target Moves: {_generatedLevel.TargetMoves} (Solver Verified)");

            DrawLevelVisual(_generatedLevel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI Solver Path:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Status: {_solveStatus}", EditorStyles.wordWrappedLabel);

            if (_solutionSteps.Count == 0) return;
            _solutionScroll = EditorGUILayout.BeginScrollView(_solutionScroll, GUILayout.Height(120));
            for (int i = 0; i < _solutionSteps.Count; i++)
            {
                EditorGUILayout.LabelField($"{i + 1}. {_solutionSteps[i]}");
            }
            EditorGUILayout.EndScrollView();
        }

        public static void DrawLevelVisual(LevelData levelData)
        {
            if (levelData == null || levelData.Poles == null || levelData.Poles.Count == 0)
            {
                EditorGUILayout.HelpBox("No level data to display.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Level Visual Preview:", EditorStyles.boldLabel);

            float poleWidth = 60f;
            float ringHeight = 18f;
            float poleGap = 8f;
            int maxCapacity = 4; // default GDD capacity

            // Determine max capacity dynamically based on level poles if possible
            if (levelData.Poles.Count > 0)
            {
                maxCapacity = levelData.Poles[0].MaxCapacity;
            }

            // Begin horizontal layout
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                for (int p = 0; p < levelData.Poles.Count; p++)
                {
                    var pole = levelData.Poles[p];

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(poleWidth)))
                    {
                        var poleLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                        EditorGUILayout.LabelField($"Pole {p}", poleLabelStyle, GUILayout.Width(poleWidth));

                        // Draw pole background (vertical column)
                        float height = maxCapacity * (ringHeight + 2f) + 12f;
                        Rect rect = GUILayoutUtility.GetRect(poleWidth, height);

                        Color colBg = pole.IsLocked ? new Color(0.18f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.16f, 0.18f, 1f);
                        Color borderCol = pole.IsLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.35f, 0.35f, 0.38f);
                        
                        EditorGUI.DrawRect(rect, colBg);
                        DrawRectBorder(rect, borderCol, 1);

                        // Draw rings from bottom to top
                        for (int r = 0; r < pole.Rings.Count; r++)
                        {
                            var ring = pole.Rings[r];
                            float ringY = rect.yMax - 6f - (r + 1) * (ringHeight + 2f);
                            Rect ringRect = new Rect(rect.x + 4f, ringY, poleWidth - 8f, ringHeight);

                            Color ringColor = RingPalette.Get(ring.Color);
                            EditorGUI.DrawRect(ringRect, ringColor);
                            DrawRectBorder(ringRect, Color.black, 1);

                            string ringLabel = GetRingShortLabel(ring.Type);
                            if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                            {
                                ringLabel += ring.AdditionalData;
                            }

                            var textStyle = new GUIStyle(EditorStyles.miniLabel)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                fontStyle = FontStyle.Bold,
                                normal = { textColor = GetContrastColor(ringColor) }
                            };
                            GUI.Label(ringRect, ringLabel, textStyle);
                        }

                        // Locked label at top of pole if locked
                        if (pole.IsLocked)
                        {
                            Rect lockRect = new Rect(rect.x + 3f, rect.y + 4f, poleWidth - 6f, 13f);
                            EditorGUI.DrawRect(lockRect, new Color(0.8f, 0.1f, 0.1f, 0.9f));
                            var lockStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                            {
                                alignment = TextAnchor.MiddleCenter,
                                normal = { textColor = Color.white }
                            };
                            GUI.Label(lockRect, "LOCKED", lockStyle);
                        }
                    }

                    if (p < levelData.Poles.Count - 1)
                    {
                        GUILayout.Space(poleGap);
                    }
                }
            }
            EditorGUILayout.Space(5f);
        }

        private static string GetRingShortLabel(RingType type)
        {
            return type switch
            {
                RingType.Standard => "STD",
                RingType.Key => "KEY",
                RingType.Mystery => "MYS",
                RingType.Frozen => "FRZ",
                RingType.Locked => "LCK",
                RingType.Stone => "STN",
                RingType.Glass => "GLS",
                RingType.Rainbow => "RNB",
                RingType.Bomb => "BMB",
                RingType.Chain => "CHN",
                RingType.Magnet => "MAG",
                RingType.Paint => "PNT",
                RingType.Ghost => "GHS",
                _ => "???"
            };
        }

        private static Color GetContrastColor(Color color)
        {
            float y = (color.r * 299 + color.g * 587 + color.b * 114) / 1000f;
            return y >= 0.5f ? Color.black : Color.white;
        }

        private static void DrawRectBorder(Rect rect, Color color, int width)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
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
                $"Applied GDD curve: Poles={_poleCount}, Colors={_colorCount}, MaxCapacity={_maxCapacity}");
        }

        public void GenerateFromDashboard()
        {
            Generate();
        }

        private void Generate()
        {
            if (_levelIndex < 1 || _levelIndex > WorldConfigSO.TotalLevels)
            {
                NexusLog.Warn("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                    $"Level index out of bounds, adjusting.");
                _levelIndex = Mathf.Clamp(_levelIndex, 1, WorldConfigSO.TotalLevels);
            }
            if (_poleCount < _colorCount + 1) _poleCount = _colorCount + 1;
            if (_poleCount > 12) _poleCount = 12;

            NexusLog.Info("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                $"Generating level {_levelIndex}: Seed={_seed}, Poles={_poleCount}, Colors={_colorCount}, Cap={_maxCapacity}");

            _generatedLevel = LevelGenerator.GenerateLevel(_levelIndex, _seed, _poleCount, _colorCount, _maxCapacity);

            if (_generatedLevel == null)
            {
                _solveStatus = "Generation failed (exhausted 50 seeds).";
                _solutionSteps.Clear();
                return;
            }

            // Solver validation board'u raw data ile doldur (Paint/Rainbow dönüşümleri uygulanmaz)
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
            NexusLog.Info("RingFlowEditor", nameof(Generate), _levelIndex.ToString(),
                $"Solver board built from raw data ({_generatedLevel.Poles.Count} poles, using direct state copy).");

            var solveResult = LevelSolver.Solve(board, _maxCapacity);
            if (solveResult.IsSolvable)
            {
                _solveStatus = $"Solvable in {solveResult.MoveCount} moves.";
                _solutionSteps.Clear();
                foreach (var move in solveResult.Moves)
                {
                    _solutionSteps.Add($"Move top ring from Pole {move.FromPoleId} to Pole {move.ToPoleId}");
                }
            }
            else
            {
                _solveStatus = "Unsolvable! Seed failed validation.";
                _solutionSteps.Clear();
            }
        }

        private void SaveLevelAsset(LevelData levelData)
        {
            if (levelData == null) return;

            string folderPath = "Assets/Resources/Levels";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Levels");
            }

            string assetPath = $"{folderPath}/Level_{levelData.LevelIndex}.asset";
            
            // Check if asset already exists
            LevelDataSO levelSO = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
            bool isNew = false;
            if (levelSO == null)
            {
                levelSO = ScriptableObject.CreateInstance<LevelDataSO>();
                isNew = true;
            }

            // Clone data
            levelSO.Data = CloneLevelData(levelData);

            if (isNew)
            {
                AssetDatabase.CreateAsset(levelSO, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(levelSO);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Level saved to asset: {assetPath}\nThis level will now load automatically at runtime!", "OK");
        }

        private LevelData CloneLevelData(LevelData source)
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
                var poleClone = new PoleData(p.MaxCapacity)
                {
                    IsLocked = p.IsLocked,
                    Rings = new List<RingData>()
                };
                foreach (var r in p.Rings)
                {
                    poleClone.Rings.Add(new RingData(r.Color, r.Type, r.AdditionalData));
                }
                clone.Poles.Add(poleClone);
            }

            return clone;
        }
    }
}
