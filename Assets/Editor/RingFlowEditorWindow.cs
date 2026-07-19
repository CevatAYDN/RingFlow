using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RingFlow.Gameplay;
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
        private GeneratorSection _generator;
        private VisualBuilderSection _visualBuilder;
        private RuntimeSection _runtime;
        private SettingsSection _settings;
        private AdTesterSection _adTester;
        private DiagnosticsSection _diagnostics;
        private DatabaseSection _databaseSection;
        private GameFeelSection _gameFeel;
        private ConfigSection _configSection;
        private LevelBrowserSection _levelBrowser;
        private DataOverviewSection _dataOverview;
        private RingFlowEditorUiStudioController _uiStudio;
        private UnityEditor.Editor _cachedActiveLevelEditor;

        private Vector2 _scroll;
        private int _selectedTab;
        private readonly string[] _tabs = { "Ana Sayfa", "Seviye İşlemleri", "Arayüz Stüdyosu", "Veri & GDD Denetimi", "Ayarlar & Araçlar" };

        private int _selectedConfigSubTab = 0;
        private readonly string[] _configSubTabs = {
            "Oyun Modu & Çalışma",
            "Reklam Test Cihazı",
            "Genel Ayarlar",
            "Analiz & Sinyaller",
            "Oyun Hissiyatı (Game Feel)",
            "Oyun Veritabanı (Database)",
            "Halka Mekanikleri",
            "Ses Yapılandırması",
            "Arayüz Teması",
            "Mağaza Kataloğu",
            "Tema/Skin Veritabanı",
            "Yerelleştirme Ayarları",
            "Ekran Kayıt Defteri (Screen Registry)"
        };

        private UnityEditor.Editor _cachedAssetEditor;
        private ScriptableObject _cachedAssetObj;
        private string _cachedAssetKey;

        private double _lastValidationUpdateTime;
        private string _cachedSceneName;
        private bool _cachedHasRoot;
        private bool _cachedHasUIRoot;
        private bool _cachedHasEventSystem;
        private float _cachedWindowWidth;
        private bool _cachedCompactLayout;

        [MenuItem("Ring Flow/Dashboard &G", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("RingFlow Dashboard");
            window.minSize = EditorPaths.MinWindowSize;
            window.Show();
        }

        [MenuItem("Ring Flow/Generate Levels (Batch) &L", false, 11)]
        public static void GenerateAllLevels()
        {
            GetWindow<RingFlowEditorWindow>("RingFlow Dashboard")._generator.GenerateFromDashboardAll();
        }

        [MenuItem("Ring Flow/Create Working Scene &N", false, 10)]
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
            _configSection = new ConfigSection();
            _levelBrowser = new LevelBrowserSection();
            _dataOverview = new DataOverviewSection();
            _uiStudio = new RingFlowEditorUiStudioController();

            foreach (var s in new EditorSection[]
                { _generator, _visualBuilder, _runtime, _settings, _adTester, _diagnostics, _databaseSection, _gameFeel, _configSection, _levelBrowser, _dataOverview })
                s.HideHeader = true;

            // Wire generator → level browser cache invalidation.
            // When a level asset is saved (single or batch), the LevelBrowser
            // must re-scan the filesystem so newly created levels appear green
            // and deleted/missing levels appear grey immediately.
            _generator.OnLevelAssetsChanged += _levelBrowser.InvalidateExistenceCache;

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
            // Unsubscribe callback before clearing references to prevent leaks
            // if the window is re-opened (OnEnable re-subscribes fresh).
            if (_generator != null && _levelBrowser != null)
                _generator.OnLevelAssetsChanged -= _levelBrowser.InvalidateExistenceCache;

            _generator?.OnDisable();
            EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
            EditorSceneManager.sceneOpened -= OnAnySceneChanged;
            EditorSceneManager.sceneClosed -= OnAnySceneClosed;
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;

            if (_cachedAssetEditor != null)
            {
                DestroyImmediate(_cachedAssetEditor);
                _cachedAssetEditor = null;
            }

            if (_cachedActiveLevelEditor != null)
            {
                DestroyImmediate(_cachedActiveLevelEditor);
                _cachedActiveLevelEditor = null;
            }

            _uiStudio?.Cleanup();
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

            if (Event.current.type == EventType.Layout)
            {
                _cachedWindowWidth = position.width;
                _cachedCompactLayout = _cachedWindowWidth < 980f;
            }

            float windowWidth = _cachedWindowWidth;
            bool compactLayout = _cachedCompactLayout;
            float sidebarWidth = compactLayout ? windowWidth : Mathf.Clamp(windowWidth * 0.22f, 190f, 260f);

            if (compactLayout)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawSidebar(sidebarWidth, compactLayout);
                    EditorGUILayout.Space(6f);
                    DrawMainArea(compactLayout);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSidebar(sidebarWidth, compactLayout);
                    GUILayout.Box("", GUILayout.Width(2f), GUILayout.ExpandHeight(true));
                    DrawMainArea(compactLayout);
                }
            }
        }

        private void DrawSidebar(float sidebarWidth, bool compactLayout)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(sidebarWidth), GUILayout.ExpandHeight(!compactLayout)))
            {
                var prevColor = GUI.color;
                GUI.color = EditorPaths.EditorColors.Info;
                if (!compactLayout)
                {
                    EditorGUILayout.LabelField("RINGFLOW", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Studio Dashboard", EditorStyles.miniLabel);
                    EditorGUILayout.Space(8f);
                }
                GUI.color = prevColor;

                DrawTabButtons(compactLayout, sidebarWidth);

                if (!compactLayout)
                {
                    GUILayout.FlexibleSpace();
                    DrawLogSummarySidebar();
                }
            }
        }

        private void DrawTabButtons(bool compactLayout, float sidebarWidth)
        {
            if (compactLayout)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    for (int i = 0; i < _tabs.Length; i++)
                    {
                        DrawHorizontalTab(i, 32f);
                        if (i < _tabs.Length - 1)
                            EditorGUILayout.Space(2f);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _tabs.Length; i++)
                {
                    DrawSidebarTab(i, sidebarWidth - 10f);
                    EditorGUILayout.Space(2f);
                }
            }
        }

        private void DrawSidebarTab(int tabIndex, float width)
        {
            bool isActive = _selectedTab == tabIndex;
            Rect rect = GUILayoutUtility.GetRect(width, 36f);
            
            bool isHover = rect.Contains(Event.current.mousePosition);
            Color bgColor = isActive 
                ? new Color(0.2f, 0.22f, 0.26f, 1f) 
                : (isHover ? new Color(0.16f, 0.18f, 0.22f, 0.8f) : Color.clear);
            
            if (bgColor != Color.clear)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            if (isActive)
            {
                Rect barRect = new(rect.x, rect.y, 4f, rect.height);
                EditorGUI.DrawRect(barRect, EditorPaths.EditorColors.Info);
            }
            
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                fontSize = 11,
                padding = new RectOffset(isActive ? 12 : 8, 4, 0, 0)
            };
            style.normal.textColor = isActive ? Color.white : EditorPaths.EditorColors.MutedText;
            
            GUI.Label(rect, _tabs[tabIndex], style);
            
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedTab = tabIndex;
                _scroll = Vector2.zero;
                EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
                if (_cachedAssetEditor != null)
                {
                    DestroyImmediate(_cachedAssetEditor);
                    _cachedAssetEditor = null;
                    _cachedAssetObj = null;
                }
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private void DrawHorizontalTab(int tabIndex, float height = 30f)
        {
            bool isActive = _selectedTab == tabIndex;
            Rect rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            
            bool isHover = rect.Contains(Event.current.mousePosition);
            Color bgColor = isActive 
                ? new Color(0.2f, 0.22f, 0.26f, 1f) 
                : (isHover ? new Color(0.16f, 0.18f, 0.22f, 0.8f) : Color.clear);
            
            if (bgColor != Color.clear)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            if (isActive)
            {
                Rect barRect = new(rect.x, rect.yMax - 3f, rect.width, 3f);
                EditorGUI.DrawRect(barRect, EditorPaths.EditorColors.Info);
            }
            
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                fontSize = 10,
            };
            style.normal.textColor = isActive ? Color.white : EditorPaths.EditorColors.MutedText;
            
            GUI.Label(rect, _tabs[tabIndex], style);
            
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedTab = tabIndex;
                _scroll = Vector2.zero;
                EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
                if (_cachedAssetEditor != null)
                {
                    DestroyImmediate(_cachedAssetEditor);
                    _cachedAssetEditor = null;
                    _cachedAssetObj = null;
                }
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private void DrawMainArea(bool compactLayout)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                // Show active tab header
                EditorGUILayout.LabelField(_tabs[_selectedTab].ToUpper(), RingFlowEditorUtils.HeaderStyle);
                EditorGUILayout.Space(4f);

                if (compactLayout)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField("Kompakt Düzen", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Dar pencerede sekmeler üst üste gösterilir.", EditorStyles.wordWrappedMiniLabel);
                    }
                    EditorGUILayout.Space(4f);
                }

                switch (_selectedTab)
                {
                    case 0: DrawHomeTab(); break;
                    case 1: DrawLevelsTab(); break;
                    case 2: _uiStudio.DrawTab(); break;
                    case 3: DrawDataTab(); break;
                    case 4: DrawToolsTab(); break;
                }

                EditorGUILayout.EndScrollView();
            }

            DrawStatusBar();
        }

        private void DrawLogSummarySidebar()
        {
            int errCount = LogMonitor.ErrorCount;
            int warnCount = LogMonitor.WarningCount;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Log Özeti", EditorStyles.boldLabel);
                
                var prevColor = GUI.color;
                if (errCount > 0)
                {
                    GUI.color = EditorPaths.EditorColors.Error;
                    EditorGUILayout.LabelField($"✘ Hatalar: {errCount}", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("✔ Hata Yok", EditorStyles.miniLabel);
                }

                if (warnCount > 0)
                {
                    GUI.color = EditorPaths.EditorColors.Warning;
                    EditorGUILayout.LabelField($"⚠ Uyarılar: {warnCount}", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("✔ Uyarı Yok", EditorStyles.miniLabel);
                }
                GUI.color = prevColor;

                EditorGUILayout.Space(2f);
                if (RingFlowEditorUtils.IsNarrowWidth(260f))
                {
                    if (GUILayout.Button("Konsol", EditorStyles.miniButton))
                         EditorApplication.ExecuteMenuItem("Window/General/Console");
                    if (GUILayout.Button("Temizle", EditorStyles.miniButton))
                        LogMonitor.Reset();
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Konsol", EditorStyles.miniButton))
                            EditorApplication.ExecuteMenuItem("Window/General/Console");
                        if (GUILayout.Button("Temizle", EditorStyles.miniButton))
                            LogMonitor.Reset();
                    }
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Status Bar
        // ──────────────────────────────────────────────────────────────

        private void DrawStatusBar()
        {
            var scene = EditorSceneManager.GetActiveScene();
            string mode = RingFlowEditorUtils.GetEditorModeLabel();
            Color modeColor = RingFlowEditorUtils.GetEditorModeColor();
            bool compact = RingFlowEditorUtils.IsNarrowWidth(760f);

            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.color = modeColor;
                    EditorGUILayout.LabelField($"[{mode}]", EditorStyles.boldLabel, GUILayout.Width(66f));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(scene.name) ? "Untitled Scene" : scene.name, EditorStyles.label);
                    if (!compact)
                    {
                        if (_cachedHasRoot)
                            EditorGUILayout.LabelField("Root: Tamam", GUILayout.Width(80f));
                        if (_cachedHasUIRoot)
                            EditorGUILayout.LabelField("UI: Tamam", GUILayout.Width(65f));
                        GUILayout.FlexibleSpace();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    int errCount = LogMonitor.ErrorCount;
                    int warnCount = LogMonitor.WarningCount;
                    if (errCount > 0)
                    {
                        GUI.color = EditorPaths.EditorColors.Error;
                        EditorGUILayout.LabelField($"Hatalar: {errCount}", EditorStyles.boldLabel, GUILayout.Width(80f));
                        GUI.color = Color.white;
                    }
                    if (warnCount > 0)
                    {
                        GUI.color = EditorPaths.EditorColors.Warning;
                        EditorGUILayout.LabelField($"Uyarılar: {warnCount}", EditorStyles.boldLabel, GUILayout.Width(90f));
                        GUI.color = Color.white;
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Konsol", EditorStyles.miniButton, GUILayout.Width(65f)))
                        EditorApplication.ExecuteMenuItem("Window/General/Console");
                    if (GUILayout.Button("Temizle", EditorStyles.miniButton, GUILayout.Width(55f)))
                        LogMonitor.Reset();
                    GUI.color = Color.white;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Home Tab
        // ──────────────────────────────────────────────────────────────

        private void DrawHomeTab()
        {
            DrawSceneSection();
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
            DrawActionCards();
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
            DrawLevelStatus();
        }

        private void DrawSceneSection()
        {
            RingFlowEditorUtils.BeginSectionBox("Aktif Sahne Yapılandırması", "Sahnede Nexus Root, UIRoot ve EventSystem durumunu izleyin.");
            
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
            
            RingFlowEditorUtils.EndSectionBox();
        }

        private void DrawActionCards()
        {
            RingFlowEditorUtils.BeginSectionBox("Hızlı İşlemler Paneli", "Seviye üretimi, arayüz stüdyosu ve araçlar arasında hızlı geçiş yapın.");
            
            float rightWidth = position.width - 200f;
            int cols = 3;
            if (rightWidth < 420f) cols = 1;
            else if (rightWidth < 600f) cols = 2;

            var cards = new System.Func<bool>[]
            {
                () => ActionCard("Seviyeler", "Üret & Kur", EditorPaths.EditorColors.CardLevels, "Seviye üretici + veritabanı + sahne tahtası sekmesine atlar."),
                () => ActionCard("Arayüz", "Ekran & Sinyal", EditorPaths.EditorColors.CardInterface, "UI Studio sekmesine atlar: ekranlar, sinyaller, JSON dışa aktarımı."),
                () => ActionCard("Araçlar", "Çalışma & Ayar", EditorPaths.EditorColors.CardTools, "Runtime, reklam test, ayarlar, tanılama, game-feel sekmesine atlar."),
                () => ActionCard("Hızlı Üret", "Seçili Seviye", EditorPaths.EditorColors.CardQuickGen, "Seviye Üretici sekmesindeki parametrelerle tek seviye üretir."),
                () => ActionCard("Hızlı Kur", "Sahne Tahtası", EditorPaths.EditorColors.CardQuickSetup, "Üretilen seviyeyi (veya aktif oyunu) sahnede tahta olarak kurar.")
            };

            int index = 0;
            while (index < cards.Length)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int c = 0; c < cols && index < cards.Length; c++)
                    {
                        if (cards[index]())
                        {
                            // handle click actions
                            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                            {
                                if (index == 0) _selectedTab = 1;
                                else if (index == 1) _selectedTab = 2;
                                else if (index == 2) _selectedTab = 4; // Tools tab is 4
                                else if (index == 3) _generator.GenerateFromDashboard();
                                else if (index == 4) _visualBuilder.BuildFromDashboard();
                            }

                            if (index < 3 && _cachedAssetEditor != null)
                            {
                                DestroyImmediate(_cachedAssetEditor);
                                _cachedAssetEditor = null;
                                _cachedAssetObj = null;
                            }
                        }
                        index++;
                        if (c < cols - 1) EditorGUILayout.Space(4f);
                    }
                }
                if (index < cards.Length) EditorGUILayout.Space(4f);
            }
            
            RingFlowEditorUtils.EndSectionBox();
        }

        private static GUIStyle s_cardSubStyle;

        private static bool ActionCard(string title, string subtitle, Color accent, string tooltip)
        {
            var defaultBg = GUI.backgroundColor;
            GUI.backgroundColor = accent;

            var content = new GUIContent(title, tooltip);
            bool clicked = GUILayout.Button(content, RingFlowEditorUtils.CompactBoldButton,
                GUILayout.MinHeight(44f), GUILayout.ExpandWidth(true));
            GUI.backgroundColor = defaultBg;

            var r = GUILayoutUtility.GetLastRect();
            r.x += 2; r.y += 2; r.width -= 4; r.height -= 4;

            if (s_cardSubStyle == null)
            {
                s_cardSubStyle = new GUIStyle(RingFlowEditorUtils.CenteredMiniLabel)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = 9,
                    normal = { textColor = Color.white }
                };
            }
            EditorGUI.LabelField(r, subtitle, s_cardSubStyle);

            return clicked;
        }

        private void DrawLevelStatus()
        {
            RingFlowEditorUtils.BeginSectionBox("Seviye İlerleme Durumu", "Kayıtlı bölümlerin ve GDD veritabanının genel durumunu görüntüleyin.");
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Seviye Varlığı", EditorStyles.boldLabel, GUILayout.Width(90f));
                if (_generator.GeneratedLevel != null)
                    EditorGUILayout.LabelField($"Seviye {_generator.GeneratedLevel.LevelIndex} yüklendi", GUILayout.MinWidth(120f));
                else
                    EditorGUILayout.LabelField("Yüklü seviye yok", GUILayout.MinWidth(120f));

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Üret", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                        _generator.GenerateFromDashboard();
                }
                if (GUILayout.Button("Toplu Üret", EditorStyles.miniButton, GUILayout.Width(110f)))
                {
                    if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                        _generator.GenerateFromDashboardAll();
                }
            }

            EditorGUILayout.HelpBox("İpucu: Seçili seviye üzerinde çalışın, sonra toplu üretimle aynı kuralları tüm aralığa uygulayın.", MessageType.Info);
            
            RingFlowEditorUtils.EndSectionBox();
        }

        // ──────────────────────────────────────────────────────────────
        //  Levels Tab
        // ──────────────────────────────────────────────────────────────

        private void DrawLevelsTab()
        {
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldGenerator, "Seviye Üretici & Çözücü Ayarları", _generator.OnGUI);
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDatabase, "Seviye Denetleyici", _databaseSection.OnGUI);
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldLevelBrowser, "Seviye Tarayıcı (Level Browser)", _levelBrowser.OnGUI);
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            var activeLevel = Selection.activeObject as LevelDataSO;
            if (activeLevel != null)
            {
                RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldActiveLevelEditor, $"Aktif Seviye Düzenleyici (Seviye {activeLevel.Data.LevelIndex})", () =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var db = new RingFlow.Gameplay.Services.ResourcesAssetService()
                        .LoadAsync<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)
                        .GetAwaiter().GetResult();
                        int maxLevels = db != null ? db.TotalLevels : 2000;

                        using (new EditorGUI.DisabledScope(activeLevel.Data.LevelIndex <= 1))
                        {
                            if (GUILayout.Button("◀ Önceki Seviye", GUILayout.Height(24)))
                            {
                                int prevIdx = activeLevel.Data.LevelIndex - 1;
                                string path = $"{EditorPaths.LevelsFolder}/Level_{prevIdx}.asset";
                                var prevAsset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
                                if (prevAsset != null)
                                {
                                    Selection.activeObject = prevAsset;
                                    EditorGUIUtility.PingObject(prevAsset);
                                }
                            }
                        }

                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = EditorPaths.EditorColors.Success;
                        if (GUILayout.Button("▶ Oyunu Başlat & Oyna (Play)", GUILayout.Height(24)))
                        {
                            EditorPlayFromLevel.Play(activeLevel.Data.LevelIndex);
                        }
                        GUI.backgroundColor = prevColor;

                        using (new EditorGUI.DisabledScope(activeLevel.Data.LevelIndex >= maxLevels))
                        {
                            if (GUILayout.Button("Sonraki Seviye ▶", GUILayout.Height(24)))
                            {
                                int nextIdx = activeLevel.Data.LevelIndex + 1;
                                string path = $"{EditorPaths.LevelsFolder}/Level_{nextIdx}.asset";
                                var nextAsset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
                                if (nextAsset != null)
                                {
                                    Selection.activeObject = nextAsset;
                                    EditorGUIUtility.PingObject(nextAsset);
                                }
                            }
                        }
                    }

                    EditorGUILayout.Space(4f);

                    if (_cachedActiveLevelEditor == null || _cachedActiveLevelEditor.target != activeLevel)
                    {
                        if (_cachedActiveLevelEditor != null)
                            DestroyImmediate(_cachedActiveLevelEditor);
                        _cachedActiveLevelEditor = UnityEditor.Editor.CreateEditor(activeLevel);
                    }

                    if (_cachedActiveLevelEditor != null)
                    {
                        _cachedActiveLevelEditor.OnInspectorGUI();
                    }
                });
                EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            }

            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldBuilder, "Sahne Tahtası Oluşturucu", _visualBuilder.OnGUI);
        }

        // ──────────────────────────────────────────────────────────────
        //  Tools Tab
        // ──────────────────────────────────────────────────────────────

        private void DrawToolsTab()
        {
            bool compact = RingFlowEditorUtils.IsNarrowWidth(860f);
            if (compact)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    RingFlowEditorUtils.BeginSectionBox("Konfigürasyon Panelleri", "Sistem yapılandırma dosyalarını yönetmek için bir kategori seçin.");
                    int columns = RingFlowEditorUtils.GetResponsiveColumns(150f, 2, 3);
                    int index = 0;
                    while (index < _configSubTabs.Length)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            for (int col = 0; col < columns && index < _configSubTabs.Length; col++)
                            {
                                DrawConfigTabButton(index++, true);
                                if (col < columns - 1)
                                    EditorGUILayout.Space(4f);
                            }
                        }
                        EditorGUILayout.Space(2f);
                    }
                    RingFlowEditorUtils.EndSectionBox();

                    EditorGUILayout.Space(8f);
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        DrawActiveConfigSubTab();
                    }
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Left Column: Categories
                    Rect colRect = EditorGUILayout.BeginVertical(GUILayout.Width(200f), GUILayout.ExpandHeight(true));
                    if (Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(colRect, new Color(0.15f, 0.17f, 0.20f, 1f));
                        RingFlowEditorUtils.DrawRectBorder(colRect, new Color(0.24f, 0.26f, 0.30f, 1f), 1);
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(8f);
                    GUILayout.BeginVertical();
                    GUILayout.Space(8f);
                    
                    EditorGUILayout.LabelField("KATEGORİLER", EditorStyles.miniBoldLabel);
                    EditorGUILayout.Space(4f);
                    for (int i = 0; i < _configSubTabs.Length; i++)
                    {
                        DrawConfigTabButton(i, false);
                        EditorGUILayout.Space(2f);
                    }
                    
                    GUILayout.Space(8f);
                    GUILayout.EndVertical();
                    GUILayout.Space(8f);
                    GUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    
                    // Vertical Separator stripe
                    GUILayout.Box("", GUILayout.Width(2f), GUILayout.ExpandHeight(true));
                    EditorGUILayout.Space(4f);
                    
                    // Right Column: Active Config Panel
                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                    {
                        DrawActiveConfigSubTab();
                    }
                }
            }
        }

        private void DrawConfigTabButton(int index, bool horizontal)
        {
            bool isActive = _selectedConfigSubTab == index;
            float height = horizontal ? 28f : 30f;
            Rect rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            
            bool isHover = rect.Contains(Event.current.mousePosition);
            Color bgColor = isActive 
                ? new Color(0.22f, 0.25f, 0.30f, 1f) 
                : (isHover ? new Color(0.18f, 0.20f, 0.24f, 0.8f) : Color.clear);
            
            if (bgColor != Color.clear)
            {
                EditorGUI.DrawRect(rect, bgColor);
            }
            
            if (isActive)
            {
                if (horizontal)
                {
                    Rect barRect = new(rect.x, rect.yMax - 3f, rect.width, 3f);
                    EditorGUI.DrawRect(barRect, EditorPaths.EditorColors.Info);
                }
                else
                {
                    Rect barRect = new(rect.x, rect.y, 4f, rect.height);
                    EditorGUI.DrawRect(barRect, EditorPaths.EditorColors.Info);
                }
            }
            
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = horizontal ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft,
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                fontSize = 10,
                padding = horizontal ? new RectOffset(0, 0, 0, 0) : new RectOffset(isActive ? 12 : 8, 4, 0, 0)
            };
            style.normal.textColor = isActive ? Color.white : EditorPaths.EditorColors.MutedText;
            
            GUI.Label(rect, _configSubTabs[index], style);
            
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedConfigSubTab = index;
                _scroll = Vector2.zero;
                if (_cachedAssetEditor != null)
                {
                    DestroyImmediate(_cachedAssetEditor);
                    _cachedAssetEditor = null;
                    _cachedAssetObj = null;
                }
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        private void DrawActiveConfigSubTab()
        {
            switch (_selectedConfigSubTab)
            {
                case 0:
                    EditorGUILayout.LabelField("PlayMode Çalışma Modları", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4f);
                    _runtime.OnGUI();
                    break;
                case 1:
                    EditorGUILayout.LabelField("Reklam ve Ödüllendirme Test Cihazı", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4f);
                    _adTester.OnGUI();
                    break;
                case 2:
                    EditorGUILayout.LabelField("Genel Ayarlar", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4f);
                    _settings.OnGUI();
                    break;
                case 3:
                    EditorGUILayout.LabelField("Analiz ve Sinyal İnceleyici", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4f);
                    _diagnostics.OnGUI();
                    break;
                case 4:
                    DrawConfigAssetInspector(EditorPaths.GameFeelConfigKey, "Oyun Hissiyatı (Game Feel)");
                    break;
                case 5:
                    DrawConfigAssetInspector(EditorPaths.GameConfigDatabaseKey, "Oyun Veritabanı (GameConfigDatabase)");
                    break;
                case 6:
                    DrawConfigAssetInspector(EditorPaths.RingMechanicDataKey, "Halka Mekanik Verisi (RingMechanicData)");
                    break;
                case 7:
                    DrawConfigAssetInspector(EditorPaths.AudioConfigKey, "Ses Yapılandırması (AudioConfig)");
                    break;
                case 8:
                    DrawConfigAssetInspector(EditorPaths.UIThemeConfigKey, "Arayüz Teması (UIThemeConfig)");
                    break;
                case 9:
                    DrawConfigAssetInspector(EditorPaths.StoreCatalogKey, "Mağaza Kataloğu (StoreCatalog)");
                    break;
                case 10:
                    DrawConfigAssetInspector(EditorPaths.ThemeSkinDatabaseKey, "Tema/Skin Veritabanı (ThemeSkinDatabase)");
                    break;
                case 11:
                    DrawConfigAssetInspector(EditorPaths.LocalizationConfigKey, "Yerelleştirme (LocalizationConfig)");
                    break;
                case 12:
                    DrawConfigAssetInspector(EditorPaths.ScreenRegistryKey, "Ekran Kayıt Defteri (Screen Registry)");
                    break;
            }
        }

        private void DrawConfigAssetInspector(string key, string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            // Only load from Resources on tab switch (key change), not every frame
            if (_cachedAssetObj == null || _cachedAssetKey != key)
            {
                if (_cachedAssetEditor != null)
                {
                    DestroyImmediate(_cachedAssetEditor);
                    _cachedAssetEditor = null;
                }
                _cachedAssetKey = key;
                _cachedAssetObj = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<ScriptableObject>(key)
                    .GetAwaiter().GetResult();
            }

            if (_cachedAssetObj == null)
            {
                EditorGUILayout.HelpBox($"{title} bulunamadı! Önce proje klasöründe oluşturulduğundan emin olun.", MessageType.Warning);
                return;
            }

            if (_cachedAssetEditor == null || _cachedAssetEditor.target != _cachedAssetObj)
            {
                if (_cachedAssetEditor != null)
                    DestroyImmediate(_cachedAssetEditor);
                _cachedAssetEditor = UnityEditor.Editor.CreateEditor(_cachedAssetObj);
            }

            if (_cachedAssetEditor != null)
            {
                _cachedAssetEditor.OnInspectorGUI();
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Data Tab
        // ──────────────────────────────────────────────────────────────

        private void DrawDataTab()
        {
            _dataOverview.OnGUI();
        }
    }
}
