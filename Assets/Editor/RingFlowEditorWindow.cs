using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RingFlow.Gameplay.UI;

namespace RingFlow.Editor
{
    /// <summary>
    /// Top-level dashboard window. The window itself is intentionally thin —
    /// it owns the tab list, status bar, and the section lifecycle. The
    /// heavy work (UI Studio tree, prefab reload, signal tester, JSON
    /// export) lives in <see cref="RingFlowEditorUiStudioController"/>.
    /// </summary>
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
        private RingFlowEditorUiStudioController _uiStudio;

        private Vector2 _scroll;
        private int _selectedTab;
        private readonly string[] _tabs = { "Ana Sayfa", "Seviyeler", "Arayüz Stüdyosu", "Ayarlar & Araçlar" };

        private double _lastValidationUpdateTime;
        private string _cachedSceneName;
        private bool _cachedHasRoot;
        private bool _cachedHasUIRoot;
        private bool _cachedHasEventSystem;

        [MenuItem("Ring Flow/Dashboard &G", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("RingFlow Dashboard");
            window.minSize = EditorPaths.MinWindowSize;
            window.Show();
        }

        [MenuItem("Ring Flow/Generate Levels (Batch)", false, 11)]
        public static void GenerateAllLevels()
        {
            GetWindow<RingFlowEditorWindow>("RingFlow Dashboard")._generator.GenerateFromDashboardAll();
        }

        [MenuItem("Ring Flow/Create Working Scene", false, 10)]
        public static void CreateWorkingScene()
        {
            const string scenePath = EditorPaths.ScenePath;
            if (System.IO.File.Exists(scenePath))
            {
                if (EditorUtility.DisplayDialog("Scene Exists",
                    $"A scene already exists at {scenePath}. Open it instead?", "Open", "Cancel"))
                    EditorSceneManager.OpenScene(scenePath);
                return;
            }

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            var result = EditorBootstrapper.Bootstrap();
            if (!result.Success)
            {
                EditorUtility.DisplayDialog("Bootstrap Failed", result.Message, "OK");
                return;
            }

            var uiRoot = result.Root.GetComponent<UIRoot>();
            if (uiRoot != null)
                RingFlowEditorUiStudioController.ReloadPrefabScreens(uiRoot, showDialog: false);

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Scene Created",
                $"Working scene saved to {scenePath} with all UI screens instantiated as Prefab Links.", "OK");
        }

