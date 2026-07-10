using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    /// <summary>
    /// Builds the runtime scene graph: NexusRoot + GameplayLifecycle + UIRoot + EventSystem.
    /// Extracted from RingFlowEditorWindow so the window stays focused on UI rendering
    /// and the bootstrapper can be invoked from CI or other tools.
    /// </summary>
    public static class EditorBootstrapper
    {
        private const string ContextDataPath = "Assets/Settings/GameplayContextData.asset";

        public static BootstrapResult Bootstrap()
        {
            if (Application.isPlaying)
            {
                return BootstrapResult.Fail("Cannot run setup during PlayMode.");
            }

            if (Object.FindAnyObjectByType<Root>() != null)
            {
                return BootstrapResult.Fail("Nexus Bootstrapper already exists in the scene.");
            }

            var contextData = EnsureContextData();
            var rootObj = CreateRootWithContext(contextData);
            AttachComponents(rootObj);
            EnsureEventSystem();
            EnsureMainCamera();
            EnsureDirectionalLight();
            EnsureCameraRaycasters();
            MarkSceneDirty();

            NexusLog.Info("EditorBootstrapper", nameof(Bootstrap), "",
                "Nexus Bootstrapper successfully added to the active scene.");
            return BootstrapResult.Ok(rootObj);
        }

        public struct BootstrapResult
        {
            public bool Success;
            public string Message;
            public GameObject Root;

            public static BootstrapResult Ok(GameObject root) => new() { Success = true, Root = root };
            public static BootstrapResult Fail(string msg) => new() { Success = false, Message = msg };
        }

        private static ContextData EnsureContextData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ContextData>(ContextDataPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            var data = ScriptableObject.CreateInstance<ContextData>();
            data.AssemblyScopes = new[] { "RingFlow" };
            data.EnableAutoDiscovery = true;

            AssetDatabase.CreateAsset(data, ContextDataPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static GameObject CreateRootWithContext(ContextData contextData)
        {
            var rootObj = new GameObject("NexusRoot");
            var newRoot = rootObj.AddComponent<Root>();

            var serialized = new SerializedObject(newRoot);
            var prop = serialized.FindProperty("contextData");
            if (prop != null)
            {
                prop.objectReferenceValue = contextData;
                serialized.ApplyModifiedProperties();
            }
            return rootObj;
        }

        private static void AttachComponents(GameObject rootObj)
        {
            var boardView = rootObj.AddComponent<BoardView>();
            var torusPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Torus.obj");
            if (torusPrefab != null)
                boardView.SetTorusPrefab(torusPrefab);
            rootObj.AddComponent<GameplayLifecycle>();
            rootObj.AddComponent<UIRoot>();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
            }

            var inputModuleType = ResolveInputSystemUIInputModuleType() ?? typeof(StandaloneInputModule);

            var existing = eventSystem.GetComponent<BaseInputModule>();
            if (existing != null && !inputModuleType.IsInstanceOfType(existing))
            {
                Undo.DestroyObjectImmediate(existing);
            }
            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                var instance = eventSystem.gameObject.AddComponent(inputModuleType);
                if (instance != null) Undo.RegisterCreatedObjectUndo(instance, "Attach Input System Module");
            }
        }

        private static void EnsureMainCamera()
        {
            var camObj = GameObject.Find("Main Camera");
            Camera cam;
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
            }
            else
            {
                cam = camObj.GetComponent<Camera>();
                if (cam == null) cam = camObj.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.17f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.depth = -1;

            var t = camObj.transform;
            t.position = new Vector3(10f, 6f, -10f);
            t.rotation = Quaternion.Euler(20f, 0f, 0f);

            camObj.tag = "MainCamera";
        }

        private static void EnsureDirectionalLight()
        {
            var lightObj = GameObject.Find("Directional Light");
            Light light;
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                light = lightObj.AddComponent<Light>();
            }
            else
            {
                light = lightObj.GetComponent<Light>();
                if (light == null) light = lightObj.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.96f, 0.90f);

            var t = lightObj.transform;
            t.position = new Vector3(0f, 10f, 0f);
            t.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureCameraRaycasters()
        {
            foreach (var cam in Object.FindObjectsByType<Camera>())
            {
                if (cam == null) continue;
                if (cam.GetComponent<PhysicsRaycaster>() != null) continue;
                Undo.AddComponent<PhysicsRaycaster>(cam.gameObject);
            }
        }

        private static void MarkSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        private static System.Type ResolveInputSystemUIInputModuleType()
        {
            return System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI")
                ?? System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        }
    }
}
