using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;

namespace RingFlow.Editor
{
    /// <summary>
    /// Editor utility for programmatically generating or regenerating the base UI prefabs.
    /// Unifies the UI layout by directly calling the runtime View's BuildUI() method.
    /// </summary>
    public static class RingFlowEditorUiStudio
    {
        private const string ScreenPrefabFolder = EditorPaths.UiScreensFolder;

        public static string GetPrefabPathForScreen(ScreenType screen)
        {
            var registry = Resources.Load<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
            if (registry != null && registry.TryGetMapping(screen, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.PrefabPath))
                {
                    if (mapping.PrefabPath.EndsWith(".prefab"))
                        return mapping.PrefabPath;
                    if (mapping.PrefabPath.StartsWith("UI/"))
                        return $"Assets/Resources/{mapping.PrefabPath}.prefab";
                    return mapping.PrefabPath;
                }
            }
            return $"{ScreenPrefabFolder}/{screen}.prefab";
        }

        public static void CreateMissingUIScreenPrefabs()
        {
            CreateUIScreenPrefabs(recreateExisting: false);
        }

        public static void RecreateAllUIScreenPrefabs()
        {
            CreateUIScreenPrefabs(recreateExisting: true);
        }

        public static IReadOnlyList<string> GetUIScreenPrefabPreviewLines()
        {
            var screens = new List<ScreenType>(GetRequiredUiScreens());
            var lines = new List<string>(screens.Count);
            foreach (var screen in screens)
            {
                lines.Add($"{screen} => {GetPrefabPathForScreen(screen)}");
            }
            return lines;
        }

        private static void CreateUIScreenPrefabs(bool recreateExisting)
        {
            try
            {
                var screens = new List<ScreenType>(GetRequiredUiScreens());
                RingFlowEditorUtils.EnsureAssetFolders(ScreenPrefabFolder);

                var created = new List<string>();
                var skipped = new List<string>();
                var deleted = new List<string>();

                string screenList = string.Join(", ", screens);
                string confirmTitle = recreateExisting ? "Recreate UI Screens" : "Create Missing UI Screens";
                string confirmMessage = recreateExisting
                    ? $"This will delete and regenerate all base UI prefabs:\n{screenList}\n\nContinue?"
                    : $"This will generate missing base UI prefabs:\n{screenList}\n\nContinue?";

                if (!EditorUtility.DisplayDialog(confirmTitle, confirmMessage, "Continue", "Cancel"))
                    return;

                foreach (var screen in screens)
                {
                    var prefabPath = GetPrefabPathForScreen(screen);
                    bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;

                    if (exists && !recreateExisting)
                    {
                        skipped.Add(screen.ToString());
                        continue;
                    }

                    if (exists && recreateExisting)
                    {
                        if (AssetDatabase.DeleteAsset(prefabPath))
                            deleted.Add(screen.ToString());
                    }

                    var root = CreateScreenPrefabRoot(screen);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var success);
                    UnityEngine.Object.DestroyImmediate(root);

                    if (success)
                        created.Add(screen.ToString());
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var title = recreateExisting ? "Recreate UI Screens" : "Create Missing UI Screens";
                var message = created.Count > 0
                    ? $"Created {created.Count} prefab(s): {string.Join(", ", created)}"
                    : (recreateExisting ? "No UI prefabs were recreated." : "No missing UI prefabs were created.");

                if (deleted.Count > 0)
                    message += $"\nDeleted before recreate: {string.Join(", ", deleted)}";

                if (skipped.Count > 0)
                    message += $"\nAlready present: {string.Join(", ", skipped)}";

                EditorUtility.DisplayDialog(title, message, "OK");
            }
            catch (Exception ex)
            {
                var label = recreateExisting ? "RecreateUiScreens" : "CreateMissingUiScreens";
                NexusLog.Error("RingFlowEditorUiStudio", label, "CreateScreens", ex.ToString());
                EditorUtility.DisplayDialog(recreateExisting ? "Recreate UI Screens" : "Create Missing UI Screens", ex.Message, "OK");
            }
        }

        private static IEnumerable<ScreenType> GetRequiredUiScreens()
        {
            var registry = Resources.Load<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
            if (registry != null && registry.Mappings.Count > 0)
            {
                var list = new List<ScreenType>();
                foreach (var m in registry.Mappings)
                    list.Add(m.Screen);
                return list;
            }

            return new[]
            {
                ScreenType.Splash,
                ScreenType.MainMenu,
                ScreenType.WorldMap,
                ScreenType.LevelSelect,
                ScreenType.Gameplay,
                ScreenType.Pause,
                ScreenType.Win,
                ScreenType.Settings,
                ScreenType.DailyReward,
                ScreenType.Onboarding,
                ScreenType.GameOver,
                ScreenType.ChestPopup,
                ScreenType.ParentalGate,
            };
        }

        private static GameObject CreateScreenPrefabRoot(ScreenType screen)
        {
            var root = new GameObject(screen.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = root.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = false;

            AttachScreenView(root, screen);

            // Unify editor-generated prefab with runtime self-build
            if (root.TryGetComponent<IAuthoredView>(out var view))
            {
                view.BuildUI();
            }

            return root;
        }

        private static void AttachScreenView(GameObject root, ScreenType screen)
        {
            var registry = Resources.Load<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
            if (registry != null && registry.TryGetMapping(screen, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.ViewTypeName))
                {
                    var resolvedType = System.Type.GetType(mapping.ViewTypeName);
                    if (resolvedType != null)
                    {
                        root.AddComponent(resolvedType);
                        return;
                    }
                    else
                    {
                        NexusLog.Warn("RingFlowEditorUiStudio", nameof(AttachScreenView), screen.ToString(),
                            $"Failed to resolve view class type from type name: {mapping.ViewTypeName}. Falling back to default switch-case.");
                    }
                }
            }

            var viewType = screen switch
            {
                ScreenType.Splash => typeof(SplashView),
                ScreenType.MainMenu => typeof(MainMenuView),
                ScreenType.LevelSelect => typeof(LevelSelectView),
                ScreenType.Gameplay => typeof(HUDView),
                ScreenType.Pause => typeof(PauseView),
                ScreenType.Win => typeof(WinView),
                ScreenType.Settings => typeof(SettingsView),
                ScreenType.DailyReward => typeof(DailyRewardPopupView),
                ScreenType.ChestPopup => typeof(ChestPopupView),
                ScreenType.GameOver => typeof(GameOverView),
                ScreenType.ParentalGate => typeof(ParentalGatePopupView),
                ScreenType.WorldMap => typeof(WorldMapView),
                ScreenType.Onboarding => typeof(OnboardingView),
                _ => null
            };

            if (viewType != null)
                root.AddComponent(viewType);
        }
    }
}
