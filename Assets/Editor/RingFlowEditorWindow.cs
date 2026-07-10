using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;

namespace RingFlow.Editor
{
    public class RingFlowEditorWindow : EditorWindow
    {
        private const float HeaderHeight = 40f;
        private const float ToolbarHeight = 28f;
        private const float SectionSpacing = 4f;

        private GeneratorSection _generator;
        private VisualBuilderSection _visualBuilder;
        private RuntimeSection _runtime;
        private SettingsSection _settings;
        private AdTesterSection _adTester;
        private DiagnosticsSection _diagnostics;
        private DatabaseSection _databaseSection;
        private GameFeelSection _gameFeel;

        private Vector2 _scroll;
        private int _selectedTab;
        private readonly string[] _tabs = { "Ana Sayfa", "Seviyeler", "Arayüz Stüdyosu", "Ayarlar & Araçlar" };
        private Vector2 _uiScroll;
        private ScreenType _uiSelectedScreen = ScreenType.Splash;

        private double _lastValidationUpdateTime;
        private const double ValidationUpdateInterval = 1.0;
        private string _cachedSceneName;
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

        [MenuItem("Ring Flow/Generate All 500 Levels", false, 11)]
        public static void GenerateAllLevels()
        {
            GetWindow<RingFlowEditorWindow>("RingFlow Dashboard")._generator.GenerateFromDashboardAll();
        }

        [MenuItem("Ring Flow/Create Working Scene", false, 10)]
        public static void CreateWorkingScene()
        {
            const string scenePath = "Assets/Scenes/RingFlow.unity";
            if (System.IO.File.Exists(scenePath))
            {
                if (EditorUtility.DisplayDialog("Scene Exists",
                    $"A scene already exists at {scenePath}. Open it instead?", "Open", "Cancel"))
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
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

            var uiRoot = result.Root.GetComponent<UIRoot>();
            if (uiRoot != null) ReloadPrefabScreens(uiRoot, false);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Scene Created",
                $"Working scene saved to {scenePath} with all UI screens instantiated as Prefab Links.", "OK");
        }

        public static void BootstrapScene()
        {
            var result = EditorBootstrapper.Bootstrap();
            if (result.Success)
            {
                var uiRoot = result.Root.GetComponent<UIRoot>();
                if (uiRoot != null) ReloadPrefabScreens(uiRoot, false);
                EditorUtility.DisplayDialog("Setup",
                    "Nexus Bootstrapper successfully added to the active scene, and UI screens populated as Prefab Links! Press Play to run.", "OK");
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
            _gameFeel = new GameFeelSection();

            foreach (var s in new EditorSection[]
                { _generator, _visualBuilder, _runtime, _settings, _adTester, _diagnostics, _databaseSection, _gameFeel })
                s.HideHeader = true;

            _generator.OnEnable();
            _selectedTab = Mathf.Clamp(EditorPrefs.GetInt(EditorPrefsKeys.SelectedTab, 0), 0, _tabs.Length - 1);

            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnAnySceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnAnySceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated += OnNewSceneCreated;
        }

        private void OnAnySceneChanged(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
            => InvalidateCaches();

        private void OnAnySceneClosed(UnityEngine.SceneManagement.Scene scene)
            => InvalidateCaches();

        private void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene,
            UnityEditor.SceneManagement.NewSceneSetup setup,
            UnityEditor.SceneManagement.NewSceneMode mode)
            => InvalidateCaches();

        private void InvalidateCaches()
        {
            s_cachedUIRoot = null;
            _cachedSceneName = null;
            _lastValidationUpdateTime = 0;
            _cachedHasRoot = false;
            _cachedHasUIRoot = false;
            _cachedHasEventSystem = false;
        }

        private void OnDisable()
        {
            _generator?.OnDisable();
            EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnAnySceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= OnAnySceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
        }

        private void RefreshValidationCache()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastValidationUpdateTime < ValidationUpdateInterval && !string.IsNullOrEmpty(_cachedSceneName))
                return;

