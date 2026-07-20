using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Object = UnityEngine.Object;

namespace RingFlow.Editor
{
    /// <summary>
    /// Data-driven prefab builder — generates the complete UI hierarchy for every
    /// screen type used during the game. Called from RingFlowEditorUiStudio when
    /// (re)creating prefabs.
    ///
    /// Uses UIThemeConfigSO colors and UISpriteLibrarySO sprites directly so no
    /// DI or runtime service is required during generation. Each BuildXxx method
    /// creates a fully-structured prefab that Views reference via BindReferencesFromChildren().
    ///
    /// Generated prefabs are game-ready for layout review; sprite assignments and
    /// toggle visual templates are set up automatically from the asset libraries.
    /// </summary>
    public static class UIPrefabBuilder
    {
        private static UIThemeConfigSO _theme;
        private static UISpriteLibrarySO _spriteLib;

        // ── Cached theme accessors ──────────────────────────────────────
        private static Color Pc => _theme.PrimaryColor;
        private static Color Pl => _theme.PrimaryLight;
        private static Color Pp => _theme.PrimaryPressed;
        private static Color Ac => _theme.AccentColor;
        private static Color Al => _theme.AccentLight;
        private static Color Bg => _theme.BgColor;
        private static Color Bd => _theme.BgDark;
        private static Color Sc => _theme.SurfaceColor;
        private static Color Sd => _theme.SurfaceDark;
        private static Color Pnl => _theme.PanelColor;
        private static Color Pd => _theme.PanelDark;
        private static Color Tc => _theme.TextColor;
        private static Color Top => _theme.TextOnPrimary;
        private static Color Tod => _theme.TextOnDark;
        private static Color Mt => _theme.MutedText;
        private static Color Mtd => _theme.MutedTextDark;
        private static Color Dc => _theme.DangerColor;
        private static Color Dl => _theme.DangerLight;
        private static Color Scs => _theme.SuccessColor;
        private static Color Sl => _theme.SuccessLight;
        private static Color Wc => _theme.WarningColor;
        private static Color OvL => _theme.OverlayLight;
        private static Color OvM => _theme.OverlayMedium;
        private static Color OvH => _theme.OverlayHeavy;
        private static Color CoinC => _theme.CoinColor;
        private static Color DiamC => _theme.DiamondColor;
        private static float Bh => _theme.ButtonHeight;
        private static float Bw => _theme.ButtonWidth;
        private static float SbH => _theme.SmallButtonHeight;
        private static float IbS => _theme.IconButtonSize;
        private static int BfS => _theme.ButtonFontSize;
        private static int DfS => _theme.DisplayFontSize;
        private static int TfS => _theme.TitleFontSize;
        private static int HfS => _theme.HeaderFontSize;

        // ── Initialization ──────────────────────────────────────────────

        public static void Initialize()
        {
            _theme = Resources.Load<UIThemeConfigSO>(GameplayAssetKeys.UIThemeConfig);
            if (_theme != null)
                _spriteLib = _theme.SpriteLibrary;
            else
                Debug.LogError("[UIPrefabBuilder] UIThemeConfigSO not found at " + GameplayAssetKeys.UIThemeConfig);
        }

        // ── Public entry point ──────────────────────────────────────────

