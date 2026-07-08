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
        private List<EditorSection> _sections;

        private Vector2 _scroll;

        [MenuItem("Game Editor Tools/Game Control Panel &G", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<RingFlowEditorWindow>("Control Panel");
            window.minSize = new Vector2(420, 680);
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

            _generator.OnEnable();

            _sections = new List<EditorSection>
            {
                _generator,
                _visualBuilder,
                _runtime,
                _settings,
                _adTester,
                _diagnostics,
            };
        }

        private void OnDisable()
        {
            _generator?.OnDisable();
        }

        private void OnGUI()
        {
            DrawHeader("RING FLOW — GAME CONTROL PANEL");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_sections == null) { EditorGUILayout.EndScrollView(); return; }

            for (int i = 0; i < _sections.Count; i++)
            {
                _sections[i]?.OnGUI();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
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
