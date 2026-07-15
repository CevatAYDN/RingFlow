using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    public sealed class GameFeelSection : EditorSection
    {
        public override string DisplayName => "Game Feel & Camera Config";
        public override string PrefKey => EditorPrefsKeys.FoldGameFeel;

        private Gameplay.GameFeelConfigSO _config;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            if (_config == null)
                _config = AssetDatabase.LoadAssetAtPath<Gameplay.GameFeelConfigSO>(
                    EditorPaths.GameFeelConfigPath);

            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "GameFeelConfig asset dosyası bulunamadı.\nAssets → Create → RingFlow → Game Feel Config menüsünden oluşturup Assets/Resources/ klasörüne yerleştirin.",
                    MessageType.Warning);

                if (GUILayout.Button("GameFeelConfig Dosyası Oluştur", GUILayout.Height(36)))
                    CreateConfigAsset();
                return;
            }

            var serialized = new SerializedObject(_config);
            EditorGUI.BeginChangeCheck();

            RingFlowEditorUtils.BeginSectionBox("Tahta Düzeni (Board Layout)");
            DrawProp(serialized, "PoleSpacing");
            DrawProp(serialized, "PoleYPosition");
            DrawProp(serialized, "PoleScale");
            DrawProp(serialized, "PoleColliderWidthFraction");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Halka Boyutlandırması (Ring Sizing)");
            DrawProp(serialized, "RingScaleTorus");
            DrawProp(serialized, "RingScaleFallback");
            DrawProp(serialized, "RingBaseYOffset");
            DrawProp(serialized, "RingStackSpacing");
            DrawProp(serialized, "RingSelectionLift");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Hareket Animasyonu (Move Animation)");
            DrawProp(serialized, "MoveDuration");
            DrawProp(serialized, "MoveJumpPower");
            DrawProp(serialized, "RingPlacePulseScale");
            DrawProp(serialized, "RingPlacePulseDuration");
            DrawProp(serialized, "SelectionLiftDuration");
            DrawProp(serialized, "SlowModeMultiplier");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Kamera Ayarları (Camera)");
            DrawProp(serialized, "CameraBaseOrtho");
            DrawProp(serialized, "CameraMaxOrtho");
            DrawProp(serialized, "CameraBasePoles");
            DrawProp(serialized, "CameraMaxPoles");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Kamera Sarsıntısı (Camera Shake)");
            DrawProp(serialized, "ShakeErrorIntensity");
            DrawProp(serialized, "ShakeErrorDuration");
            DrawProp(serialized, "ShakeExplosionIntensity");
            DrawProp(serialized, "ShakeExplosionDuration");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Direk Renkleri (Pole Colors)");
            DrawProp(serialized, "SelectedTint");
            DrawProp(serialized, "ErrorTint");
            DrawProp(serialized, "LockedTint");
            DrawProp(serialized, "ErrorFlashDuration");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Gökkuşağı Efekti (Rainbow Cycle)");
            DrawProp(serialized, "RainbowHueSpeed");
            DrawProp(serialized, "RainbowSaturation");
            DrawProp(serialized, "RainbowValue");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Efektler (VFX)");
            DrawProp(serialized, "RingPopCount");
            DrawProp(serialized, "RingPopDuration");
            DrawProp(serialized, "RingPopDespawnDelay");
            DrawProp(serialized, "ConfettiCount");
            DrawProp(serialized, "ConfettiFallDuration");
            DrawProp(serialized, "ConfettiDespawnDelay");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Zemin Görselleri (Floor & Ground)");
            DrawProp(serialized, "FloorMesh");
            DrawProp(serialized, "FloorColor");
            DrawProp(serialized, "FloorMetallic");
            DrawProp(serialized, "FloorSmoothness");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Model Örgüleri (Meshes)");
            DrawProp(serialized, "RingMesh");
            DrawProp(serialized, "PoleBodyMesh");
            DrawProp(serialized, "PoleCapMesh");
            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Bellek Havuzları (Object Pools)");
            DrawProp(serialized, "RingPoolSize");
            DrawProp(serialized, "RingPopPoolSize");
            DrawProp(serialized, "ConfettiPoolSize");
            RingFlowEditorUtils.EndSectionBox();

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
            AssetDatabase.CreateAsset(config, EditorPaths.GameFeelConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success",
                $"GameFeelConfig asset created at {EditorPaths.GameFeelConfigPath}", "OK");
        }
    }
}
