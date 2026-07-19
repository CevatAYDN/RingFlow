using System.Collections.Generic;
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

            GameObject rootObj = null;
            var createdObjects = new List<GameObject>(4);
            try
            {
                var contextData = EnsureContextData();
                rootObj = CreateRootWithContext(contextData);
                createdObjects.Add(rootObj);
                Undo.RegisterCreatedObjectUndo(rootObj, "Create Nexus Root");
                AttachComponents(rootObj);
                EnsureEventSystem(createdObjects);
                EnsureMainCamera(createdObjects);
                EnsureDirectionalLight(createdObjects);
                EnsureCameraRaycasters();
                EnsureEditorSceneReady(rootObj);
                MarkSceneDirty();

                NexusLog.Info("EditorBootstrapper", nameof(Bootstrap), "",
                    "Nexus Bootstrapper successfully added to the active scene.");
                return BootstrapResult.Ok(rootObj);
            }
            catch (System.Exception ex)
            {
                for (int i = createdObjects.Count - 1; i >= 0; i--)
                {
                    if (createdObjects[i] != null)
                        Object.DestroyImmediate(createdObjects[i]);
                }
                NexusLog.Error("EditorBootstrapper", nameof(Bootstrap), "", ex.Message);
                return BootstrapResult.Fail(ex.Message);
            }
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
            if (torusPrefab == null)
                throw new System.InvalidOperationException("[EditorBootstrapper] Torus prefab is required.");
            boardView.SetTorusPrefab(torusPrefab);

            var lifecycle = rootObj.AddComponent<GameplayLifecycle>();
            Undo.RegisterCreatedObjectUndo(lifecycle, "Attach GameplayLifecycle");

            var uiRoot = rootObj.AddComponent<UIRoot>();
            Undo.RegisterCreatedObjectUndo(uiRoot, "Attach UIRoot");
        }

        private static void EnsureEventSystem(List<GameObject> createdObjects)
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                createdObjects.Add(go);
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            var inputModuleType = ResolveInputSystemUIInputModuleType();
            if (inputModuleType == null)
                throw new System.InvalidOperationException("[EditorBootstrapper] Input System UI module type is required.");

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent(inputModuleType);
            }
        }

        private static void EnsureMainCamera(List<GameObject> createdObjects)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                createdObjects.Add(go);
                Undo.RegisterCreatedObjectUndo(go, "Create Main Camera");
                camera = go.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            var feel = new RingFlow.Gameplay.Services.ResourcesAssetService()
                .LoadAsync<Gameplay.GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)
                .GetAwaiter().GetResult();
            if (feel == null)
                throw new System.InvalidOperationException("[EditorBootstrapper] GameFeelConfigSO is required.");

            camera.orthographic = true;
            camera.orthographicSize = feel.CameraBaseOrtho;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = EditorPaths.CameraBackground;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.depth = -1;

            var t = camera.transform;
            t.position = feel.CameraPosition;
            t.rotation = Quaternion.Euler(feel.CameraRotation);
        }

        private static void EnsureDirectionalLight(List<GameObject> createdObjects)
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
                createdObjects.Add(lightObj);
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

        private static void EnsureEditorSceneReady(GameObject rootObj)
        {
            if (rootObj == null) return;

            var uiRoot = rootObj.GetComponent<UIRoot>();
            if (uiRoot != null)
                RingFlowEditorUiStudioController.ReloadPrefabScreens(uiRoot, showDialog: false);
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
