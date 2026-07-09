using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;

namespace RingFlow.Editor
{
    /// <summary>
    /// Developer dashboard for RingFlow.
    /// Groups level, UI, data, runtime, and setup tools into a single editor shell.
    /// </summary>
    public class RingFlowEditorWindow : EditorWindow
    {
        private const float HeaderHeight = 40f;
        private static readonly Color HeaderColor = new(0.15f, 0.15f, 0.18f);
        private static readonly Color HeaderTextColor = new(0.2f, 0.8f, 1.0f);
        private static Texture2D s_headerTex;

        private GeneratorSection _generator;
        private VisualBuilderSection _visualBuilder;
        private RuntimeSection _runtime;
        private SettingsSection _settings;
        private AdTesterSection _adTester;
        private DiagnosticsSection _diagnostics;
        private DatabaseSection _databaseSection;
        private List<EditorSection> _sections;

        private Vector2 _scroll;
        private string _quickSearch = "";
        private string _commandPalette = "";
        private int _selectedTab = 0;
        private string[] _tabs = { "Home", "Level Design", "UI Studio", "Runtime & Player", "System & Config", "Project Setup", "Database Editor" };
        private Vector2 _uiScroll;
        private ScreenType _uiSelectedScreen = ScreenType.Splash;
        private Vector2 _favoritesScroll;
        private List<string> _favoriteActions = new();
        private List<string> _recentActions = new();
        private Vector2 _recentScroll;
        private bool _showStatusBar = true;
        private bool _showPerfMonitor = false;
        private bool _showSceneTree = false;
        private const int MaxRecent = 10;
        private const int MaxFavorites = 20;

        // Throttling & Caching for Performance Monitor
        private double _lastRepaintTime;
        private double _lastPerfStatsUpdateTime;
        private const double PerfStatsUpdateInterval = 1.0;
        private float _cachedTotalMemory;
        private float _cachedMonoHeap;
        private int _cachedLoadedObjectsCount;
        private int _cachedActiveGameObjectsCount;

        // Caching for Scene Validator & Missing Reference Checks
        private double _lastValidationUpdateTime;
        private const double ValidationUpdateInterval = 1.0;
        private string _cachedSceneName;
        private bool _cachedSceneIsValid;
        private bool _cachedSceneIsLoaded;
        private int _cachedSceneRootCount;
        private bool _cachedHasRoot;
        private bool _cachedHasUIRoot;
        private bool _cachedHasEventSystem;

        [MenuItem("Ring Flow/Dashboard &G", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("RingFlow Dashboard");
            window.minSize = new Vector2(720, 760);
            window.Show();
        }

        [MenuItem("Ring Flow/Create Working Scene", false, 10)]
        public static void CreateWorkingScene()
        {
            const string scenePath = "Assets/Scenes/RingFlow.unity";
            if (System.IO.File.Exists(scenePath))
            {
                if (EditorUtility.DisplayDialog("Scene Exists",
                    $"A scene already exists at {scenePath}. Open it instead?", "Open", "Cancel"))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
                return;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var result = EditorBootstrapper.Bootstrap();
            if (!result.Success)
            {
                EditorUtility.DisplayDialog("Bootstrap Failed", result.Message, "OK");
                return;
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Scene Created",
                $"Working scene saved to {scenePath}.", "OK");
        }

        public static void BootstrapScene()
        {
            var result = EditorBootstrapper.Bootstrap();
            if (result.Success)
            {
                EditorUtility.DisplayDialog("Setup",
                    "Nexus Bootstrapper successfully added to the active scene! Press Play to run.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Setup", result.Message, "OK");
            }
        }

        private void OnEnable()
        {
            _generator = new GeneratorSection();
            _visualBuilder = new VisualBuilderSection(_generator);
            _runtime = new RuntimeSection();
            _settings = new SettingsSection();
            _adTester = new AdTesterSection();
            _diagnostics = new DiagnosticsSection();
            _databaseSection = new DatabaseSection();

            _generator.HideHeader = true;
            _visualBuilder.HideHeader = true;
            _runtime.HideHeader = true;
            _settings.HideHeader = true;
            _adTester.HideHeader = true;
            _diagnostics.HideHeader = true;
            _databaseSection.HideHeader = true;

            _generator.OnEnable();

            _sections = new List<EditorSection>
            {
                _generator,
                _visualBuilder,
                _runtime,
                _settings,
                _adTester,
                _diagnostics,
                _databaseSection,
            };

            _selectedTab = Mathf.Clamp(EditorPrefs.GetInt(EditorPrefsKeys.SelectedTab, 0), 0, _tabs.Length - 1);
            LoadFavorites();
            LoadRecent();
            _showStatusBar = EditorPrefs.GetBool(EditorPrefsKeys.ShowStatusBar, true);
            _showPerfMonitor = EditorPrefs.GetBool(EditorPrefsKeys.ShowPerfMonitor, false);
            _showSceneTree = EditorPrefs.GetBool(EditorPrefsKeys.ShowSceneTree, false);

            EditorApplication.update += OnEditorUpdate;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated += OnNewSceneCreated;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            InvalidateUIRootCacheStatic();
            InvalidateValidationCache();
        }

        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            InvalidateUIRootCacheStatic();
            InvalidateValidationCache();
        }

        private void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.NewSceneSetup setup, UnityEditor.SceneManagement.NewSceneMode mode)
        {
            InvalidateUIRootCacheStatic();
            InvalidateValidationCache();
        }

        private static void InvalidateUIRootCacheStatic()
        {
            s_cachedUIRoot = null;
        }

        private void OnDisable()
        {
            _generator?.OnDisable();
            EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
            EditorPrefs.SetBool(EditorPrefsKeys.ShowStatusBar, _showStatusBar);
            EditorPrefs.SetBool(EditorPrefsKeys.ShowPerfMonitor, _showPerfMonitor);
            EditorPrefs.SetBool(EditorPrefsKeys.ShowSceneTree, _showSceneTree);
            SaveFavorites();
            SaveRecent();
            EditorApplication.update -= OnEditorUpdate;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
        }

        private void OnEditorUpdate()
        {
            if (_showPerfMonitor)
            {
                var now = EditorApplication.timeSinceStartup;
                if (now - _lastRepaintTime >= 0.25) // Throttle to max 4 FPS for performance display
                {
                    _lastRepaintTime = now;
                    Repaint();
                }
            }
        }

