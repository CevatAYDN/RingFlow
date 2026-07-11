using System;
using System.Collections.Generic;
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
                RingFlowEditorUtils.EnsureAssetFolders(ScreenPrefabFolder);

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
                        created.Add(screen.ToString());
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var message = created.Count > 0
                    ? $"Created {created.Count} prefab(s): {string.Join(", ", created)}"
                    : "No missing UI prefabs were created.";

                if (existing.Count > 0)
                    message += $"\nAlready present: {string.Join(", ", existing)}";

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
            ApplyScreenTemplate(root.transform, screen);

            return root;
        }

        private static void AttachScreenView(GameObject root, ScreenType screen)
        {
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
                _ => null
            };

            if (viewType != null)
                root.AddComponent(viewType);
        }

        private static void ApplyScreenTemplate(Transform root, ScreenType screen)
        {
            switch (screen)
            {
                case ScreenType.Splash:
                    CreateSplashTemplate(root);
                    break;
                case ScreenType.MainMenu:
                    CreateMainMenuTemplate(root);
                    break;
                case ScreenType.LevelSelect:
                    CreateLevelSelectTemplate(root);
                    break;
                case ScreenType.Gameplay:
                    CreateGameplayTemplate(root);
                    break;
                case ScreenType.Pause:
                case ScreenType.Win:
                case ScreenType.GameOver:
                case ScreenType.Settings:
                case ScreenType.DailyReward:
                case ScreenType.ChestPopup:
                    CreatePopupTemplate(root, screen);
                    break;
                case ScreenType.ParentalGate:
                    CreateParentalGateTemplate(root);
                    break;
                default:
                    AddScreenText(root, "Header", screen.ToString(), 34,
                        new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0f, -20f), new Vector2(0f, 80f));
                    AddScreenText(root, "Body",
                        $"{screen} prefab generated from RingFlow Dashboard. Replace this placeholder with your authored UI.",
                        22, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(720f, 180f));
                    break;
            }
        }

        private static void CreateSplashTemplate(Transform root)
        {
            AddScreenText(root, "LogoText", "RING FLOW", 64,
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 120f));
            AddScreenText(root, "TaglineText", "Loading...", 18,
                new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600f, 50f));
            AddScreenText(root, "ProgressText", "", 14,
                new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500f, 40f));
        }

        private static void CreateMainMenuTemplate(Transform root)
        {
            AddScreenText(root, "Title", "Main Menu", 54,
                new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 100f));
            AddScreenText(root, "Subtitle", "Welcome back", 20,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600f, 40f));
            AddScreenText(root, "Body",
                "Add buttons as children here and wire them with MainMenuView.", 20,
                new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 160f));
        }

        private static void CreateLevelSelectTemplate(Transform root)
        {
            AddScreenText(root, "Title", "Level Select", 48,
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 100f));
            AddScreenText(root, "Body",
                "Create a grid of level buttons under this prefab. LevelSelectView binds children only.", 20,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760f, 180f));
        }

        private static void CreateGameplayTemplate(Transform root)
        {
            AddScreenText(root, "HUDTitle", "Gameplay HUD", 36,
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(800f, 70f));
            AddScreenText(root, "Body",
                "Add HUD widgets and action buttons as prefab children.", 20,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760f, 160f));
        }

        private static void CreatePopupTemplate(Transform root, ScreenType screen)
        {
            AddScreenText(root, "Title", screen.ToString(), 44,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 80f));
            AddScreenText(root, "Body",
                "Add popup content and buttons as children. Popup views bind existing prefab children only.", 20,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 160f));
        }

        private static void CreateParentalGateTemplate(Transform root)
        {
            // Card background (dark panel)
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.10f, 0.20f);
            cardRt.anchorMax = new Vector2(0.90f, 0.80f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.20f);

            // Title text
            AddScreenText(card.transform, "Title", "Parental Verification", 36,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.88f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            // Question text (named "Question" for BindReferencesFromChildren)
            var questionGo = AddScreenText(card.transform, "Question", "5 x 7 = ?", 28,
                new Vector2(0.05f, 0.56f), new Vector2(0.95f, 0.68f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var questionText = questionGo.GetComponent<Text>();
            questionText.color = new Color(1f, 0.76f, 0.03f);
            questionText.fontStyle = FontStyle.Bold;

            // Answer input field
            var inputGo = new GameObject("Answer", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(card.transform, false);
            var inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0.28f, 0.44f);
            inputRt.anchorMax = new Vector2(0.72f, 0.53f);
            inputRt.offsetMin = Vector2.zero;
            inputRt.offsetMax = Vector2.zero;
            inputGo.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.18f);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            var phRt = placeholderGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10, 0);
            phRt.offsetMax = new Vector2(-10, 0);
            var phText = placeholderGo.GetComponent<Text>();
            phText.text = "Enter answer...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 22;
            phText.alignment = TextAnchor.MiddleLeft;
            phText.color = new Color(0.6f, 0.6f, 0.65f);
            phText.fontStyle = FontStyle.Italic;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(inputGo.transform, false);
            var tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(10, 0);
            tRt.offsetMax = new Vector2(-10, 0);
            var tText = textGo.GetComponent<Text>();
            tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tText.fontSize = 22;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.color = Color.white;

            var inputField = inputGo.GetComponent<InputField>();
            inputField.textComponent = tText;
            inputField.placeholder = phText;
            inputField.characterLimit = 5;
            inputField.contentType = InputField.ContentType.IntegerNumber;

            // Error text (named "Error")
            var errorGo = AddScreenText(card.transform, "Error", "", 18,
                new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.43f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            errorGo.GetComponent<Text>().color = new Color(0.78f, 0.20f, 0.20f);

            // Accept button (named "Accept")
            var acceptGo = CreateButtonPrefabChild(card.transform, "Accept", "ACCEPT & CONTINUE", 320, 64,
                new Vector2(0.20f, 0.24f), new Vector2(0.80f, 0.34f));
            var acceptImg = acceptGo.GetComponent<Image>();
            acceptImg.color = new Color(0.22f, 0.51f, 0.91f);

            // Terms button (named "Terms")
            var termsGo = CreateButtonPrefabChild(card.transform, "Terms", "Terms of Service", 180, 40,
                new Vector2(0.08f, 0.10f), new Vector2(0.48f, 0.18f));
            var termsImg = termsGo.GetComponent<Image>();
            termsImg.color = new Color(0.12f, 0.13f, 0.18f);
            var termsTxt = termsGo.GetComponentInChildren<Text>();
            if (termsTxt != null) termsTxt.fontSize = 14;

            // Privacy button (named "Privacy")
            var privacyGo = CreateButtonPrefabChild(card.transform, "Privacy", "Privacy Policy", 180, 40,
                new Vector2(0.52f, 0.10f), new Vector2(0.92f, 0.18f));
            var privacyImg = privacyGo.GetComponent<Image>();
            privacyImg.color = new Color(0.12f, 0.13f, 0.18f);
            var privacyTxt = privacyGo.GetComponentInChildren<Text>();
            if (privacyTxt != null) privacyTxt.fontSize = 14;
        }

        private static GameObject CreateButtonPrefabChild(Transform parent, string name, string label, float width, float height,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.fontStyle = FontStyle.Bold;

            return go;
        }

        private static GameObject AddScreenText(Transform parent, string name, string textValue, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
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
            return go;
        }
    }
}