        public static void BootstrapScene()
        {
            var result = EditorBootstrapper.Bootstrap();
            if (result.Success)
            {
                var uiRoot = result.Root.GetComponent<UIRoot>();
                if (uiRoot != null)
                    RingFlowEditorUiStudioController.ReloadPrefabScreens(uiRoot, showDialog: false);
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
            _uiStudio = new RingFlowEditorUiStudioController();

            foreach (var s in new EditorSection[]
                { _generator, _visualBuilder, _runtime, _settings, _adTester, _diagnostics, _databaseSection, _gameFeel })
                s.HideHeader = true;

            _generator.OnEnable();
            _selectedTab = Mathf.Clamp(EditorPrefs.GetInt(EditorPrefsKeys.SelectedTab, 0), 0, _tabs.Length - 1);

            EditorSceneManager.sceneOpened += OnAnySceneChanged;
            EditorSceneManager.sceneClosed += OnAnySceneClosed;
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;
        }

        private void OnAnySceneChanged(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
            => InvalidateCaches();

        private void OnAnySceneClosed(UnityEngine.SceneManagement.Scene scene)
            => InvalidateCaches();

        private void OnNewSceneCreated(UnityEngine.SceneManagement.Scene scene,
            NewSceneSetup setup, NewSceneMode mode)
            => InvalidateCaches();

        private void InvalidateCaches()
        {
            EditorSceneContext.InvalidateAll();
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
            EditorSceneManager.sceneOpened -= OnAnySceneChanged;
            EditorSceneManager.sceneClosed -= OnAnySceneClosed;
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
        }

        private void RefreshValidationCache()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastValidationUpdateTime < EditorPaths.ValidationCacheSeconds && !string.IsNullOrEmpty(_cachedSceneName))
                return;

            _lastValidationUpdateTime = now;
            var activeScene = EditorSceneManager.GetActiveScene();
            _cachedSceneName = activeScene.name;

            var root = EditorSceneContext.GetRoot();
            _cachedHasRoot = root != null;
            _cachedHasUIRoot = root != null && root.GetComponentInChildren<UIRoot>(true) != null;
            _cachedHasEventSystem = EditorSceneContext.GetEventSystem() != null;
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
                case 2: _uiStudio.DrawTab(); break;
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

        // ──────────────────────────────────────────────────────────────
        //  Status Bar
        // ──────────────────────────────────────────────────────────────

        private void DrawStatusBar()
        {
            var scene = EditorSceneManager.GetActiveScene();
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
                    EditorGUILayout.LabelField($"Uyarılar: {warnCount}", EditorStyles.boldLabel, GUILayout.Width(90f));
                    GUI.color = Color.white;
                }

                if (GUILayout.Button("Konsol", EditorStyles.miniButton, GUILayout.Width(65f)))
                    EditorApplication.ExecuteMenuItem("Window/General/Console");
                if (GUILayout.Button("Temizle", EditorStyles.miniButton, GUILayout.Width(55f)))
                    LogMonitor.Reset();
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Home Tab
        // ──────────────────────────────────────────────────────────────

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
                var scene = EditorSceneManager.GetActiveScene();
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
                    if (GUILayout.Button(new GUIContent("Sahne Oluştur",
                            "Yeni boş sahne oluşturur, Nexus Root + UIRoot ekler ve tüm UI prefablerini bağlar."),
                            EditorStyles.miniButton, GUILayout.Width(110f)))
                        CreateWorkingScene();
                    if (GUILayout.Button(new GUIContent("Kurulum Yap (Bootstrap)",
                            "Aktif sahneye Nexus Root + UIRoot ekler (sahneyi sıfırlamaz)."),
                            EditorStyles.miniButton, GUILayout.Width(160f)))
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
                EditorGUILayout.LabelField("1) Üret → 2) Doğrula → 3) Sahneye Uygula", EditorStyles.miniLabel);
                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (ActionCard("Seviyeler", "Üret & Kur", new Color(0.25f, 0.55f, 0.85f),
                        "Seviye üretici + veritabanı + sahne tahtası sekmesine atlar."))
                        _selectedTab = 1;
                    if (ActionCard("Arayüz", "Ekran & Sinyal", new Color(0.7f, 0.35f, 0.8f),
                        "UI Studio sekmesine atlar: ekranlar, sinyaller, JSON dışa aktarımı."))
                        _selectedTab = 2;
                    if (ActionCard("Araçlar", "Çalışma & Ayar", new Color(0.4f, 0.7f, 0.4f),
                        "Runtime, reklam test, ayarlar, tanılama, game-feel sekmesine atlar."))
                        _selectedTab = 3;
                    if (ActionCard("Hızlı Üret", "Seçili Seviye", new Color(0.85f, 0.5f, 0.2f),
                        "Seviye Üretici sekmesindeki parametrelerle tek seviye üretir."))
                        _generator.GenerateFromDashboard();
                    if (ActionCard("Hızlı Kur", "Sahne Tahtası", new Color(0.85f, 0.3f, 0.3f),
                        "Üretilen seviyeyi (veya aktif oyunu) sahnede tahta olarak kurar."))
                        _visualBuilder.BuildFromDashboard();
                }
            }
        }

        private static bool ActionCard(string title, string subtitle, Color accent, string tooltip)
        {
            var defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = accent;

            var content = new GUIContent(title, tooltip);
            bool clicked = GUILayout.Button(content, RingFlowEditorUtils.CompactBoldButton,
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

                EditorGUILayout.HelpBox("İpucu: Seçili seviye üzerinde çalışın, sonra toplu üretimle aynı kuralları tüm aralığa uygulayın.", MessageType.Info);
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Levels Tab
        // ──────────────────────────────────────────────────────────────

        private void DrawLevelsTab()
        {
            EditorGUILayout.LabelField("Seviye Üretici & Çözücü Ayarları", EditorStyles.boldLabel);
            _generator.OnGUI();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Seviye Denetleyici", EditorStyles.boldLabel);
            _databaseSection.OnGUI();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Sahne Tahtası Oluşturucu", EditorStyles.boldLabel);
            _visualBuilder.OnGUI();
        }

        // ──────────────────────────────────────────────────────────────
        //  Tools Tab
        // ──────────────────────────────────────────────────────────────

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
    }
}
