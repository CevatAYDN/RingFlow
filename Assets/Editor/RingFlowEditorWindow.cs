using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// EditorWindow shell that composes <see cref="EditorSection"/>s into a single
    /// scroll view. Sections are stateless from the window's perspective — they
    /// manage their own foldout, EditorPrefs keys, and per-section actions.
    /// Bootstrapper logic lives in <see cref="EditorBootstrapper"/>.
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
        private string[] _tabs = { "Level Design", "Runtime & Player", "System & Config", "Project Setup", "Database Editor" };
        private int _selectedTab = 0;

        [MenuItem("Ring Flow/Dashboard &G", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("RingFlow Dashboard");
            window.minSize = new Vector2(500, 700);
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
            _generator    = new GeneratorSection();
            _visualBuilder = new VisualBuilderSection(_generator);
            _runtime      = new RuntimeSection();
            _settings     = new SettingsSection();
            _adTester     = new AdTesterSection();
            _diagnostics  = new DiagnosticsSection();
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
        }

        private void OnDisable()
        {
            _generator?.OnDisable();
        }

        private void OnGUI()
        {
            DrawHeader("RING FLOW — DASHBOARD");

            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs, GUILayout.Height(30));
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_sections == null) { EditorGUILayout.EndScrollView(); return; }

            switch (_selectedTab)
            {
                case 0: // Level Design
                    EditorGUILayout.LabelField("Level Generator & Solver", EditorStyles.boldLabel);
                    _generator.OnGUI();
                    EditorGUILayout.Space(15f);
                    EditorGUILayout.LabelField("Scene Board Builder", EditorStyles.boldLabel);
                    _visualBuilder.OnGUI();
                    break;

                case 1: // Runtime & Player
                    EditorGUILayout.LabelField("PlayMode Lifecycle & States", EditorStyles.boldLabel);
                    _runtime.OnGUI();
                    EditorGUILayout.Space(15f);
                    EditorGUILayout.LabelField("Ad & Reward Tester", EditorStyles.boldLabel);
                    _adTester.OnGUI();
                    break;

                case 2: // System & Config
                    EditorGUILayout.LabelField("Settings & Configurations", EditorStyles.boldLabel);
                    _settings.OnGUI();
                    EditorGUILayout.Space(15f);
                    EditorGUILayout.LabelField("Diagnostics & Signals", EditorStyles.boldLabel);
                    _diagnostics.OnGUI();
                    break;

                case 3: // Project Setup
                    DrawSetupTab();
                    break;

                case 4: // Database Editor
                    EditorGUILayout.LabelField("Game Balance & Configurations", EditorStyles.boldLabel);
                    _databaseSection.OnGUI();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSetupTab()
        {
            EditorGUILayout.LabelField("Project Setup Utilities", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Use these tools to initialize a test scene with the required Nexus components, " +
                    "or create a fresh working scene prepopulated with game lifecycle systems.",
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
