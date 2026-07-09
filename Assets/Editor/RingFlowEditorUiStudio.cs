using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;

namespace RingFlow.Editor
{
    internal static class RingFlowEditorUiStudio
    {
        private const string ScreenPrefabFolder = "Assets/Resources/UI";

        public static string GetPrefabPathForScreen(ScreenType screen)
            => $"{ScreenPrefabFolder}/{screen}.prefab";

        public static void CreateMissingUIScreenPrefabs()
        {
            try
            {
                EnsureFolderExists("Assets", "Resources");
                EnsureFolderExists("Assets/Resources", "UI");

                var created = new List<string>();
                var existing = new List<string>();

                foreach (var screen in GetRequiredUiScreens())
                {
                    var prefabPath = GetPrefabPathForScreen(screen);
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    {
                        existing.Add(screen.ToString());
                        continue;
                    }

                    var root = CreateScreenPrefabRoot(screen);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var success);
                    UnityEngine.Object.DestroyImmediate(root);

                    if (success)
                    {
                        created.Add(screen.ToString());
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var message = created.Count > 0
                    ? $"Created {created.Count} prefab(s): {string.Join(", ", created)}"
                    : "No missing UI prefabs were created.";

                if (existing.Count > 0)
                {
                    message += $"\nAlready present: {string.Join(", ", existing)}";
                }

                EditorUtility.DisplayDialog("Create Missing UI Screens", message, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Create Missing UI Screens", ex.Message, "OK");
            }
        }

        private static IEnumerable<ScreenType> GetRequiredUiScreens()
        {
            return new[]
            {
                ScreenType.Splash,
                ScreenType.MainMenu,
                ScreenType.LevelSelect,
                ScreenType.Gameplay,
                ScreenType.Pause,
                ScreenType.Win,
                ScreenType.Settings,
                ScreenType.DailyReward,
                ScreenType.ChestPopup,
                ScreenType.GameOver,
                ScreenType.ParentalGate,
            };
        }

        private static void EnsureFolderExists(string parent, string folderName)
        {
            var path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
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

            if (screen == ScreenType.Splash)
            {
                root.AddComponent<SplashView>();
            }

            switch (screen)
            {
                case ScreenType.Splash:
                    CreateSplashTemplate(root.transform);
                    break;
                case ScreenType.MainMenu:
                    CreateMainMenuTemplate(root.transform);
                    break;
                case ScreenType.LevelSelect:
                    CreateLevelSelectTemplate(root.transform);
                    break;
                case ScreenType.Gameplay:
                    CreateGameplayTemplate(root.transform);
                    break;
                case ScreenType.Pause:
                case ScreenType.Win:
                case ScreenType.GameOver:
                case ScreenType.Settings:
                case ScreenType.DailyReward:
                case ScreenType.ChestPopup:
                case ScreenType.ParentalGate:
                    CreatePopupTemplate(root.transform, screen);
                    break;
                default:
                    AddScreenText(root.transform, "Header", screen.ToString(), 34, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, 80f));
                    AddScreenText(root.transform, "Body", $"{screen} prefab generated from RingFlow Dashboard. Replace this placeholder with your authored UI.", 22, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 180f));
                    break;
            }

            return root;
        }

        private static void CreateSplashTemplate(Transform root)
        {
            AddScreenText(root, "LogoText", "RING FLOW", 64, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 120f));
            AddScreenText(root, "TaglineText", "Loading...", 18, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 50f));
            AddScreenText(root, "ProgressText", "", 14, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 40f));
        }

        private static void CreateMainMenuTemplate(Transform root)
        {
            AddScreenText(root, "Title", "Main Menu", 54, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));
            AddScreenText(root, "Subtitle", "Welcome back", 20, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 40f));
            AddScreenText(root, "Body", "Add buttons as children here and wire them with MainMenuView.", 20, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 160f));
        }

        private static void CreateLevelSelectTemplate(Transform root)
        {
            AddScreenText(root, "Title", "Level Select", 48, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));
            AddScreenText(root, "Body", "Create a grid of level buttons under this prefab. LevelSelectView binds children only.", 20, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 180f));
        }

        private static void CreateGameplayTemplate(Transform root)
        {
            AddScreenText(root, "HUDTitle", "Gameplay HUD", 36, new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 70f));
            AddScreenText(root, "Body", "Add HUD widgets and action buttons as prefab children.", 20, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 160f));
        }

        private static void CreatePopupTemplate(Transform root, ScreenType screen)
        {
            AddScreenText(root, "Title", screen.ToString(), 44, new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 80f));
            AddScreenText(root, "Body", "Add popup content and buttons as children. Popup views bind existing prefab children only.", 20, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 160f));
        }

        private static void AddScreenText(Transform parent, string name, string textValue, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.text = textValue;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
