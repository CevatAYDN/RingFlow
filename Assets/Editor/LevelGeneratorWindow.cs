using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    /// <summary>
    /// Standalone Level Generator window available under Game Editor Tools / Level Generator.
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        private GeneratorSection _generator;
        private Vector2 _scroll;

        [MenuItem("Game Editor Tools/Level Generator", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelGeneratorWindow>("Level Generator");
            window.minSize = new Vector2(480, 520);
            window.Show();
        }

        private void OnEnable()
        {
            _generator = new GeneratorSection();
            _generator.HideHeader = true; // Hide the accordion foldout header inside the window
            _generator.OnEnable();
        }

        private void OnDisable()
        {
            _generator?.OnDisable();
        }

        private void OnGUI()
        {
            // Draw premium header
            DrawHeader("LEVEL GENERATOR & AI SOLVER");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                _generator.OnGUI();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader(string title)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 0.8f, 1.0f) }
            };
            
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            GUILayout.Box(title, style, GUILayout.ExpandWidth(true), GUILayout.Height(30));
            GUI.backgroundColor = bg;
            
            EditorGUILayout.Space(2f);
        }
    }
}
