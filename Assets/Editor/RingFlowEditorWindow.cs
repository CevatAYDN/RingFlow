using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public class RingFlowEditorWindow : EditorWindow
    {
        [MenuItem("Ring Flow/Game Control Panel &G", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("Control Panel");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        // Foldouts
        private bool _foldoutGenerator = true;
        private bool _foldoutVisualBuilder = true;
        private bool _foldoutRuntime = true;
        private bool _foldoutSettings = true;

        // Generator parameters
        private int _levelIndex = 1;
        private int _seed = 100;
        private int _poleCount = 4;
        private int _colorCount = 3;
        private int _maxCapacity = 4;

        // Solver / Generation Results
        private LevelData _generatedLevel;
        private string _solveStatus = "No level loaded / generated.";
        private List<string> _solutionSteps = new();
        private Vector2 _solutionScroll;

        // Visual Builder parameters
        private const float PoleSpacing = 2.5f;
        private const float RingHeight = 0.5f;

        private void OnGUI()
        {
            DrawHeader("RING FLOW — GAME CONTROL PANEL");

            using (var scroll = new EditorGUILayout.ScrollViewScope(Vector2.zero))
            {
                // 1. LEVEL GENERATOR & SOLVER
                _foldoutGenerator = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutGenerator, "Level Generator & AI Solver");
                if (_foldoutGenerator)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        _levelIndex = EditorGUILayout.IntField("Level Index", _levelIndex);
                        _seed = EditorGUILayout.IntField("Random Seed", _seed);
                        _poleCount = EditorGUILayout.IntSlider("Poles Count", _poleCount, 3, 10);
                        _colorCount = EditorGUILayout.IntSlider("Colors Count", _colorCount, 2, 8);
                        _maxCapacity = EditorGUILayout.IntSlider("Max Ring Capacity", _maxCapacity, 3, 5);

                        EditorGUILayout.Space();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Apply GDD Curve", GUILayout.Height(30)))
                            {
                                ApplyGddCurveParams();
                            }
                            if (GUILayout.Button("Generate Level", GUILayout.Height(30)))
                            {
                                GenerateLevel();
                            }
                        }

                        if (_generatedLevel != null)
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Generated Level Info:", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"Level Index: {_generatedLevel.LevelIndex} | Poles: {_generatedLevel.Poles.Count}");
                            EditorGUILayout.LabelField($"Target Moves: {_generatedLevel.TargetMoves} (Solver Verified)");

                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("AI Solver Path:", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"Status: {_solveStatus}", EditorStyles.wordWrappedLabel);

                            if (_solutionSteps.Count > 0)
                            {
                                _solutionScroll = EditorGUILayout.BeginScrollView(_solutionScroll, GUILayout.Height(120));
                                for (int i = 0; i < _solutionSteps.Count; i++)
                                {
                                    EditorGUILayout.LabelField($"{i + 1}. {_solutionSteps[i]}");
                                }
                                EditorGUILayout.EndScrollView();
                            }
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space();

                // 2. SCENE VISUAL BUILDER (CYLINDERS & TORUS)
                _foldoutVisualBuilder = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutVisualBuilder, "Scene Visual Board Builder");
                if (_foldoutVisualBuilder)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.HelpBox("Spawns Cylinder primitives as poles, and uses Torus.obj models as rings in the active scene to visualize the generated level layout.", MessageType.Info);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Build Board in Scene", GUILayout.Height(35)))
                            {
                                BuildBoardInScene();
                            }
                            if (GUILayout.Button("Clear Scene Board", GUILayout.Height(35)))
                            {
                                ClearSceneBoard();
                            }
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space();

                // 3. RUNTIME LIKECYCLE & ECONOMY DEBUGGER
                _foldoutRuntime = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutRuntime, "PlayMode Lifecycle & State Controller");
                if (_foldoutRuntime)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (!Application.isPlaying)
                        {
                            EditorGUILayout.HelpBox("Enter PlayMode to control game states, unlock progress, and inject coins/diamonds in real-time.", MessageType.Warning);
                        }
                        else
                        {
                            DrawRuntimeControls();
                        }
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space();

                // 4. ACCESSIBILITY & SETTINGS SENDER
                _foldoutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutSettings, "Accessibility & Localizer Settings");
                if (_foldoutSettings)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawSettingsControls();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawHeader(string title)
        {
            var headerStyle = new GUIStyle(GUI.skin.box);
            headerStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f));
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(0.2f, 0.8f, 1.0f);
            
            GUILayout.Box(title, headerStyle, GUILayout.ExpandWidth(true), GUILayout.Height(40));
            EditorGUILayout.Space();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void ApplyGddCurveParams()
        {
            _poleCount = DifficultyCurve.PoleCountForLevel(_levelIndex);
            _colorCount = DifficultyCurve.ColorCountForLevel(_levelIndex);
            _maxCapacity = 4;
        }

        private void GenerateLevel()
        {
            _generatedLevel = LevelGenerator.GenerateLevel(_levelIndex, _seed, _poleCount, _colorCount, _maxCapacity);

            if (_generatedLevel != null)
            {
                // Run Solver
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
                    _solveStatus = "Unsolvable! Level generator seed failed validation.";
                    _solutionSteps.Clear();
                }
            }
        }

        private void BuildBoardInScene()
        {
            if (_generatedLevel == null)
            {
                EditorUtility.DisplayDialog("Error", "Please generate a level first!", "OK");
                return;
            }

            ClearSceneBoard();

            // Root game object
            var boardRoot = new GameObject("RingFlow_VisualBoard");
            Undo.RegisterCreatedObjectUndo(boardRoot, "Build Visual Board");

            // Load Torus model
            var torusModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Torus.obj");
            if (torusModel == null)
            {
                Debug.LogWarning("[RingFlowEditor] Torus.obj not found in Assets/Models. Creating simple disk primitives instead.");
            }

            // Build Poles & Rings
            for (int p = 0; p < _generatedLevel.Poles.Count; p++)
            {
                var poleData = _generatedLevel.Poles[p];

                // 1. Spawn Pole Cylinder
                var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                poleObj.name = $"Pole_{p}" + (poleData.IsLocked ? " [LOCKED]" : "");
                poleObj.transform.SetParent(boardRoot.transform);
                poleObj.transform.position = new Vector3(p * PoleSpacing, 2.0f, 0f);
                poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

                // Add locked pole color decoration
                if (poleData.IsLocked)
                {
                    var renderer = poleObj.GetComponent<Renderer>();
                    renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                    renderer.sharedMaterial.color = Color.black; // locked poles are dark
                }

                // 2. Spawn Rings
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    var ringData = poleData.Rings[r];
                    GameObject ringObj;

                    if (torusModel != null)
                    {
                        ringObj = Instantiate(torusModel);
                        ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                        ringObj.transform.SetParent(poleObj.transform);
                        // Convert parent space (cylinder is scaled, so we adjust local position/scale)
                        ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                        ringObj.transform.localRotation = Quaternion.identity;
                        ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
                    }
                    else
                    {
                        // Fallback cylinder disk
                        ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                        ringObj.transform.SetParent(poleObj.transform);
                        ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                        ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);
                    }

                    // Apply Material Color matching RingColor
                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                    {
                        var mat = new Material(Shader.Find("Standard"));
                        
                        // Set basic color
                        mat.color = GetUnityColor(ringData.Color);

                        // Special ring visual adjustments
                        switch (ringData.Type)
                        {
                            case RingType.Frozen:
                                mat.color = Color.cyan; // Frozen ice coating
                                break;
                            case RingType.Locked:
                                mat.color = new Color(1f, 0.84f, 0f); // Gold key color
                                break;
                            case RingType.Stone:
                                mat.color = Color.grey; // Stone
                                break;
                            case RingType.Glass:
                                mat.color = new Color(1f, 1f, 1f, 0.3f); // transparent Glass
                                // Enable Alpha Blend
                                mat.SetFloat("_Mode", 3f);
                                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                mat.SetInt("_ZWrite", 0);
                                mat.DisableKeyword("_ALPHATEST_ON");
                                mat.EnableKeyword("_ALPHABLEND_ON");
                                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                mat.renderQueue = 3000;
                                break;
                            case RingType.Rainbow:
                                mat.color = Color.magenta; // Magenta standin for rainbow
                                break;
                            case RingType.Ghost:
                                mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.1f); // nearly invisible until selected
                                mat.SetFloat("_Mode", 3f);
                                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                mat.SetInt("_ZWrite", 0);
                                mat.renderQueue = 3000;
                                break;
                        }

                        ringRenderer.sharedMaterial = mat;
                    }
                }
            }

            Selection.activeGameObject = boardRoot;
            SceneView.FrameLastActiveSceneView();
        }

        private void ClearSceneBoard()
        {
            var boardRoot = GameObject.Find("RingFlow_VisualBoard");
            if (boardRoot != null)
            {
                Undo.DestroyObjectImmediate(boardRoot);
            }
        }

        private Color GetUnityColor(RingColor color)
        {
            return color switch
            {
                RingColor.Red => Color.red,
                RingColor.Blue => Color.blue,
                RingColor.Green => Color.green,
                RingColor.Yellow => Color.yellow,
                RingColor.Purple => new Color(0.5f, 0f, 0.5f),
                RingColor.Orange => new Color(1f, 0.5f, 0f),
                RingColor.Cyan => Color.cyan,
                _ => Color.white
            };
        }

        private void DrawRuntimeControls()
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null) return;

            var fsm = context.Resolve<IGameStateMachine>();
            var model = context.TryResolve<GameplayModel>();
            var progress = context.TryResolve<PlayerProgressModel>();
            var economy = context.TryResolve<IEconomyService>();

            if (fsm != null)
            {
                EditorGUILayout.LabelField($"Active State: {fsm.CurrentState?.GetType().Name}", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Go MainMenu")) fsm.ChangeStateAsync<MainMenuState>();
                    if (GUILayout.Button("Go LevelSelect")) fsm.ChangeStateAsync<LevelSelectState>();
                    if (GUILayout.Button("Go Playing")) fsm.ChangeStateAsync<PlayingState>();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Go Paused")) fsm.ChangeStateAsync<PausedState>();
                    if (GUILayout.Button("Go Win")) fsm.ChangeStateAsync<WinState>();
                }
            }

            if (model != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Gameplay Model Values:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Moves Count: {model.MovesCount.Value}");
                EditorGUILayout.LabelField($"Is Win State: {model.IsGameWon.Value}");
            }

            if (progress != null && economy != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Player Economy & Progress Debug:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Coins: {progress.Coins.Value} | Diamonds: {progress.Diamonds.Value}");
                EditorGUILayout.LabelField($"Player Level: {progress.PlayerLevel.Value} | XP: {progress.Xp.Value}/100");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add 100 Coins")) economy.Earn("Coins", 100);
                    if (GUILayout.Button("Add 10 Diamonds")) economy.Earn("Diamonds", 10);
                    if (GUILayout.Button("Unlock All Levels"))
                    {
                        progress.MaxUnlockedLevel.Value = WorldConfigSO.TotalLevels;
                        for (int i = 0; i < progress.UnlockedWorlds.Count; i++)
                        {
                            progress.UnlockedWorlds[i] = true;
                        }
                    }
                }
            }
        }

        private void DrawSettingsControls()
        {
            var context = Application.isPlaying ? NexusRuntime.CurrentContext : null;
            var settings = context?.TryResolve<SettingsModel>();
            var localization = context?.TryResolve<ILocalizationService>();

            if (settings != null)
            {
                // Music & SFX
                bool newMusic = EditorGUILayout.Toggle("Music Enabled", settings.MusicEnabled.Value);
                if (newMusic != settings.MusicEnabled.Value) settings.MusicEnabled.Value = newMusic;

                bool newSfx = EditorGUILayout.Toggle("SFX Enabled", settings.SfxEnabled.Value);
                if (newSfx != settings.SfxEnabled.Value) settings.SfxEnabled.Value = newSfx;

                bool newHaptic = EditorGUILayout.Toggle("Haptic Feedback", settings.HapticEnabled.Value);
                if (newHaptic != settings.HapticEnabled.Value) settings.HapticEnabled.Value = newHaptic;

                // Color Blind
                int newBlind = EditorGUILayout.IntSlider("Color Blind Mode", settings.ColorBlindMode.Value, 0, 3);
                if (newBlind != settings.ColorBlindMode.Value) settings.ColorBlindMode.Value = newBlind;

                // Language
                string[] langs = { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
                int currentLangIndex = Array.IndexOf(langs, settings.LanguageCode.Value);
                if (currentLangIndex == -1) currentLangIndex = 0;

                int newLangIndex = EditorGUILayout.Popup("Language", currentLangIndex, langs);
                if (newLangIndex != currentLangIndex)
                {
                    settings.LanguageCode.Value = langs[newLangIndex];
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Settings parameters can be controlled reactively in PlayMode.", MessageType.Info);
            }
        }
    }
}
