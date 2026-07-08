using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Advanced custom inspector for LevelDataSO asset files.
    /// Provides a visual columnar preview of the level's poles and rings,
    /// along with tools to validate, solve, and modify the level in real-time.
    /// </summary>
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : UnityEditor.Editor
    {
        private bool _showRawData = false;

        // Static brush states so they persist during editor selection changes
        private static RingColor _brushColor = RingColor.Red;
        private static RingType _brushType = RingType.Standard;
        private static bool _eraserMode = false;
        private static int _bombCounter = 3;

        public override void OnInspectorGUI()
        {
            var levelSO = (LevelDataSO)target;
            if (levelSO == null || levelSO.Data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            // Premium header
            DrawHeader($"LEVEL {levelSO.Data.LevelIndex} CONFIGURATION");

            // Meta fields
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                levelSO.Data.LevelIndex = EditorGUILayout.IntField("Level Index", levelSO.Data.LevelIndex);
                levelSO.Data.Seed = EditorGUILayout.IntField("Random Seed", levelSO.Data.Seed);
                levelSO.Data.TargetMoves = EditorGUILayout.IntField("Target Moves", levelSO.Data.TargetMoves);
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(levelSO);
                }
            }

            // Draw Paint Brush Toolbars
            DrawColorPalette();
            DrawTypePalette();

            // Visual preview section (Interactive)
            DrawLevelVisualInteractive(levelSO.Data, levelSO);

            // Tools section
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Designer Tools", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Verify & Solve Level", GUILayout.Height(30)))
                    {
                        VerifyAndSolve(levelSO);
                    }

                    if (GUILayout.Button("Re-Scramble Level", GUILayout.Height(30)))
                    {
                        ReScramble(levelSO);
                    }
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add Empty Pole", GUILayout.Height(24)))
                    {
                        int maxCap = levelSO.Data.Poles.Count > 0 ? levelSO.Data.Poles[0].MaxCapacity : 4;
                        Undo.RecordObject(levelSO, "Add Pole");
                        levelSO.Data.Poles.Add(new PoleData(maxCap));
                        EditorUtility.SetDirty(levelSO);
                    }

                    if (levelSO.Data.Poles.Count > 0 && GUILayout.Button("Remove Last Pole", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Pole", "Remove last pole from this level configuration?", "Remove", "Cancel"))
                        {
                            Undo.RecordObject(levelSO, "Remove Pole");
                            levelSO.Data.Poles.RemoveAt(levelSO.Data.Poles.Count - 1);
                            EditorUtility.SetDirty(levelSO);
                        }
                    }
                }
            }

            // Raw data foldout
            EditorGUILayout.Space(5f);
            _showRawData = EditorGUILayout.Foldout(_showRawData, "Raw Serialized Data", true);
            if (_showRawData)
            {
                // Render default object inspector fields
                DrawPropertiesExcluding(serializedObject, "m_Script");
            }

            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
            {
                EditorUtility.SetDirty(levelSO);
            }
        }

        private void VerifyAndSolve(LevelDataSO levelSO)
        {
            var board = new BoardState { PoleCount = levelSO.Data.Poles.Count };
            for (int p = 0; p < levelSO.Data.Poles.Count; p++)
            {
                var pole = levelSO.Data.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    board.AddRing(p, pole.Rings[r]);
                }
            }

            int maxCapacity = levelSO.Data.Poles.Count > 0 ? levelSO.Data.Poles[0].MaxCapacity : 4;
            var solveResult = LevelSolver.Solve(board, maxCapacity);

            if (solveResult.IsSolvable)
            {
                Undo.RecordObject(levelSO, "Update Target Moves");
                levelSO.Data.TargetMoves = solveResult.MoveCount;
                EditorUtility.DisplayDialog("Solver Results", 
                    $"The level is SOLVABLE!\nOptimal moves required: {solveResult.MoveCount} (TargetMoves has been updated).", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Solver Results", 
                    "The level is UNSOLVABLE!\nNo sequence of valid moves can solve this configuration.", "OK");
            }
            EditorUtility.SetDirty(levelSO);
        }

        private void ReScramble(LevelDataSO levelSO)
        {
            if (levelSO.Data.Poles.Count == 0) return;
            
            int maxCap = levelSO.Data.Poles[0].MaxCapacity;
            int colorCount = 0;
            
            // Gather unique colors
            var colorsList = new HashSet<RingColor>();
            foreach (var pole in levelSO.Data.Poles)
            {
                foreach (var ring in pole.Rings)
                {
                    if (ring.Color != RingColor.None)
                        colorsList.Add(ring.Color);
                }
            }

            colorCount = colorsList.Count;
            if (colorCount == 0)
            {
                EditorUtility.DisplayDialog("Re-Scramble Error", "No colored rings found in the level to scramble.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Re-Scramble", 
                $"Re-generate and scramble this level index {levelSO.Data.LevelIndex} using seed {levelSO.Data.Seed}?", "Scramble", "Cancel"))
            {
                Undo.RecordObject(levelSO, "Re-Scramble Level");
                var generated = LevelGenerator.GenerateLevel(
                    levelSO.Data.LevelIndex, 
                    levelSO.Data.Seed, 
                    levelSO.Data.Poles.Count, 
                    colorCount, 
                    maxCap);

                if (generated != null)
                {
                    levelSO.Data = generated;
                    EditorUtility.DisplayDialog("Re-Scramble", "Level generated and scrambled successfully!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Re-Scramble Failed", "Unable to generate a solvable configuration with these parameters.", "OK");
                }
            }
        }

        private void DrawColorPalette()
        {
            EditorGUILayout.LabelField("Select Color Brush:", EditorStyles.boldLabel);
            
            var colors = (RingColor[])System.Enum.GetValues(typeof(RingColor));
            var prevColor = GUI.backgroundColor;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // Eraser mode button
                GUI.backgroundColor = _eraserMode ? Color.red : Color.gray;
                var eraserStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = _eraserMode ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = _eraserMode ? Color.white : Color.black }
                };
                if (GUILayout.Button("ERASER", eraserStyle, GUILayout.Width(70), GUILayout.Height(24)))
                {
                    _eraserMode = true;
                }
                GUI.backgroundColor = prevColor;

                GUILayout.Space(8f);

                for (int i = 1; i < colors.Length; i++) // skip None
                {
                    var color = colors[i];
                    Color c = RingPalette.Get(color);
                    
                    bool isSelected = (!_eraserMode && _brushColor == color);
                    GUI.backgroundColor = c;
                    
                    string label = isSelected ? $"[{color.ToString().Substring(0, 3).ToUpper()}]" : color.ToString().Substring(0, 3).ToUpper();
                    
                    var style = new GUIStyle(GUI.skin.button)
                    {
                        fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        fontSize = 9,
                        normal = { textColor = GetContrastColor(c) }
                    };

                    if (GUILayout.Button(label, style, GUILayout.Width(42), GUILayout.Height(24)))
                    {
                        _brushColor = color;
                        _eraserMode = false;
                    }
                }
                GUI.backgroundColor = prevColor;
            }
        }

        private void DrawTypePalette()
        {
            if (_eraserMode) return;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Select Ring Type Brush:", EditorStyles.boldLabel);

            var types = (RingType[])System.Enum.GetValues(typeof(RingType));
            var prevBg = GUI.backgroundColor;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = 0; i < types.Length; i++)
                    {
                        var type = types[i];
                        bool isSelected = _brushType == type;

                        var style = new GUIStyle(GUI.skin.button)
                        {
                            fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                            fontSize = 9
                        };

                        if (isSelected)
                        {
                            GUI.backgroundColor = new Color(0.2f, 0.8f, 1.0f);
                            if (GUILayout.Button(type.ToString(), style, GUILayout.Height(20)))
                            {
                                _brushType = type;
                            }
                            GUI.backgroundColor = prevBg;
                        }
                        else
                        {
                            if (GUILayout.Button(type.ToString(), style, GUILayout.Height(20)))
                            {
                                _brushType = type;
                            }
                        }

                        // Wrap after 5 elements
                        if ((i + 1) % 5 == 0 && i < types.Length - 1)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                        }
                    }
                }

                if (_brushType == RingType.Bomb)
                {
                    EditorGUILayout.Space(2f);
                    _bombCounter = EditorGUILayout.IntSlider("Bomb Counter Value", _bombCounter, 1, 9);
                }
            }
        }

        private void DrawLevelVisualInteractive(LevelData levelData, LevelDataSO levelSO)
        {
            if (levelData == null || levelData.Poles == null || levelData.Poles.Count == 0)
            {
                EditorGUILayout.HelpBox("No level data to display.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Visual Board Designer (Click to Draw / Edit):", EditorStyles.boldLabel);

            float poleWidth = 70f;
            float ringHeight = 20f;
            float poleGap = 8f;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var prevColor = GUI.backgroundColor;
                for (int p = 0; p < levelData.Poles.Count; p++)
                {
                    var pole = levelData.Poles[p];

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(poleWidth)))
                    {
                        // Pole title
                        var poleLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                        EditorGUILayout.LabelField($"Pole {p}", poleLabelStyle, GUILayout.Width(poleWidth));

                        // Column layout
                        int maxCapacity = pole.MaxCapacity;
                        float height = maxCapacity * (ringHeight + 2f) + 12f;
                        Rect rect = GUILayoutUtility.GetRect(poleWidth, height);

                        Color colBg = pole.IsLocked ? new Color(0.18f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.16f, 0.18f, 1f);
                        Color borderCol = pole.IsLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.35f, 0.35f, 0.38f);
                        
                        EditorGUI.DrawRect(rect, colBg);
                        DrawRectBorder(rect, borderCol, 1);

                        // Draw slots from bottom to top
                        for (int r = 0; r < maxCapacity; r++)
                        {
                            float ringY = rect.yMax - 6f - (r + 1) * (ringHeight + 2f);
                            Rect ringRect = new Rect(rect.x + 4f, ringY, poleWidth - 8f, ringHeight);

                            bool hasRing = r < pole.Rings.Count;
                            bool isAddSlot = r == pole.Rings.Count;

                            if (hasRing)
                            {
                                var ring = pole.Rings[r];
                                Color ringColor = RingPalette.Get(ring.Color);
                                GUI.backgroundColor = ringColor;

                                string label = GetRingShortLabel(ring.Type);
                                if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                                {
                                    label += ring.AdditionalData;
                                }

                                var style = new GUIStyle(GUI.skin.button)
                                {
                                    fontStyle = FontStyle.Bold,
                                    fontSize = 8,
                                    normal = { textColor = GetContrastColor(ringColor) }
                                };

                                if (GUI.Button(ringRect, label, style))
                                {
                                    Undo.RecordObject(levelSO, "Modify Ring");
                                    if (_eraserMode)
                                    {
                                        pole.Rings.RemoveAt(r);
                                    }
                                    else
                                    {
                                        pole.Rings[r] = new RingData(_brushColor, _brushType, _brushType == RingType.Bomb ? _bombCounter : 0);
                                    }
                                    EditorUtility.SetDirty(levelSO);
                                }
                            }
                            else if (isAddSlot)
                            {
                                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                                var style = new GUIStyle(GUI.skin.button)
                                {
                                    fontStyle = FontStyle.Normal,
                                    fontSize = 11,
                                    normal = { textColor = Color.white }
                                };

                                if (!_eraserMode && GUI.Button(ringRect, "+", style))
                                {
                                    Undo.RecordObject(levelSO, "Add Ring");
                                    pole.Rings.Add(new RingData(_brushColor, _brushType, _brushType == RingType.Bomb ? _bombCounter : 0));
                                    EditorUtility.SetDirty(levelSO);
                                }
                            }
                            else
                            {
                                Rect emptyRect = new Rect(rect.x + 6f, ringY + 2f, poleWidth - 12f, ringHeight - 4f);
                                EditorGUI.DrawRect(emptyRect, new Color(0.25f, 0.25f, 0.25f, 0.2f));
                                DrawRectBorder(emptyRect, new Color(0.35f, 0.35f, 0.38f, 0.3f), 1);
                            }
                        }

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

                        GUI.backgroundColor = prevColor;
                        EditorGUILayout.Space(2f);

                        // Settings below pole
                        EditorGUI.BeginChangeCheck();
                        bool isLockedNew = EditorGUILayout.Toggle("Locked", pole.IsLocked, GUILayout.Width(poleWidth));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(levelSO, "Toggle Locked Pole");
                            pole.IsLocked = isLockedNew;
                            EditorUtility.SetDirty(levelSO);
                        }

                        EditorGUI.BeginChangeCheck();
                        int capNew = EditorGUILayout.IntField("Cap", pole.MaxCapacity, GUILayout.Width(poleWidth));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(levelSO, "Change Pole Capacity");
                            pole.MaxCapacity = Mathf.Clamp(capNew, 2, 8);
                            while (pole.Rings.Count > pole.MaxCapacity)
                            {
                                pole.Rings.RemoveAt(pole.Rings.Count - 1);
                            }
                            EditorUtility.SetDirty(levelSO);
                        }

                        EditorGUILayout.Space(2f);
                        if (GUILayout.Button("Clear", GUILayout.Width(poleWidth), GUILayout.Height(18)))
                        {
                            Undo.RecordObject(levelSO, "Clear Pole");
                            pole.Rings.Clear();
                            EditorUtility.SetDirty(levelSO);
                        }
                    }

                    if (p < levelData.Poles.Count - 1)
                    {
                        GUILayout.Space(poleGap);
                    }
                }
                GUI.backgroundColor = prevColor;
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

        private static void DrawHeader(string title)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.8f, 1.0f) }
            };
            
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            GUILayout.Box(title, style, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            GUI.backgroundColor = bg;
            
            EditorGUILayout.Space(2f);
        }
    }
}