        private void RefreshCachedPerfStats()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastPerfStatsUpdateTime >= PerfStatsUpdateInterval)
            {
                _lastPerfStatsUpdateTime = now;
                _cachedTotalMemory = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                _cachedMonoHeap = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                _cachedLoadedObjectsCount = Resources.FindObjectsOfTypeAll<Object>().Length;
                _cachedActiveGameObjectsCount = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Length;
            }
        }

        private void RefreshCachedValidation()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastValidationUpdateTime >= ValidationUpdateInterval || string.IsNullOrEmpty(_cachedSceneName))
            {
                _lastValidationUpdateTime = now;
                var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                _cachedSceneName = activeScene.name;
                _cachedSceneIsValid = activeScene.IsValid();
                _cachedSceneIsLoaded = activeScene.isLoaded;
                _cachedSceneRootCount = activeScene.rootCount;

                var root = Object.FindAnyObjectByType<Root>();
                _cachedHasRoot = root != null;
                _cachedHasUIRoot = root != null && root.GetComponentInChildren<UIRoot>(true) != null;
                _cachedHasEventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null;
            }
        }

        private void InvalidateValidationCache()
        {
            _cachedSceneName = null;
            _lastValidationUpdateTime = 0;
        }

        private void LoadFavorites()
        {
            var raw = EditorPrefs.GetString(EditorPrefsKeys.Favorites, "");
            if (string.IsNullOrEmpty(raw))
            {
                _favoriteActions = new List<string>
                {
                    "Create Working Scene",
                    "Setup Bootstrapper",
                    "Generate Level",
                    "Build Board",
                    "Open UI Studio",
                    "Open Database Editor",
                };
                return;
            }
            _favoriteActions = raw.Split('\u0001').Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        private void SaveFavorites()
        {
            EditorPrefs.SetString(EditorPrefsKeys.Favorites, string.Join("\u0001", _favoriteActions));
        }

        private void LoadRecent()
        {
            var raw = EditorPrefs.GetString(EditorPrefsKeys.RecentActions, "");
            _recentActions = string.IsNullOrEmpty(raw)
                ? new List<string>()
                : raw.Split('\u0001').Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        private void SaveRecent()
        {
            EditorPrefs.SetString(EditorPrefsKeys.RecentActions, string.Join("\u0001", _recentActions));
        }

        private void OnGUI()
        {
            RefreshCachedPerfStats();
            RefreshCachedValidation();
            UpdateFps();
            DrawHeader("RING FLOW — DASHBOARD");
            DrawTopBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_sections == null)
            {
                EditorGUILayout.EndScrollView();
                DrawStatusBar();
                return;
            }

            switch (_selectedTab)
            {
                case 0:
                    DrawHomeTab();
                    break;
                case 1:
                    EditorGUILayout.LabelField("Level Generator & Solver", EditorStyles.boldLabel);
                    _generator.OnGUI();
                    EditorGUILayout.Space(15f);
                    EditorGUILayout.LabelField("Scene Board Builder", EditorStyles.boldLabel);
                    _visualBuilder.OnGUI();
                    break;
                case 2:
                    DrawUIStudioTab();
                    break;
                case 3:
                    EditorGUILayout.LabelField("PlayMode Lifecycle & States", EditorStyles.boldLabel);
                    _runtime.OnGUI();
                    EditorGUILayout.Space(15f);
                    EditorGUILayout.LabelField("Ad & Reward Tester", EditorStyles.boldLabel);
                    _adTester.OnGUI();
                    break;
                case 4:
                    EditorGUILayout.LabelField("Settings & Configurations", EditorStyles.boldLabel);
                    _settings.OnGUI();
                    EditorGUILayout.Space(15f);
                    EditorGUILayout.LabelField("Diagnostics & Signals", EditorStyles.boldLabel);
                    _diagnostics.OnGUI();
                    break;
                case 5:
                    DrawSetupTab();
                    break;
                case 6:
                    DrawDataStudioTab();
                    break;
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("QA / Scene Validation", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSceneValidator();
                EditorGUILayout.Space(6f);
                DrawUIBindingInspector();
                EditorGUILayout.Space(6f);
                DrawRuntimeSnapshot();
            }

            if (_showPerfMonitor) DrawPerformanceMonitor();
            if (_showSceneTree) DrawSceneTree();

            EditorGUILayout.EndScrollView();
            if (_showStatusBar) DrawStatusBar();
        }

        private double _fpsLastTime;
        private int _fpsFrames;
        private float _fpsValue;

        private void UpdateFps()
        {
            _fpsFrames++;
            var now = EditorApplication.timeSinceStartup;
            if (now - _fpsLastTime >= 0.5)
            {
                _fpsValue = (float)(_fpsFrames / (now - _fpsLastTime));
                _fpsFrames = 0;
                _fpsLastTime = now;
            }
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.Space(2f);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string mode = Application.isPlaying
                ? (EditorApplication.isPaused ? "PAUSED" : "PLAY")
                : "EDIT";
            Color modeColor = Application.isPlaying
                ? (EditorApplication.isPaused ? new Color(1f, 0.8f, 0.2f) : new Color(0.4f, 0.9f, 0.4f))
                : new Color(0.7f, 0.7f, 0.7f);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Scene: {scene.name}", GUILayout.MinWidth(140f));
                var prevColor = GUI.color;
                GUI.color = modeColor;
                EditorGUILayout.LabelField($"[{mode}]", EditorStyles.boldLabel, GUILayout.Width(70f));
                GUI.color = prevColor;
                EditorGUILayout.LabelField($"FPS: {_fpsValue:0.0}", GUILayout.Width(70f));
                EditorGUILayout.LabelField($"Mem: {(int)_cachedTotalMemory} MB", GUILayout.Width(110f));

                int errCount = LogMonitor.ErrorCount;
                int warnCount = LogMonitor.WarningCount;
                GUI.color = errCount > 0 ? new Color(1f, 0.5f, 0.5f) : prevColor;
                EditorGUILayout.LabelField($"E:{errCount}", GUILayout.Width(40f));
                GUI.color = warnCount > 0 ? new Color(1f, 0.9f, 0.4f) : prevColor;
                EditorGUILayout.LabelField($"W:{warnCount}", GUILayout.Width(40f));
                GUI.color = prevColor;
                if (GUILayout.Button("Console", EditorStyles.miniButton, GUILayout.Width(60f)))
                    EditorApplication.ExecuteMenuItem("Window/General/Console");
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(40f)))
                    LogMonitor.Reset();

                EditorGUILayout.LabelField($"Time: {System.DateTime.Now:HH:mm:ss}", GUILayout.Width(90f));
                GUILayout.FlexibleSpace();

                bool newShowStatus = GUILayout.Toggle(_showStatusBar, "Status", EditorStyles.miniButton, GUILayout.Width(60f));
                bool newShowPerf = GUILayout.Toggle(_showPerfMonitor, "Perf", EditorStyles.miniButton, GUILayout.Width(50f));
                bool newShowTree = GUILayout.Toggle(_showSceneTree, "Tree", EditorStyles.miniButton, GUILayout.Width(50f));
                if (newShowStatus != _showStatusBar) { _showStatusBar = newShowStatus; EditorPrefs.SetBool(EditorPrefsKeys.ShowStatusBar, _showStatusBar); }
                if (newShowPerf != _showPerfMonitor) { _showPerfMonitor = newShowPerf; EditorPrefs.SetBool(EditorPrefsKeys.ShowPerfMonitor, _showPerfMonitor); }
                if (newShowTree != _showSceneTree) { _showSceneTree = newShowTree; EditorPrefs.SetBool(EditorPrefsKeys.ShowSceneTree, _showSceneTree); }
            }
        }

        private void DrawPerformanceMonitor()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Performance Monitor", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Frame Time: {1000f / Mathf.Max(_fpsValue, 0.01f):0.0} ms");
                EditorGUILayout.LabelField($"FPS: {_fpsValue:0.0}");
                EditorGUILayout.LabelField($"Total Memory: {_cachedTotalMemory:0.0} MB");
                EditorGUILayout.LabelField($"Mono Heap: {_cachedMonoHeap:0.0} MB");
                EditorGUILayout.LabelField($"Loaded Objects: {_cachedLoadedObjectsCount}");
                EditorGUILayout.LabelField($"Active GameObjects: {_cachedActiveGameObjectsCount}");
            }
        }

        private void DrawSceneTree()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Scene Tree (Unity Scene)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var roots = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root == null) continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var expanded = EditorGUILayout.Foldout(true, $"{root.name}  [{root.transform.childCount} children]", true);
                        if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                        {
                            EditorGUIUtility.PingObject(root);
                        }
                        if (GUILayout.Button("Select", GUILayout.Width(60f)))
                        {
                            Selection.activeGameObject = root;
                        }
                    }
                    if (root.transform.childCount > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (Transform child in root.transform)
                        {
                            if (child == null) continue;
                            var compCount = child.GetComponents<Component>().Length;
                            EditorGUILayout.LabelField($"├─ {child.name}  [{compCount} components]");
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
        }

        private void DrawTopBar()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                _quickSearch = EditorGUILayout.TextField(_quickSearch, GUILayout.MinWidth(220f));
                if (GUILayout.Button("Clear", GUILayout.Width(60f)))
                {
                    _quickSearch = string.Empty;
                    GUI.FocusControl(null);
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                _commandPalette = EditorGUILayout.TextField(_commandPalette, GUILayout.MinWidth(220f));
                if (GUILayout.Button("Run", GUILayout.Width(60f)))
                {
                    ExecuteCommandPalette();
                }
            }

            if (!string.IsNullOrWhiteSpace(_quickSearch))
            {
                DrawQuickSearchResults();
            }

            var newTab = GUILayout.Toolbar(_selectedTab, _tabs, GUILayout.Height(30));
            if (newTab != _selectedTab)
            {
                _selectedTab = newTab;
                EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
            }
            EditorGUILayout.Space();
        }

        private void DrawQuickSearchResults()
        {
            var query = _quickSearch.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(query)) return;

            var matches = new List<(string category, string label, System.Action action)>();

            for (int i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i].ToLowerInvariant().Contains(query))
                {
                    int tab = i;
                    matches.Add(("Tab", _tabs[i], () => _selectedTab = tab));
                }
            }

            foreach (var section in _sections)
            {
                if (section.DisplayName.ToLowerInvariant().Contains(query))
                {
                    string name = section.DisplayName;
                    matches.Add(("Section", name, () => Debug.Log($"[RingFlow] Section: {name}")));
                }
            }

            foreach (var screen in GetUiScreens())
            {
                if (screen.ToString().ToLowerInvariant().Contains(query))
                {
                    ScreenType s = screen;
                    matches.Add(("Screen", screen.ToString(), () =>
                    {
                        _selectedTab = 2;
                        _uiSelectedScreen = s;
                    }));
                }
            }

            var commands = new (string key, string label, System.Action action)[]
            {
                ("reset canvas", "Reset UIRoot Canvas", () =>
                {
                    var uiRoot = GetCachedUIRoot();
                    if (uiRoot != null) ResetUIRootCanvas(uiRoot);
                }),
                ("snapshot", "Capture Runtime Snapshot", () => _runtimeSnapshotText = BuildRuntimeSnapshot()),
                ("ping root", "Ping NexusRoot", () =>
                {
                    var r = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
                    if (r != null) EditorGUIUtility.PingObject(r);
                }),
                ("ping ui", "Ping UIRoot", () =>
                {
                    var u = GetCachedUIRoot();
                    if (u != null) EditorGUIUtility.PingObject(u);
                }),
                ("setup", "Run Setup Bootstrapper", BootstrapScene),
                ("create scene", "Create Working Scene", CreateWorkingScene),
                ("play", "Enter PlayMode", () => EditorApplication.isPlaying = true),
                ("stop", "Exit PlayMode", () => EditorApplication.isPlaying = false),
            };
            foreach (var cmd in commands)
            {
                if (cmd.key.Contains(query) || cmd.label.ToLowerInvariant().Contains(query))
                    matches.Add(("Action", cmd.label, cmd.action));
            }

            if (matches.Count == 0)
            {
                EditorGUILayout.HelpBox("No matching dashboard item found.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Quick Search Results ({matches.Count})", EditorStyles.boldLabel);
                foreach (var match in matches.Take(10))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var catStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = CategoryColor(match.category) } };
                        EditorGUILayout.LabelField($"[{match.category}]", catStyle, GUILayout.Width(70f));
                        if (GUILayout.Button(match.label, EditorStyles.linkLabel, GUILayout.MinWidth(200f)))
                        {
                            match.action?.Invoke();
                            _quickSearch = "";
                            GUI.FocusControl(null);
                        }
                    }
                }
            }
        }

        private static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "Tab": return new Color(0.4f, 0.8f, 1f);
                case "Screen": return new Color(0.8f, 0.6f, 1f);
                case "Section": return new Color(1f, 0.85f, 0.4f);
                case "Action": return new Color(0.5f, 1f, 0.5f);
                default: return Color.white;
            }
        }

        private void DrawHomeTab()
        {
            EditorGUILayout.LabelField("Developer Overview", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Loaded Sections: {_sections.Count}");
                EditorGUILayout.LabelField($"Open Unity Scene: {UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name}");
                EditorGUILayout.LabelField($"Level Asset Ready: {(_generator.GeneratedLevel != null ? $"Yes (Level {_generator.GeneratedLevel.LevelIndex})" : "No")}");
                EditorGUILayout.LabelField($"Dashboard Search: {(_quickSearch.Length > 0 ? _quickSearch : "—")}");
                EditorGUILayout.LabelField($"Command Palette: {(_commandPalette.Length > 0 ? _commandPalette : "—")}");

                EditorGUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Working Scene", GUILayout.Height(30))) CreateWorkingScene();
                    if (GUILayout.Button("Setup Bootstrapper", GUILayout.Height(30))) BootstrapScene();
                }
            }

            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Go Level Studio", GUILayout.Height(28))) ExecuteAndRemember("Go Level Studio", () => _selectedTab = 1);
                    if (GUILayout.Button("Go UI Studio", GUILayout.Height(28))) ExecuteAndRemember("Go UI Studio", () => _selectedTab = 2);
                    if (GUILayout.Button("Go Runtime", GUILayout.Height(28))) ExecuteAndRemember("Go Runtime", () => _selectedTab = 3);
                    if (GUILayout.Button("Go Data", GUILayout.Height(28))) ExecuteAndRemember("Go Data", () => _selectedTab = 6);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Generate Level", GUILayout.Height(28))) ExecuteAndRemember("Generate Level", _generator.GenerateFromDashboard);
                    if (GUILayout.Button("Build Board", GUILayout.Height(28))) ExecuteAndRemember("Build Board", _visualBuilder.BuildFromDashboard);
                    if (GUILayout.Button("Open Database", GUILayout.Height(28))) ExecuteAndRemember("Open Database", () => _selectedTab = 6);
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Recent Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Recent ({_recentActions.Count}/{MaxRecent})", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Clear", GUILayout.Width(50f), GUILayout.Height(18f)))
                    {
                        _recentActions.Clear();
                        SaveRecent();
                    }
                }
                _recentScroll = EditorGUILayout.BeginScrollView(_recentScroll, GUILayout.Height(80));
                if (_recentActions.Count == 0)
                {
                    EditorGUILayout.LabelField("No actions yet.");
                }
                else
                {
                    foreach (var action in _recentActions.Take(MaxRecent))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"→ {action}", GUILayout.MinWidth(220f));
                            if (GUILayout.Button("Re-run", GUILayout.Width(60f), GUILayout.Height(18f)))
                            {
                                RunFavoriteAction(action);
                            }
                            if (GUILayout.Button("★", GUILayout.Width(24f), GUILayout.Height(18f)))
                            {
                                AddFavorite(action);
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Favorites", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Favorites ({_favoriteActions.Count}/{MaxFavorites})", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(_recentActions.Count == 0 || _favoriteActions.Count >= MaxFavorites))
                    {
                        if (GUILayout.Button("+ Last Action", GUILayout.Width(100f), GUILayout.Height(18f)))
                        {
                            AddFavorite(_recentActions[0]);
                        }
                    }
                }
                _favoritesScroll = EditorGUILayout.BeginScrollView(_favoritesScroll, GUILayout.Height(130));
                if (_favoriteActions.Count == 0)
                {
                    EditorGUILayout.LabelField("No favorites yet. Use '+ Last Action' to add the most recent action.");
                }
                else
                {
                    for (int i = 0; i < _favoriteActions.Count; i++)
                    {
                        var action = _favoriteActions[i];
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField($"★ {action}", GUILayout.MinWidth(200f));
                            if (GUILayout.Button("Run", GUILayout.Width(50f), GUILayout.Height(18f)))
                            {
                                RunFavoriteAction(action);
                            }
                            if (GUILayout.Button("X", GUILayout.Width(24f), GUILayout.Height(18f)))
                            {
                                RemoveFavoriteAt(i);
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void AddFavorite(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return;
            if (_favoriteActions.Contains(actionName)) return;
            if (_favoriteActions.Count >= MaxFavorites) return;
            _favoriteActions.Add(actionName);
            SaveFavorites();
        }

        private void RemoveFavoriteAt(int index)
        {
            if (index < 0 || index >= _favoriteActions.Count) return;
            _favoriteActions.RemoveAt(index);
            SaveFavorites();
        }

        private void DrawUIStudioTab()
        {
            EditorGUILayout.LabelField("UI Studio", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Inspect and test existing UIRoot screens directly from the editor. " +
                    "Show/Hide in the editor only toggles existing scene objects; runtime Signal Show/Hide routes through ISignalBus (requires PlayMode + Root.Context).",
                    MessageType.Info);

                DrawUIStudioInitControls();
                EditorGUILayout.Space(6f);
                DrawUIStudioScreenTree();
                EditorGUILayout.Space(6f);
                DrawUIStudioSignalControls();
                EditorGUILayout.Space(6f);
                DrawUIStudioPersistence();
            }
        }

        private void DrawUIStudioInitControls()
        {
            EditorGUILayout.LabelField("UIRoot State", EditorStyles.miniBoldLabel);
            var uiRoot = GetCachedUIRoot();

            using (new EditorGUILayout.HorizontalScope())
            {
                string status;
                if (uiRoot == null) status = "MISSING — run Setup Bootstrapper";
                else if (GetUIRootCanvas(uiRoot) != null) status = "Initialized";
                else status = "Not initialized (Awake pending)";

                EditorGUILayout.LabelField($"UIRoot: {(uiRoot != null ? uiRoot.name : "—")}  [{status}]", GUILayout.MinWidth(320f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(uiRoot == null))
                {
                    if (GUILayout.Button("Reset Canvas", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog("Reset UIRoot Canvas",
                            "Destroy the runtime canvas and all loaded screen instances?",
                            "Reset", "Cancel"))
                        {
                            ResetUIRootCanvas(uiRoot);
                        }
                    }
                    if (GUILayout.Button("Reload Prefab Screens", GUILayout.Height(24)))
                    {
                        ReloadPrefabScreens(uiRoot);
                    }
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(80f), GUILayout.Height(24)))
                {
                    Repaint();
                }
            }
        }

        private void DrawUIStudioScreenTree()
        {
            EditorGUILayout.LabelField("Screen Tree", EditorStyles.miniBoldLabel);
            var uiRoot = GetCachedUIRoot();
            if (uiRoot == null)
            {
                EditorGUILayout.HelpBox("UIRoot missing in scene.", MessageType.Warning);
                return;
            }

            var screens = GetUIRootScreens(uiRoot);
            var active = GetUIRootActiveScreen(uiRoot);
            var popupStack = GetUIRootPopupStack(uiRoot);
            var subs = GetUIRootSubscriptions(uiRoot);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Active Screen: {active ?? "—"}");
                EditorGUILayout.LabelField($"Popup Stack: {FormatStack(popupStack)}");
                string subsNote = Application.isPlaying ? "" : "  (PlayMode required to populate)";
                EditorGUILayout.LabelField($"Subscriptions: {subs?.Count ?? 0}{subsNote}");
            }

            if (screens == null || screens.Count == 0)
            {
                EditorGUILayout.HelpBox("UIRoot has no screens yet. Add your authored UI screens to the scene or load prefabs.", MessageType.Info);
                return;
            }

            _uiScroll = EditorGUILayout.BeginScrollView(_uiScroll, GUILayout.Height(280));
            foreach (var key in screens.Keys)
            {
                ScreenType screen;
                try { screen = (ScreenType)key; }
                catch { continue; }

                var go = screens[key] as GameObject;
                bool isActive = go != null && go.activeSelf;
                bool isActiveScreen = screen.Equals(active);
                int childCount = go != null ? go.transform.childCount : 0;
                int btnCount = go != null ? go.GetComponentsInChildren<Button>(true).Length : 0;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    string marker = isActiveScreen ? "●" : "○";
                    string state = go == null ? "NULL" : (isActive ? "ACTIVE" : "hidden");
                    var style = new GUIStyle(EditorStyles.label) { fontStyle = isActiveScreen ? FontStyle.Bold : FontStyle.Normal };
                    EditorGUILayout.LabelField($"{marker} {screen,-15} {state,-8} | {childCount} children | {btnCount} btn", style, GUILayout.MinWidth(280f));

                    using (new EditorGUI.DisabledScope(go == null))
                    {
                        if (GUILayout.Button("Show", GUILayout.Width(50f))) ManualSetScreenActive(uiRoot, screen, true);
                        if (GUILayout.Button("Hide", GUILayout.Width(50f))) ManualSetScreenActive(uiRoot, screen, false);
                        if (GUILayout.Button("Signal", GUILayout.Width(55f))) FireShowScreen(screen);
                        if (GUILayout.Button("Open", GUILayout.Width(50f))) OpenPrefabSourceForScreen(screen, go);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawUIStudioSignalControls()
        {
            EditorGUILayout.LabelField("Signal Tester (requires PlayMode + Root.Context)", EditorStyles.miniBoldLabel);
            _uiSelectedScreen = (ScreenType)EditorGUILayout.EnumPopup("Selected Screen", _uiSelectedScreen);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Show via Signal", GUILayout.Height(28))) FireShowScreen(_uiSelectedScreen);
                if (GUILayout.Button("Hide via Signal", GUILayout.Height(28))) FireHideScreen(_uiSelectedScreen);
            }
        }

        private void DrawUIStudioPersistence()
        {
            EditorGUILayout.LabelField("Persistence & Export", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Manage UI authored in the scene or imported from prefabs. The editor does not auto-build screens anymore.\n\n" +
                    "• Reload Prefab Screens loads screen prefabs from Assets/Resources/UI into the active UIRoot canvas.\n" +
                    "• Open lets you jump to the prefab asset backing a screen.\n" +
                    "• Save Scene (Ctrl+S) writes the current UI tree into the active scene asset.\n" +
                    "• Export UI Tree (JSON) serializes the hierarchy for backup or external tooling.\n\n" +
                    "Design workflow: edit the prefab asset → reload it into the scene → save the scene if you want the layout persisted.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save Scene", GUILayout.Height(28)))
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                        Debug.Log("[RingFlow] Scene saved with current UI state.");
                    }
                    if (GUILayout.Button("Reload Prefab Screens", GUILayout.Height(28)))
                    {
                        try
                        {
                            var uiRoot = GetCachedUIRoot();
                            if (uiRoot != null) ReloadPrefabScreens(uiRoot);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogException(ex);
                            EditorUtility.DisplayDialog("Reload Prefab Screens", ex.Message, "OK");
                        }
                    }
                    if (GUILayout.Button("Create Missing UI Screens", GUILayout.Height(28)))
                    {
                        RingFlowEditorUiStudio.CreateMissingUIScreenPrefabs();
                    }
                    if (GUILayout.Button("Export UI Tree (JSON)", GUILayout.Height(28)))
                    {
                        ExportUIHierarchyAsJson();
                    }
                }
            }
        }

        private static void ReloadPrefabScreens(UIRoot uiRoot)
        {
            if (uiRoot == null)
            {
                EditorUtility.DisplayDialog("Reload Prefab Screens", "UIRoot missing. Run Setup Bootstrapper first.", "OK");
                return;
            }

            uiRoot.RebindFromSceneForEditor();

            var screens = GetUIRootScreens(uiRoot);
            if (screens == null || screens.Count == 0)
            {
                uiRoot.LoadPrefabScreensFromResources();
                screens = GetUIRootScreens(uiRoot);
            }

            if (screens == null || screens.Count == 0)
            {
                EditorUtility.DisplayDialog("Reload Prefab Screens",
                    "No screen objects were found in the UICanvas and no matching prefabs exist in Assets/Resources/UI.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog("Reload Prefab Screens", $"Loaded {screens.Count} screen object(s) into UIRoot.", "OK");
        }

        private static void OpenPrefabSourceForScreen(ScreenType screen, GameObject go)
        {
            var path = RingFlowEditorUiStudio.GetPrefabPathForScreen(screen);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                AssetDatabase.OpenAsset(prefab);
                return;
            }

            if (go != null)
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }

            EditorUtility.DisplayDialog("Open Prefab", $"Prefab not found at:\n{path}", "OK");
        }

        private static void ExportUIHierarchyAsJson()
        {
            var uiRoot = GetCachedUIRoot();
            if (uiRoot == null)
            {
                EditorUtility.DisplayDialog("Export UI Tree", "UIRoot missing.", "OK");
                return;
            }
            var screens = GetUIRootScreens(uiRoot);
            if (screens == null || screens.Count == 0)
            {
                EditorUtility.DisplayDialog("Export UI Tree", "No screens to export. Add authored UI screens to the scene first.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"timestamp\":\"").Append(System.DateTime.UtcNow.ToString("o")).Append("\",");
            sb.Append("\"scene\":\"").Append(JsonEscape(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name)).Append("\",");
            sb.Append("\"screens\":[");
            bool first = true;
            foreach (var key in screens.Keys)
            {
                var go = screens[key] as GameObject;
                if (go == null) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{");
                sb.Append("\"type\":\"").Append(key).Append("\",");
                sb.Append("\"name\":\"").Append(JsonEscape(go.name)).Append("\",");
                sb.Append("\"active\":").Append(go.activeSelf ? "true" : "false").Append(",");
                sb.Append("\"root\":");
                SerializeHierarchyNode(go.transform, sb);
                sb.Append("}");
            }
            sb.Append("]}");

            var path = EditorUtility.SaveFilePanel("Export UI Tree", "Assets/Snapshots", "ui-tree.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export UI Tree", $"UI tree exported to:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
            }
        }

        private static void SerializeHierarchyNode(Transform t, StringBuilder sb)
        {
            sb.Append("{");
            sb.Append("\"name\":\"").Append(JsonEscape(t.name)).Append("\",");

            var rect = t.GetComponent<RectTransform>();
            if (rect != null)
            {
                sb.Append("\"rect\":{");
                sb.Append("\"x\":").Append(rect.anchoredPosition.x).Append(",");
                sb.Append("\"y\":").Append(rect.anchoredPosition.y).Append(",");
                sb.Append("\"w\":").Append(rect.sizeDelta.x).Append(",");
                sb.Append("\"h\":").Append(rect.sizeDelta.y).Append(",");
                sb.Append("\"axMin\":[").Append(rect.anchorMin.x).Append(",").Append(rect.anchorMin.y).Append("],");
                sb.Append("\"axMax\":[").Append(rect.anchorMax.x).Append(",").Append(rect.anchorMax.y).Append("]");
                sb.Append("},");
            }

            var text = t.GetComponent<Text>();
            if (text != null)
            {
                sb.Append("\"text\":{");
                sb.Append("\"content\":\"").Append(JsonEscape(text.text)).Append("\",");
                sb.Append("\"size\":").Append(text.fontSize).Append(",");
                sb.Append("\"color\":\"#").Append(ColorUtility.ToHtmlStringRGB(text.color)).Append("\",");
                sb.Append("\"bold\":").Append((text.fontStyle & FontStyle.Bold) != 0 ? "true" : "false");
                sb.Append("},");
            }

            var img = t.GetComponent<Image>();
            if (img != null)
            {
                sb.Append("\"image\":{");
                sb.Append("\"color\":\"#").Append(ColorUtility.ToHtmlStringRGB(img.color)).Append("\",");
                sb.Append("\"raycast\":").Append(img.raycastTarget ? "true" : "false");
                sb.Append("},");
            }

            var btn = t.GetComponent<Button>();
            if (btn != null)
            {
                sb.Append("\"button\":{");
                sb.Append("\"interactable\":").Append(btn.interactable ? "true" : "false");
                sb.Append("},");
            }

            sb.Append("\"children\":[");
            bool first = true;
            for (int i = 0; i < t.childCount; i++)
            {
                if (!first) sb.Append(",");
                first = false;
                SerializeHierarchyNode(t.GetChild(i), sb);
            }
            sb.Append("]}");
        }

        private void DrawDataStudioTab()
        {
            EditorGUILayout.LabelField("Data Studio", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Edit world progression, game config, difficulty bands, and player-facing economy data from one place.",
                    MessageType.Info);

                EditorGUILayout.LabelField("Core Data Surfaces", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• GameConfigDatabaseSO: difficulty bands, color curve, world table");
                EditorGUILayout.LabelField("• WorldConfigSO: per-world theme and mechanic mapping");
                EditorGUILayout.LabelField("• PlayerProgressModel: live progress counters and unlock state");
                EditorGUILayout.LabelField("• RingColorPaletteSO: accessibility and palette control");

                EditorGUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Database Editor", GUILayout.Height(30)))
                    {
                        _selectedTab = 6;
                    }
                    if (GUILayout.Button("Go Runtime Diagnostics", GUILayout.Height(30)))
                    {
                        _selectedTab = 4;
                    }
                }

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Current Database Panel", EditorStyles.boldLabel);
                _databaseSection.OnGUI();
            }
        }

        private void DrawSetupTab()
        {
            EditorGUILayout.LabelField("Project Setup Utilities", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Use these tools to initialize a test scene with the required Nexus components, or create a fresh working scene prepopulated with game lifecycle systems.",
                    MessageType.Info);

                EditorGUILayout.Space(10f);

                if (GUILayout.Button("Create Working Scene", GUILayout.Height(36)))
                {
                    CreateWorkingScene();
                }

                EditorGUILayout.Space(5f);

                if (GUILayout.Button("Setup Bootstrapper in Active Scene", GUILayout.Height(36)))
                {
                    BootstrapScene();
                }
            }
        }

        private void RunFavoriteAction(string action)
        {
            switch (action)
            {
                case "Create Working Scene":
                    CreateWorkingScene();
                    break;
                case "Setup Bootstrapper":
                    BootstrapScene();
                    break;
                case "Generate Level":
                    _generator.GenerateFromDashboard();
                    break;
                case "Build Board":
                    _visualBuilder.BuildFromDashboard();
                    break;
                case "Open UI Studio":
                    _selectedTab = 2;
                    break;
                case "Open Database Editor":
                    _selectedTab = 6;
                    break;
            }
        }

        private void ExecuteAndRemember(string actionName, System.Action action)
        {
            action?.Invoke();
            if (string.IsNullOrEmpty(actionName)) return;
            _recentActions.Remove(actionName);
            _recentActions.Insert(0, actionName);
            if (_recentActions.Count > MaxRecent)
            {
                _recentActions.RemoveRange(MaxRecent, _recentActions.Count - MaxRecent);
            }
            SaveRecent();
        }

        private void DrawMissingReferenceCheck()
        {
            if (!_cachedHasRoot)
            {
                EditorGUILayout.HelpBox("No NexusRoot found in the open scene.", MessageType.Warning);
                return;
            }

            if (!_cachedHasUIRoot)
            {
                EditorGUILayout.HelpBox("UIRoot is missing from the active scene.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("UIRoot: OK");
            }

            if (!_cachedHasEventSystem)
            {
                EditorGUILayout.HelpBox("EventSystem is missing from the active scene.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("EventSystem: OK");
            }
        }

        private void DrawSceneValidator()
        {
            EditorGUILayout.LabelField($"Active Scene: {_cachedSceneName}");
            EditorGUILayout.LabelField($"Is Valid: {_cachedSceneIsValid}");
            EditorGUILayout.LabelField($"Is Loaded: {_cachedSceneIsLoaded}");
            EditorGUILayout.LabelField($"Root Object Count: {_cachedSceneRootCount}");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Missing Reference Check", EditorStyles.miniBoldLabel);
            DrawMissingReferenceCheck();
        }

        private void ExecuteCommandPalette()
        {
            var command = _commandPalette.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(command)) return;

            void SwitchTo(int tab, string label) => ExecuteAndRemember(label, () => _selectedTab = tab);

            if (ContainsAny(command, "ui", "ui studio", "screen")) SwitchTo(2, "Open UI Studio");
            else if (ContainsAny(command, "level", "board", "level design")) SwitchTo(1, "Go Level Studio");
            else if (ContainsAny(command, "data", "database", "data studio")) SwitchTo(6, "Go Data Studio");
            else if (ContainsAny(command, "runtime", "play", "play mode", "lifecycle")) SwitchTo(3, "Go Runtime");
            else if (ContainsAny(command, "system", "config", "settings", "diagnostic")) SwitchTo(4, "Go System & Config");
            else if (ContainsAny(command, "setup", "project setup", "bootstrap"))
                ExecuteAndRemember("Setup Bootstrapper", BootstrapScene);
            else if (ContainsAny(command, "home", "overview", "dashboard")) SwitchTo(0, "Go Home");
            else if (ContainsAny(command, "create scene", "new scene"))
                ExecuteAndRemember("Create Working Scene", CreateWorkingScene);
            else if (ContainsAny(command, "build", "build board"))
                ExecuteAndRemember("Build Board", _visualBuilder.BuildFromDashboard);
            else if (ContainsAny(command, "generate", "generate level"))
                ExecuteAndRemember("Generate Level", _generator.GenerateFromDashboard);
            else if (ContainsAny(command, "reset canvas", "reset ui"))
            {
                var uiRoot = GetCachedUIRoot();
                if (uiRoot != null) ResetUIRootCanvas(uiRoot);
            }
            else if (ContainsAny(command, "snapshot", "capture"))
            {
                _runtimeSnapshotText = BuildRuntimeSnapshot();
                ExecuteAndRemember("Capture Snapshot", null);
            }
            else
            {
                EditorUtility.DisplayDialog("Command Palette",
                    $"Unknown command: '{command}'. Try: ui, level, data, runtime, system, setup, home, scene, build, generate, reset, snapshot.",
                    "OK");
            }
        }

        private static bool ContainsAny(string source, params string[] keys)
        {
            foreach (var k in keys) if (source.Contains(k)) return true;
            return false;
        }

        private IEnumerable<ScreenType> GetUiScreens()
        {
            return new[]
            {
                ScreenType.Splash,
                ScreenType.MainMenu,
                ScreenType.LevelSelect,
                ScreenType.Gameplay,
                ScreenType.Pause,
                ScreenType.Win,
                ScreenType.Settings,
                ScreenType.DailyReward,
                ScreenType.ChestPopup,
                ScreenType.GameOver,
                ScreenType.ParentalGate,
            };
        }

        private static void FireShowScreen(ScreenType screen)
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root == null || root.Context == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "No initialized Root found in the open scene.", "OK");
                return;
            }

            var bus = root.Context.TryResolve<ISignalBus>();
            if (bus == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "Signal bus is unavailable in the current scene.", "OK");
                return;
            }

            bus.Fire(new ShowScreenSignal(screen));
        }

        private static readonly BindingFlags s_privInst = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly System.Type s_uiRootType = typeof(UIRoot);
        private static readonly System.Collections.Generic.Dictionary<string, FieldInfo> s_uiRootFieldCache
            = new System.Collections.Generic.Dictionary<string, FieldInfo>();

        private static FieldInfo GetCachedUIRootField(string name)
        {
            if (!s_uiRootFieldCache.TryGetValue(name, out var fi))
            {
                fi = s_uiRootType.GetField(name, s_privInst);
                s_uiRootFieldCache[name] = fi;
            }
            return fi;
        }

        private static Canvas GetUIRootCanvas(UIRoot uiRoot)
            => GetCachedUIRootField("_canvas")?.GetValue(uiRoot) as Canvas;

        private static System.Collections.IDictionary GetUIRootScreens(UIRoot uiRoot)
            => GetCachedUIRootField("_screens")?.GetValue(uiRoot) as System.Collections.IDictionary;

        private static object GetUIRootActiveScreen(UIRoot uiRoot)
            => GetCachedUIRootField("_activeExclusiveScreen")?.GetValue(uiRoot);

        private static System.Collections.ICollection GetUIRootPopupStack(UIRoot uiRoot)
            => GetCachedUIRootField("_popupStack")?.GetValue(uiRoot) as System.Collections.ICollection;

        private static System.Collections.ICollection GetUIRootSubscriptions(UIRoot uiRoot)
            => GetCachedUIRootField("_subscriptions")?.GetValue(uiRoot) as System.Collections.ICollection;

        private static UIRoot s_cachedUIRoot;
        private static double s_lastUIRootLookup;
        private const double UIRootCacheSeconds = 0.5;

        private static UIRoot GetCachedUIRoot()
        {
            var now = EditorApplication.timeSinceStartup;
            if (s_cachedUIRoot == null || (now - s_lastUIRootLookup) > UIRootCacheSeconds)
            {
                s_cachedUIRoot = Object.FindAnyObjectByType<UIRoot>(FindObjectsInactive.Include);
                s_lastUIRootLookup = now;
            }
            return s_cachedUIRoot;
        }

        private static void InvalidateUIRootCache()
        {
            s_cachedUIRoot = null;
        }

        private static void ResetUIRootCanvas(UIRoot uiRoot)
        {
            if (uiRoot == null) return;
            var screens = GetUIRootScreens(uiRoot);
            if (screens != null)
            {
                var toDestroy = new List<GameObject>();
                foreach (var key in screens.Keys)
                {
                    if (screens[key] is GameObject go && go != null) toDestroy.Add(go);
                }
                screens.Clear();
                foreach (var go in toDestroy) Object.DestroyImmediate(go);
            }
            var canvas = GetUIRootCanvas(uiRoot);
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            s_uiRootType.GetField("_canvas", s_privInst)?.SetValue(uiRoot, null);
            s_uiRootType.GetField("_subscribed", s_privInst)?.SetValue(uiRoot, false);
        }

        private static bool ManualSetScreenActive(UIRoot uiRoot, ScreenType screen, bool active)
        {
            if (uiRoot == null) return false;
            var screens = GetUIRootScreens(uiRoot);
            if (screens == null || !screens.Contains(screen)) return false;
            var go = screens[screen] as GameObject;
            if (go == null) return false;

            go.SetActive(active);
            if (active)
            {
                s_uiRootType.GetField("_activeExclusiveScreen", s_privInst)?.SetValue(uiRoot, screen);
            }
            return true;
        }

        private static string FormatStack(System.Collections.ICollection stack)
        {
            if (stack == null || stack.Count == 0) return "(empty)";
            var items = new List<string>();
            foreach (var item in stack) items.Add(item?.ToString() ?? "null");
            return string.Join(" → ", items);
        }

        private static void FireHideScreen(ScreenType screen)
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root == null || root.Context == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "No initialized Root found in the open scene.", "OK");
                return;
            }

            var bus = root.Context.TryResolve<ISignalBus>();
            if (bus == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "Signal bus is unavailable in the current scene.", "OK");
                return;
            }

            bus.Fire(new HideScreenSignal(screen));
        }

        private void DrawUIBindingInspector()
        {
            EditorGUILayout.LabelField("UI Binding Inspector", EditorStyles.miniBoldLabel);

            var uiRoot = GetCachedUIRoot();
            if (uiRoot == null)
            {
                EditorGUILayout.HelpBox("UIRoot not found in the active scene.", MessageType.Warning);
                return;
            }

            var canvas = GetUIRootCanvas(uiRoot);
            if (canvas == null)
            {
                EditorGUILayout.HelpBox("UIRoot canvas is missing. Reload prefab screens from Resources/UI.", MessageType.Info);
                return;
            }

            var screens = GetUIRootScreens(uiRoot);
            var active = GetUIRootActiveScreen(uiRoot);
            var popupStack = GetUIRootPopupStack(uiRoot);
            var subs = GetUIRootSubscriptions(uiRoot);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Canvas: {canvas.name} ({canvas.renderMode})");
                var scaler = canvas.GetComponent<CanvasScaler>();
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                EditorGUILayout.LabelField($"  CanvasScaler: {(scaler != null ? "OK" : "MISSING")}");
                EditorGUILayout.LabelField($"  GraphicRaycaster: {(raycaster != null ? "OK" : "MISSING")}");
            }

            EditorGUILayout.LabelField($"Registered Screens: {screens?.Count ?? 0}");
            EditorGUILayout.LabelField($"Active Exclusive Screen: {active ?? "—"}");
            EditorGUILayout.LabelField($"Popup Stack Depth: {popupStack?.Count ?? 0}");
            EditorGUILayout.LabelField($"Signal Subscriptions: {subs?.Count ?? 0}");

            if (screens == null || screens.Count == 0)
            {
                EditorGUILayout.HelpBox("UIRoot has no registered screens. Load prefab screens from Resources/UI first.", MessageType.Warning);
                return;
            }

            int nullCount = 0;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var key in screens.Keys)
                {
                    var go = screens[key] as GameObject;
                    bool isActive = go != null && go.activeSelf;
                    if (go == null) nullCount++;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string status = go == null ? "NULL" : (isActive ? "ACTIVE" : "hidden");
                        EditorGUILayout.LabelField($"{key,-15}  {status}");
                        if (go != null && GUILayout.Button("Ping", GUILayout.Width(50f)))
                        {
                            EditorGUIUtility.PingObject(go);
                        }
                    }
                }
            }

            if (nullCount > 0)
            {
                EditorGUILayout.HelpBox($"{nullCount} screen reference(s) are null. UIRoot may not have finished Awake().", MessageType.Error);
            }
            else if ((subs?.Count ?? 0) == 0)
            {
                EditorGUILayout.HelpBox("No signal subscriptions registered. UIRoot is not yet wired to the signal bus (likely needs PlayMode).", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"UI binding healthy: {screens.Count} screens, {subs?.Count ?? 0} subscriptions.", MessageType.Info);
            }
        }

        private string _runtimeSnapshotText = "";
        private string _lastSnapshotPath = "";

        private void DrawRuntimeSnapshot()
        {
            EditorGUILayout.LabelField("Runtime Snapshot", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture Snapshot", GUILayout.Height(24)))
                {
                    _runtimeSnapshotText = BuildRuntimeSnapshot();
                }
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_runtimeSnapshotText)))
                {
                    if (GUILayout.Button("Copy to Clipboard", GUILayout.Width(130f), GUILayout.Height(24)))
                    {
                        EditorGUIUtility.systemCopyBuffer = _runtimeSnapshotText;
                        _lastSnapshotPath = "(clipboard)";
                    }
                    if (GUILayout.Button("Save to File…", GUILayout.Width(130f), GUILayout.Height(24)))
                    {
                        var path = EditorUtility.SaveFilePanel(
                            "Save Runtime Snapshot", "Assets/Snapshots", "ringflow-snapshot.json", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            try
                            {
                                File.WriteAllText(path, _runtimeSnapshotText);
                                _lastSnapshotPath = path;
                            }
                            catch (System.Exception ex)
                            {
                                EditorUtility.DisplayDialog("Snapshot Save Failed", ex.Message, "OK");
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(_lastSnapshotPath))
            {
                EditorGUILayout.LabelField($"Last saved: {_lastSnapshotPath}", EditorStyles.miniLabel);
            }

            if (string.IsNullOrEmpty(_runtimeSnapshotText))
            {
                EditorGUILayout.HelpBox("Click 'Capture Snapshot' to dump the current scene/UI/runtime state as JSON.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("Snapshot (JSON):", EditorStyles.miniBoldLabel);
            _uiScroll = EditorGUILayout.BeginScrollView(_uiScroll, GUILayout.Height(160));
            EditorGUILayout.TextArea(_runtimeSnapshotText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static string BuildRuntimeSnapshot()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"timestamp\":\"").Append(System.DateTime.UtcNow.ToString("o")).Append("\",");

            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            sb.Append("\"scene\":{");
            sb.Append("\"name\":\"").Append(JsonEscape(scene.name)).Append("\",");
            sb.Append("\"path\":\"").Append(JsonEscape(scene.path)).Append("\",");
            sb.Append("\"isValid\":").Append(scene.IsValid() ? "true" : "false").Append(",");
            sb.Append("\"isLoaded\":").Append(scene.isLoaded ? "true" : "false").Append(",");
            sb.Append("\"rootCount\":").Append(scene.rootCount);
            sb.Append("},");

            sb.Append("\"app\":{");
            sb.Append("\"isPlaying\":").Append(Application.isPlaying ? "true" : "false").Append(",");
            sb.Append("\"isPaused\":").Append(EditorApplication.isPaused ? "true" : "false").Append(",");
            sb.Append("\"platform\":\"").Append(JsonEscape(Application.platform.ToString())).Append("\",");
            sb.Append("\"unityVersion\":\"").Append(JsonEscape(Application.unityVersion)).Append("\"");
            sb.Append("},");

            var root = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
            sb.Append("\"nexusRoot\":");
            if (root == null)
            {
                sb.Append("null");
            }
            else
            {
                const BindingFlags rf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var ctxProp = typeof(Root).GetProperty("Context", rf);
                var isInitProp = typeof(Root).GetProperty("IsInitialized", rf);
                object ctx = ctxProp?.GetValue(root);
                bool isInit = isInitProp != null && (bool)isInitProp.GetValue(root);
                sb.Append("{");
                sb.Append("\"name\":\"").Append(JsonEscape(root.gameObject.name)).Append("\",");
                sb.Append("\"isInitialized\":").Append(isInit ? "true" : "false").Append(",");
                sb.Append("\"context\":").Append(ctx == null ? "null" : "\"resolved\"");
                sb.Append("}");
            }
            sb.Append(",");

            var uiRoot = GetCachedUIRoot();
            sb.Append("\"uiRoot\":");
            if (uiRoot == null)
            {
                sb.Append("null");
            }
            else
            {
                const BindingFlags uf = BindingFlags.Instance | BindingFlags.NonPublic;
                var screens = typeof(UIRoot).GetField("_screens", uf)?.GetValue(uiRoot) as System.Collections.IDictionary;
                var active = typeof(UIRoot).GetField("_activeExclusiveScreen", uf)?.GetValue(uiRoot);
                var popupStack = typeof(UIRoot).GetField("_popupStack", uf)?.GetValue(uiRoot) as System.Collections.ICollection;
                var subs = typeof(UIRoot).GetField("_subscriptions", uf)?.GetValue(uiRoot) as System.Collections.ICollection;
                var canvas = typeof(UIRoot).GetField("_canvas", uf)?.GetValue(uiRoot) as Canvas;

                sb.Append("{");
                sb.Append("\"screenCount\":").Append(screens?.Count ?? 0).Append(",");
                sb.Append("\"activeExclusive\":\"").Append(active ?? "null").Append("\",");
                sb.Append("\"popupStack\":[");
                if (popupStack != null)
                {
                    bool first = true;
                    foreach (var item in popupStack)
                    {
                        if (!first) sb.Append(",");
                        sb.Append("\"").Append(item).Append("\"");
                        first = false;
                    }
                }
                sb.Append("],");
                sb.Append("\"subscriptionCount\":").Append(subs?.Count ?? 0).Append(",");
                sb.Append("\"canvas\":").Append(canvas == null ? "null" : "\"" + JsonEscape(canvas.name) + "\"");

                if (screens != null)
                {
                    sb.Append(",\"screens\":{");
                    bool first = true;
                    foreach (var key in screens.Keys)
                    {
                        var go = screens[key] as GameObject;
                        if (!first) sb.Append(",");
                        sb.Append("\"").Append(key).Append("\":");
                        if (go == null) sb.Append("null");
                        else sb.Append("{\"name\":\"").Append(JsonEscape(go.name)).Append("\",\"active\":").Append(go.activeSelf ? "true" : "false").Append("}");
                        first = false;
                    }
                    sb.Append("}");
                }
                sb.Append("}");
            }
            sb.Append(",");

            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
            sb.Append("\"eventSystem\":").Append(es == null ? "null" : "\"" + JsonEscape(es.gameObject.name) + "\"").Append(",");

            sb.Append("\"diagnostics\":{");
            sb.Append("\"mode\":\"").Append(Application.isPlaying ? "PlayMode" : "EditMode").Append("\",");
            sb.Append("\"editorTime\":\"").Append(System.DateTime.UtcNow.ToString("HH:mm:ss")).Append("\"");
            sb.Append("}");

            sb.Append("}");
            return sb.ToString();
        }

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void DrawHeader(string title)
        {
            if (s_headerTex == null)
            {
                s_headerTex = new Texture2D(2, 2);
                var px = new[] { HeaderColor, HeaderColor, HeaderColor, HeaderColor };
                s_headerTex.SetPixels(px);
                s_headerTex.Apply();
            }

            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { background = s_headerTex, textColor = HeaderTextColor }
            };
            GUILayout.Box(title, style, GUILayout.ExpandWidth(true), GUILayout.Height(HeaderHeight));
            EditorGUILayout.Space();
        }
    }
}
