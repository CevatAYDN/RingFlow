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
    internal static class RingFlowEditorUiStudio
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
            // Try resolving via ScreenRegistrySO reflection first
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

            // Fallback hardcoded switch-case
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
                    CreatePauseTemplate(root);
                    break;
                case ScreenType.Win:
                    CreateWinTemplate(root);
                    break;
                case ScreenType.GameOver:
                    CreateGameOverTemplate(root);
                    break;
                case ScreenType.Settings:
                    CreateSettingsTemplate(root);
                    break;
                case ScreenType.DailyReward:
                    CreateDailyRewardTemplate(root);
                    break;
                case ScreenType.ChestPopup:
                    CreateChestPopupTemplate(root);
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
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "LogoCard", 0.18f, 0.58f, 0.82f, 0.84f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            AddScreenText(root, "LogoText", "RING FLOW", 68,
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 120f));
            AddScreenText(root, "TaglineText", "Sculpted puzzles. Clean moves. Premium flow.", 18,
                new Vector2(0.5f, 0.61f), new Vector2(0.5f, 0.61f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(720f, 50f));
            AddScreenText(root, "ProgressText", "", 14,
                new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500f, 40f));
        }

        private static void CreateMainMenuTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "Card", 0.16f, 0.24f, 0.84f, 0.76f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            CreatePanel(root, "AccentStrip", 0.14f, 0.12f, 0.86f, 0.14f, new Color(0.84f, 0.56f, 0.22f, 0.92f));
            CreatePanel(root, "ContentCard", 0.20f, 0.62f, 0.80f, 0.70f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            AddScreenText(root, "Title", "RING FLOW", 68,
                new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 120f));
            AddScreenText(root, "Subtitle", "Continue your progress", 22,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 40f));
            CreatePanel(root, "ProfilePanel", 0.12f, 0.58f, 0.88f, 0.68f, new Color(0.20f, 0.22f, 0.30f, 0.96f));
            AddScreenText(root, "Coins", "0 Coins", 18,
                new Vector2(0.18f, 0.62f), new Vector2(0.42f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(260f, 30f));
            AddScreenText(root, "Diamonds", "0 Gems", 18,
                new Vector2(0.58f, 0.62f), new Vector2(0.82f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(260f, 30f));
            AddScreenText(root, "Version", "", 12,
                new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(250f, 24f));

            CreateButtonNode(root, "Btn_CONTINUE", "CONTINUE", 340, 70, new Vector2(0.28f, 0.50f), new Vector2(0.72f, 0.58f));
            CreateButtonNode(root, "Btn_QUICK PLAY", "QUICK PLAY", 340, 62, new Vector2(0.28f, 0.42f), new Vector2(0.72f, 0.49f));
            CreateButtonNode(root, "Btn_LEVELS", "LEVEL SELECT", 340, 62, new Vector2(0.28f, 0.34f), new Vector2(0.72f, 0.41f));
            CreateButtonNode(root, "Btn_DAILY REWARD", "DAILY REWARD", 340, 58, new Vector2(0.28f, 0.26f), new Vector2(0.72f, 0.33f));
            CreateButtonNode(root, "Btn_SETTINGS", "SETTINGS", 64, 64, new Vector2(0.86f, 0.82f), new Vector2(0.94f, 0.90f));
        }

        private static void CreateWorldMapTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "HeroCard", 0.12f, 0.18f, 0.88f, 0.82f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            CreatePanel(root, "AccentStrip", 0.12f, 0.12f, 0.88f, 0.14f, new Color(0.22f, 0.56f, 0.84f, 0.92f));
            AddScreenText(root, "Title", "WORLD MAP", 50,
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(800f, 90f));
            AddScreenText(root, "Subtitle", "Choose a world to continue", 18,
                new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(600f, 30f));
            CreatePanel(root, "MapPanel", 0.18f, 0.32f, 0.82f, 0.60f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            AddScreenText(root, "Body", "World nodes, rewards, and progression will appear here.", 22,
                new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760f, 120f));
            CreateButtonNode(root, "Btn_BACK", "BACK", 220, 54, new Vector2(0.04f, 0.08f), new Vector2(0.20f, 0.14f));
        }

        private static void CreateOnboardingTemplate(Transform root)
        {
            CreatePanel(root, "Card", 0.12f, 0.20f, 0.88f, 0.80f, new Color(0.12f, 0.14f, 0.18f, 0.92f));
            CreatePanel(root, "AccentStrip", 0.12f, 0.12f, 0.88f, 0.14f, new Color(0.84f, 0.56f, 0.22f, 0.92f));
            CreatePanel(root, "ContentCard", 0.16f, 0.28f, 0.84f, 0.68f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            AddScreenText(root, "Title", "MASTER THE FLOW", 42,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(800f, 90f));
            AddScreenText(root, "Body", "Tap a pole to select a ring, then tap a matching pole to move it.\nClear the board by sorting every color.", 20,
                new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760f, 160f));
        }

        private static void CreateLevelSelectTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "HeroCard", 0.10f, 0.16f, 0.90f, 0.84f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            CreatePanel(root, "AccentStrip", 0.10f, 0.08f, 0.90f, 0.11f, new Color(0.22f, 0.56f, 0.84f, 0.92f));
            AddScreenText(root, "Title", "LEVEL SELECT", 50,
                new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 90f));
            AddScreenText(root, "Subtitle", "Choose your next challenge", 18,
                new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 30f));
            CreateButtonNode(root, "Btn_BACK", "BACK", 220, 54, new Vector2(0.04f, 0.86f), new Vector2(0.20f, 0.92f));

            CreatePanel(root, "SummaryPanel", 0.10f, 0.74f, 0.90f, 0.82f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            AddScreenText(root, "WorldLabel", "WORLD 1", 18,
                new Vector2(0.12f, 0.75f), new Vector2(0.28f, 0.81f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(root, "ProgressLabel", "0 / 120 unlocked", 18,
                new Vector2(0.35f, 0.75f), new Vector2(0.60f, 0.81f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(root, "DifficultyLabel", "Difficulty: Easy", 18,
                new Vector2(0.68f, 0.75f), new Vector2(0.88f, 0.81f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            var grid = new GameObject("LevelGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(root, false);
            var gridRt = grid.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.10f, 0.16f);
            gridRt.anchorMax = new Vector2(0.90f, 0.70f);
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;
            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150f, 60f);
            gridLayout.spacing = new Vector2(16f, 16f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 1; i <= 8; i++)
            {
                CreateButtonNode(grid.transform, $"Btn_Level {i}", $"LEVEL {i}", 150, 60, Vector2.zero, Vector2.one);
            }
        }

        private static void CreateGameplayTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "HeroCard", 0.04f, 0.04f, 0.96f, 0.96f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            CreatePanel(root, "AccentStrip", 0.04f, 0.02f, 0.96f, 0.05f, new Color(0.22f, 0.56f, 0.84f, 0.92f));

            CreatePanel(root, "TopBar", 0.08f, 0.84f, 0.92f, 0.94f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            AddScreenText(root, "MovesText", "Moves: 0", 20,
                new Vector2(0.08f, 0.88f), new Vector2(0.28f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(root, "LevelText", "Level 1", 20,
                new Vector2(0.72f, 0.88f), new Vector2(0.92f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(root, "CoinsText", "0", 18,
                new Vector2(0.44f, 0.88f), new Vector2(0.53f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(100f, 24f));
            AddScreenText(root, "DiamondsText", "0", 18,
                new Vector2(0.54f, 0.88f), new Vector2(0.63f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(100f, 24f));

            CreatePanel(root, "BoardFrame", 0.10f, 0.22f, 0.90f, 0.80f, new Color(0.10f, 0.11f, 0.15f, 0.92f));
            AddScreenText(root, "BoardPlaceholder", "Board area", 22,
                new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500f, 60f));
            AddScreenText(root, "BoardHint", "This panel is where the ring board appears.", 16,
                new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.46f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(520f, 40f));

            CreatePanel(root, "ActionBar", 0.06f, 0.06f, 0.94f, 0.16f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            CreateButtonNode(root, "Btn_UNDO", "UNDO", 140, 48, new Vector2(0.08f, 0.08f), new Vector2(0.22f, 0.14f));
            CreateButtonNode(root, "Btn_RESTART", "RESTART", 160, 48, new Vector2(0.26f, 0.08f), new Vector2(0.42f, 0.14f));
            CreateButtonNode(root, "Btn_HINT", "HINT", 140, 48, new Vector2(0.44f, 0.08f), new Vector2(0.58f, 0.14f));
            CreateButtonNode(root, "Btn_PAUSE", "PAUSE", 140, 48, new Vector2(0.62f, 0.08f), new Vector2(0.76f, 0.14f));
        }

        private static void CreatePauseTemplate(Transform root)
        {
            // PauseView self-builds when there are no authored children.
        }

        private static void CreateWinTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "Card", 0.16f, 0.20f, 0.84f, 0.80f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            AddScreenText(root, "Text", "YOU WIN!", 48,
                new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 90f));
            AddScreenText(root, "RewardText", "Rewards unlocked", 18,
                new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(500f, 30f));
            CreateButtonNode(root, "Btn_NEXT LEVEL", "NEXT LEVEL", 320, 70, new Vector2(0.26f, 0.18f), new Vector2(0.74f, 0.26f));
            CreateButtonNode(root, "Btn_MAIN MENU", "MAIN MENU", 300, 58, new Vector2(0.28f, 0.10f), new Vector2(0.72f, 0.16f));
        }

        private static void CreateGameOverTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            CreatePanel(root, "Card", 0.16f, 0.20f, 0.84f, 0.80f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            AddScreenText(root, "Title", "GAME OVER", 46,
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 90f));
            AddScreenText(root, "Message", "You can solve this one.", 22,
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620f, 60f));
            CreateButtonNode(root, "Btn_RESTART", "RESTART", 300, 60, new Vector2(0.28f, 0.22f), new Vector2(0.72f, 0.30f));
            CreateButtonNode(root, "Btn_MAIN MENU", "MAIN MENU", 300, 54, new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.18f));
        }

        private static void CreateDailyRewardTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            var card = CreatePanel(root, "Card", 0.20f, 0.24f, 0.80f, 0.76f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            AddScreenText(card.transform, "Title", "DAILY REWARD", 36,
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.85f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Day", "TODAY", 56,
                new Vector2(0.20f, 0.46f), new Vector2(0.80f, 0.64f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Reward", "+50 Coins", 24,
                new Vector2(0.20f, 0.30f), new Vector2(0.80f, 0.42f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            CreateButtonNode(card.transform, "Btn_CLAIM", "CLAIM REWARD", 300, 60, new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.25f));
            CreateButtonNode(card.transform, "Btn_CLOSE", "CLOSE", 120, 38, new Vector2(0.40f, 0.04f), new Vector2(0.60f, 0.11f));
        }

        private static void CreateChestPopupTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            var card = CreatePanel(root, "Card", 0.12f, 0.20f, 0.88f, 0.80f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            AddScreenText(card.transform, "Title", "CHEST REWARDS", 34,
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Subtitle", "Claim the rewards you have earned", 18,
                new Vector2(0.10f, 0.74f), new Vector2(0.90f, 0.80f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Bronze", "Bronze Chest: x0  (+0 XP)", 20,
                new Vector2(0.10f, 0.60f), new Vector2(0.90f, 0.67f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Silver", "Silver Chest: x0  (+0 XP)", 20,
                new Vector2(0.10f, 0.50f), new Vector2(0.90f, 0.57f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Gold", "Gold Chest: x0  (+0 XP)", 20,
                new Vector2(0.10f, 0.40f), new Vector2(0.90f, 0.47f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Diamond", "Diamond Chest: x0  (+0 XP)", 20,
                new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.37f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "TotalXp", "Total XP: +0", 22,
                new Vector2(0.15f, 0.20f), new Vector2(0.85f, 0.27f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            CreateButtonNode(card.transform, "Btn_CLAIM ALL", "CLAIM ALL", 280, 60, new Vector2(0.15f, 0.10f), new Vector2(0.85f, 0.20f));
            CreateButtonNode(card.transform, "Btn_CLOSE", "CLOSE", 140, 42, new Vector2(0.40f, 0.02f), new Vector2(0.60f, 0.09f));
        }

        private static void CreateSettingsTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            var card = CreatePanel(root, "Card", 0.06f, 0.08f, 0.94f, 0.92f, new Color(0.14f, 0.16f, 0.22f, 0.96f));
            AddScreenText(card.transform, "Title", "SETTINGS", 44,
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 90f));

            CreateToggleRow(card.transform, "Music", "MUSIC", 0.74f, true);
            CreateToggleRow(card.transform, "SFX", "SFX", 0.68f, true);
            CreateToggleRow(card.transform, "Haptic", "HAPTIC", 0.62f, true);
            CreateToggleRow(card.transform, "Motion", "REDUCE MOTION", 0.56f, false);
            CreateToggleRow(card.transform, "Big", "BIG BUTTONS", 0.50f, false);

            CreateSliderNode(card.transform, "ColorBlindSlider", 0.08f, 0.38f, 0.92f, 0.44f);
            AddScreenText(card.transform, "ColorBlind", "COLOR BLIND", 18,
                new Vector2(0.08f, 0.44f), new Vector2(0.30f, 0.48f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            CreateDropdownNode(card.transform, "LanguageDropdown", 0.52f, 0.38f, 0.92f, 0.44f);
            AddScreenText(card.transform, "Lang", "LANGUAGE", 18,
                new Vector2(0.52f, 0.44f), new Vector2(0.76f, 0.48f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            CreateButtonNode(card.transform, "Btn_REMOVE ADS", "REMOVE ADS", 280, 54, new Vector2(0.16f, 0.18f), new Vector2(0.52f, 0.26f));
            CreateButtonNode(card.transform, "Btn_RESTORE", "RESTORE", 220, 54, new Vector2(0.56f, 0.18f), new Vector2(0.84f, 0.26f));
            CreateButtonNode(card.transform, "Btn_CLOSE", "CLOSE", 120, 40, new Vector2(0.40f, 0.06f), new Vector2(0.60f, 0.14f));
        }

        private static void CreateParentalGateTemplate(Transform root)
        {
            CreateBackdrop(root, "Backdrop", new Color(0.06f, 0.08f, 0.12f, 0.96f));
            var card = CreatePanel(root, "Card", 0.10f, 0.18f, 0.90f, 0.82f, new Color(0.14f, 0.14f, 0.20f, 0.96f));
            AddScreenText(card.transform, "Title", "PARENTAL CONSENT", 34,
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.86f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            AddScreenText(card.transform, "Question", "Please review the terms and continue with consent.", 22,
                new Vector2(0.10f, 0.42f), new Vector2(0.90f, 0.62f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            CreatePanel(card.transform, "ChallengePanel", 0.12f, 0.28f, 0.88f, 0.40f, new Color(0.18f, 0.20f, 0.28f, 0.96f));
            AddScreenText(card.transform, "Error", "", 18,
                new Vector2(0.15f, 0.36f), new Vector2(0.85f, 0.41f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            CreateButtonNode(card.transform, "Btn_ACCEPT", "ACCEPT & CONTINUE", 340, 66, new Vector2(0.18f, 0.24f), new Vector2(0.82f, 0.34f));
            CreateButtonNode(card.transform, "Btn_TERMS", "TERMS", 180, 40, new Vector2(0.08f, 0.10f), new Vector2(0.48f, 0.18f));
            CreateButtonNode(card.transform, "Btn_PRIVACY", "PRIVACY", 180, 40, new Vector2(0.52f, 0.10f), new Vector2(0.92f, 0.18f));
        }

        private static void CreateToggleRow(Transform parent, string name, string label, float anchorY, bool initialValue)
        {
            AddScreenText(parent, label, label, 18,
                new Vector2(0.10f, anchorY - 0.015f), new Vector2(0.42f, anchorY + 0.015f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var toggle = new GameObject($"{name} Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggle.transform.SetParent(parent, false);
            var rect = toggle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.46f, anchorY - 0.02f);
            rect.anchorMax = new Vector2(0.58f, anchorY + 0.02f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = toggle.GetComponent<Image>();
            image.color = new Color(0.18f, 0.20f, 0.24f, 1f);
            var tg = toggle.GetComponent<Toggle>();
            tg.isOn = initialValue;
            var checkmarkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGo.transform.SetParent(toggle.transform, false);
            var cmRect = checkmarkGo.GetComponent<RectTransform>();
            cmRect.anchorMin = new Vector2(0.5f, 0.5f);
            cmRect.anchorMax = new Vector2(0.5f, 0.5f);
            cmRect.sizeDelta = new Vector2(26f, 26f);
            checkmarkGo.GetComponent<Image>().color = new Color(0.22f, 0.56f, 0.84f, 1f);
            tg.targetGraphic = checkmarkGo.GetComponent<Image>();
            tg.graphic = checkmarkGo.GetComponent<Image>();
        }

        private static void CreateSliderNode(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.24f, 1f);
        }

        private static void CreateDropdownNode(Transform parent, string name, float xMin, float yMin, float xMax, float yMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.24f, 1f);
        }

        private static void CreateBackdrop(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void CreateButtonNode(Transform parent, string name, string label, float width, float height, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.56f, 0.84f, 1f);
            image.sprite = GameUIResources.GetRoundedSprite();
            image.type = Image.Type.Sliced;

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -4f);

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
        }

        private static GameObject CreatePanel(Transform parent, string name, float xMin, float yMin, float xMax, float yMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = GameUIResources.GetRoundedSprite();
            image.type = Image.Type.Sliced;
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
