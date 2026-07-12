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

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                for (int p = 0; p < levelData.Poles.Count; p++)
                {
                    DrawPole(levelData.Poles[p], p);
                    if (p < levelData.Poles.Count - 1)
                        GUILayout.Space(PoleGap);
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

            var palette = Resources.Load<RingColorPaletteSO>("RingColorPalette");
            Color ringColor = palette != null ? palette.GetColor(ring.Color, RingColorPaletteSO.ColorBlindMode.Off) : Color.grey;
            EditorGUI.DrawRect(ringRect, ringColor);
            RingFlowEditorUtils.DrawRectBorder(ringRect, Color.black, 1);

            string ringLabel = RingFlowEditorUtils.GetRingShortLabel(ring.Type);
            if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                ringLabel += ring.AdditionalData;

            var textStyle = new GUIStyle(RingFlowEditorUtils.CenteredMiniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = RingFlowEditorUtils.GetContrastColor(ringColor) }
            };
            GUI.Label(ringRect, ringLabel, textStyle);
        }
    }
}
