using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    /// <summary>
    /// Abstract base for EditorWindow sections. Each section owns its foldout
    /// state, GUI layout, and per-section actions. The window delegates OnGUI
    /// to each section in order; sections can return early if folded.
    /// </summary>
    public abstract class EditorSection
    {
        protected const float HeaderHeight = 22f;
        protected const float ButtonHeight = 32f;
        protected const float RowHeight = 26f;

        public abstract string DisplayName { get; }
        public abstract string PrefKey { get; }
        public abstract void OnGUI();

        protected bool IsFoldedOut
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        protected void DrawFoldoutHeader()
        {
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f, 0.3f);
            IsFoldedOut = EditorGUILayout.Foldout(IsFoldedOut, DisplayName, true, EditorStyles.foldoutHeader);
            GUI.backgroundColor = bg;
            EditorGUILayout.Space(2f);
        }
    }
}
