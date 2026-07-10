using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : UnityEditor.Editor
    {
        private bool _showRawData;

        private static RingColor s_brushColor = RingColor.Red;
        private static RingType s_brushType = RingType.Standard;
        private static bool s_eraserMode;
        private static int s_bombCounter = 3;

        private static GUIStyle s_compactButtonStyle;
        private static GUIStyle s_boldButtonStyle;
        private static GUIStyle s_headerStyle;

        private static GUIStyle CompactButton => s_compactButtonStyle ??= new GUIStyle(GUI.skin.button)
            { fontSize = 9, fontStyle = FontStyle.Normal };

        private static GUIStyle BoldCompactButton => s_boldButtonStyle ??= new GUIStyle(GUI.skin.button)
            { fontSize = 8, fontStyle = FontStyle.Bold };

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (s_headerStyle == null)
                {
                    s_headerStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.2f, 0.8f, 1.0f) }
                    };
                }
                return s_headerStyle;
            }
        }

        public override void OnInspectorGUI()
        {
            var levelSO = (LevelDataSO)target;
            if (levelSO == null || levelSO.Data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            DrawHeader($"LEVEL {levelSO.Data.LevelIndex} CONFIGURATION");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                levelSO.Data.LevelIndex = EditorGUILayout.IntField("Level Index", levelSO.Data.LevelIndex);
                levelSO.Data.Seed = EditorGUILayout.IntField("Random Seed", levelSO.Data.Seed);
                levelSO.Data.TargetMoves = EditorGUILayout.IntField("Target Moves", levelSO.Data.TargetMoves);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(levelSO);
            }

            DrawColorPalette();
            DrawTypePalette();
            DrawLevelVisualInteractive(levelSO.Data, levelSO);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Level Designer Tools", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Verify & Solve Level", GUILayout.Height(30)))
                        VerifyAndSolve(levelSO);

                    if (GUILayout.Button("Re-Scramble Level", GUILayout.Height(30)))
                        ReScramble(levelSO);
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

            EditorGUILayout.Space(5f);
            _showRawData = EditorGUILayout.Foldout(_showRawData, "Raw Serialized Data", true);
            if (_showRawData)
                DrawPropertiesExcluding(serializedObject, "m_Script");

            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
                EditorUtility.SetDirty(levelSO);
        }

        private void VerifyAndSolve(LevelDataSO levelSO)
        {
            var board = new BoardState { PoleCount = levelSO.Data.Poles.Count };
            for (int p = 0; p < levelSO.Data.Poles.Count; p++)
            {
                var pole = levelSO.Data.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                    board.AddRing(p, pole.Rings[r]);
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
            var colorsList = new HashSet<RingColor>();
            foreach (var pole in levelSO.Data.Poles)
                foreach (var ring in pole.Rings)
                    if (ring.Color != RingColor.None)
                        colorsList.Add(ring.Color);

            int colorCount = colorsList.Count;
            if (colorCount == 0)
            {
                EditorUtility.DisplayDialog("Re-Scramble Error", "No colored rings found in the level to scramble.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Re-Scramble",
                $"Regenerate level {levelSO.Data.LevelIndex} with seed {levelSO.Data.Seed}?\n\nWarning: All manual edits will be lost.",
                "Scramble", "Cancel"))
            {
                Undo.RecordObject(levelSO, "Re-Scramble Level");
                var generated = LevelGenerator.GenerateLevel(
                    levelSO.Data.LevelIndex, levelSO.Data.Seed,
                    levelSO.Data.Poles.Count, colorCount, maxCap);

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

        private static void DrawColorPalette()
        {
            EditorGUILayout.LabelField("Select Color Brush:", EditorStyles.boldLabel);

            var colors = (RingColor[])System.Enum.GetValues(typeof(RingColor));
            var prevColor = GUI.backgroundColor;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = s_eraserMode ? Color.red : Color.gray;
                var eraserStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = s_eraserMode ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = s_eraserMode ? Color.white : Color.black }
                };
                if (GUILayout.Button("ERASER", eraserStyle, GUILayout.Width(70), GUILayout.Height(24)))
                    s_eraserMode = true;
                GUI.backgroundColor = prevColor;

                GUILayout.Space(8f);

                for (int i = 1; i < colors.Length; i++)
                {
                    var color = colors[i];
                    Color c = RingPalette.Get(color);

                    bool isSelected = (!s_eraserMode && s_brushColor == color);
                    GUI.backgroundColor = c;

                    string label = isSelected
                        ? $"[{color.ToString().Substring(0, 3).ToUpper()}]"
                        : color.ToString().Substring(0, 3).ToUpper();

                    var style = new GUIStyle(CompactButton)
                    {
                        fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        normal = { textColor = RingFlowEditorUtils.GetContrastColor(c) }
                    };

                    if (GUILayout.Button(label, style, GUILayout.Width(42), GUILayout.Height(24)))
                    {
                        s_brushColor = color;
                        s_eraserMode = false;
                    }
                }
                GUI.backgroundColor = prevColor;
            }
        }

        private static void DrawTypePalette()
        {
            if (s_eraserMode) return;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Select Ring Type Brush:", EditorStyles.boldLabel);

            var types = (RingType[])System.Enum.GetValues(typeof(RingType));
            var prevBg = GUI.backgroundColor;
            int typesPerRow = 5;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < types.Length; i += typesPerRow)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int j = i; j < i + typesPerRow && j < types.Length; j++)
                        {
                            var type = types[j];
                            bool isSelected = s_brushType == type;

                            if (isSelected)
                            {
                                GUI.backgroundColor = new Color(0.2f, 0.8f, 1.0f);
                                if (GUILayout.Button(type.ToString(), CompactButton, GUILayout.Height(20)))
                                    s_brushType = type;
                                GUI.backgroundColor = prevBg;
                            }
                            else
                            {
                                if (GUILayout.Button(type.ToString(), CompactButton, GUILayout.Height(20)))
                                    s_brushType = type;
                            }
                        }
                    }
                }

                if (s_brushType == RingType.Bomb)
                {
                    EditorGUILayout.Space(2f);
                    s_bombCounter = EditorGUILayout.IntSlider("Bomb Counter Value", s_bombCounter, 1, 9);
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
                        EditorGUILayout.LabelField($"Pole {p}", RingFlowEditorUtils.CenteredMiniLabel, GUILayout.Width(poleWidth));

                        int maxCapacity = pole.MaxCapacity;
                        float height = maxCapacity * (ringHeight + 2f) + 12f;
                        Rect rect = GUILayoutUtility.GetRect(poleWidth, height);

                        Color colBg = pole.IsLocked ? new Color(0.18f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.16f, 0.18f, 1f);
                        Color borderCol = pole.IsLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.35f, 0.35f, 0.38f);

                        EditorGUI.DrawRect(rect, colBg);
                        RingFlowEditorUtils.DrawRectBorder(rect, borderCol, 1);

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

                                string label = RingFlowEditorUtils.GetRingShortLabel(ring.Type);
                                if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                                    label += ring.AdditionalData;

                                var style = new GUIStyle(BoldCompactButton)
                                {
                                    normal = { textColor = RingFlowEditorUtils.GetContrastColor(ringColor) }
                                };

                                if (GUI.Button(ringRect, label, style))
                                {
                                    Undo.RecordObject(levelSO, "Modify Ring");
                                    if (s_eraserMode)
                                        pole.Rings.RemoveAt(r);
                                    else
                                        pole.Rings[r] = new RingData(s_brushColor, s_brushType, s_brushType == RingType.Bomb ? s_bombCounter : 0);
                                    EditorUtility.SetDirty(levelSO);
                                }
                            }
                            else if (isAddSlot)
                            {
                                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                                var style = new GUIStyle(BoldCompactButton)
                                {
                                    fontSize = 11,
                                    normal = { textColor = Color.white }
                                };

                                if (!s_eraserMode && GUI.Button(ringRect, "+", style))
                                {
                                    Undo.RecordObject(levelSO, "Add Ring");
                                    pole.Rings.Add(new RingData(s_brushColor, s_brushType, s_brushType == RingType.Bomb ? s_bombCounter : 0));
                                    EditorUtility.SetDirty(levelSO);
                                }
                            }
                            else
                            {
                                Rect emptyRect = new Rect(rect.x + 6f, ringY + 2f, poleWidth - 12f, ringHeight - 4f);
                                EditorGUI.DrawRect(emptyRect, new Color(0.25f, 0.25f, 0.25f, 0.2f));
                                RingFlowEditorUtils.DrawRectBorder(emptyRect, new Color(0.35f, 0.35f, 0.38f, 0.3f), 1);
                            }
                        }

                        if (pole.IsLocked)
                        {
                            Rect lockRect = new Rect(rect.x + 3f, rect.y + 4f, poleWidth - 6f, 13f);
                            EditorGUI.DrawRect(lockRect, new Color(0.8f, 0.1f, 0.1f, 0.9f));
                            var lockStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                                { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                            GUI.Label(lockRect, "LOCKED", lockStyle);
                        }

                        GUI.backgroundColor = prevColor;
                        EditorGUILayout.Space(2f);

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
                                pole.Rings.RemoveAt(pole.Rings.Count - 1);
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
                        GUILayout.Space(poleGap);
                }
                GUI.backgroundColor = prevColor;
            }
            EditorGUILayout.Space(5f);
        }

        private static void DrawHeader(string title)
        {
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            GUILayout.Box(title, HeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            GUI.backgroundColor = bg;
            EditorGUILayout.Space(2f);
        }
    }
}
