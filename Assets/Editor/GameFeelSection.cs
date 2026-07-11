using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    public sealed class GameFeelSection : EditorSection
    {
        public override string DisplayName => "Game Feel & Camera Config";
        public override string PrefKey => "RF_FoldGameFeel";

        private Gameplay.GameFeelConfigSO _config;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            if (_config == null)
                _config = AssetDatabase.LoadAssetAtPath<Gameplay.GameFeelConfigSO>(
                    "Assets/Resources/GameFeelConfig.asset");

            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "GameFeelConfig asset not found.\nCreate it from Assets → Create → RingFlow → Game Feel Config, then place it in Assets/Resources/.",
                    MessageType.Warning);

                if (GUILayout.Button("Create GameFeelConfig Asset", GUILayout.Height(36)))
                    CreateConfigAsset();
                return;
            }

            var serialized = new SerializedObject(_config);
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Board Layout", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "PoleSpacing");
                DrawProp(serialized, "PoleYPosition");
                DrawProp(serialized, "PoleScale");
                DrawProp(serialized, "PoleColliderWidthFraction");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Ring Sizing", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "RingScaleTorus");
                DrawProp(serialized, "RingScaleFallback");
                DrawProp(serialized, "RingBaseYOffset");
                DrawProp(serialized, "RingStackSpacing");
                DrawProp(serialized, "RingSelectionLift");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Move Animation", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "MoveDuration");
                DrawProp(serialized, "MoveJumpPower");
                DrawProp(serialized, "RingPlacePulseScale");
                DrawProp(serialized, "RingPlacePulseDuration");
                DrawProp(serialized, "SelectionLiftDuration");
                DrawProp(serialized, "SlowModeMultiplier");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "CameraBaseOrtho");
                DrawProp(serialized, "CameraMaxOrtho");
                DrawProp(serialized, "CameraBasePoles");
                DrawProp(serialized, "CameraMaxPoles");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Camera Shake", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "ShakeErrorIntensity");
                DrawProp(serialized, "ShakeErrorDuration");
                DrawProp(serialized, "ShakeExplosionIntensity");
                DrawProp(serialized, "ShakeExplosionDuration");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Pole Colors", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "SelectedTint");
                DrawProp(serialized, "ErrorTint");
                DrawProp(serialized, "LockedTint");
                DrawProp(serialized, "ErrorFlashDuration");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Rainbow Cycle", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "RainbowHueSpeed");
                DrawProp(serialized, "RainbowSaturation");
                DrawProp(serialized, "RainbowValue");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "RingPopCount");
                DrawProp(serialized, "RingPopDuration");
                DrawProp(serialized, "RingPopDespawnDelay");
                DrawProp(serialized, "ConfettiCount");
                DrawProp(serialized, "ConfettiFallDuration");
                DrawProp(serialized, "ConfettiDespawnDelay");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Floor/Ground Visuals", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "FloorMesh");
                DrawProp(serialized, "FloorColor");
                DrawProp(serialized, "FloorMetallic");
                DrawProp(serialized, "FloorSmoothness");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Model Meshes", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "RingMesh");
                DrawProp(serialized, "PoleBodyMesh");
                DrawProp(serialized, "PoleCapMesh");
            }

            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Object Pools", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawProp(serialized, "RingPoolSize");
                DrawProp(serialized, "RingPopPoolSize");
                DrawProp(serialized, "ConfettiPoolSize");
            }

            EditorGUILayout.Space(8f);

            if (EditorGUI.EndChangeCheck())
            {
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }
        }

        private static void DrawProp(SerializedObject so, string propName)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                EditorGUILayout.PropertyField(prop);
        }

        private static void CreateConfigAsset()
        {
            var config = ScriptableObject.CreateInstance<Gameplay.GameFeelConfigSO>();
            RingFlowEditorUtils.EnsureAssetFolders("Assets/Resources");
            AssetDatabase.CreateAsset(config, "Assets/Resources/GameFeelConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success",
                "GameFeelConfig asset created at Assets/Resources/GameFeelConfig.asset", "OK");
        }
    }
}
