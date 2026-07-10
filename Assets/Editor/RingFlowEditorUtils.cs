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
                fixedHeight = 60,
            };

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
                RingType.Ghost => "GHS",
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
            if (!Application.isPlaying) return new Color(0.65f, 0.65f, 0.65f);
            return EditorApplication.isPaused
                ? new Color(1f, 0.8f, 0.2f)
                : new Color(0.3f, 0.85f, 0.3f);
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
