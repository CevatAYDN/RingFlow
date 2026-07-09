using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class VisualBuilderSection : EditorSection
    {
        private const float PoleSpacing = 2.5f;

        private GeneratorSection _generator;

        public override string DisplayName => "Scene Visual Board Builder";
        public override string PrefKey => EditorPrefsKeys.FoldBuilder;

        public VisualBuilderSection(GeneratorSection generator) { _generator = generator; }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Spawns Cylinder primitives as poles and uses Torus.obj models as rings in the active scene.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Build Board in Scene", GUILayout.Height(36)))
                    {
                        BuildInScene();
                    }
                    if (GUILayout.Button("Clear Scene Board", GUILayout.Height(36)))
                    {
                        ClearScene();
                    }
                }
            }
        }

        public void BuildFromDashboard()
        {
            BuildInScene();
        }

        private void BuildInScene()
        {
            List<PoleState> polesToBuild = null;
            if (Application.isPlaying)
            {
                var context = NexusRuntime.CurrentContext;
                var model = context?.TryResolve<GameplayModel>();
                if (model != null && model.Poles.Count > 0)
                {
                    polesToBuild = model.Poles;
                }
            }

            if (polesToBuild == null && _generator.GeneratedLevel == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "Please generate a level first OR enter PlayMode to load from active game!", "OK");
                return;
            }

            int poleCount = polesToBuild != null ? polesToBuild.Count : _generator.GeneratedLevel.Poles.Count;
            NexusLog.Info("RingFlowEditor", nameof(BuildInScene), "", $"Building visual board with {poleCount} poles.");

            ClearScene();

            var boardRoot = new GameObject("RingFlow_VisualBoard");
            Undo.RegisterCreatedObjectUndo(boardRoot, "Build Visual Board");

            var torusModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Torus.obj");
            if (torusModel == null)
            {
                NexusLog.Warn("RingFlowEditor", nameof(BuildInScene), "",
                    "Torus.obj not found in Assets/Resources. Using Cylinder disks as fallback rings.");
            }

            for (int p = 0; p < poleCount; p++)
            {
                bool isLocked = false;
                List<RingData> rings = new();

                if (polesToBuild != null)
                {
                    isLocked = polesToBuild[p].IsLocked;
                    rings = polesToBuild[p].Rings;
                }
                else
                {
                    isLocked = _generator.GeneratedLevel.Poles[p].IsLocked;
                    rings = _generator.GeneratedLevel.Poles[p].Rings;
                }

                CreatePole(boardRoot.transform, p, isLocked, rings, torusModel);
            }

            EditorApplication.delayCall += () =>
            {
                Selection.activeGameObject = boardRoot;
                SceneView.FrameLastActiveSceneView();
            };
            NexusLog.Info("RingFlowEditor", nameof(BuildInScene), "", "Visual board built successfully.");
        }

        private void CreatePole(Transform parent, int index, bool isLocked, List<RingData> rings, GameObject torusModel)
        {
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = $"Pole_{index}" + (isLocked ? " [LOCKED]" : "");
            poleObj.transform.SetParent(parent);
            poleObj.transform.position = new Vector3(index * PoleSpacing, 2.0f, 0f);
            poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

            var poleView = poleObj.AddComponent<PoleView>();
            poleView.PoleId = index;

            var capsule = poleObj.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                Object.DestroyImmediate(capsule);
            }
            var box = poleObj.AddComponent<BoxCollider>();
            box.size = new Vector3(3.0f, 2.0f, 3.0f);

            if (isLocked)
            {
                var renderer = poleObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var poleMat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                        ?? Shader.Find("Standard"));
                    poleMat.color = Color.black;
                    renderer.sharedMaterial = poleMat;
                }
            }

            for (int r = 0; r < rings.Count; r++)
            {
                CreateRing(poleObj.transform, r, rings[r], torusModel);
            }
        }

        private static void CreateRing(Transform parent, int index, RingData ringData, GameObject torusModel)
        {
            GameObject ringObj;
            if (torusModel != null)
            {
                ringObj = Object.Instantiate(torusModel);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, -0.9f + (index * 0.4f), 0f);
                ringObj.transform.localRotation = Quaternion.identity;
                ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
            }
            else
            {
                ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, -0.9f + (index * 0.4f), 0f);
                ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);
            }
            ringObj.name = $"Ring_{index}_{ringData.Color}_{ringData.Type}";

            var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
            if (ringRenderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                    ?? Shader.Find("Standard"));
                mat.color = RingPalette.Get(ringData.Color);
                ringRenderer.sharedMaterial = mat;
            }

            var col = ringObj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        private static void ClearScene()
        {
            var boardRoot = GameObject.Find("RingFlow_VisualBoard");
            if (boardRoot != null) Undo.DestroyObjectImmediate(boardRoot);
        }
    }
}
