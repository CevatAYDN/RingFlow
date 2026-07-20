using UnityEngine;
using UnityEditor;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    internal static class RingFlowEditorUtils
    {
        private static GUIStyle s_cachedBoldMiniLabel;
        private static GUIStyle s_cachedWordWrappedMiniLabel;
        private static GUIStyle s_cachedCenteredMiniLabel;
        private static GUIStyle s_cachedCenteredBoldLabel;
        private static GUIStyle s_cachedHeaderStyle;
        private static GUIStyle s_cachedButtonStyle;
        private static GUIStyle s_cachedSectionBoxStyle;
        private static Texture2D s_headerTex;

        private static readonly Color HeaderColor = new(0.15f, 0.15f, 0.18f);
        private static readonly Color HeaderTextColor = new(0.2f, 0.8f, 1.0f);

        public static GUIStyle BoldMiniLabel =>
            s_cachedBoldMiniLabel ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };

        public static GUIStyle WordWrappedMiniLabel =>
            s_cachedWordWrappedMiniLabel ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        public static GUIStyle CenteredMiniLabel =>
            s_cachedCenteredMiniLabel ??= new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleCenter };

        public static GUIStyle CenteredBoldLabel =>
            s_cachedCenteredBoldLabel ??= new GUIStyle(EditorStyles.boldLabel)
                { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (s_cachedHeaderStyle == null)
                {
                    if (s_headerTex == null)
                    {
                        s_headerTex = new Texture2D(2, 2);
                        s_headerTex.SetPixels(new[] { HeaderColor, HeaderColor, HeaderColor, HeaderColor });
                        s_headerTex.Apply();
                    }
                    s_cachedHeaderStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        normal = { background = s_headerTex, textColor = HeaderTextColor }
                    };
                }
                return s_cachedHeaderStyle;
            }
        }

        public static GUIStyle CompactBoldButton =>
            s_cachedButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 10, 10),
                wordWrap = true
            };

        public static GUIStyle SectionBoxStyle =>
            s_cachedSectionBoxStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 4, 4)
            };
        private static float s_cachedLayoutWidth = -1f;

        public static void UpdateLayoutWidth()
        {
            if (s_cachedLayoutWidth < 0f || (Event.current != null && Event.current.type == EventType.Layout))
            {
                s_cachedLayoutWidth = EditorGUIUtility.currentViewWidth;
            }
        }

        public static float GetResponsiveWidth(float minWidth, float maxWidth, float fraction = 0.4f)
        {
            float w = s_cachedLayoutWidth > 0f ? s_cachedLayoutWidth : EditorGUIUtility.currentViewWidth;
            return Mathf.Clamp(w * fraction, minWidth, maxWidth);
        }

        public static float GetResponsiveLabelWidth(float minWidth = 120f, float maxWidth = 240f, float fraction = 0.38f)
        {
            return GetResponsiveWidth(minWidth, maxWidth, fraction);
        }

        public static bool IsNarrowWidth(float threshold = 520f)
        {
            float w = s_cachedLayoutWidth > 0f ? s_cachedLayoutWidth : EditorGUIUtility.currentViewWidth;
            return w < threshold;
        }

        public static int GetResponsiveColumns(float targetCellWidth, int minColumns = 1, int maxColumns = 6, float availableWidth = -1f)
        {
            float w = availableWidth > 0f ? availableWidth : (s_cachedLayoutWidth > 0f ? s_cachedLayoutWidth : EditorGUIUtility.currentViewWidth);
            float usableWidth = Mathf.Max(1f, w - 24f);
            int columns = Mathf.Max(minColumns, Mathf.FloorToInt(usableWidth / Mathf.Max(1f, targetCellWidth)));
            return Mathf.Clamp(columns, minColumns, maxColumns);
        }

        // -----------------------------------------------------------------
        //  Ring / Level helpers
        // -----------------------------------------------------------------

        public static string GetRingShortLabel(RingType type)
        {
            return type switch
            {
                RingType.Standard => "STD",
                RingType.Key => "KEY",
                RingType.Mystery => "MYS",
                RingType.Frozen => "FRZ",
                RingType.Locked => "LCK",
                RingType.Stone => "STN",
                RingType.Glass => "GLS",
                RingType.Rainbow => "RNB",
                RingType.Bomb => "BMB",
                RingType.Chain => "CHN",
                RingType.Magnet => "MAG",
                RingType.Paint => "PNT",
                _ => "???"
            };
        }

        public static Color GetContrastColor(Color color)
        {
            float y = (color.r * 299 + color.g * 587 + color.b * 114) / 1000f;
            return y >= 0.5f ? Color.black : Color.white;
        }

        public static void DrawRectBorder(Rect rect, Color color, int width)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
        }

        /// <summary>
        /// Uniform section title used by every SO custom editor so group
        /// headers read identically across Audio / GameFeel / Palette / Theme /
        /// Level inspectors. Draws a thin accent bar + bold label.
        /// </summary>
        public static void SectionTitle(string title)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = EditorPaths.EditorColors.Info;
                GUILayout.Box("", GUILayout.Width(3f), GUILayout.Height(16f));
                GUI.backgroundColor = prev;
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            }
        }

        public static void SectionHeader(string title, string subtitle = null)
        {
            using (new EditorGUILayout.VerticalScope(SectionBoxStyle))
            {
                SectionTitle(title);
                if (!string.IsNullOrEmpty(subtitle))
                    EditorGUILayout.LabelField(subtitle, WordWrappedMiniLabel);
            }
        }

        public static void BeginSectionBox(string title, string subtitle = null)
        {
            Rect rect = EditorGUILayout.BeginVertical(GUIStyle.none);
            
            if (Event.current.type == EventType.Repaint)
            {
                // Draw background and border
                EditorGUI.DrawRect(rect, new Color(0.18f, 0.20f, 0.23f, 1f));
                DrawRectBorder(rect, new Color(0.28f, 0.30f, 0.35f, 1f), 1);
                
                // Left blue vertical stripe
                Rect stripeRect = new Rect(rect.x, rect.y, 4f, rect.height);
                EditorGUI.DrawRect(stripeRect, EditorPaths.EditorColors.Info);
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            GUILayout.BeginVertical();
            GUILayout.Space(8f);
            
            if (!string.IsNullOrEmpty(title))
            {
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, normal = { textColor = Color.white } };
                EditorGUILayout.LabelField(title.ToUpper(), titleStyle);
                if (!string.IsNullOrEmpty(subtitle))
                {
                    var subStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = EditorPaths.EditorColors.MutedText } };
                    EditorGUILayout.LabelField(subtitle, subStyle);
                }
                EditorGUILayout.Space(4f);
            }
        }
        
        public static void EndSectionBox()
        {
            GUILayout.Space(8f);
            GUILayout.EndVertical();
            GUILayout.Space(12f);
            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }

        /// <summary>
        /// Exception-safe wrapper for <see cref="BeginSectionBox"/> / <see cref="EndSectionBox"/>.
        /// Use with <c>using</c> to guarantee <c>EndSectionBox()</c> is called even if
        /// the content throws, preventing GUILayout state corruption.
        /// </summary>
        public static SectionBoxScope BeginSectionBoxScope(string title, string subtitle = null)
        {
            BeginSectionBox(title, subtitle);
            return new SectionBoxScope();
        }

        /// <summary>
        /// Calls <see cref="EndSectionBox()"/> on dispose. Designed for use with
        /// <c>using (RingFlowEditorUtils.BeginSectionBoxScope(...))</c>.
        /// </summary>
        public readonly struct SectionBoxScope : System.IDisposable
        {
            public void Dispose()
            {
                EndSectionBox();
            }
        }

        // -----------------------------------------------------------------
        //  Foldout section (shared by dashboard + SO editors)
        // -----------------------------------------------------------------

        public static bool DrawHeaderFoldout(string title, bool expanded)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));
            
            bool isHover = rect.Contains(Event.current.mousePosition);
            Color bgColor = expanded 
                ? new Color(0.2f, 0.22f, 0.26f, 1f) 
                : (isHover ? new Color(0.18f, 0.20f, 0.24f, 0.8f) : new Color(0.14f, 0.16f, 0.18f, 1f));
            
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, bgColor);
                DrawRectBorder(rect, new Color(0.24f, 0.26f, 0.30f, 1f), 1);
                
                // Left indicator stripe
                Rect stripeRect = new Rect(rect.x, rect.y, 4f, rect.height);
                EditorGUI.DrawRect(stripeRect, expanded ? EditorPaths.EditorColors.Info : Color.gray);
            }
            
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                padding = new RectOffset(16, 4, 0, 0)
            };
            style.normal.textColor = expanded ? Color.white : EditorPaths.EditorColors.MutedText;
            
            GUI.Label(rect, (expanded ? "▼ " : "► ") + title.ToUpper(), style);
            
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                expanded = !expanded;
                GUI.FocusControl(null);
                Event.current.Use();
            }
            
            return expanded;
        }

        private static readonly System.Collections.Generic.Dictionary<string, bool> s_foldStates = new();

        /// <summary>
        /// Draws a persistent (EditorPrefs-backed) collapsible section. The
        /// fold state survives domain reloads and editor restarts, so large
        /// inspector/dashboard blocks stay collapsed between sessions.
        /// </summary>
        public static void FoldoutSection(string foldKey, string title, System.Action drawContent)
        {
            if (Event.current.type == EventType.Layout)
            {
                s_foldStates[foldKey] = EditorPrefs.GetBool(foldKey, true);
            }

            if (!s_foldStates.TryGetValue(foldKey, out bool expanded))
            {
                expanded = EditorPrefs.GetBool(foldKey, true);
                s_foldStates[foldKey] = expanded;
            }

            bool newExpanded = DrawHeaderFoldout(title, expanded);
            if (newExpanded != expanded)
            {
                EditorPrefs.SetBool(foldKey, newExpanded);
            }
            
            if (expanded)
            {
                EditorGUILayout.Space(6f);
                drawContent();
                EditorGUILayout.Space(12f);
            }
            else
            {
                EditorGUILayout.Space(2f); // minimal spacing when collapsed so accordion headers align beautifully!
            }
        }

        // -----------------------------------------------------------------
        //  Scene / Mode helpers
        // -----------------------------------------------------------------

        public static string GetEditorModeLabel()
        {
            if (!Application.isPlaying) return "EDIT";
            return EditorApplication.isPaused ? "PAUSED" : "PLAY";
        }

        public static Color GetEditorModeColor()
        {
            if (!Application.isPlaying) return EditorPaths.EditorColors.MutedText;
            return EditorApplication.isPaused
                ? EditorPaths.EditorColors.Warning
                : EditorPaths.EditorColors.Success;
        }

        // -----------------------------------------------------------------
        //  Folder helpers
        // -----------------------------------------------------------------

        public static void EnsureAssetFolders(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string parent = current;
                current += "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(current))
                    AssetDatabase.CreateFolder(parent, parts[i]);
            }
        }
    }
}
