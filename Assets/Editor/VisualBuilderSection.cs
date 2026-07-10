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

        private static Shader s_cachedShader;
        private static Material s_cachedDefaultMaterial;

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
                        BuildInScene();

                    if (GUILayout.Button("Clear Scene Board", GUILayout.Height(36)))
                        ClearScene();
                }
            }
        }

        public void BuildFromDashboard()
        {
            BuildInScene();
        }

        private static Shader ResolveShader()
        {
            if (s_cachedShader != null) return s_cachedShader;
            s_cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                          ?? Shader.Find("Standard");
            return s_cachedShader;
        }

        private static Material GetDefaultMaterial() => s_cachedDefaultMaterial ??= new Material(ResolveShader());

        private void BuildInScene()
        {
            List<PoleState> polesToBuild = null;
            if (Application.isPlaying)
            {
                var context = NexusRuntime.CurrentContext;
                var model = context?.TryResolve<GameplayModel>();
                if (model != null && model.Poles.Count > 0)
                    polesToBuild = model.Poles;
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
                NexusLog.Warn("RingFlowEditor", nameof(BuildInScene), "",
                    "Torus.obj not found in Assets/Resources. Using Cylinder disks as fallback rings.");

            var f = GameFeelConfigSO.Instance;
            float spacing = f != null ? f.PoleSpacing : 2.5f;
            float boardWidth = (poleCount - 1) * spacing;
            float startX = -boardWidth * 0.5f;

            for (int p = 0; p < poleCount; p++)
            {
                bool isLocked;
                List<RingData> rings;

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

                CreatePole(boardRoot.transform, p, startX, spacing, isLocked, rings, torusModel, f);
            }

            EditorApplication.delayCall += () =>
            {
                Selection.activeGameObject = boardRoot;
                SceneView.FrameLastActiveSceneView();
            };
            NexusLog.Info("RingFlowEditor", nameof(BuildInScene), "", "Visual board built successfully.");
        }

        private static void CreatePole(Transform parent, int index, float startX, float spacing, bool isLocked, List<RingData> rings, GameObject torusModel, GameFeelConfigSO f)
        {
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = $"Pole_{index}" + (isLocked ? " [LOCKED]" : "");
            poleObj.transform.SetParent(parent);
            float poleY = f != null ? f.PoleYPosition : 2.0f;
            poleObj.transform.position = new Vector3(startX + index * spacing, poleY, 0f);
            poleObj.transform.localScale = f != null ? f.PoleScale : new Vector3(0.2f, 2.0f, 0.2f);

            var poleView = poleObj.AddComponent<PoleView>();
            poleView.PoleId = index;

            var capsule = poleObj.GetComponent<CapsuleCollider>();
            if (capsule != null)
                Object.DestroyImmediate(capsule);

            var box = poleObj.AddComponent<BoxCollider>();
            float colWidth = f != null ? (spacing * f.PoleColliderWidthFraction) : 2.125f;
            box.size = new Vector3(colWidth, 3.0f, 2.0f);

            var renderer = poleObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = ResolveShader();
                Material poleMat;
                if (isLocked)
                {
                    var darkColor = new Color(0.12f, 0.12f, 0.14f);
                    poleMat = new Material(shader) { color = darkColor, name = "PoleMat_Locked" };
                    if (poleMat.HasProperty("_BaseColor"))
                        poleMat.SetColor("_BaseColor", darkColor);
                    poleMat.SetFloat("_Metallic", 0.9f);
                    poleMat.SetFloat("_Smoothness", 0.9f);
                }
                else
                {
                    var slateColor = new Color(0.20f, 0.22f, 0.25f);
                    poleMat = new Material(shader) { color = slateColor, name = "PoleMat_Open" };
                    if (poleMat.HasProperty("_BaseColor"))
                        poleMat.SetColor("_BaseColor", slateColor);
                    poleMat.SetFloat("_Metallic", 0.8f);
                    poleMat.SetFloat("_Smoothness", 0.8f);
                }
                renderer.sharedMaterial = poleMat;
            }

            var shaderForRings = ResolveShader();
            for (int r = 0; r < rings.Count; r++)
                CreateRing(poleObj.transform, r, rings[r], torusModel, shaderForRings, f);
        }

        private static void CreateRing(Transform parent, int index, RingData ringData, GameObject torusModel, Shader shader, GameFeelConfigSO f)
        {
            GameObject ringObj;
            float ringBaseY = f != null ? f.RingBaseYOffset : -0.9f;
            float ringSpacing = f != null ? f.RingStackSpacing : 0.4f;

            if (torusModel != null)
            {
                ringObj = Object.Instantiate(torusModel);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, ringBaseY + (index * ringSpacing), 0f);
                ringObj.transform.localRotation = Quaternion.identity;
                ringObj.transform.localScale = f != null ? f.RingScaleTorus : new Vector3(3.5f, 0.2f, 3.5f);
            }
            else
            {
                ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, ringBaseY + (index * ringSpacing), 0f);
                ringObj.transform.localScale = f != null ? f.RingScaleFallback : new Vector3(4.0f, 0.08f, 4.0f);
            }

            ringObj.name = $"Ring_{index}_{ringData.Color}_{ringData.Type}";

            var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
            if (ringRenderer != null)
            {
                var mat = new Material(shader);
                Color baseColor = RingPalette.Get(ringData.Color);
                mat.color = baseColor;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.85f);
                
                switch (ringData.Type)
                {
                    case RingType.Frozen:
                        mat.color = Color.Lerp(baseColor, Color.cyan, 0.5f);
                        mat.SetFloat("_Metallic", 0.1f); mat.SetFloat("_Smoothness", 0.9f); break;
                    case RingType.Key: case RingType.Locked:
                        mat.color = new Color(1f, 0.84f, 0f);
                        mat.SetFloat("_Metallic", 0.8f); mat.SetFloat("_Smoothness", 0.6f); break;
                    case RingType.Stone:
                        mat.color = new Color(0.4f, 0.38f, 0.35f);
                        mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.1f); break;
                    case RingType.Glass:
                        mat.color = new Color(1f, 1f, 1f, 0.25f);
                        mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.95f); break;
                    case RingType.Ghost:
                        mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.15f);
                        mat.SetFloat("_Metallic", 0.3f); mat.SetFloat("_Smoothness", 0.3f); break;
                }
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", mat.color);

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
