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
            "Yerelleştirme Ayarları"
        };

        private UnityEditor.Editor _cachedAssetEditor;
        private ScriptableObject _cachedAssetObj;

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

            if (_cachedAssetEditor != null)
            {
                DestroyImmediate(_cachedAssetEditor);
                _cachedAssetEditor = null;
            }
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

            using (new EditorGUILayout.HorizontalScope())
            {
                // ── Left Sidebar Navigation ──
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(180f), GUILayout.ExpandHeight(true)))
                {
                    var prevColor = GUI.color;
                    GUI.color = EditorPaths.EditorColors.Info;
                    EditorGUILayout.LabelField("RINGFLOW", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Studio Dashboard", EditorStyles.miniLabel);
                    GUI.color = prevColor;

                    EditorGUILayout.Space(8f);

                    for (int i = 0; i < _tabs.Length; i++)
                    {
                        var prevBg = GUI.backgroundColor;
                        if (_selectedTab == i)
                        {
                            GUI.backgroundColor = EditorPaths.EditorColors.HeaderAccent;
                        }

                        if (GUILayout.Button(_tabs[i], GUILayout.Height(32f)))
                        {
                            _selectedTab = i;
                            EditorPrefs.SetInt(EditorPrefsKeys.SelectedTab, _selectedTab);
                            if (_cachedAssetEditor != null)
                            {
                                DestroyImmediate(_cachedAssetEditor);
                                _cachedAssetEditor = null;
                                _cachedAssetObj = null;
                            }
                            GUI.FocusControl(null);
                        }
                        GUI.backgroundColor = prevBg;
                        EditorGUILayout.Space(2f);
                    }

                    GUILayout.FlexibleSpace();

                    // Sidebar logs overview
                    DrawLogSummarySidebar();
                }

                // ── Vertical Line ──
                GUILayout.Box("", GUILayout.Width(2f), GUILayout.ExpandHeight(true));

                // ── Right Main Area ──
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    _scroll = EditorGUILayout.BeginScrollView(_scroll);

                    // Show active tab header
                    EditorGUILayout.LabelField(_tabs[_selectedTab].ToUpper(), RingFlowEditorUtils.HeaderStyle);
                    EditorGUILayout.Space(4f);

                    switch (_selectedTab)
                    {
                        case 0: DrawHomeTab(); break;
                        case 1: DrawLevelsTab(); break;
                        case 2: _uiStudio.DrawTab(); break;
                        case 3: DrawDataTab(); break;
                        case 4: DrawToolsTab(); break;
                    }

                    EditorGUILayout.EndScrollView();
                    DrawStatusBar();
                }
            }
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Konsol", EditorStyles.miniButton))
                        EditorApplication.ExecuteMenuItem("Window/General/Console");
                    if (GUILayout.Button("Temizle", EditorStyles.miniButton))
                        LogMonitor.Reset();
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Status Bar
        // ──────────────────────────────────────────────────────────────

        private void DrawFoldableSection(string foldKey, string title, System.Action drawContent)
            => RingFlowEditorUtils.FoldoutSection(foldKey, title, drawContent);

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
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
            DrawActionCards();
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
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
                EditorGUILayout.Space(4f);

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
                                if (index == 0) _selectedTab = 1;
                                else if (index == 1) _selectedTab = 2;
                                else if (index == 2) _selectedTab = 4; // Tools tab is 4
                                else if (index == 3) _generator.GenerateFromDashboard();
                                else if (index == 4) _visualBuilder.BuildFromDashboard();

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
            }
        }

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
            
            var subStyle = new GUIStyle(RingFlowEditorUtils.CenteredMiniLabel)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 9,
                normal = { textColor = Color.white }
            };
            EditorGUI.LabelField(r, subtitle, subStyle);

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
            DrawFoldableSection(EditorPrefsKeys.FoldGenerator, "Seviye Üretici & Çözücü Ayarları", _generator.OnGUI);
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            DrawFoldableSection(EditorPrefsKeys.FoldDatabase, "Seviye Denetleyici", _databaseSection.OnGUI);
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            DrawFoldableSection(EditorPrefsKeys.FoldLevelBrowser, "Seviye Tarayıcı (Level Browser)", _levelBrowser.OnGUI);
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            DrawFoldableSection(EditorPrefsKeys.FoldBuilder, "Sahne Tahtası Oluşturucu", _visualBuilder.OnGUI);
        }

        // ──────────────────────────────────────────────────────────────
        //  Tools Tab
        // ──────────────────────────────────────────────────────────────

        private void DrawToolsTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Left Sidebar
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(200f)))
                {
                    EditorGUILayout.LabelField("Kategoriler / Configler", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f);

                    for (int i = 0; i < _configSubTabs.Length; i++)
                    {
                        var prevBg = GUI.backgroundColor;
                        if (_selectedConfigSubTab == i)
                        {
                            GUI.backgroundColor = EditorPaths.EditorColors.HeaderAccent;
                        }

                        if (GUILayout.Button(_configSubTabs[i], GUILayout.Height(28f)))
                        {
                            _selectedConfigSubTab = i;
                            if (_cachedAssetEditor != null)
                            {
                                DestroyImmediate(_cachedAssetEditor);
                                _cachedAssetEditor = null;
                                _cachedAssetObj = null;
                            }
                            GUI.FocusControl(null);
                        }
                        GUI.backgroundColor = prevBg;
                    }
                }

                // Divider line
                GUILayout.Box("", GUILayout.Width(2f), GUILayout.ExpandHeight(true));

                // Right Panel Content
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    DrawActiveConfigSubTab();
                }
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
            }
        }

        private void DrawConfigAssetInspector(string key, string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            var obj = Resources.Load<ScriptableObject>(key);
            if (obj == null)
            {
                EditorGUILayout.HelpBox($"{title} bulunamadı! Önce proje klasöründe oluşturulduğundan emin olun.", MessageType.Warning);
                return;
            }

            if (_cachedAssetObj != obj)
            {
                if (_cachedAssetEditor != null)
                {
                    DestroyImmediate(_cachedAssetEditor);
                }
                _cachedAssetObj = obj;
                _cachedAssetEditor = UnityEditor.Editor.CreateEditor(obj);
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