        public static void Build(GameObject root, ScreenType screen)
        {
            if (_theme == null) Initialize();
            if (_theme == null) return;

            switch (screen)
            {
                case ScreenType.Splash:         BuildSplash(root); break;
                case ScreenType.MainMenu:       BuildMainMenu(root); break;
                case ScreenType.LevelSelect:    BuildLevelSelect(root); break;
                case ScreenType.Gameplay:       BuildHUD(root); break;
                case ScreenType.Pause:          BuildPause(root); break;
                case ScreenType.Win:            BuildWin(root); break;
                case ScreenType.GameOver:       BuildGameOver(root); break;
                case ScreenType.Settings:       BuildSettings(root); break;
                case ScreenType.DailyReward:    BuildDailyReward(root); break;
                case ScreenType.ChestPopup:     BuildChestPopup(root); break;
                case ScreenType.Onboarding:     BuildOnboarding(root); break;
                case ScreenType.ParentalGate:   BuildParentalGate(root); break;
                case ScreenType.WorldMap:       BuildWorldMap(root); break;
                case ScreenType.MechanicGuide:  BuildMechanicGuide(root); break;
                default:
                    Debug.LogWarning($"[UIPrefabBuilder] No builder for {screen}");
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SCREEN BUILDERS
        // ══════════════════════════════════════════════════════════════════

        private static void BuildSplash(GameObject root)
        {
            SetRootImage(root, Bd, false);

            var card = CreatePanel("Card", root.transform);
            SetAnchors(card, 0f, 0f, 1f, 1f);
            card.GetComponent<Image>().color = Color.clear;
            card.AddComponent<CanvasGroup>();

            CreateDisplayText(card.transform, "RING FLOW", DfS, Tod, "LogoText", new Rect(0.12f, 0.62f, 0.76f, 0.16f));
            var tag = CreateText(card.transform, "", 18, TextAlignmentOptions.Center, Mtd, "TaglineText");
            SetAnchors(tag, 0.15f, 0.54f, 0.85f, 0.60f);

            var barBg = CreatePanel("ProgressBarBg", card.transform, null);
            SetAnchors(barBg, 0.30f, 0.32f, 0.70f, 0.36f);
            SetImageColor(barBg, new Color(0.20f, 0.22f, 0.28f));

            var barFill = CreatePanel("ProgressBarFill", barBg.transform, GetSprite("HUDProgressFill"));
            SetAnchors(barFill, 0f, 0f, 0f, 1f);
            SetImageColor(barFill, Ac);

            var progText = CreateText(card.transform, "", 14, TextAlignmentOptions.Center, Mtd, "ProgressText");
            SetAnchors(progText, 0.20f, 0.26f, 0.80f, 0.30f);

            var verText = CreateText(card.transform, "v1.0", 12, TextAlignmentOptions.Bottom, Mtd, "VersionLabel");
            SetAnchors(verText, 0.30f, 0.04f, 0.70f, 0.08f);
        }

        private static void BuildMainMenu(GameObject root)
        {
            var bgGo = CreatePanel("Background", root.transform);
            SetAnchors(bgGo, 0f, 0f, 1f, 1f);
            SetImageColor(bgGo, Bg);
            bgGo.AddComponent<CanvasGroup>();

            var topBar = CreatePanel("TopBar", bgGo.transform);
            SetAnchors(topBar, 0f, 0.90f, 1f, 0.98f);

            var avatar = CreateCard("Avatar", topBar.transform, Sc);
            SetAnchors(avatar, 0.04f, 0.15f, 0.14f, 0.85f);
            var avText = CreateText(avatar.transform, "C", 18, TextAlignmentOptions.Center, Tc, "AvatarText");
            avText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Level pill
            var lvlBtn = CreatePanel("Btn_LEVEL_SELECT", topBar.transform);
            SetAnchors(lvlBtn, 0.18f, 0.15f, 0.50f, 0.85f);
            SetImageColor(lvlBtn, Sc);
            lvlBtn.AddComponent<Button>();
            lvlBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            lvlBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
            var pLevel = CreateText(lvlBtn.transform, "Lv 1", 14, TextAlignmentOptions.MidlineLeft, Tc, "LvText");
            pLevel.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(pLevel, 0.08f, 0f, 0.35f, 1f);

            var lvlBarBg = CreatePanel("LevelBarBg", lvlBtn.transform, GetSprite("HUDProgressBackground"));
            SetAnchors(lvlBarBg, 0.38f, 0.35f, 0.92f, 0.65f);
            SetImageColor(lvlBarBg, Pnl);
            var lvlBarFill = CreatePanel("LevelBarFill", lvlBarBg.transform, GetSprite("HUDProgressFill"));
            SetAnchors(lvlBarFill, 0f, 0f, 0.5f, 1f);
            SetImageColor(lvlBarFill, Pc);

            // Coins pill
            var coinsPill = CreateCard("CoinsPill", topBar.transform, Sc);
            SetAnchors(coinsPill, 0.52f, 0.15f, 0.73f, 0.85f);
            CreateSpriteImage(coinsPill.transform, "CoinIcon", GetSprite("IconCoin"), new Rect(0.05f, 0.15f, 0.30f, 0.85f));
            var coinsText = CreateText(coinsPill.transform, "0", 14, TextAlignmentOptions.MidlineLeft, Tc, "CoinsText");
            coinsText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(coinsText, 0.40f, 0f, 0.95f, 1f);

            // Gems pill
            var gemsPill = CreateCard("GemsPill", topBar.transform, Sc);
            SetAnchors(gemsPill, 0.76f, 0.15f, 0.97f, 0.85f);
            CreateSpriteImage(gemsPill.transform, "GemIcon", GetSprite("IconGem"), new Rect(0.05f, 0.15f, 0.30f, 0.85f));
            var gemsText = CreateText(gemsPill.transform, "0", 14, TextAlignmentOptions.MidlineLeft, Tc, "DiamondsText");
            gemsText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(gemsText, 0.40f, 0f, 0.95f, 1f);

            // Title + logo
            var titleGo = CreateDisplayText(bgGo.transform, "", 56, Color.clear, "Title", new Rect(0.15f, 0.75f, 0.70f, 0.13f));
            CreateSpriteImage(titleGo.transform, "LogoImage", GetSprite("BackgroundMain"), new Rect(0f, 0f, 1f, 1f));

            var subtitle = CreateText(bgGo.transform, "Sort the rings, relax your mind", 18, TextAlignmentOptions.Center, Mt, "Subtitle");
            SetAnchors(subtitle, 0.05f, 0.73f, 0.95f, 0.77f);

            // Board preview card
            var boardCard = CreateCard("BoardPreviewCard", bgGo.transform, Sc);
            SetAnchors(boardCard, 0.12f, 0.44f, 0.88f, 0.71f);
            float[] poleX = { 0.25f, 0.50f, 0.75f };
            for (int i = 0; i < 3; i++)
            {
                var pole = CreatePanel($"MockPole_{i}", boardCard.transform);
                SetAnchors(pole, poleX[i] - 0.015f, 0.15f, poleX[i] + 0.015f, 0.85f);
                SetImageColor(pole, new Color(0.82f, 0.81f, 0.75f));
                int rings = i switch { 0 => 3, 1 => 2, 2 => 1, _ => 0 };
                for (int r = 0; r < rings; r++)
                {
                    var ring = CreatePanel($"MockRing_{i}_{r}", boardCard.transform);
                    float rY = 0.15f + r * 0.16f;
                    SetAnchors(ring, poleX[i] - 0.08f, rY, poleX[i] + 0.08f, rY + 0.13f);
                    SetImageColor(ring, (i, r) switch
                    {
                        (0, 0) => Pc, (0, 1) => Scs, (0, 2) => Pc,
                        (1, 0) => Ac, (1, 1) => Scs, (2, 0) => Ac, _ => Pc
                    });
                }
            }

            // Continue button
            var contBtn = CreateButton("PLAY", bgGo.transform, 320f, 60f, "Btn_CONTINUE", GetSprite("ButtonPrimary"));
            SetAnchors(contBtn, 0.12f, 0.33f, 0.88f, 0.41f);
            ApplyPrimaryStyle(contBtn);

            // Daily challenge
            var dailyBtn = CreatePanel("Btn_DAILY", bgGo.transform, GetSprite("PanelCard"));
            SetAnchors(dailyBtn, 0.12f, 0.20f, 0.48f, 0.30f);
            SetImageColor(dailyBtn, Sc);
            dailyBtn.AddComponent<Button>();
            dailyBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            dailyBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
            CreateSpriteImage(dailyBtn.transform, "IconImage", GetSprite("IconDaily"), new Rect(0.08f, 0.2f, 0.27f, 0.8f));
            var dailyLabel = CreateText(dailyBtn.transform, "DAILY\nCHALLENGE", 11, TextAlignmentOptions.MidlineLeft, Tc, "DailyLabel");
            dailyLabel.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(dailyLabel, 0.40f, 0f, 0.95f, 1f);

            // Chest button
            var chestBtn = CreatePanel("Btn_CHEST", bgGo.transform, GetSprite("PanelCard"));
            SetAnchors(chestBtn, 0.52f, 0.20f, 0.90f, 0.30f);
            SetImageColor(chestBtn, Sc);
            chestBtn.AddComponent<Button>();
            chestBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            chestBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
            CreateSpriteImage(chestBtn.transform, "IconImage", GetSprite("IconChest"), new Rect(0.08f, 0.2f, 0.27f, 0.8f));
            var chestLabel = CreateText(chestBtn.transform, "CHESTS", 11, TextAlignmentOptions.MidlineLeft, Tc, "ChestLabel");
            chestLabel.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(chestLabel, 0.40f, 0f, 0.95f, 1f);

            // Footer
            var footer = CreateCard("FooterBar", bgGo.transform, Sc);
            SetAnchors(footer, 0.12f, 0.08f, 0.90f, 0.16f);
            CreateIconButton(footer.transform, "Btn_SETTINGS", GetSprite("IconSettings"), new Rect(0.75f, 0.15f, 0.90f, 0.85f));
            CreateIconButton(footer.transform, "Btn_LEADERBOARD", GetSprite("IconTrophy"), new Rect(0.52f, 0.15f, 0.67f, 0.85f));
            CreateIconButton(footer.transform, "Btn_WORLD_MAP", GetSprite("IconWorld"), new Rect(0.29f, 0.15f, 0.44f, 0.85f));
            CreateIconButton(footer.transform, "Btn_GIFT", GetSprite("IconChest"), new Rect(0.06f, 0.15f, 0.21f, 0.85f));

            var verText = CreateText(bgGo.transform, "v1.0", 11, TextAlignmentOptions.Bottom, Mt, "VersionLabel");
            SetAnchors(verText, 0.30f, 0.04f, 0.70f, 0.07f);
        }

        private static void BuildLevelSelect(GameObject root)
        {
            var bd = CreatePanel("Backdrop", root.transform);
            SetAnchors(bd, 0f, 0f, 1f, 1f);
            SetImageColor(bd, Bd);

            var card = CreatePanel("Card", root.transform, GetSprite("PanelCard"));
            SetImageColor(card, Sd);
            SetAnchors(card, 0.04f, 0.06f, 0.96f, 0.94f);
            card.AddComponent<CanvasGroup>();

            var title = CreateText(card.transform, "SELECT LEVEL", 40, TextAlignmentOptions.Center, Tod, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.10f, 0.88f, 0.90f, 0.96f);

            var infoPanel = CreatePanel("InfoPanel", card.transform);
            SetImageColor(infoPanel, new Color(0.08f, 0.10f, 0.14f));
            SetAnchors(infoPanel, 0.06f, 0.80f, 0.94f, 0.86f);

            var worldLabel = CreateText(infoPanel.transform, "WORLD 1", 16, TextAlignmentOptions.MidlineLeft, Ac, "WorldLabel");
            SetAnchors(worldLabel, 0.04f, 0f, 0.30f, 1f);
            var progLabel = CreateText(infoPanel.transform, "0 / 0", 16, TextAlignmentOptions.MidlineRight, Mtd, "ProgressLabel");
            SetAnchors(progLabel, 0.70f, 0f, 0.96f, 1f);

            var barBg = CreatePanel("ProgressBarBg", infoPanel.transform, GetSprite("HUDProgressBackground"));
            SetAnchors(barBg, 0.32f, 0.30f, 0.68f, 0.70f);
            SetImageColor(barBg, new Color(0.20f, 0.22f, 0.28f));
            var barFill = CreatePanel("ProgressBarFill", barBg.transform, GetSprite("HUDProgressFill"));
            SetAnchors(barFill, 0f, 0f, 0f, 1f);
            SetImageColor(barFill, Ac);

            var gridGo = new GameObject("LevelGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(card.transform, false);
            SetAnchors(gridGo, 0.04f, 0.10f, 0.96f, 0.76f);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130f, 58f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(8, 8, 8, 8);

            for (int i = 1; i <= 12; i++)
                CreateButton($"LEVEL {i}", gridGo.transform, 130, 58, $"Btn_Level_{i}", GetSprite("ButtonPrimary"));

            var backBtn = CreateButton("BACK", card.transform, 140, 44, "Btn_BACK", GetSprite("ButtonSecondary"));
            ApplyOutlineStyle(backBtn);
            SetAnchors(backBtn, 0.04f, 0.02f, 0.20f, 0.08f);
        }

        private static void BuildHUD(GameObject root)
        {
            root.AddComponent<CanvasGroup>();

            var topBar = CreatePanel("TopBar", root.transform);
            SetAnchors(topBar, 0f, 0.90f, 1f, 0.98f);

            var lvlPill = CreateCard("LevelPill", topBar.transform, Sc);
            SetAnchors(lvlPill, 0.04f, 0.15f, 0.32f, 0.85f);
            var lvlText = CreateText(lvlPill.transform, "Level 1", 14, TextAlignmentOptions.Center, Tc, "LevelText");
            lvlText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var movesPill = CreateCard("MovesPill", topBar.transform, Sc);
            SetAnchors(movesPill, 0.35f, 0.15f, 0.58f, 0.85f);
            var movesText = CreateText(movesPill.transform, "Moves: 0", 13, TextAlignmentOptions.Center, Tc, "MovesText");
            movesText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            var timerText = CreateText(movesPill.transform, "", 13, TextAlignmentOptions.Center, Wc, "TimerText");
            timerText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            timerText.SetActive(false);

            var coinsPill = CreateCard("CoinsPill", topBar.transform, Sc);
            SetAnchors(coinsPill, 0.61f, 0.15f, 0.82f, 0.85f);
            CreateSpriteImage(coinsPill.transform, "CoinIcon", GetSprite("IconCoin"), new Rect(0.05f, 0.15f, 0.30f, 0.85f));
            var coinText = CreateText(coinsPill.transform, "0", 14, TextAlignmentOptions.MidlineLeft, Tc, "CoinsText");
            coinText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(coinText, 0.40f, 0f, 0.95f, 1f);

            var pauseBtn = CreateIconButton(topBar.transform, "Btn_PAUSE", GetSprite("IconPause"), new Rect(0.85f, 0.15f, 0.96f, 0.85f));
            SetImageColor(pauseBtn, Sc);

            var instr = CreateText(root.transform, "", 14, TextAlignmentOptions.Center, Tc, "InstructionText");
            instr.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(instr, 0.05f, 0.82f, 0.95f, 0.88f);

            var actionBar = CreatePanel("ActionBar", root.transform);
            SetAnchors(actionBar, 0f, 0.05f, 1f, 0.18f);

            CreateActionButton(actionBar.transform, "Btn_UNDO", GetSprite("IconUndo"), new Vector2(0.22f, 0.5f), 48f);
            CreateActionButton(actionBar.transform, "Btn_HINT", GetSprite("IconHint"), new Vector2(0.50f, 0.5f), 48f);
            CreateActionButton(actionBar.transform, "Btn_RESTART", GetSprite("IconPlay"), new Vector2(0.78f, 0.5f), 48f);
            CreateActionButton(actionBar.transform, "Btn_GUIDE", null, new Vector2(0.06f, 0.5f), 44f, isGuide: true);
        }

        private static void CreateActionButton(Transform parent, string name, Sprite icon,
            Vector2 anchor, float size, bool isGuide = false)
        {
            var container = new GameObject(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var cr = container.GetComponent<RectTransform>();
            cr.anchorMin = anchor; cr.anchorMax = anchor;
            cr.anchoredPosition = Vector2.zero;
            cr.sizeDelta = new Vector2(size + 24f, size + 36f);

            var btn = CreateIconButtonCore(container.transform, name + "_Button", Sc, GetSprite("ButtonIcon"));
            var br = btn.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 1f);
            br.anchorMax = new Vector2(0.5f, 1f);
            br.anchoredPosition = new Vector2(0f, -size / 2f);
            br.sizeDelta = new Vector2(size, size);

            if (isGuide)
            {
                var gl = CreateText(btn.transform, "?", 24, TextAlignmentOptions.Center, Tc, "GuideIconText");
                gl.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                SetAnchors(gl, 0.15f, 0.15f, 0.85f, 0.85f);
            }
            else if (icon != null)
            {
                var iconGo = CreateSpriteImage(btn.transform, "IconImage", icon, new Rect(0.2f, 0.2f, 0.8f, 0.8f));
            }

            var label = CreateText(container.transform, name.Replace("Btn_", ""), 11, TextAlignmentOptions.Top, Tc, "Label");
            label.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(label, 0f, 0f, 1f, 0.28f);

            btn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            btn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
        }

        private static void BuildPause(GameObject root)
        {
            SetRootImage(root, OvH, true);

            var card = CreateCard("Card", root.transform, Sc);
            SetAnchors(card, 0.12f, 0.22f, 0.88f, 0.78f);
            if (card.GetComponent<CanvasGroup>() == null) card.AddComponent<CanvasGroup>();

            var title = CreateText(card.transform, "PAUSED", 30, TextAlignmentOptions.Center, Tc, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.05f, 0.80f, 0.95f, 0.92f);
            var sub = CreateText(card.transform, "Take a moment", 16, TextAlignmentOptions.Center, Mt, "Subtitle");
            SetAnchors(sub, 0.05f, 0.70f, 0.95f, 0.78f);
            var prog = CreateText(card.transform, "", 14, TextAlignmentOptions.Center, Mt, "ProgressLabel");
            SetAnchors(prog, 0.05f, 0.62f, 0.95f, 0.68f);

            var resumeBtn = CreateButton("RESUME", card.transform, 240f, 52f, "Btn_RESUME", GetSprite("ButtonPrimary"));
            ApplyPrimaryStyle(resumeBtn);
            SetAnchors(resumeBtn, 0.12f, 0.44f, 0.88f, 0.54f);

            var restartBtn = CreateButton("RESTART", card.transform, 110f, 44f, "Btn_RESTART", GetSprite("ButtonSuccess"));
            ApplyAccentStyle(restartBtn);
            SetAnchors(restartBtn, 0.12f, 0.30f, 0.48f, 0.39f);

            var quitBtn = CreateButton("MENU", card.transform, 110f, 44f, "Btn_QUIT", GetSprite("ButtonSecondary"));
            ApplyOutlineStyle(quitBtn);
            SetAnchors(quitBtn, 0.52f, 0.30f, 0.88f, 0.39f);

            var soundPanel = CreatePanel("SoundPanel", card.transform);
            SetImageColor(soundPanel, Ac);
            SetAnchors(soundPanel, 0.12f, 0.12f, 0.88f, 0.22f);

            CreateIconButton(soundPanel.transform, "Btn_SETTINGS", GetSprite("IconSettings"), new Rect(0.70f, 0.15f, 0.90f, 0.85f));
        }

        private static void BuildWin(GameObject root)
        {
            SetRootImage(root, OvM, true);

            var card = CreateCard("Card", root.transform, Sd);
            SetAnchors(card, 0.08f, 0.12f, 0.92f, 0.88f);
            card.AddComponent<CanvasGroup>();

            var accentBar = CreatePanel("AccentBar", card.transform);
            SetAnchors(accentBar, 0.10f, 0.82f, 0.90f, 0.84f);
            SetImageColor(accentBar, Scs);

            CreateDisplayText(card.transform, "YOU WIN!", 48, Scs, "Title", new Rect(0.12f, 0.68f, 0.76f, 0.12f));
            CreateDisplayText(card.transform, "LEVEL 1", 72, Tc, "LevelText", new Rect(0.08f, 0.54f, 0.84f, 0.12f));

            var starRow = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starRow.transform.SetParent(card.transform, false);
            SetAnchors(starRow, 0.15f, 0.38f, 0.85f, 0.52f);
            var hlg = starRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false; hlg.childControlHeight = false;

            var starSprite = GetSprite("IconStarEmpty");
            for (int i = 0; i < 3; i++)
            {
                var starGo = new GameObject($"Star{i + 1}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(starRow.transform, false);
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
                var si = starGo.GetComponent<Image>();
                si.sprite = starSprite;
                si.color = Color.white;
                si.preserveAspect = true;
            }

            var movesText = CreateText(card.transform, "", 20, TextAlignmentOptions.Center, Mtd, "MovesText");
            SetAnchors(movesText, 0.12f, 0.30f, 0.88f, 0.36f);
            var bestText = CreateText(card.transform, "", 16, TextAlignmentOptions.Center, Mtd, "BestScoreText");
            SetAnchors(bestText, 0.12f, 0.26f, 0.88f, 0.30f);

            var rewardRow = new GameObject("RewardRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rewardRow.transform.SetParent(card.transform, false);
            SetAnchors(rewardRow, 0.10f, 0.20f, 0.90f, 0.28f);
            var rhlg = rewardRow.GetComponent<HorizontalLayoutGroup>();
            rhlg.spacing = 8f; rhlg.childAlignment = TextAnchor.MiddleCenter;
            rhlg.childControlWidth = false; rhlg.childControlHeight = false;

            CreateSpriteImage(rewardRow.transform, "CoinIcon", GetSprite("IconCoin"), new Rect(0, 0, 28, 28));
            var coinVal = CreateText(rewardRow.transform, "+0", 18, TextAlignmentOptions.MidlineLeft, CoinC, "CoinsValue");
            coinVal.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            coinVal.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 32f);

            CreateSpriteImage(rewardRow.transform, "XPIcon", GetSprite("IconXP"), new Rect(0, 0, 28, 28));
            var xpVal = CreateText(rewardRow.transform, "+0", 18, TextAlignmentOptions.MidlineLeft, Scs, "XPValue");
            xpVal.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            xpVal.GetComponent<RectTransform>().sizeDelta = new Vector2(60f, 32f);

            var nextBtn = CreateButton("NEXT LEVEL", card.transform, 300, 60, "Btn_NEXT LEVEL", GetSprite("ButtonSuccess"));
            ApplySuccessStyle(nextBtn);
            SetAnchors(nextBtn, 0.20f, 0.10f, 0.80f, 0.18f);

            var quitBtn = CreateButton("MAIN MENU", card.transform, 300, 46, "Btn_MAIN MENU", GetSprite("ButtonSecondary"));
            ApplyTextButtonStyle(quitBtn);
            SetAnchors(quitBtn, 0.20f, 0.04f, 0.80f, 0.09f);
        }

        private static void BuildGameOver(GameObject root)
        {
            SetRootImage(root, OvH, true);

            var card = CreateCard("Card", root.transform, Sd);
            SetAnchors(card, 0.10f, 0.16f, 0.90f, 0.84f);
            card.AddComponent<CanvasGroup>();

            var accent = CreatePanel("AccentBar", card.transform);
            SetAnchors(accent, 0.12f, 0.76f, 0.88f, 0.78f);
            SetImageColor(accent, Dc);

            CreateDisplayText(card.transform, "GAME OVER", 52, Dc, "Title", new Rect(0.12f, 0.62f, 0.76f, 0.12f));
            var lvlText = CreateText(card.transform, "", 20, TextAlignmentOptions.Center, Mtd, "LevelText");
            SetAnchors(lvlText, 0.12f, 0.54f, 0.88f, 0.60f);
            var msgText = CreateText(card.transform, "Keep trying — you'll get it!", 18, TextAlignmentOptions.Center, Tod, "Message");
            SetAnchors(msgText, 0.12f, 0.42f, 0.88f, 0.50f);
            var progText = CreateText(card.transform, "", 16, TextAlignmentOptions.Center, Mtd, "ProgressText");
            SetAnchors(progText, 0.12f, 0.36f, 0.88f, 0.40f);

            var restartBtn = CreateButton("TRY AGAIN", card.transform, 280, 60, "Btn_RESTART", GetSprite("ButtonPrimary"));
            ApplyPrimaryStyle(restartBtn);
            SetAnchors(restartBtn, 0.22f, 0.22f, 0.78f, 0.32f);

            var quitBtn = CreateButton("MAIN MENU", card.transform, 280, 48, "Btn_MAIN MENU", GetSprite("ButtonSecondary"));
            ApplyTextButtonStyle(quitBtn);
            SetAnchors(quitBtn, 0.22f, 0.10f, 0.78f, 0.18f);
        }

        private static void BuildSettings(GameObject root)
        {
            SetRootImage(root, OvM, true);

            var card = CreateCard("Card", root.transform, Sd);
            SetAnchors(card, 0.06f, 0.06f, 0.94f, 0.94f);
            card.AddComponent<CanvasGroup>();

            var title = CreateText(card.transform, "SETTINGS", 40, TextAlignmentOptions.Center, Tod, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.10f, 0.90f, 0.90f, 0.98f);

            float rowY = 0.82f;
            float rowStep = 0.065f;

            void CreateToggleRow(string name, string label)
            {
                var lbl = CreateText(card.transform, label, 16, TextAlignmentOptions.MidlineLeft, Tod, $"{name}Label");
                SetAnchors(lbl, 0.08f, rowY - 0.025f, 0.46f, rowY + 0.025f);

                // Proper Toggle with Background + Checkmark
                var toggleGo = new GameObject($"{name}Toggle", typeof(RectTransform));
                toggleGo.transform.SetParent(card.transform, false);
                SetAnchors(toggleGo, 0.72f, rowY - 0.028f, 0.92f, rowY + 0.028f);

                var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(toggleGo.transform, false);
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                var bgImg = bg.GetComponent<Image>();
                bgImg.color = new Color(0.20f, 0.22f, 0.28f);
                bgImg.sprite = GetSprite("PanelCard");

                var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkmark.transform.SetParent(bg.transform, false);
                var cmRt = checkmark.GetComponent<RectTransform>();
                cmRt.anchorMin = new Vector2(0.1f, 0.1f);
                cmRt.anchorMax = new Vector2(0.9f, 0.9f);
                cmRt.offsetMin = Vector2.zero; cmRt.offsetMax = Vector2.zero;
                var cmImg = checkmark.GetComponent<Image>();
                cmImg.sprite = GetSprite("IconCheck");
                cmImg.color = Ac;

                var toggle = toggleGo.AddComponent<Toggle>();
                toggle.targetGraphic = bgImg;
                toggle.graphic = cmImg;
                toggle.isOn = true;

                rowY -= rowStep;
            }

            CreateToggleRow("Music", "MUSIC");
            CreateToggleRow("SFX", "SFX");
            CreateToggleRow("Haptic", "HAPTIC FEEDBACK");
            CreateToggleRow("Motion", "REDUCE MOTION");
            CreateToggleRow("Big", "BIG BUTTONS");

            rowY -= rowStep * 0.8f;
            var cbLabel = CreateText(card.transform, "COLOR BLIND MODE", 16, TextAlignmentOptions.MidlineLeft, Tod, "CbLabel");
            SetAnchors(cbLabel, 0.08f, rowY - 0.02f, 0.44f, rowY + 0.02f);
            var sliderGo = new GameObject("ColorBlindSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderGo.transform.SetParent(card.transform, false);
            SetAnchors(sliderGo, 0.48f, rowY - 0.025f, 0.92f, rowY + 0.025f);
            sliderGo.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);

            rowY -= rowStep;
            var langLabel = CreateText(card.transform, "LANGUAGE", 16, TextAlignmentOptions.MidlineLeft, Tod, "LangLabel");
            SetAnchors(langLabel, 0.08f, rowY - 0.02f, 0.40f, rowY + 0.02f);
            var ddGo = new GameObject("LanguageDropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            ddGo.transform.SetParent(card.transform, false);
            SetAnchors(ddGo, 0.44f, rowY - 0.028f, 0.92f, rowY + 0.028f);
            ddGo.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);

            rowY -= rowStep * 1.5f;
            var adsBtn = CreateButton("REMOVE ADS", card.transform, 220, 48, "Btn_REMOVE ADS", GetSprite("ButtonPrimary"));
            ApplyPrimaryStyle(adsBtn);
            SetAnchors(adsBtn, 0.08f, rowY - 0.03f, 0.46f, rowY + 0.03f);
            var restoreBtn = CreateButton("RESTORE", card.transform, 180, 48, "Btn_RESTORE", GetSprite("ButtonSecondary"));
            ApplyOutlineStyle(restoreBtn);
            SetAnchors(restoreBtn, 0.52f, rowY - 0.03f, 0.92f, rowY + 0.03f);

            var closeBtn = CreateButton("CLOSE", card.transform, 140, 42, "Btn_CLOSE", GetSprite("ButtonClose"));
            ApplyOutlineStyle(closeBtn);
            SetAnchors(closeBtn, 0.04f, 0.02f, 0.20f, 0.07f);
        }

        private static void BuildDailyReward(GameObject root)
        {
            SetRootImage(root, OvM, true);

            var card = CreateCard("Card", root.transform, Sd);
            SetAnchors(card, 0.08f, 0.14f, 0.92f, 0.86f);
            card.AddComponent<CanvasGroup>();

            var accent = CreatePanel("AccentBar", card.transform);
            SetAnchors(accent, 0.08f, 0.80f, 0.92f, 0.82f);
            SetImageColor(accent, Ac);

            CreateDisplayText(card.transform, "DAILY REWARD", 36, Ac, "Title", new Rect(0.05f, 0.68f, 0.90f, 0.10f));

            var dayBg = CreatePanel("DayCircle", card.transform);
            SetAnchors(dayBg, 0.35f, 0.44f, 0.65f, 0.66f);
            SetImageColor(dayBg, new Color(0.12f, 0.14f, 0.20f));

            CreateDisplayText(card.transform, "Day 1", 52, Tod, "Day", new Rect(0.20f, 0.46f, 0.60f, 0.18f));
            var streakText = CreateText(card.transform, "", 14, TextAlignmentOptions.Center, Mtd, "Streak");
            SetAnchors(streakText, 0.15f, 0.38f, 0.85f, 0.42f);
            var rewardText = CreateText(card.transform, "", 22, TextAlignmentOptions.Center, Ac, "Reward");
            SetAnchors(rewardText, 0.12f, 0.30f, 0.88f, 0.36f);

            var claimBtn = CreateButton("CLAIM", card.transform, 300, 60, "Btn_CLAIM", GetSprite("ButtonPrimary"));
            ApplyAccentStyle(claimBtn);
            SetAnchors(claimBtn, 0.18f, 0.16f, 0.82f, 0.26f);

            var closeBtn = CreateButton("CLOSE", card.transform, 120, 38, "Btn_CLOSE", GetSprite("ButtonClose"));
            ApplyTextButtonStyle(closeBtn);
            SetAnchors(closeBtn, 0.40f, 0.04f, 0.60f, 0.10f);
        }

        private static void BuildChestPopup(GameObject root)
        {
            SetRootImage(root, OvH, true);

            var card = CreatePanel("Card", root.transform, GetSprite("PanelPopup"));
            SetAnchors(card, 0.08f, 0.15f, 0.92f, 0.85f);
            SetImageColor(card, Pnl);

            CreateDisplayText(card.transform, "CHEST REWARDS", 34, Ac, "Title", new Rect(0.05f, 0.80f, 0.90f, 0.12f));

            var bronze = CreateText(card.transform, "Bronze: x0  (+0 XP)", 20, TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.5f, 0.2f), "Bronze");
            SetAnchors(bronze, 0.08f, 0.62f, 0.92f, 0.70f);
            var silver = CreateText(card.transform, "Silver: x0  (+0 XP)", 20, TextAlignmentOptions.MidlineLeft, new Color(0.75f, 0.75f, 0.80f), "Silver");
            SetAnchors(silver, 0.08f, 0.52f, 0.92f, 0.60f);
            var gold = CreateText(card.transform, "Gold: x0  (+0 XP)", 20, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.84f, 0f), "Gold");
            SetAnchors(gold, 0.08f, 0.42f, 0.92f, 0.50f);
            var diamond = CreateText(card.transform, "Diamond: x0  (+0 XP)", 20, TextAlignmentOptions.MidlineLeft, DiamC, "Diamond");
            SetAnchors(diamond, 0.08f, 0.32f, 0.92f, 0.40f);
            var totalXp = CreateText(card.transform, "Total XP: +0", 22, TextAlignmentOptions.Center, Ac, "TotalXp");
            totalXp.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(totalXp, 0.15f, 0.22f, 0.85f, 0.30f);

            var claimBtn = CreateButton("CLAIM ALL", card.transform, 280, 60, "ClaimAll", GetSprite("ButtonPrimary"));
            SetAnchors(claimBtn, 0.15f, 0.10f, 0.85f, 0.20f);
            var closeBtn = CreateButton("CLOSE", card.transform, 120, 38, "Close", GetSprite("ButtonClose"));
            ApplySecondaryStyle(closeBtn);
            SetAnchors(closeBtn, 0.40f, 0.02f, 0.60f, 0.09f);
        }

        private static void BuildOnboarding(GameObject root)
        {
            var rootGo = new GameObject("OnboardingRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(root.transform, false);
            SetAnchors(rootGo, 0f, 0f, 1f, 1f);
            rootGo.GetComponent<CanvasGroup>().alpha = 1f;
            rootGo.GetComponent<CanvasGroup>().blocksRaycasts = true;

            var overlay = CreatePanel("Overlay", rootGo.transform);
            SetAnchors(overlay, 0f, 0f, 1f, 1f);
            SetImageColor(overlay, OvH);

            var card = CreatePanel("Card", rootGo.transform, GetSprite("PanelPopup"));
            SetAnchors(card, 0.08f, 0.14f, 0.92f, 0.86f);
            SetImageColor(card, Pnl);

            var progressBar = CreatePanel("ProgressBar", card.transform);
            SetAnchors(progressBar, 0.30f, 0.90f, 0.70f, 0.94f);
            SetImageColor(progressBar, Sc);

            for (int i = 0; i < 4; i++)
            {
                var dot = new GameObject($"Dot{i}", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(progressBar.transform, false);
                var dr = dot.GetComponent<RectTransform>();
                dr.sizeDelta = new Vector2(18f, 18f);
                float cx = (i + 0.5f) * 0.25f;
                dr.anchorMin = new Vector2(cx, 0.5f);
                dr.anchorMax = new Vector2(cx, 0.5f);
                dr.offsetMin = new Vector2(-9f, -9f);
                dr.offsetMax = new Vector2(9f, 9f);
                dot.GetComponent<Image>().color = i == 0 ? Ac : new Color(0.60f, 0.62f, 0.68f);
            }

            var title = CreateText(card.transform, "", 36, TextAlignmentOptions.Center, Tc, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.08f, 0.72f, 0.60f, 0.88f);
            var body = CreateText(card.transform, "", 22, TextAlignmentOptions.Center, Mt, "Body");
            body.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Overflow;
            SetAnchors(body, 0.10f, 0.24f, 0.90f, 0.58f);

            var buttonRow = CreatePanel("ButtonRow", card.transform);
            SetAnchors(buttonRow, 0.10f, 0.16f, 0.90f, 0.22f);

            var skipBtn = CreateButton("SKIP", buttonRow.transform, Bw * 0.55f, SbH, "SkipButton", GetSprite("ButtonSecondary"));
            ApplyTextButtonStyle(skipBtn);
            var sr = skipBtn.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0f); sr.anchorMax = new Vector2(0.35f, 1f);
            sr.offsetMin = Vector2.zero; sr.offsetMax = Vector2.zero;

            var nextBtn = CreateButton("NEXT", buttonRow.transform, Bw * 0.55f, SbH, "NextButton", GetSprite("ButtonPrimary"));
            ApplyPrimaryStyle(nextBtn);
            var nr = nextBtn.GetComponent<RectTransform>();
            nr.anchorMin = new Vector2(0.42f, 0f); nr.anchorMax = new Vector2(1f, 1f);
            nr.offsetMin = Vector2.zero; nr.offsetMax = Vector2.zero;
        }

        private static void BuildParentalGate(GameObject root)
        {
            SetRootImage(root, OvH, true);

            var card = CreatePanel("Card", root.transform, GetSprite("PanelPopup"));
            SetAnchors(card, 0.10f, 0.20f, 0.90f, 0.80f);
            SetImageColor(card, Pnl);

            var title = CreateText(card.transform, "Parental Verification", 34, TextAlignmentOptions.Center, Tc, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.05f, 0.72f, 0.95f, 0.88f);
            var question = CreateText(card.transform, "", 22, TextAlignmentOptions.Center, Ac, "Question");
            question.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(question, 0.08f, 0.44f, 0.92f, 0.64f);
            var error = CreateText(card.transform, "", 18, TextAlignmentOptions.Center, Dc, "Error");
            SetAnchors(error, 0.15f, 0.36f, 0.85f, 0.41f);

            var acceptBtn = CreateButton("ACCEPT & CONTINUE", card.transform, 320, 64, "Accept", GetSprite("ButtonPrimary"));
            SetAnchors(acceptBtn, 0.20f, 0.22f, 0.80f, 0.34f);
            var termsBtn = CreateButton("Terms of Service", card.transform, 180, 40, "Terms", GetSprite("ButtonSecondary"));
            ApplySecondaryStyle(termsBtn);
            SetAnchors(termsBtn, 0.08f, 0.10f, 0.48f, 0.18f);
            var privacyBtn = CreateButton("Privacy Policy", card.transform, 180, 40, "Privacy", GetSprite("ButtonSecondary"));
            ApplySecondaryStyle(privacyBtn);
            SetAnchors(privacyBtn, 0.52f, 0.10f, 0.92f, 0.18f);
        }

        private static void BuildWorldMap(GameObject root)
        {
            var card = CreatePanel("Card", root.transform, GetSprite("PanelCard"));
            SetAnchors(card, 0.08f, 0.18f, 0.92f, 0.82f);
            SetImageColor(card, Pnl);

            var title = CreateText(card.transform, "WORLD MAP", 40, TextAlignmentOptions.Center, Tc, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.08f, 0.68f, 0.92f, 0.84f);
            var body = CreateText(card.transform, "Explore the kingdoms ahead.", 22, TextAlignmentOptions.Center, Mt, "Body");
            SetAnchors(body, 0.10f, 0.30f, 0.90f, 0.52f);

            CreateIconButton(root.transform, "Btn_BACK", GetSprite("ButtonBack"), new Rect(0.04f, 0.90f, 0.15f, 0.98f));
        }

        private static void BuildMechanicGuide(GameObject root)
        {
            var overlay = CreatePanel("Overlay", root.transform);
            SetAnchors(overlay, 0f, 0f, 1f, 1f);
            SetImageColor(overlay, OvH);

            var card = CreatePanel("Card", root.transform, GetSprite("PanelPopup"));
            SetAnchors(card, 0.05f, 0.08f, 0.95f, 0.88f);
            SetImageColor(card, Pnl);

            var title = CreateText(card.transform, "MECHANICS GUIDE", 28, TextAlignmentOptions.Center, Ac, "Title");
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            SetAnchors(title, 0.08f, 0.88f, 0.92f, 0.97f);

            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(card.transform, false);
            SetAnchors(scrollGo, 0.04f, 0.06f, 0.96f, 0.84f);
            scrollGo.GetComponent<Image>().color = Color.clear;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            SetAnchors(viewportGo, 0f, 0f, 1f, 1f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;
            viewportGo.GetComponent<Image>().color = Color.white;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var cr = contentGo.GetComponent<RectTransform>();
            cr.anchorMin = new Vector2(0f, 1f); cr.anchorMax = new Vector2(1f, 1f);
            cr.pivot = new Vector2(0.5f, 1f);
            cr.sizeDelta = new Vector2(0f, 1200f);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = cr;
            scroll.horizontal = false;

            var mechData = MechanicGuideView.AllMechanics;
            for (int i = 0; i < mechData.Length; i++)
            {
                var entry = mechData[i];
                var row = new GameObject($"Entry_{i}", typeof(RectTransform));
                row.transform.SetParent(contentGo.transform, false);
                var rr = row.GetComponent<RectTransform>();
                rr.anchorMin = new Vector2(0f, 1f); rr.anchorMax = new Vector2(1f, 1f);
                rr.pivot = new Vector2(0.5f, 1f);
                rr.anchoredPosition = new Vector2(0f, -(i * 110f + 10f));
                rr.sizeDelta = new Vector2(0f, 102f);

                var symGo = new GameObject("Symbol", typeof(RectTransform), typeof(Image));
                symGo.transform.SetParent(row.transform, false);
                var symR = symGo.GetComponent<RectTransform>();
                symR.anchorMin = new Vector2(0f, 0.1f); symR.anchorMax = new Vector2(0f, 0.9f);
                symR.sizeDelta = new Vector2(52f, 0f);
                symR.anchoredPosition = new Vector2(38f, 0f);
                symGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.20f, 1f);
                symGo.GetComponent<Image>().sprite = GetSprite("PanelCard");

                CreateText(symGo.transform, entry.Symbol, 26, TextAlignmentOptions.Center, entry.SymbolColor, "SymbolText");
                var nameText = CreateText(row.transform, entry.NameFallback, 18, TextAlignmentOptions.MidlineLeft, Tc, "Name");
                nameText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
                SetAnchors(nameText, 0.14f, 0.55f, 0.96f, 0.95f);
                var descText = CreateText(row.transform, entry.DescFallback, 13, TextAlignmentOptions.TopLeft, Mt, "Desc");
                descText.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Overflow;
                SetAnchors(descText, 0.14f, 0.05f, 0.96f, 0.55f);
            }

            var closeBtn = CreateButton("CLOSE", card.transform, 200, 44, "Btn_Close", GetSprite("ButtonPrimary"));
            ApplyPrimaryStyle(closeBtn);
            SetAnchors(closeBtn, 0.30f, 0.01f, 0.70f, 0.055f);
        }

        // ══════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════

        private static void SetRootImage(GameObject root, Color color, bool raycastTarget)
        {
            var img = root.GetComponent<Image>();
            if (img != null) { img.color = color; img.raycastTarget = raycastTarget; }
        }

        private static void SetImageColor(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private static Sprite GetSprite(string key)
        {
            if (_spriteLib != null)
            {
                var s = _spriteLib.GetSprite(key);
                if (s != null) return s;
            }
            // Fallback: try Resources.Load
            return Resources.Load<Sprite>($"UI/Sprites/{key}");
        }

        private static GameObject CreatePanel(string name, Transform parent, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            if (sprite != null)
            {
                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        private static GameObject CreateCard(string name, Transform parent, Color bg)
        {
            var go = CreatePanel(name, parent);
            go.GetComponent<Image>().color = bg;
            return go;
        }

        private static GameObject CreateSpriteImage(Transform parent, string name, Sprite sprite, Rect anchors)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            if (anchors.width > 1 || anchors.height > 1)
            {
                // Pixel size mode
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(anchors.width, anchors.height);
            }
            else
            {
                SetAnchors(go, anchors.xMin, anchors.yMin, anchors.xMax, anchors.yMax);
            }
            return go;
        }

        private static GameObject CreateText(Transform parent, string content, int fontSize,
            TextAlignmentOptions align, Color color, string objName)
        {
            var go = new GameObject(objName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.overflowMode = TextOverflowModes.Overflow;
            return go;
        }

        private static GameObject CreateDisplayText(Transform parent, string content, int fontSize,
            Color color, string objName, Rect anchors)
        {
            var go = CreateText(parent, content, fontSize, TextAlignmentOptions.Center, color, objName);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
            rt.anchorMax = new Vector2(anchors.xMax, anchors.yMax);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject CreateButton(string label, Transform parent, float width, float height,
            string objName, Sprite bgSprite)
        {
            var go = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var img = go.GetComponent<Image>();
            img.color = _theme.PrimaryButtonColors.NormalColor;
            if (bgSprite != null) { img.sprite = bgSprite; img.type = Image.Type.Sliced; }

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = _theme.PrimaryButtonColors.NormalColor;
            colors.highlightedColor = _theme.PrimaryButtonColors.HighlightedColor;
            colors.pressedColor = _theme.PrimaryButtonColors.PressedColor;
            colors.selectedColor = _theme.PrimaryButtonColors.SelectedColor;
            colors.disabledColor = _theme.PrimaryButtonColors.DisabledColor;
            btn.colors = colors;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero; labelRt.offsetMax = Vector2.zero;
            var lblText = labelGo.GetComponent<TextMeshProUGUI>();
            lblText.text = label;
            lblText.fontSize = BfS;
            lblText.alignment = TextAlignmentOptions.Center;
            lblText.color = Top;

            return go;
        }

        private static GameObject CreateIconButton(Transform parent, string objName, Sprite icon, Rect anchors)
        {
            var go = CreateIconButtonCore(parent, objName, _theme.IconButtonColors.NormalColor, null);
            SetAnchors(go, anchors.xMin, anchors.yMin, anchors.xMax, anchors.yMax);
            if (icon != null)
            {
                var iconGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var ii = iconGo.GetComponent<Image>();
                ii.sprite = icon;
                ii.preserveAspect = true;
                SetAnchors(iconGo, 0.15f, 0.15f, 0.85f, 0.85f);
            }
            return go;
        }

        private static GameObject CreateIconButtonCore(Transform parent, string objName, Color bg, Sprite sprite)
        {
            var go = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(IbS, IbS);

            var img = go.GetComponent<Image>();
            img.color = bg;
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = _theme.IconButtonColors.NormalColor;
            colors.highlightedColor = _theme.IconButtonColors.HighlightedColor;
            colors.pressedColor = _theme.IconButtonColors.PressedColor;
            colors.disabledColor = _theme.IconButtonColors.DisabledColor;
            btn.colors = colors;

            return go;
        }

        // ── Style appliers ──────────────────────────────────────────────

        private static void ApplyPrimaryStyle(GameObject go)
        {
            ApplyButtonColors(go, _theme.PrimaryButtonColors);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = Pc;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = Top;
        }

        private static void ApplyAccentStyle(GameObject go)
        {
            ApplyButtonColors(go, _theme.AccentButtonColors);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = Ac;
        }

        private static void ApplySuccessStyle(GameObject go)
        {
            ApplyButtonColors(go, _theme.SuccessButtonColors);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = Scs;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = Top;
        }

        private static void ApplyOutlineStyle(GameObject go)
        {
            ApplyButtonColors(go, _theme.OutlineButtonColors);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = _theme.OutlineButtonColors.NormalColor;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = Tc;
        }

        private static void ApplySecondaryStyle(GameObject go)
        {
            ApplyButtonColors(go, _theme.IconButtonColors);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = Sc;
        }

        private static void ApplyTextButtonStyle(GameObject go)
        {
            ApplyButtonColors(go, _theme.TextButtonColors);
            var img = go.GetComponent<Image>();
            if (img != null) img.color = _theme.TextButtonColors.NormalColor;
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = Tc;
        }

        private static void ApplyButtonColors(GameObject go, ButtonColorConfig colors)
        {
            var btn = go.GetComponent<Button>();
            if (btn == null) return;
            var c = btn.colors;
            c.normalColor = colors.NormalColor;
            c.highlightedColor = colors.HighlightedColor;
            c.pressedColor = colors.PressedColor;
            c.selectedColor = colors.SelectedColor;
            c.disabledColor = colors.DisabledColor;
            btn.colors = c;
        }

        private static void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
