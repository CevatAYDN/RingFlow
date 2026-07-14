using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(Gameplay.GameFeelConfigSO))]
    public sealed class GameFeelConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Game Feel & Kamera Yapılandırması", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            serializedObject.Update();

            // ── Board Layout ──
            RingFlowEditorUtils.SectionTitle("Board Layout");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Pole Spacing", "PoleSpacing", "Pole spacing on X axis (units).");
                DrawField("Pole Y Position", "PoleYPosition", "Pole Y position (height).");
                DrawField("Pole Scale", "PoleScale", "Pole visual scale.");
                DrawSlider("Pole Collider Width Fraction", "PoleColliderWidthFraction", 0f, 1f, "Collider width as fraction of pole spacing (0-1).");
            }

            EditorGUILayout.Space(6f);

            // ── Ring Sizing ──
            RingFlowEditorUtils.SectionTitle("Ring Sizing");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Ring Scale (Torus)", "RingScaleTorus", "Ring scale when Torus prefab is available.");
                DrawField("Ring Scale (Fallback)", "RingScaleFallback", "Ring scale fallback (cylinder).");
                DrawField("Ring Base Y Offset", "RingBaseYOffset", "Vertical offset of the first (bottom) ring from pole top.");
                DrawField("Ring Stack Spacing", "RingStackSpacing", "Vertical stacking distance between rings.");
                DrawField("Ring Selection Lift", "RingSelectionLift", "Y lift for the selected top ring.");
            }

            EditorGUILayout.Space(6f);

            // ── Move Animation ──
            RingFlowEditorUtils.SectionTitle("Move Animation");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSlider("Move Duration", "MoveDuration", 0.05f, 2.0f, "Base duration for ring move jump (seconds).");
                DrawSlider("Move Jump Power", "MoveJumpPower", 0f, 3.0f, "Jump arc power.");
                DrawSlider("Ring Place Pulse Scale", "RingPlacePulseScale", 1.0f, 2.0f, "Scale-pop multiplier on ring placement (1 = off).");
                DrawSlider("Ring Place Pulse Duration", "RingPlacePulseDuration", 0.05f, 1.0f, "Pulse return duration.");
                DrawSlider("Selection Lift Duration", "SelectionLiftDuration", 0.05f, 1.0f, "Duration for selection highlight lift.");
                DrawSlider("Slow Mode Multiplier", "SlowModeMultiplier", 1.0f, 5.0f, "Slow-mode speed multiplier.");
            }

            EditorGUILayout.Space(6f);

            // ── Camera ──
            RingFlowEditorUtils.SectionTitle("Camera");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Camera Position", "CameraPosition", "Camera position (world).");
                DrawField("Camera Rotation", "CameraRotation", "Camera rotation (euler).");
                DrawField("Camera Base Ortho", "CameraBaseOrtho", "Base orthographic size for 4 poles.");
                DrawField("Camera Max Ortho", "CameraMaxOrtho", "Max orthographic size for 10 poles.");
                DrawField("Camera Base Poles", "CameraBasePoles", "Reference pole count for base ortho.");
                DrawField("Camera Max Poles", "CameraMaxPoles", "Reference pole count for max ortho.");
            }

            EditorGUILayout.Space(6f);

            // ── Camera Shake ──
            RingFlowEditorUtils.SectionTitle("Camera Shake");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSlider("Shake Error Intensity", "ShakeErrorIntensity", 0f, 0.5f, "Shake intensity on error.");
                DrawSlider("Shake Error Duration", "ShakeErrorDuration", 0.05f, 1.0f, "Shake duration on error.");
                DrawSlider("Shake Explosion Intensity", "ShakeExplosionIntensity", 0f, 1.0f, "Shake intensity on bomb explosion.");
                DrawSlider("Shake Explosion Duration", "ShakeExplosionDuration", 0.05f, 2.0f, "Shake duration on bomb explosion.");
            }

            EditorGUILayout.Space(6f);

            // ── Pole Colors ──
            RingFlowEditorUtils.SectionTitle("Pole Colors");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Selected Tint", "SelectedTint", "Pole selection highlight color.");
                DrawField("Error Tint", "ErrorTint", "Pole error flash color.");
                DrawField("Locked Tint", "LockedTint", "Locked pole color.");
                DrawField("Error Flash Duration", "ErrorFlashDuration", "Error flash duration.");
                DrawField("Pole Color Open", "PoleColorOpen", "Open pole standard color.");
                DrawField("Pole Color Locked", "PoleColorLocked", "Locked pole standard color.");
                DrawSlider("Pole Metallic", "PoleMetallic", 0f, 1f, "Pole material metallic value.");
                DrawSlider("Pole Smoothness", "PoleSmoothness", 0f, 1f, "Pole material smoothness value.");
            }

            EditorGUILayout.Space(6f);

            // ── Ring Materials ──
            RingFlowEditorUtils.SectionTitle("Ring Materials");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSlider("Ring Metallic", "RingMetallic", 0f, 1f, "Ring material metallic value.");
                DrawSlider("Ring Smoothness", "RingSmoothness", 0f, 1f, "Ring material smoothness value.");
            }

            EditorGUILayout.Space(6f);

            // ── Selection Glow ──
            RingFlowEditorUtils.SectionTitle("Selection Glow");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Glow Color", "SelectionGlowColor", "Glow point light color.");
                DrawSlider("Glow Intensity", "SelectionGlowIntensity", 0f, 5f, "Glow point light intensity.");
                DrawSlider("Glow Range", "SelectionGlowRange", 0.5f, 10f, "Glow point light range.");
                DrawField("Emission Color", "SelectionEmissionColor", "Selected ring emission color.");
            }

            EditorGUILayout.Space(6f);

            // ── Tutorial Visuals ──
            RingFlowEditorUtils.SectionTitle("Tutorial Visuals");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Arrow Color", "TutorialArrowColor", "Tutorial arrow/cone tint color.");
                DrawField("Arrow Scale", "TutorialArrowScale", "Tutorial arrow/cone scale.");
                DrawField("Arrow Bob Height", "TutorialArrowBobHeight", "Tutorial arrow/cone bobbing height.");
                DrawField("Arrow Bob Speed", "TutorialArrowBobSpeed", "Tutorial arrow/cone bobbing speed.");
                DrawField("Arrow Rotation Speed", "TutorialArrowRotationSpeed", "Tutorial arrow/cone rotation speed.");
            }

            EditorGUILayout.Space(6f);

            // ── Rainbow Cycle ──
            RingFlowEditorUtils.SectionTitle("Rainbow Cycle");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSlider("Rainbow Hue Speed", "RainbowHueSpeed", 0.1f, 10f, "Hue rotation speed multiplier.");
                DrawSlider("Rainbow Saturation", "RainbowSaturation", 0f, 1f, "Rainbow saturation.");
                DrawSlider("Rainbow Value", "RainbowValue", 0f, 1f, "Rainbow value/brightness.");
            }

            EditorGUILayout.Space(6f);

            // ── VFX ──
            RingFlowEditorUtils.SectionTitle("VFX");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Ring Pop Count", "RingPopCount", "RingPop particle count.");
                DrawField("Ring Pop Duration", "RingPopDuration", "RingPop burst duration.");
                DrawField("Ring Pop Despawn Delay", "RingPopDespawnDelay", "RingPop auto-despawn delay.");
                DrawField("Confetti Count", "ConfettiCount", "Confetti piece count.");
                DrawField("Confetti Fall Duration", "ConfettiFallDuration", "Confetti fall duration range.");
                DrawField("Confetti Despawn Delay", "ConfettiDespawnDelay", "Confetti despawn delay.");
            }

            EditorGUILayout.Space(6f);

            // ── Pool Sizes ──
            RingFlowEditorUtils.SectionTitle("Object Pool Sizes");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawField("Ring Pool Size", "RingPoolSize", "Ring pool capacity.");
                DrawField("Ring Pop Pool Size", "RingPopPoolSize", "RingPop effect pool capacity.");
                DrawField("Confetti Pool Size", "ConfettiPoolSize", "Confetti effect pool capacity.");
            }

            EditorGUILayout.Space(8f);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawField(string label, string propertyName, string tooltip)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(RingFlowEditorUtils.GetResponsiveLabelWidth()));
                EditorGUILayout.PropertyField(prop, GUIContent.none);
            }
        }

        private void DrawSlider(string label, string propertyName, float min, float max, string tooltip)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(200f));
                EditorGUILayout.Slider(prop, min, max, GUIContent.none);
            }
        }
    }
}
