using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using System.IO;

namespace RingFlow.Editor
{
    public sealed class DatabaseSection : EditorSection
    {
        public override string DisplayName => "Game Configuration Database";
        public override string PrefKey => "RF_FoldDatabase";

        private GameConfigDatabaseSO _database;
        private int _selectedWorldIndex = 0;
        private Vector2 _bandsScroll;
        private Vector2 _colorsScroll;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            // Load or create database asset
            if (_database == null)
            {
                _database = Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");
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
            }
        }

        private void CreateDatabaseAsset()
        {
            var db = ScriptableObject.CreateInstance<GameConfigDatabaseSO>();
            db.InitializeDefaults();

            const string parentDir = "Assets/Resources";
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            const string assetPath = parentDir + "/GameConfigDatabase.asset";
            AssetDatabase.CreateAsset(db, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _database = db;
            EditorUtility.DisplayDialog("Success", $"GameConfigDatabase asset created successfully at {assetPath}!", "OK");
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
