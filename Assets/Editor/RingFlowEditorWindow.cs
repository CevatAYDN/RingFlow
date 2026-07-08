using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    /// <summary>
    /// Editor control panel for RingFlow. Surfaces level generation, visual board
    /// building, runtime state machine control, and accessibility settings.
    /// </summary>
    public class RingFlowEditorWindow : EditorWindow
    {
        // EditorPrefs keys
        private const string PrefFoldGenerator = "RingFlow.Foldout.Generator";
        private const string PrefFoldBuilder = "RingFlow.Foldout.Builder";
        private const string PrefFoldRuntime = "RingFlow.Foldout.Runtime";
        private const string PrefFoldSettings = "RingFlow.Foldout.Settings";
        private const string PrefLevelIndex = "RingFlow.LevelIndex";
        private const string PrefSeed = "RingFlow.Seed";
        private const string PrefPoles = "RingFlow.Poles";
        private const string PrefColors = "RingFlow.Colors";
        private const string PrefMaxCap = "RingFlow.MaxCap";

        // Cached header texture (one per window, not per OnGUI)
        private static Texture2D s_headerTex;

        [MenuItem("Ring Flow/Game Control Panel &G", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("Control Panel");
            window.minSize = new Vector2(420, 620);
            window.Show();
        }

        [MenuItem("Ring Flow/Create Working Scene", false, 10)]
        public static void CreateWorkingScene()
        {
            const string scenePath = "Assets/Scenes/RingFlow.unity";
            if (System.IO.File.Exists(scenePath))
            {
                if (!EditorUtility.DisplayDialog("Scene Exists",
                    $"A scene already exists at {scenePath}. Open it instead?", "Open", "Cancel"))
                {
                    return;
                }
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                return;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            SetupNexusBootstrapper();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Scene Created", $"Working scene saved to {scenePath}.", "OK");
        }

        // Foldouts (persisted via EditorPrefs)
        private bool _foldoutGenerator;
        private bool _foldoutVisualBuilder;
        private bool _foldoutRuntime;
        private bool _foldoutSettings;

        // Generator parameters
        private int _levelIndex = 1;
        private int _seed = 100;
        private int _poleCount = 4;
        private int _colorCount = 3;
        private int _maxCapacity = 4;

        // Solver / Generation Results
        [NonSerialized] private LevelData _generatedLevel;
        private string _solveStatus = "No level loaded / generated.";
        private readonly List<string> _solutionSteps = new();
        private Vector2 _solutionScroll;

        // Visual Builder parameters
        private const float PoleSpacing = 2.5f;
        private const float RingHeight = 0.5f;

        private Vector2 _mainScrollPosition;

        private void OnEnable()
        {
            _foldoutGenerator = EditorPrefs.GetBool(PrefFoldGenerator, true);
            _foldoutVisualBuilder = EditorPrefs.GetBool(PrefFoldBuilder, true);
            _foldoutRuntime = EditorPrefs.GetBool(PrefFoldRuntime, true);
            _foldoutSettings = EditorPrefs.GetBool(PrefFoldSettings, true);
            _levelIndex = EditorPrefs.GetInt(PrefLevelIndex, 1);
            _seed = EditorPrefs.GetInt(PrefSeed, 100);
            _poleCount = EditorPrefs.GetInt(PrefPoles, 4);
            _colorCount = EditorPrefs.GetInt(PrefColors, 3);
            _maxCapacity = EditorPrefs.GetInt(PrefMaxCap, 4);
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool(PrefFoldGenerator, _foldoutGenerator);
            EditorPrefs.SetBool(PrefFoldBuilder, _foldoutVisualBuilder);
            EditorPrefs.SetBool(PrefFoldRuntime, _foldoutRuntime);
            EditorPrefs.SetBool(PrefFoldSettings, _foldoutSettings);
            EditorPrefs.SetInt(PrefLevelIndex, _levelIndex);
            EditorPrefs.SetInt(PrefSeed, _seed);
            EditorPrefs.SetInt(PrefPoles, _poleCount);
            EditorPrefs.SetInt(PrefColors, _colorCount);
            EditorPrefs.SetInt(PrefMaxCap, _maxCapacity);
        }

        private void OnGUI()
        {
            DrawHeader("RING FLOW — GAME CONTROL PANEL");

            bool triggerGenerate = false;
            bool triggerBuild = false;
            bool triggerClear = false;
            bool triggerSetupBootstrapper = false;
            bool triggerGoMainMenu = false;
            bool triggerGoLevelSelect = false;
            bool triggerGoPlaying = false;
            bool triggerGoPaused = false;
            bool triggerGoWin = false;
            bool triggerAddCoins = false;
            bool triggerAddDiamonds = false;
            bool triggerUnlockAll = false;

            _mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition);

            // 1. LEVEL GENERATOR & SOLVER
            _foldoutGenerator = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutGenerator, "Level Generator & AI Solver");
            if (_foldoutGenerator)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUI.BeginChangeCheck();
                    _levelIndex = EditorGUILayout.IntSlider("Level Index", _levelIndex, 1, WorldConfigSO.TotalLevels);
                    _seed = EditorGUILayout.IntField("Random Seed", _seed);
                    _poleCount = EditorGUILayout.IntSlider("Poles Count", _poleCount, 3, 12);
                    _colorCount = EditorGUILayout.IntSlider("Colors Count", _colorCount, 2, 10);
                    _maxCapacity = EditorGUILayout.IntSlider("Max Ring Capacity", _maxCapacity, 3, 5);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetInt(PrefLevelIndex, _levelIndex);
                        EditorPrefs.SetInt(PrefSeed, _seed);
                        EditorPrefs.SetInt(PrefPoles, _poleCount);
                        EditorPrefs.SetInt(PrefColors, _colorCount);
                        EditorPrefs.SetInt(PrefMaxCap, _maxCapacity);
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Apply GDD Curve", GUILayout.Height(30)))
                        {
                            ApplyGddCurveParams();
                        }
                        if (GUILayout.Button("Generate Level", GUILayout.Height(30)))
                        {
                            triggerGenerate = true;
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

            // 2. SCENE VISUAL BUILDER
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
                            triggerBuild = true;
                        }
                        if (GUILayout.Button("Clear Scene Board", GUILayout.Height(35)))
                        {
                            triggerClear = true;
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            // 3. RUNTIME LIFECYCLE & ECONOMY DEBUGGER
            _foldoutRuntime = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutRuntime, "PlayMode Lifecycle & State Controller");
            if (_foldoutRuntime)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (!Application.isPlaying)
                    {
                        EditorGUILayout.HelpBox("Enter PlayMode to control game states, unlock progress, and inject coins/diamonds in real-time.", MessageType.Warning);

                        EditorGUILayout.Space();
                        if (GUILayout.Button("Setup Nexus Bootstrapper in Scene", GUILayout.Height(30)))
                        {
                            triggerSetupBootstrapper = true;
                        }
                    }
                    else
                    {
                        DrawRuntimeControls(ref triggerGoMainMenu, ref triggerGoLevelSelect, ref triggerGoPlaying, ref triggerGoPaused, ref triggerGoWin, ref triggerAddCoins, ref triggerAddDiamonds, ref triggerUnlockAll);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            // 4. ACCESSIBILITY & SETTINGS
            _foldoutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutSettings, "Accessibility & Localizer Settings");
            if (_foldoutSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawSettingsControls();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndScrollView();

            // Actions outside Layout groups
            if (triggerGenerate) GenerateLevel();
            if (triggerBuild) BuildBoardInScene();
            if (triggerClear) ClearSceneBoard();
            if (triggerSetupBootstrapper) SetupNexusBootstrapper();

            if (Application.isPlaying)
            {
                var context = NexusRuntime.CurrentContext;
                if (context != null)
                {
                    var fsm = context.TryResolve<IGameStateMachine>();
                    var progress = context.TryResolve<PlayerProgressModel>();
                    var economy = context.TryResolve<IEconomyService>();

                    if (fsm != null)
                    {
                        if (triggerGoMainMenu)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "FSMTransition", "", $"Requesting state change to MainMenuState. Current={fsm.CurrentState?.GetType().Name ?? "null"}");
                            _ = fsm.ChangeStateAsync<MainMenuState>();
                        }
                        if (triggerGoLevelSelect)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "FSMTransition", "", "Requesting state change to LevelSelectState");
                            _ = fsm.ChangeStateAsync<LevelSelectState>();
                        }
                        if (triggerGoPlaying)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "FSMTransition", "", "Requesting state change to PlayingState");
                            _ = fsm.ChangeStateAsync<PlayingState>();
                        }
                        if (triggerGoPaused)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "FSMTransition", "", "Requesting state change to PausedState");
                            _ = fsm.ChangeStateAsync<PausedState>();
                        }
                        if (triggerGoWin)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "FSMTransition", "", "Requesting state change to WinState");
                            _ = fsm.ChangeStateAsync<WinState>();
                        }
                    }

                    if (progress != null && economy != null)
                    {
                        if (triggerAddCoins)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "EconomyCheat", "", "Admin Cheat: Adding 100 Coins");
                            economy.Earn("Coins", 100);
                        }
                        if (triggerAddDiamonds)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "EconomyCheat", "", "Admin Cheat: Adding 10 Diamonds");
                            economy.Earn("Diamonds", 10);
                        }
                        if (triggerUnlockAll)
                        {
                            NexusLog.Info("RingFlowEditorWindow", "ProgressionCheat", "", "Admin Cheat: Unlocking all levels & worlds");
                            progress.MaxUnlockedLevel.Value = WorldConfigSO.TotalLevels;
                            for (int i = 0; i < progress.UnlockedWorlds.Count; i++)
                            {
                                progress.UnlockedWorlds[i] = true;
                            }
                        }
                    }
                }
            }
        }

        private void DrawHeader(string title)
        {
            var headerStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            if (s_headerTex == null)
            {
                s_headerTex = new Texture2D(2, 2);
                var px = new Color[4] { new Color(0.15f, 0.15f, 0.18f), new Color(0.15f, 0.15f, 0.18f), new Color(0.15f, 0.15f, 0.18f), new Color(0.15f, 0.15f, 0.18f) };
                s_headerTex.SetPixels(px);
                s_headerTex.Apply();
            }
            headerStyle.normal.background = s_headerTex;
            headerStyle.normal.textColor = new Color(0.2f, 0.8f, 1.0f);

            GUILayout.Box(title, headerStyle, GUILayout.ExpandWidth(true), GUILayout.Height(40));
            EditorGUILayout.Space();
        }

        private void ApplyGddCurveParams()
        {
            _poleCount = DifficultyCurve.PoleCountForLevel(_levelIndex);
            _colorCount = DifficultyCurve.ColorCountForLevel(_levelIndex);
            _maxCapacity = DifficultyCurve.MaxCapacityForLevel(_levelIndex);

            if (_poleCount < _colorCount + 1) _poleCount = _colorCount + 1;
            if (_poleCount > 12) _poleCount = 12;

            EditorPrefs.SetInt(PrefPoles, _poleCount);
            EditorPrefs.SetInt(PrefColors, _colorCount);
            EditorPrefs.SetInt(PrefMaxCap, _maxCapacity);

            NexusLog.Info("RingFlowEditorWindow", "ApplyGddCurveParams", _levelIndex.ToString(),
                $"Applied GDD curve difficulty parameters: Poles={_poleCount}, Colors={_colorCount}, MaxCapacity={_maxCapacity}");
        }

        private void GenerateLevel()
        {
            if (_levelIndex < 1 || _levelIndex > WorldConfigSO.TotalLevels)
            {
                NexusLog.Warn("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                    $"Level index {_levelIndex} is out of bounds (1..{WorldConfigSO.TotalLevels}). Adjusting.");
                _levelIndex = Mathf.Clamp(_levelIndex, 1, WorldConfigSO.TotalLevels);
                EditorPrefs.SetInt(PrefLevelIndex, _levelIndex);
            }

            if (_poleCount < _colorCount + 1)
            {
                int correctedPoles = _colorCount + 1;
                NexusLog.Warn("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                    $"Pole count ({_poleCount}) is less than Color count ({_colorCount}) + 1. Correcting Pole count to {correctedPoles}.");
                _poleCount = correctedPoles;
                EditorPrefs.SetInt(PrefPoles, _poleCount);
            }

            if (_poleCount > 12)
            {
                NexusLog.Warn("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                    $"Pole count ({_poleCount}) exceeds maximum capacity of 12. Capping at 12.");
                _poleCount = 12;
                EditorPrefs.SetInt(PrefPoles, _poleCount);
            }

            NexusLog.Info("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                $"Attempting to generate level {_levelIndex} with Seed={_seed}, Poles={_poleCount}, Colors={_colorCount}, MaxCapacity={_maxCapacity}");

            _generatedLevel = LevelGenerator.GenerateLevel(_levelIndex, _seed, _poleCount, _colorCount, _maxCapacity);

            if (_generatedLevel == null)
            {
                _solveStatus = "Generation failed (exhausted 50 seeds).";
                _solutionSteps.Clear();
                NexusLog.Error("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                    $"Failed to generate a solvable level for index {_levelIndex} after 50 attempts.");
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
                NexusLog.Info("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                    $"Successfully generated and solved level {_levelIndex}. Optimal moves: {solveResult.MoveCount}.");
            }
            else
            {
                _solveStatus = "Unsolvable! Level generator seed failed validation.";
                _solutionSteps.Clear();
                NexusLog.Error("RingFlowEditorWindow", "GenerateLevel", _levelIndex.ToString(),
                    $"Level generator produced an unsolvable board state for Level {_levelIndex} (Seed={_seed}). Check mechanics.");
            }
        }

        private void BuildBoardInScene()
        {
            List<PoleState> polesToBuild = null;
            if (Application.isPlaying)
            {
                var context = NexusRuntime.CurrentContext;
                var model = context?.TryResolve<GameplayModel>();
                if (model != null && model.Poles.Count > 0)
                {
                    polesToBuild = model.Poles;
                }
            }

            if (polesToBuild == null && _generatedLevel == null)
            {
                EditorUtility.DisplayDialog("Error", "Please generate a level first OR enter PlayMode to load from active game!", "OK");
                return;
            }

            int poleCount = polesToBuild != null ? polesToBuild.Count : _generatedLevel.Poles.Count;
            NexusLog.Info("RingFlowEditorWindow", "BuildBoardInScene", "", $"Building visual board in scene with {poleCount} poles.");

            ClearSceneBoard();

            var boardRoot = new GameObject("RingFlow_VisualBoard");
            Undo.RegisterCreatedObjectUndo(boardRoot, "Build Visual Board");

            var torusModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Torus.obj");
            if (torusModel == null)
            {
                NexusLog.Warn("RingFlowEditorWindow", "BuildBoardInScene", "", "Torus.obj not found in Assets/Models. Using Cylinder disks as fallback rings.");
            }

            for (int p = 0; p < poleCount; p++)
            {
                bool isLocked = false;
                List<RingData> rings = new();

                if (polesToBuild != null)
                {
                    var poleState = polesToBuild[p];
                    isLocked = poleState.IsLocked;
                    rings = poleState.Rings;
                }
                else
                {
                    var poleData = _generatedLevel.Poles[p];
                    isLocked = poleData.IsLocked;
                    rings = poleData.Rings;
                }

                var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                poleObj.name = $"Pole_{p}" + (isLocked ? " [LOCKED]" : "");
                poleObj.transform.SetParent(boardRoot.transform);
                poleObj.transform.position = new Vector3(p * PoleSpacing, 2.0f, 0f);
                poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

                var poleView = poleObj.AddComponent<PoleView>();
                poleView.PoleId = p;

                var capsule = poleObj.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    capsule.radius = 1.5f;
                }

                if (isLocked)
                {
                    var renderer = poleObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var poleMat = new Material(GetDefaultShader());
                        poleMat.color = Color.black;
                        renderer.sharedMaterial = poleMat;
                    }
                }

                for (int r = 0; r < rings.Count; r++)
                {
                    var ringData = rings[r];
                    GameObject ringObj;

                    if (torusModel != null)
                    {
                        ringObj = Instantiate(torusModel);
                        ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                        ringObj.transform.SetParent(poleObj.transform);
                        ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                        ringObj.transform.localRotation = Quaternion.identity;
                        ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
                    }
                    else
                    {
                        ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                        ringObj.transform.SetParent(poleObj.transform);
                        ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                        ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);
                    }

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                    {
                        var mat = new Material(GetDefaultShader());
                        mat.color = RingPalette.Get(ringData.Color);
                        ApplySpecialRingMaterial(mat, ringData.Type);
                        ringRenderer.sharedMaterial = mat;
                    }

                    var col = ringObj.GetComponent<Collider>();
                    if (col != null)
                    {
                        DestroyImmediate(col);
                    }
                }
            }

            Selection.activeGameObject = boardRoot;
            SceneView.FrameLastActiveSceneView();
            NexusLog.Info("RingFlowEditorWindow", "BuildBoardInScene", "", "Visual board successfully built in the scene.");
        }

        private void ClearSceneBoard()
        {
            var boardRoot = GameObject.Find("RingFlow_VisualBoard");
            if (boardRoot != null)
            {
                Undo.DestroyObjectImmediate(boardRoot);
                NexusLog.Info("RingFlowEditorWindow", "ClearSceneBoard", "", "Cleared visual board from scene.");
            }
        }

        private void DrawRuntimeControls(
            ref bool goMainMenu, ref bool goLevelSelect, ref bool goPlaying,
            ref bool goPaused, ref bool goWin,
            ref bool addCoins, ref bool addDiamonds, ref bool unlockAll)
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null) return;

            var fsm = context.TryResolve<IGameStateMachine>();
            var model = context.TryResolve<GameplayModel>();
            var progress = context.TryResolve<PlayerProgressModel>();
            var economy = context.TryResolve<IEconomyService>();

            if (fsm != null)
            {
                EditorGUILayout.LabelField($"Active State: {fsm.CurrentState?.GetType().Name ?? "None"}", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Go MainMenu")) goMainMenu = true;
                    if (GUILayout.Button("Go LevelSelect")) goLevelSelect = true;
                    if (GUILayout.Button("Go Playing")) goPlaying = true;
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Go Paused")) goPaused = true;
                    if (GUILayout.Button("Go Win")) goWin = true;
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
                    if (GUILayout.Button("Add 100 Coins")) addCoins = true;
                    if (GUILayout.Button("Add 10 Diamonds")) addDiamonds = true;
                    if (GUILayout.Button("Unlock All Levels")) unlockAll = true;
                }
            }
        }

        private void DrawSettingsControls()
        {
            var context = Application.isPlaying ? NexusRuntime.CurrentContext : null;
            var settings = context?.TryResolve<SettingsModel>();
            var localization = context?.TryResolve<ILocalizationService>();

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Enter PlayMode to control settings reactively. The Nexus context resolves SettingsModel at runtime.", MessageType.Info);
                return;
            }

            bool newMusic = EditorGUILayout.Toggle("Music Enabled", settings.MusicEnabled.Value);
            if (newMusic != settings.MusicEnabled.Value) settings.MusicEnabled.Value = newMusic;

            bool newSfx = EditorGUILayout.Toggle("SFX Enabled", settings.SfxEnabled.Value);
            if (newSfx != settings.SfxEnabled.Value) settings.SfxEnabled.Value = newSfx;

            bool newHaptic = EditorGUILayout.Toggle("Haptic Feedback", settings.HapticEnabled.Value);
            if (newHaptic != settings.HapticEnabled.Value) settings.HapticEnabled.Value = newHaptic;

            int newBlind = EditorGUILayout.IntSlider("Color Blind Mode", settings.ColorBlindMode.Value, 0, 3);
            if (newBlind != settings.ColorBlindMode.Value) settings.ColorBlindMode.Value = newBlind;

            string[] langs = { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
            int currentLangIndex = Array.IndexOf(langs, settings.LanguageCode.Value);
            if (currentLangIndex == -1) currentLangIndex = 0;

            int newLangIndex = EditorGUILayout.Popup("Language", currentLangIndex, langs);
            if (newLangIndex != currentLangIndex)
            {
                settings.LanguageCode.Value = langs[newLangIndex];
            }

            if (localization != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Current Language: {localization.CurrentLanguage ?? "—"}", EditorStyles.boldLabel);
            }
        }

        private Shader GetDefaultShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private static System.Type ResolveInputSystemUIInputModuleType()
        {
            return System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI")
                ?? System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        }

        private static void AttachInputModule(GameObject target, System.Type moduleType)
        {
            if (target == null || moduleType == null) return;
            var instance = target.AddComponent(moduleType);
            if (instance != null) Undo.RegisterCreatedObjectUndo(instance, "Attach Input System Module");
        }

        private void ApplySpecialRingMaterial(Material mat, RingType type)
        {
            switch (type)
            {
                case RingType.Frozen:
                    mat.color = Color.cyan;
                    break;
                case RingType.Locked:
                    mat.color = new Color(1f, 0.84f, 0f);
                    break;
                case RingType.Stone:
                    mat.color = Color.grey;
                    break;
                case RingType.Glass:
                    mat.color = new Color(1f, 1f, 1f, 0.3f);
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                    break;
                case RingType.Rainbow:
                    mat.color = Color.magenta;
                    break;
                case RingType.Ghost:
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.1f);
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    break;
            }
        }

        private static void SetupNexusBootstrapper()
        {
            NexusLog.Info("RingFlowEditorWindow", "SetupNexusBootstrapper", "", $"Setting up Nexus Bootstrapper in active scene. PlayMode={Application.isPlaying}");

            var existingRoot = FindAnyObjectByType<Root>();
            if (existingRoot != null)
            {
                NexusLog.Warn("RingFlowEditorWindow", "SetupNexusBootstrapper", "", "Nexus Bootstrapper already exists in the scene!");
                EditorUtility.DisplayDialog("Setup", "Nexus Bootstrapper already exists in the scene!", "OK");
                return;
            }

            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Setup", "Cannot run setup during PlayMode. Exit PlayMode first.", "OK");
                return;
            }

            // 1. Create ContextData asset if missing
            const string assetPath = "Assets/Settings/GameplayContextData.asset";
            var contextData = AssetDatabase.LoadAssetAtPath<ContextData>(assetPath);
            if (contextData == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                {
                    AssetDatabase.CreateFolder("Assets", "Settings");
                }
                contextData = ScriptableObject.CreateInstance<ContextData>();
                contextData.AssemblyScopes = new[] { "RingFlow" };
                contextData.EnableAutoDiscovery = true;
                AssetDatabase.CreateAsset(contextData, assetPath);
                AssetDatabase.SaveAssets();
            }

            // 2. Spawn NexusRoot GameObject
            var rootObj = new GameObject("NexusRoot");
            var newRoot = rootObj.AddComponent<Root>();

            var serializedObject = new SerializedObject(newRoot);
            var prop = serializedObject.FindProperty("contextData");
            if (prop != null)
            {
                prop.objectReferenceValue = contextData;
                serializedObject.ApplyModifiedProperties();
            }

            // 3. BoardView for runtime board management
            var boardView = rootObj.AddComponent<BoardView>();
            var torusModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Torus.obj");
            if (torusModel != null)
            {
                var torusField = typeof(BoardView).GetField("_torusPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (torusField != null)
                {
                    torusField.SetValue(boardView, torusModel);
                }
            }

            // 4. GameplayLifecycle (binds models, services, commands, FSM)
            rootObj.AddComponent<GameplayLifecycle>();

            // 5. UIRoot creates the Canvas and all UI screens
            rootObj.AddComponent<UIRoot>();

            // 6. EventSystem + InputSystemUIInputModule
            var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }
            var oldModule = eventSystem.GetComponent<BaseInputModule>();
            var newInputModuleType = ResolveInputSystemUIInputModuleType();
            if (oldModule != null && newInputModuleType != null && !newInputModuleType.IsInstanceOfType(oldModule))
            {
                Undo.DestroyObjectImmediate(oldModule);
                AttachInputModule(eventSystem.gameObject, newInputModuleType);
            }
            else if (oldModule == null && newInputModuleType != null)
            {
                AttachInputModule(eventSystem.gameObject, newInputModuleType);
            }

            // 7. PhysicsRaycaster on every camera so 3D pole colliders forward IPointerDownHandler events
            foreach (var cam in FindObjectsByType<Camera>())
            {
                if (cam != null && cam.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
                {
                    Undo.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>(cam.gameObject);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            NexusLog.Info("RingFlowEditorWindow", "SetupNexusBootstrapper", "", "Nexus Bootstrapper successfully added to the active scene.");
            EditorUtility.DisplayDialog("Setup", "Nexus Bootstrapper successfully added to the active scene! Press Play to run.", "OK");
        }
    }
}
