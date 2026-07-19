using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Shared read-only pole/ring renderer used by the Generator section
    /// preview and the standalone LevelDataSO custom inspector. Single source
    /// of truth for "what does a level look like in the inspector" so the
    /// two surfaces cannot drift apart.
    /// </summary>
    internal static class LevelVisualRenderer
    {
        private const float PoleWidth = 60f;
        private const float RingHeight = 18f;
        private const float PoleGap = 8f;
        private const float PoleVerticalPadding = 12f;

        private static readonly Color PoleBackground = new(0.16f, 0.16f, 0.18f, 1f);
        private static readonly Color PoleBackgroundLocked = new(0.18f, 0.12f, 0.12f, 1f);
        private static readonly Color PoleBorder = new(0.35f, 0.35f, 0.38f);
        private static readonly Color PoleBorderLocked = new(0.8f, 0.2f, 0.2f);
        private static readonly Color LockLabelBackground = new(0.8f, 0.1f, 0.1f, 0.9f);
        private static readonly Color PortalBadgeBackground = new(0.0f, 0.6f, 0.8f, 0.9f);
        private static readonly Color PortalPoleBorder = new(0.0f, 0.6f, 0.8f);

        public static void Draw(LevelData levelData)
        {
            if (levelData == null || levelData.Poles == null || levelData.Poles.Count == 0)
            {
                EditorGUILayout.HelpBox("Gösterilecek seviye verisi yok.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Seviye Önizleme Görseli:", EditorStyles.boldLabel);

            int columns = RingFlowEditorUtils.GetResponsiveColumns(PoleWidth + PoleGap, 2, 8);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int p = 0;
                while (p < levelData.Poles.Count)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int col = 0; col < columns && p < levelData.Poles.Count; col++)
                        {
                            DrawPole(levelData.Poles[p], p);
                            if (col < columns - 1 && p < levelData.Poles.Count - 1)
                                GUILayout.Space(PoleGap);
                            p++;
                        }
                        GUILayout.FlexibleSpace();
                    }
                    if (p < levelData.Poles.Count)
                        EditorGUILayout.Space(6f);
                }
            }
            EditorGUILayout.Space(5f);
        }

        private static void DrawPole(PoleData pole, int index)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(PoleWidth)))
            {
                EditorGUILayout.LabelField(
                    $"Direk {index}",
                    RingFlowEditorUtils.CenteredMiniLabel,
                    GUILayout.Width(PoleWidth));

                int poleMaxCap = Mathf.Max(pole.RingCapacity, pole.Rings?.Count ?? 0);
                float height = poleMaxCap * (RingHeight + 2f) + PoleVerticalPadding;
                Rect rect = GUILayoutUtility.GetRect(PoleWidth, height);

                bool locked = pole.IsLocked;
                Color bg = locked ? PoleBackgroundLocked : PoleBackground;
                Color border = locked ? PoleBorderLocked : PoleBorder;

                EditorGUI.DrawRect(rect, bg);
                RingFlowEditorUtils.DrawRectBorder(rect, border, 1);

                if (pole.Rings != null)
                {
                    for (int r = 0; r < pole.Rings.Count; r++)
                    {
                        DrawRing(rect, r, pole.Rings[r]);
                    }
                }

                if (locked)
                {
                    Rect lockRect = new(rect.x + 3f, rect.y + 4f, PoleWidth - 6f, 13f);
                    EditorGUI.DrawRect(lockRect, LockLabelBackground);
                    var lockStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(lockRect, "KİLİTLİ", lockStyle);
                }

                if (pole.PortalTargetId >= 0)
                {
                    Rect portalRect = new(rect.x + 3f, rect.yMax - 16f, PoleWidth - 6f, 13f);
                    EditorGUI.DrawRect(portalRect, PortalBadgeBackground);
                    var portalStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(portalRect, $"PORTAL → {pole.PortalTargetId}", portalStyle);
                }
            }
        }

        private static void DrawRing(Rect poleRect, int ringIndex, RingData ring)
        {
            float ringY = poleRect.yMax - 6f - (ringIndex + 1) * (RingHeight + 2f);
            Rect ringRect = new(poleRect.x + 4f, ringY, PoleWidth - 8f, RingHeight);

            var palette = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)
                    .GetAwaiter().GetResult();
            Color ringColor = palette != null ? palette.GetColor(ring.Color, RingColorPaletteSO.ColorBlindMode.Off) : Color.grey;
            
            Color borderColor = Color.black;
            string ringLabel = RingFlowEditorUtils.GetRingShortLabel(ring.Type);
            Color labelColor = RingFlowEditorUtils.GetContrastColor(ringColor);

            if (ring.Type == RingType.Mystery)
            {
                ringColor = new Color(0.24f, 0.25f, 0.28f);
                borderColor = new Color(0.45f, 0.45f, 0.50f);
                ringLabel = "?";
                labelColor = Color.white;
            }
            else if (ring.Type == RingType.Frozen)
            {
                borderColor = new Color(0.4f, 0.8f, 1.0f);
                ringLabel = "❄️" + ringLabel;
            }
            else if (ring.Type == RingType.Locked)
            {
                borderColor = new Color(0.9f, 0.7f, 0.1f);
                ringLabel = "🔒" + ringLabel;
            }
            else if (ring.Type == RingType.Ghost)
            {
                ringColor.a = 0.5f;
                borderColor = new Color(0.8f, 0.8f, 0.9f, 0.5f);
                ringLabel = "👻" + ringLabel;
            }
            else if (ring.Type == RingType.Bomb)
            {
                borderColor = new Color(0.85f, 0.2f, 0.2f);
                if (ring.AdditionalData > 0)
                    ringLabel = $"💣{ring.AdditionalData}";
            }
            else if (ring.Type == RingType.Rainbow)
            {
                borderColor = new Color(0.9f, 0.4f, 0.8f);
                ringLabel = "🌈" + ringLabel;
            }
            else if (ring.Type == RingType.Chain)
            {
                borderColor = new Color(0.6f, 0.6f, 0.65f);
                ringLabel = "🔗" + ringLabel;
            }
            else if (ring.Type == RingType.Magnet)
            {
                borderColor = new Color(0.8f, 0.3f, 0.3f);
                ringLabel = "🧲" + ringLabel;
            }
            else if (ring.Type == RingType.Paint)
            {
                borderColor = new Color(0.2f, 0.75f, 0.2f);
                ringLabel = "🎨" + ringLabel;
            }

            EditorGUI.DrawRect(ringRect, ringColor);
            RingFlowEditorUtils.DrawRectBorder(ringRect, borderColor, 1);

            var textStyle = new GUIStyle(RingFlowEditorUtils.CenteredMiniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = labelColor }
            };
            GUI.Label(ringRect, ringLabel, textStyle);
        }
    }
}