            _lastValidationUpdateTime = now;
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            _cachedSceneName = activeScene.name;
            var root = Object.FindAnyObjectByType<Root>();
            _cachedHasRoot = root != null;
            _cachedHasUIRoot = root != null && root.GetComponentInChildren<UIRoot>(true) != null;
            _cachedHasEventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null;
        }

        private void OnGUI()
        {
            RefreshValidationCache();
            DrawHeader("RING FLOW KONTROL PANELİ");
            DrawToolbar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_selectedTab)
            {
                case 0: DrawHomeTab(); break;
                case 1: DrawLevelsTab(); break;
                case 2: DrawUIStudioTab(); break;
                case 3: DrawToolsTab(); break;
            }

            EditorGUILayout.EndScrollView();
            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            var newTab = GUILayout.Toolbar(_selectedTab, _tabs, GUILayout.Height(ToolbarHeight));
            if (newTab != _selectedTab)
            {
                _selectedTab = newTab;
                EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
            }
            EditorGUILayout.Space(SectionSpacing);
        }

        private static void DrawHeader(string title)
        {
            GUILayout.Box(title, RingFlowEditorUtils.HeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(HeaderHeight));
            EditorGUILayout.Space(2f);
        }

        // ---------------------------------------------------------------
        //  Status Bar
        // ---------------------------------------------------------------

        private void DrawStatusBar()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            string mode = RingFlowEditorUtils.GetEditorModeLabel();
            Color modeColor = RingFlowEditorUtils.GetEditorModeColor();

            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUI.color = modeColor;
                EditorGUILayout.LabelField($"  [{mode}]", EditorStyles.boldLabel, GUILayout.Width(70f));
                GUI.color = Color.white;
                EditorGUILayout.LabelField(scene.name, GUILayout.MinWidth(120f));

                if (_cachedHasRoot)
                    EditorGUILayout.LabelField("Root: Tamam", GUILayout.Width(80f));
                if (_cachedHasUIRoot)
                    EditorGUILayout.LabelField("UI: Tamam", GUILayout.Width(65f));

                GUILayout.FlexibleSpace();

                int errCount = LogMonitor.ErrorCount;
                int warnCount = LogMonitor.WarningCount;
                if (errCount > 0)
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    EditorGUILayout.LabelField($"Hatalar: {errCount}", EditorStyles.boldLabel, GUILayout.Width(80f));
                    GUI.color = Color.white;
                }
                if (warnCount > 0)
                {
                    GUI.color = new Color(1f, 0.85f, 0.3f);
                    EditorGUILayout.LabelField($"Uyarılar: {warnCount}", GUILayout.Width(90f));
                    GUI.color = Color.white;
                }

                if (GUILayout.Button("Konsol", EditorStyles.miniButton, GUILayout.Width(65f)))
                    EditorApplication.ExecuteMenuItem("Window/General/Console");
                if (GUILayout.Button("Temizle", EditorStyles.miniButton, GUILayout.Width(55f)))
                    LogMonitor.Reset();
            }
        }

        // ---------------------------------------------------------------
        //  Home Tab
        // ---------------------------------------------------------------

        private void DrawHomeTab()
        {
            DrawSceneSection();
            EditorGUILayout.Space(SectionSpacing);
            DrawActionCards();
            EditorGUILayout.Space(SectionSpacing);
            DrawLevelStatus();
        }

        private void DrawSceneSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                string mode = RingFlowEditorUtils.GetEditorModeLabel();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Aktif Sahne", EditorStyles.boldLabel, GUILayout.Width(80f));
                    EditorGUILayout.LabelField(scene.name + (scene.isDirty ? " *" : ""), GUILayout.MinWidth(150f));

                    var prevColor = GUI.color;
                    GUI.color = RingFlowEditorUtils.GetEditorModeColor();
                    EditorGUILayout.LabelField(mode, EditorStyles.miniLabel, GUILayout.Width(40f));
                    GUI.color = prevColor;

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Sahne Oluştur", EditorStyles.miniButton, GUILayout.Width(100f)))
                        CreateWorkingScene();
                    if (GUILayout.Button("Kurulum Yap (Bootstrap)", EditorStyles.miniButton, GUILayout.Width(140f)))
                        BootstrapScene();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        _cachedHasRoot ? "Root: Tamam" : "Root: EKSİK",
                        _cachedHasRoot ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel,
                        GUILayout.Width(90f));
                    EditorGUILayout.LabelField(
                        _cachedHasUIRoot ? "UIRoot: Tamam" : "UIRoot: EKSİK",
                        _cachedHasUIRoot ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel,
                        GUILayout.Width(100f));
                    EditorGUILayout.LabelField(
                        _cachedHasEventSystem ? "EventSystem: Tamam" : "EventSystem: EKSİK",
                        _cachedHasEventSystem ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel,
                        GUILayout.Width(140f));
                }
            }
        }

        private void DrawActionCards()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Hızlı İşlemler", EditorStyles.boldLabel);
                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (ActionCard("Seviyeler", "Üret & Kur", new Color(0.25f, 0.55f, 0.85f)))
                        _selectedTab = 1;
                    if (ActionCard("Arayüz", "Ekran & Sinyal", new Color(0.7f, 0.35f, 0.8f)))
                        _selectedTab = 2;
                    if (ActionCard("Araçlar", "Çalışma & Ayar", new Color(0.4f, 0.7f, 0.4f)))
                        _selectedTab = 3;
                    if (ActionCard("Üret", "Tek Seviye", new Color(0.85f, 0.5f, 0.2f)))
                        _generator.GenerateFromDashboard();
                    if (ActionCard("Tahta Kur", "Sahnede Kur", new Color(0.85f, 0.3f, 0.3f)))
                        _visualBuilder.BuildFromDashboard();
                }
            }
        }

        private static bool ActionCard(string title, string subtitle, Color accent)
        {
            var defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = accent;

            bool clicked = GUILayout.Button(title, RingFlowEditorUtils.CompactBoldButton,
                GUILayout.MinWidth(120f), GUILayout.ExpandWidth(true));
            GUI.backgroundColor = defaultBg;

            var r = GUILayoutUtility.GetLastRect();
            r.x += 2; r.y += 2; r.width -= 4; r.height -= 4;
            EditorGUI.LabelField(
                r, subtitle,
                new GUIStyle(RingFlowEditorUtils.CenteredMiniLabel)
                    { alignment = TextAnchor.LowerCenter, fontSize = 9 });

            return clicked;
        }

        private void DrawLevelStatus()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Seviye Varlığı", EditorStyles.boldLabel, GUILayout.Width(90f));
                    if (_generator.GeneratedLevel != null)
                        EditorGUILayout.LabelField($"Seviye {_generator.GeneratedLevel.LevelIndex} yüklendi", GUILayout.MinWidth(120f));
                    else
                        EditorGUILayout.LabelField("Yüklü seviye yok", GUILayout.MinWidth(120f));

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Üret", EditorStyles.miniButton, GUILayout.Width(80f)))
                        _generator.GenerateFromDashboard();
                    if (GUILayout.Button("Toplu Üret", EditorStyles.miniButton, GUILayout.Width(110f)))
                        _generator.GenerateFromDashboardAll();
                }
            }
        }

        // ---------------------------------------------------------------
        //  Levels Tab
        // ---------------------------------------------------------------

        private void DrawLevelsTab()
        {
            EditorGUILayout.LabelField("Seviye Üretici & Çözücü Ayarları", EditorStyles.boldLabel);
            _generator.OnGUI();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Sahne Tahtası Oluşturucu", EditorStyles.boldLabel);
            _visualBuilder.OnGUI();
        }

        // ---------------------------------------------------------------
        //  UI Studio Tab
        // ---------------------------------------------------------------

        private void DrawUIStudioTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawUIStudioInitControls();
                EditorGUILayout.Space(SectionSpacing);
                DrawUIStudioScreenTree();
                EditorGUILayout.Space(SectionSpacing);
                DrawUIStudioSignalControls();
                EditorGUILayout.Space(SectionSpacing);
                DrawUIStudioPersistence();
            }
        }

        private void DrawUIStudioInitControls()
        {
            var uiRoot = GetCachedUIRoot();

            using (new EditorGUILayout.HorizontalScope())
            {
                string status;
                if (uiRoot == null) status = "EKSİK";
                else if (GetUIRootCanvas(uiRoot) != null) status = "Hazır";
                else status = "Başlatma Bekliyor";

                EditorGUILayout.LabelField($"UIRoot: {(uiRoot != null ? uiRoot.name : "\u2014")}  [{status}]",
                    EditorStyles.boldLabel, GUILayout.MinWidth(280f));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(uiRoot == null))
                {
                    if (GUILayout.Button("Tuvali Sıfırla (Reset)", GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("UIRoot Tuvalini Sıfırla",
                            "Tüm yüklenen ekran örneklerini yok etmek istediğinize emin misiniz?", "Sıfırla", "İptal"))
                            ResetUIRootCanvas(uiRoot);
                    }
                    if (GUILayout.Button("Ekranları Yeniden Yükle", GUILayout.Height(22)))
                        ReloadPrefabScreens(uiRoot);
                }
                if (GUILayout.Button("Yenile", GUILayout.Width(70f), GUILayout.Height(22)))
                    Repaint();
            }
        }

        private void DrawUIStudioScreenTree()
        {
            var uiRoot = GetCachedUIRoot();
            if (uiRoot == null)
            {
                EditorGUILayout.HelpBox("Sahnede UIRoot bulunamadı. Önce kurulumu yapın (Setup Bootstrapper).", MessageType.Warning);
                return;
            }

            var screens = GetUIRootScreens(uiRoot);
            var active = GetUIRootActiveScreen(uiRoot);
            var popupStack = GetUIRootPopupStack(uiRoot);
            var subs = GetUIRootSubscriptions(uiRoot);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Aktif: {active ?? "\u2014"}   Popuplar: {FormatStack(popupStack)}   " +
                    $"Abonelik: {subs?.Count ?? 0}{(Application.isPlaying ? "" : " (PlayMode)")}",
                    EditorStyles.miniLabel);
            }

            if (screens == null || screens.Count == 0)
            {
                EditorGUILayout.HelpBox("Kayıtlı ekran bulunamadı.", MessageType.Info);
                return;
            }

            _uiScroll = EditorGUILayout.BeginScrollView(_uiScroll, GUILayout.Height(260));
            foreach (var key in screens.Keys)
            {
                ScreenType screen;
                try { screen = (ScreenType)key; }
                catch { continue; }

                var go = screens[key] as GameObject;
                bool isActive = go != null && go.activeSelf;
                bool isActiveScreen = screen.Equals(active);
                int btnCount = go != null ? go.GetComponentsInChildren<Button>(true).Length : 0;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    string marker = isActiveScreen ? ">>" : "  ";
                    string state = go == null ? "YOK" : (isActive ? "Açık" : "Gizli");
                    var style = new GUIStyle(EditorStyles.label)
                        { fontStyle = isActiveScreen ? FontStyle.Bold : FontStyle.Normal };
                    EditorGUILayout.LabelField($"{marker} {screen,-14} [{state}] {btnCount} Buton", style,
                        GUILayout.MinWidth(240f));

                    using (new EditorGUI.DisabledScope(go == null))
                    {
                        if (GUILayout.Button("Göster", GUILayout.Width(44f)))
                            ManualSetScreenActive(uiRoot, screen, true);
                        if (GUILayout.Button("Gizle", GUILayout.Width(44f)))
                            ManualSetScreenActive(uiRoot, screen, false);
                        if (GUILayout.Button("Sinyal", GUILayout.Width(50f)))
                            FireShowScreen(screen);
                        if (GUILayout.Button("Aç", GUILayout.Width(44f)))
                            OpenPrefabSourceForScreen(screen, go);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawUIStudioSignalControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Sinyal Test Edici (PlayMode gerekir)", EditorStyles.miniBoldLabel,
                    GUILayout.Width(220f));
                _uiSelectedScreen = (ScreenType)EditorGUILayout.EnumPopup(_uiSelectedScreen, GUILayout.Width(140f));
                if (GUILayout.Button("Göster Sinyali", GUILayout.Height(22)))
                    FireShowScreen(_uiSelectedScreen);
                if (GUILayout.Button("Gizle Sinyali", GUILayout.Height(22)))
                    FireHideScreen(_uiSelectedScreen);
            }
        }

        private void DrawUIStudioPersistence()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sahneyi Kaydet", GUILayout.Height(24)))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[RingFlow] Sahne mevcut durumla kaydedildi.");
                }
                if (GUILayout.Button("Prefab Ekranlarını Yenile", GUILayout.Height(24)))
                {
                    try
                    {
                        var uiRoot = GetCachedUIRoot();
                        if (uiRoot != null) ReloadPrefabScreens(uiRoot);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog("Prefab Ekranlarını Yenile", ex.Message, "Tamam");
                    }
                }
                if (GUILayout.Button("Eksik Ekranları Oluştur", GUILayout.Height(24)))
                    RingFlowEditorUiStudio.CreateMissingUIScreenPrefabs();
                if (GUILayout.Button("UI Yapısını Dışa Aktar (JSON)", GUILayout.Height(24)))
                    ExportUIHierarchyAsJson();
            }
        }

        // ---------------------------------------------------------------
        //  Tools Tab
        // ---------------------------------------------------------------

        private void DrawToolsTab()
        {
            EditorGUILayout.LabelField("PlayMode Çalışma Modları", EditorStyles.boldLabel);
            _runtime.OnGUI();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Reklam ve Ödüllendirme Test Cihazı", EditorStyles.boldLabel);
            _adTester.OnGUI();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Genel Ayarlar", EditorStyles.boldLabel);
            _settings.OnGUI();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Analiz ve Sinyal İnceleyici", EditorStyles.boldLabel);
            _diagnostics.OnGUI();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Oyun Hissiyatı (Game Feel) & Kamera", EditorStyles.boldLabel);
            _gameFeel.OnGUI();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Veritabanı Düzenleyici", EditorStyles.boldLabel);
            _databaseSection.OnGUI();
        }

        // ---------------------------------------------------------------
        //  UI Export
        // ---------------------------------------------------------------

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
                EditorUtility.DisplayDialog("Export UI Tree", "No screens to export.", "OK");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("{\"screens\":[");
            bool first = true;
            foreach (var key in screens.Keys)
            {
                var go = screens[key] as GameObject;
                if (go == null) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"type\":\"").Append(key).Append("\",");
                sb.Append("\"active\":").Append(go.activeSelf ? "true" : "false");
                sb.Append("}");
            }
            sb.Append("]}");

            var path = EditorUtility.SaveFilePanel("Export UI Tree", "Assets/Snapshots", "ui-tree.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export UI Tree", $"Exported to:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
            }
        }

        // ---------------------------------------------------------------
        //  Prefab Screens
        // ---------------------------------------------------------------

        private static void ReloadPrefabScreens(UIRoot uiRoot, bool showDialog = true)
        {
            if (uiRoot == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Reload Prefab Screens", "UIRoot missing. Run Setup Bootstrapper first.", "OK");
                return;
            }

            var canvas = GetUIRootCanvas(uiRoot);
            if (canvas == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Reload Prefab Screens", "UIRoot Canvas is missing.", "OK");
                return;
            }

            var screens = GetUIRootScreens(uiRoot);
            if (screens == null) return;

            var toDestroy = new List<GameObject>();
            foreach (var key in screens.Keys)
                if (screens[key] is GameObject go && go != null)
                    toDestroy.Add(go);
            screens.Clear();
            foreach (var go in toDestroy) Object.DestroyImmediate(go);

            var missingScreens = new List<string>();
            var allScreens = System.Enum.GetValues(typeof(ScreenType));
            foreach (ScreenType screen in allScreens)
            {
                var path = RingFlowEditorUiStudio.GetPrefabPathForScreen(screen);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    prefab = Resources.Load<GameObject>($"UI/{screen}");
                if (prefab == null)
                {
                    missingScreens.Add(screen.ToString());
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
                if (instance != null)
                {
                    instance.name = screen.ToString();
                    instance.SetActive(screen == ScreenType.Splash);
                    screens[screen] = instance;
                }
            }

            s_uiRootType.GetField("_activeExclusiveScreen", s_privInst)?.SetValue(uiRoot, ScreenType.Splash);
            EditorUtility.SetDirty(uiRoot);
            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            string msg = $"Loaded {screens.Count} screen(s) as Prefab Links.";
            if (missingScreens.Count > 0)
                msg += $"\nMissing: {string.Join(", ", missingScreens)}";
            Debug.Log($"[RingFlow] ReloadPrefabScreens: {screens.Count} screens. Missing: {missingScreens.Count}");
            if (showDialog)
                EditorUtility.DisplayDialog("Reload Prefab Screens", msg, "OK");
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

        // ---------------------------------------------------------------
        //  UIRoot Reflection Helpers
        // ---------------------------------------------------------------

        private static readonly BindingFlags s_privInst = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly System.Type s_uiRootType = typeof(UIRoot);

        private static readonly Dictionary<string, FieldInfo> s_uiRootFieldCache = new();

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

        private static void ResetUIRootCanvas(UIRoot uiRoot)
        {
            if (uiRoot == null) return;
            var screens = GetUIRootScreens(uiRoot);
            if (screens != null)
            {
                var toDestroy = new List<GameObject>();
                foreach (var key in screens.Keys)
                    if (screens[key] is GameObject go && go != null)
                        toDestroy.Add(go);
                screens.Clear();
                foreach (var go in toDestroy) Object.DestroyImmediate(go);
            }
            var canvas = GetUIRootCanvas(uiRoot);
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            s_uiRootType.GetField("_canvas", s_privInst)?.SetValue(uiRoot, null);
            s_uiRootType.GetField("_subscribed", s_privInst)?.SetValue(uiRoot, false);
        }

        private static void ManualSetScreenActive(UIRoot uiRoot, ScreenType screen, bool active)
        {
            if (uiRoot == null) return;
            var screens = GetUIRootScreens(uiRoot);
            if (screens == null || !screens.Contains(screen)) return;
            var go = screens[screen] as GameObject;
            if (go == null) return;

            go.SetActive(active);
            if (active)
                s_uiRootType.GetField("_activeExclusiveScreen", s_privInst)?.SetValue(uiRoot, screen);
        }

        private static string FormatStack(System.Collections.ICollection stack)
        {
            if (stack == null || stack.Count == 0) return "(empty)";
            var items = new List<string>();
            foreach (var item in stack) items.Add(item?.ToString() ?? "null");
            return string.Join(" \u2192 ", items);
        }

        private static void FireShowScreen(ScreenType screen)
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root?.Context == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "No initialized Root found.", "OK");
                return;
            }
            var bus = root.Context.TryResolve<ISignalBus>();
            if (bus == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "Signal bus unavailable.", "OK");
                return;
            }
            bus.Fire(new ShowScreenSignal(screen));
        }

        private static void FireHideScreen(ScreenType screen)
        {
            var root = Object.FindAnyObjectByType<Root>();
            if (root?.Context == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "No initialized Root found.", "OK");
                return;
            }
            var bus = root.Context.TryResolve<ISignalBus>();
            if (bus == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "Signal bus unavailable.", "OK");
                return;
            }
            bus.Fire(new HideScreenSignal(screen));
        }
    }
}
