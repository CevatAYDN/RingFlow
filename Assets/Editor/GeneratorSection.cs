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
            _levelIndex  = EditorPrefs.GetInt(EditorPrefsKeys.LevelIndex, 1);
            _seed        = EditorPrefs.GetInt(EditorPrefsKeys.Seed, 100);
            _poleCount   = EditorPrefs.GetInt(EditorPrefsKeys.Poles, 4);
            _colorCount  = EditorPrefs.GetInt(EditorPrefsKeys.Colors, 3);
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

            var board = new BoardState { PoleCount = _generatedLevel.Poles.Count };
            for (int p = 0; p < _generatedLevel.Poles.Count; p++)
            {
                var pole = _generatedLevel.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    board.AddRing(p, pole.Rings[r]);
                }
            }

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
    }
}
