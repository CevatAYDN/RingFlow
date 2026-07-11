using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public static class EditorBootstrapper
    {
        private const string ContextDataPath = EditorPaths.ContextDataPath;

        public static BootstrapResult Bootstrap()
        {
            if (Application.isPlaying)
                return BootstrapResult.Fail("Cannot run setup during PlayMode.");

            if (Object.FindAnyObjectByType<Root>() != null)
                return BootstrapResult.Fail("Nexus Bootstrapper already exists in the scene.");

            var contextData = EnsureContextData();
            var rootObj = CreateRootWithContext(contextData);
            Undo.RegisterCreatedObjectUndo(rootObj, "Create Nexus Root");
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
                AssetDatabase.CreateFolder("Assets", "Settings");

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
            Undo.RegisterCreatedObjectUndo(boardView, "Attach BoardView");

            var torusPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPaths.TorusPrefabPath);
            if (torusPrefab != null)
                boardView.SetTorusPrefab(torusPrefab);

            var lifecycle = rootObj.AddComponent<GameplayLifecycle>();
            Undo.RegisterCreatedObjectUndo(lifecycle, "Attach GameplayLifecycle");

            var uiRoot = rootObj.AddComponent<UIRoot>();
            Undo.RegisterCreatedObjectUndo(uiRoot, "Attach UIRoot");
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
            }

            var inputModuleType = ResolveInputSystemUIInputModuleType();
            if (inputModuleType == null)
            {
                inputModuleType = typeof(StandaloneInputModule);
                NexusLog.Warn("EditorBootstrapper", nameof(EnsureEventSystem), "",
                    "Input System package not detected. Falling back to StandaloneInputModule.");
            }

            var existing = eventSystem.GetComponent<BaseInputModule>();
            if (existing != null && !inputModuleType.IsInstanceOfType(existing))
                Undo.DestroyObjectImmediate(existing);

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                var instance = eventSystem.gameObject.AddComponent(inputModuleType);
                if (instance != null) Undo.RegisterCreatedObjectUndo(instance, "Attach Input Module");
            }
        }

        private static void EnsureMainCamera()
        {
            var camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            var camObj = camera != null ? camera.gameObject : null;

            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
                camera = camObj.AddComponent<Camera>();
            }

            var feel = Gameplay.GameFeelConfigSO.Instance;
            camera.orthographic = true;
            camera.orthographicSize = feel.CameraBaseOrtho;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = EditorPaths.CameraBackground;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.depth = -1;

            var t = camObj.transform;
            t.position = feel.CameraPosition;
            t.rotation = Quaternion.Euler(feel.CameraRotation);

            camObj.tag = "MainCamera";
        }

        private static void EnsureDirectionalLight()
        {
            var existingLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            var dirLight = (Light)null;
            foreach (var l in existingLights)
            {
                if (l.type == LightType.Directional)
                {
                    dirLight = l;
                    break;
                }
            }

            GameObject lightObj;
            bool isNew = false;

            if (dirLight != null)
            {
                lightObj = dirLight.gameObject;
            }
            else
            {
                lightObj = new GameObject("Directional Light");
                Undo.RegisterCreatedObjectUndo(lightObj, "Create Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                isNew = true;
            }

            dirLight.type = LightType.Directional;
            dirLight.intensity = 1f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.color = EditorPaths.DirectionalLightColor;

            if (isNew)
            {
                var t = lightObj.transform;
                t.position = new Vector3(0f, 10f, 0f);
                t.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
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
