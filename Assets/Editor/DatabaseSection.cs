using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    public sealed class DatabaseSection : EditorSection
    {
        public override string DisplayName => "Game Configuration Database";
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
                    "GameConfigDatabase asset not found in Resources. " +
                    "A default instance will be used at runtime, but you must create the asset file to modify parameters permanently.",
                    MessageType.Warning);

                if (GUILayout.Button("Create GameConfigDatabase Asset", GUILayout.Height(36)))
                {
                    CreateDatabaseAsset();
                }
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Database Asset Properties", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("Asset File", _database, typeof(GameConfigDatabaseSO), false);

                EditorGUILayout.Space(5f);

                EditorGUI.BeginChangeCheck();

                _database.TotalLevels = EditorGUILayout.IntField("Total Levels", _database.TotalLevels);

                EditorGUILayout.Space(10f);
                DrawDifficultyOverview();

                EditorGUILayout.Space(10f);
                // --- 1. DIFFICULTY BANDS ---
                EditorGUILayout.LabelField("Difficulty Bands Config", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    for (int i = 0; i < _database.DifficultyBands.Count; i++)
                    {
                        var bandData = _database.DifficultyBands[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(bandData.Band.ToString(), GUILayout.Width(80f));
                            
                            int maxLvl = EditorGUILayout.IntField("Max Lvl", bandData.MaxLevel, GUILayout.Width(110f));
                            int minEmpty = EditorGUILayout.IntField("Empty Poles", bandData.MinEmptyPoles, GUILayout.Width(110f));
                            int maxCap = EditorGUILayout.IntField("Cap", bandData.MaxCapacity, GUILayout.Width(90f));

                            bandData.MaxLevel = maxLvl;
                            bandData.MinEmptyPoles = minEmpty;
                            bandData.MaxCapacity = maxCap;
                            _database.DifficultyBands[i] = bandData;
                        }
                    }
                }

                EditorGUILayout.Space(10f);

                // --- 2. COLOR CURVE ---
                EditorGUILayout.LabelField("Color Progression Curve", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Level Threshold", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
                        EditorGUILayout.LabelField("Color Count", EditorStyles.miniBoldLabel, GUILayout.Width(100f));
                    }

                    for (int i = 0; i < _database.ColorCurve.Count; i++)
                    {
                        var pt = _database.ColorCurve[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            int threshold = EditorGUILayout.IntField(pt.LevelThreshold, GUILayout.Width(120f));
                            int colors = EditorGUILayout.IntSlider(pt.ColorCount, 3, 10, GUILayout.Width(200f));

                            pt.LevelThreshold = threshold;
                            pt.ColorCount = colors;
                            _database.ColorCurve[i] = pt;
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Add Curve Point", GUILayout.Width(120f)))
                        {
                            _database.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = 2000, ColorCount = 10 });
                        }
                        if (_database.ColorCurve.Count > 0 && GUILayout.Button("Remove Last Point", GUILayout.Width(120f)))
                        {
                            _database.ColorCurve.RemoveAt(_database.ColorCurve.Count - 1);
                        }
                    }
                }

                EditorGUILayout.Space(10f);

                // --- 3. WORLDS SELECTOR & EDITOR ---
                EditorGUILayout.LabelField("Worlds & Theme Config", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string[] worldNames = new string[_database.Worlds.Count];
                    for (int i = 0; i < _database.Worlds.Count; i++)
                    {
                        worldNames[i] = $"World {i + 1}: {_database.Worlds[i].Theme}";
                    }

                    _selectedWorldIndex = EditorGUILayout.Popup("Select World to Edit", _selectedWorldIndex, worldNames);

                    EditorGUILayout.Space(5f);

                    if (_selectedWorldIndex >= 0 && _selectedWorldIndex < _database.Worlds.Count)
                    {
                        var wData = _database.Worlds[_selectedWorldIndex];
                        
                        wData.Theme = EditorGUILayout.TextField("Theme Display Name", wData.Theme);
                        wData.UnlockedByWorldIndex = EditorGUILayout.IntField("Unlocked by World Index", wData.UnlockedByWorldIndex);
                        wData.IsEventWorld = EditorGUILayout.Toggle("Is Event (Boss) World", wData.IsEventWorld);
                        wData.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup("Special Mechanic Type", wData.MechanicType);

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
                    if (GUILayout.Button("Save Database Changes", GUILayout.Height(36)))
                    {
                        SaveDatabase();
                    }
                    if (GUILayout.Button("Reset to Default Settings", GUILayout.Height(36)))
                    {
                        if (EditorUtility.DisplayDialog("Reset Database",
                            "Are you sure you want to reset all database parameters to the default GDD rules? This will overwrite your custom settings.",
                            "Reset", "Cancel"))
                        {
                            _database.InitializeDefaults();
                            EditorUtility.SetDirty(_database);
                            SaveDatabase();
                        }
                    }
                }

                // --- 4. BATCH LEVEL VALIDATOR ---
                EditorGUILayout.Space(15f);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Batch Level Validator & Solver Benchmarker", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "Iterates over the selected range of level indices, generates each level, solves it, and reports timings & solvability.",
                        MessageType.Info);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _valStartLevel = EditorGUILayout.IntField("Start Level", _valStartLevel, GUILayout.Width(160f));
                        _valEndLevel = EditorGUILayout.IntField("End Level", _valEndLevel, GUILayout.Width(160f));
                    }

                    if (GUILayout.Button("Run Batch Validation", GUILayout.Height(30)))
                    {
                        RunBatchValidation();
                    }

                    if (_validationResults.Count > 0)
                    {
                        EditorGUILayout.Space(6f);
                        EditorGUILayout.LabelField($"Results ({_validationResults.Count})", EditorStyles.boldLabel);
                        
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

                        if (GUILayout.Button("Clear Results", GUILayout.Width(120f)))
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
                    res.Log = "FAILED - Generator returned null (unsolvable seed / scramble mismatch)";
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
                        res.Log = $"SUCCESS - Solvable in {res.MoveCount} moves (Solve time: {timeMs:F1}ms)";
                        solvedCount++;
                        totalTime += timeMs;
                        totalMoves += res.MoveCount;
                    }
                    else
                    {
                        res.Success = false;
                        res.Log = "FAILED - Solver returned unsolvable within state limit";
                    }
                }

                _validationResults.Add(res);
            }
            
            string summaryMsg = $"Validated {_validationResults.Count} levels.\n" +
                                $"Success: {solvedCount} / {_validationResults.Count}\n" +
                                $"Avg time: {(solvedCount > 0 ? (totalTime / solvedCount) : 0):F1}ms\n" +
                                $"Avg moves: {(solvedCount > 0 ? (totalMoves / (float)solvedCount) : 0):F1}";
            EditorUtility.DisplayDialog("Validation Complete", summaryMsg, "OK");
        }

        private const string DatabaseAssetPath = "Assets/Resources/GameConfigDatabase.asset";

        private void DrawDifficultyOverview()
        {
            if (_database == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Difficulty Overview", EditorStyles.boldLabel);
                for (int i = 0; i < _database.DifficultyBands.Count; i++)
                {
                    var band = _database.DifficultyBands[i];
                    EditorGUILayout.LabelField(
                        $"{band.Band}: Lvl≤{band.MaxLevel} | Empty={band.MinEmptyPoles} | Cap={band.MaxCapacity} | Mechanics={band.AllowedMechanics?.Count ?? 0}");
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
            EditorUtility.DisplayDialog("Success", $"GameConfigDatabase asset created successfully at {DatabaseAssetPath}!", "OK");
        }

        private void SaveDatabase()
        {
            if (_database == null) return;
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Database Saved", "All difficulty, color, and world configurations saved successfully to GameConfigDatabase.asset!", "OK");
        }
    }
}
