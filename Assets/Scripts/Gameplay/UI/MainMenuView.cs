using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(MainMenuMediator))]
    public class MainMenuView : View, IAuthoredView
    {
        public Button ContinueButton { get; private set; }
        public Button PlayButton { get; private set; }
        public Button LevelSelectButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button DailyRewardButton { get; private set; }
        public Button ChestButton { get; private set; }
        public Text VersionLabel { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Text TitleText { get; private set; }
        public Text SubtitleText { get; private set; }
        public Text PlayerLevelText { get; private set; }
        public Image PlayerLevelProgress { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private GameObject _continueBtn, _playBtn, _lvlBtn, _dailyBtn, _settingsBtn, _chestBtn;
        private GameObject _backdrop;
        private ILocalizationService _locService;
        private Button[] _allButtons;

        private void Awake()
        {
            if (transform.childCount == 0 || NeedsSelfBuild())
                BuildUI();
            else
                BindReferencesFromChildren();
        }

        private bool NeedsSelfBuild()
        {
            BindReferencesFromChildren();
            return TitleText == null;
        }

        public void BuildUI()
        {
            // Background - Full Screen Cream Panel
            var bgGo = GameUIResources.CreatePanel("Background", transform);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = GameUIResources.BgColor;

            CardGroup = bgGo.GetComponent<CanvasGroup>();
            if (CardGroup == null) CardGroup = bgGo.AddComponent<CanvasGroup>();

            // 1. TOP BAR CONTAINER (Responsive anchors)
            var topBar = new GameObject("TopBar", typeof(RectTransform));
            topBar.transform.SetParent(bgGo.transform, false);
            GameUIResources.SetAnchors(topBar.GetComponent<RectTransform>(), 0f, 0.90f, 1f, 0.98f);

            // Left circular avatar
            var avatar = GameUIResources.CreateCard("Avatar", topBar.transform);
            avatar.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            GameUIResources.SetAnchors(avatar.GetComponent<RectTransform>(), 0.04f, 0.15f, 0.14f, 0.85f);
            var avatarTextGo = GameUIResources.CreateText("C", avatar.transform, 18, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            avatarTextGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Middle Level Pill (Clicking it opens level select)
            _lvlBtn = new GameObject("Btn_LEVEL_SELECT", typeof(RectTransform), typeof(Image), typeof(Button));
            _lvlBtn.transform.SetParent(topBar.transform, false);
            GameUIResources.SetAnchors(_lvlBtn.GetComponent<RectTransform>(), 0.18f, 0.15f, 0.50f, 0.85f);
            var lvlImg = _lvlBtn.GetComponent<Image>();
            lvlImg.color = GameUIResources.SurfaceColor;
            lvlImg.sprite = GameUIResources.GetRoundedSprite();
            lvlImg.type = Image.Type.Sliced;
            _lvlBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            _lvlBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
            LevelSelectButton = _lvlBtn.GetComponent<Button>();
            GameUIResources.AddButtonEffects(LevelSelectButton);

            var lvlTextGo = GameUIResources.CreateText("Lv 1", _lvlBtn.transform, 14, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            lvlTextGo.name = "PlayerLevel";
            PlayerLevelText = lvlTextGo.GetComponent<Text>();
            PlayerLevelText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(lvlTextGo.GetComponent<RectTransform>(), 0.08f, 0f, 0.35f, 1f);

            // Level bar inside pill
            var lvlBarBg = new GameObject("LevelBarBg", typeof(RectTransform), typeof(Image));
            lvlBarBg.transform.SetParent(_lvlBtn.transform, false);
            GameUIResources.SetAnchors(lvlBarBg.GetComponent<RectTransform>(), 0.38f, 0.35f, 0.92f, 0.65f);
            lvlBarBg.GetComponent<Image>().color = GameUIResources.PanelColor;
            lvlBarBg.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            lvlBarBg.GetComponent<Image>().type = Image.Type.Sliced;

            var lvlBarFill = new GameObject("LevelBarFill", typeof(RectTransform), typeof(Image));
            lvlBarFill.transform.SetParent(lvlBarBg.transform, false);
            lvlBarFill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            lvlBarFill.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            lvlBarFill.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            lvlBarFill.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            lvlBarFill.GetComponent<Image>().color = GameUIResources.PrimaryColor; // Coral progress fill
            lvlBarFill.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            lvlBarFill.GetComponent<Image>().type = Image.Type.Sliced;
            PlayerLevelProgress = lvlBarFill.GetComponent<Image>();

            // Right Coins Pill
            var coinsPill = GameUIResources.CreateCard("CoinsPill", topBar.transform);
            GameUIResources.SetAnchors(coinsPill.GetComponent<RectTransform>(), 0.54f, 0.15f, 0.74f, 0.85f);
            var coinTextGo = GameUIResources.CreateText("🪙 0", coinsPill.transform, 14, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            coinTextGo.name = "CoinsText";
            CoinsText = coinTextGo.GetComponent<Text>();
            CoinsText.fontStyle = FontStyle.Bold;

            // Right Gems Pill
            var gemsPill = GameUIResources.CreateCard("GemsPill", topBar.transform);
            GameUIResources.SetAnchors(gemsPill.GetComponent<RectTransform>(), 0.78f, 0.15f, 0.96f, 0.85f);
            var gemsTextGo = GameUIResources.CreateText("💎 0", gemsPill.transform, 14, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            gemsTextGo.name = "DiamondsText";
            DiamondsText = gemsTextGo.GetComponent<Text>();
            DiamondsText.fontStyle = FontStyle.Bold;

            // 2. MAIN LOGO / TAGLINE
            var titleGo = GameUIResources.CreateDisplayText("<color=#2C2A44>Ring</color><color=#FFC93C>Flow</color>", bgGo.transform, 56, GameUIResources.TextColor);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.05f, 0.77f, 0.95f, 0.87f);

            var subGo = GameUIResources.CreateText("Renkleri Ayır, Zihni Dinlendir", bgGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            subGo.name = "Subtitle";
            SubtitleText = subGo.GetComponent<Text>();
            GameUIResources.SetAnchors(subGo.GetComponent<RectTransform>(), 0.05f, 0.73f, 0.95f, 0.77f);

            // 3. CENTER BOARD PREVIEW (White Card with shadow + mock poles/rings)
            var boardCard = GameUIResources.CreateCard("BoardPreviewCard", bgGo.transform);
            boardCard.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            GameUIResources.SetAnchors(boardCard.GetComponent<RectTransform>(), 0.12f, 0.44f, 0.88f, 0.71f);

            // 3 mock poles inside boardCard
            float[] poleX = { 0.25f, 0.50f, 0.75f };
            for (int i = 0; i < 3; i++)
            {
                var pole = new GameObject($"MockPole_{i}", typeof(RectTransform), typeof(Image));
                pole.transform.SetParent(boardCard.transform, false);
                var pRect = pole.GetComponent<RectTransform>();
                pRect.anchorMin = new Vector2(poleX[i] - 0.015f, 0.15f);
                pRect.anchorMax = new Vector2(poleX[i] + 0.015f, 0.85f);
                pRect.offsetMin = Vector2.zero; pRect.offsetMax = Vector2.zero;
                pole.GetComponent<Image>().color = new Color(0.82f, 0.81f, 0.75f); // wood/sand color for poles
                pole.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
                pole.GetComponent<Image>().type = Image.Type.Sliced;

                // Add mock rings on poles
                int ringsOnPole = i switch { 0 => 3, 1 => 2, 2 => 1, _ => 0 };
                for (int r = 0; r < ringsOnPole; r++)
                {
                    var ring = new GameObject($"MockRing_{i}_{r}", typeof(RectTransform), typeof(Image));
                    ring.transform.SetParent(boardCard.transform, false);
                    var rRect = ring.GetComponent<RectTransform>();
                    float rY = 0.15f + r * 0.16f;
                    rRect.anchorMin = new Vector2(poleX[i] - 0.08f, rY);
                    rRect.anchorMax = new Vector2(poleX[i] + 0.08f, rY + 0.13f);
                    rRect.offsetMin = Vector2.zero; rRect.offsetMax = Vector2.zero;

                    var ringColor = (i, r) switch
                    {
                        (0, 0) => GameUIResources.PrimaryColor, // Coral
                        (0, 1) => GameUIResources.SuccessColor, // Teal
                        (0, 2) => GameUIResources.PrimaryColor,
                        (1, 0) => GameUIResources.AccentColor, // Sunny
                        (1, 1) => GameUIResources.SuccessColor,
                        (2, 0) => GameUIResources.AccentColor,
                        _ => GameUIResources.PrimaryColor
                    };
                    ring.GetComponent<Image>().color = ringColor;
                    ring.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
                    ring.GetComponent<Image>().type = Image.Type.Sliced;
                }
            }

            // 4. MAIN PLAY BUTTON (▶ OYNA - Bölüm X)
            _continueBtn = GameUIResources.CreateButton("▶ OYNA", bgGo.transform, 320f, 60f);
            _continueBtn.name = "Btn_CONTINUE";
            GameUIResources.SetAnchors(_continueBtn.GetComponent<RectTransform>(), 0.12f, 0.33f, 0.88f, 0.41f);
            ContinueButton = _continueBtn.GetComponent<Button>();
            GameUIResources.ApplyPrimaryStyle(_continueBtn);
            
            // Map PlayButton to a separate inactive button to avoid double onClick triggers on transition
            var dummyPlayGo = new GameObject("Btn_PLAY_DUMMY", typeof(RectTransform), typeof(Button));
            dummyPlayGo.transform.SetParent(bgGo.transform, false);
            dummyPlayGo.SetActive(false);
            _playBtn = dummyPlayGo;
            PlayButton = dummyPlayGo.GetComponent<Button>();

            // 5. DAILY CHALLENGE & EVENTS SIDE-BY-SIDE PANELS
            // Daily Challenge (Left)
            _dailyBtn = new GameObject("Btn_DAILY", typeof(RectTransform), typeof(Image), typeof(Button));
            _dailyBtn.transform.SetParent(bgGo.transform, false);
            GameUIResources.SetAnchors(_dailyBtn.GetComponent<RectTransform>(), 0.12f, 0.20f, 0.48f, 0.30f);
            var dailyImg = _dailyBtn.GetComponent<Image>();
            dailyImg.color = GameUIResources.SurfaceColor;
            dailyImg.sprite = GameUIResources.GetRoundedSprite();
            dailyImg.type = Image.Type.Sliced;
            _dailyBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            _dailyBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
            DailyRewardButton = _dailyBtn.GetComponent<Button>();
            GameUIResources.AddButtonEffects(DailyRewardButton);

            var dailyIcon = GameUIResources.CreateText("📅", _dailyBtn.transform, 16, TextAnchor.MiddleLeft, GameUIResources.DangerColor); // Pink/berry color
            GameUIResources.SetAnchors(dailyIcon.GetComponent<RectTransform>(), 0.08f, 0f, 0.28f, 1f);
            var dailyLabel = GameUIResources.CreateText("GÜNLÜK\nMeydan Okuma", _dailyBtn.transform, 11, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            dailyLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(dailyLabel.GetComponent<RectTransform>(), 0.32f, 0f, 0.95f, 1f);

            // Event (Right)
            _chestBtn = new GameObject("Btn_CHEST", typeof(RectTransform), typeof(Image), typeof(Button));
            _chestBtn.transform.SetParent(bgGo.transform, false);
            GameUIResources.SetAnchors(_chestBtn.GetComponent<RectTransform>(), 0.52f, 0.20f, 0.90f, 0.30f);
            var eventImg = _chestBtn.GetComponent<Image>();
            eventImg.color = GameUIResources.SurfaceColor;
            eventImg.sprite = GameUIResources.GetRoundedSprite();
            eventImg.type = Image.Type.Sliced;
            _chestBtn.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            _chestBtn.GetComponent<Shadow>().effectDistance = new Vector2(0f, -2f);
            ChestButton = _chestBtn.GetComponent<Button>();
            GameUIResources.AddButtonEffects(ChestButton);

            var eventIcon = GameUIResources.CreateText("🎁", _chestBtn.transform, 16, TextAnchor.MiddleLeft, GameUIResources.SuccessColor); // Teal color
            GameUIResources.SetAnchors(eventIcon.GetComponent<RectTransform>(), 0.08f, 0f, 0.28f, 1f);
            var eventLabel = GameUIResources.CreateText("ETKİNLİK\n2g 14s kaldı", _chestBtn.transform, 11, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            eventLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(eventLabel.GetComponent<RectTransform>(), 0.32f, 0f, 0.95f, 1f);

            // 6. BOTTOM FOOTER PANEL (White rounded container)
            var footer = GameUIResources.CreateCard("FooterBar", bgGo.transform);
            footer.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            GameUIResources.SetAnchors(footer.GetComponent<RectTransform>(), 0.12f, 0.08f, 0.90f, 0.16f);

            // settings (gear button inside footer)
            _settingsBtn = GameUIResources.CreateIconButton("⚙", footer.transform, 36f);
            _settingsBtn.name = "Btn_SETTINGS";
            SettingsButton = _settingsBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_settingsBtn.GetComponent<RectTransform>(), 0.75f, 0.15f, 0.90f, 0.85f);
            _settingsBtn.GetComponent<Image>().color = Color.clear;
            _settingsBtn.GetComponentInChildren<Text>().color = GameUIResources.TextColor;

            // leaderboard button inside footer
            var leaderBtn = GameUIResources.CreateIconButton("🏆", footer.transform, 36f);
            leaderBtn.name = "Btn_LEADERBOARD";
            GameUIResources.SetAnchors(leaderBtn.GetComponent<RectTransform>(), 0.42f, 0.15f, 0.58f, 0.85f);
            leaderBtn.GetComponent<Image>().color = Color.clear;
            leaderBtn.GetComponentInChildren<Text>().color = GameUIResources.TextColor;

            // Gift button inside footer
            var giftBtn = GameUIResources.CreateIconButton("🎁", footer.transform, 36f);
            giftBtn.name = "Btn_GIFT";
            GameUIResources.SetAnchors(giftBtn.GetComponent<RectTransform>(), 0.10f, 0.15f, 0.25f, 0.85f);
            giftBtn.GetComponent<Image>().color = Color.clear;
            giftBtn.GetComponentInChildren<Text>().color = GameUIResources.TextColor;

            // 7. VERSION & ADS AREA
            var verGo = GameUIResources.CreateText("v1.0", bgGo.transform, 11, TextAnchor.LowerCenter, GameUIResources.MutedText);
            verGo.name = "Version";
            VersionLabel = verGo.GetComponent<Text>();
            GameUIResources.SetAnchors(verGo.GetComponent<RectTransform>(), 0.30f, 0.04f, 0.70f, 0.07f);

            var adsTextGo = GameUIResources.CreateText("BANNER REKLAM ALANI", bgGo.transform, 10, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            adsTextGo.name = "AdsAreaText";
            GameUIResources.SetAnchors(adsTextGo.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.04f);

            // Cache buttons
            _allButtons = new[] { ContinueButton, PlayButton, LevelSelectButton, DailyRewardButton, ChestButton, SettingsButton };
        }

        private GameObject CreateMenuButton(Transform parent, string name, string label, float w, float h,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = GameUIResources.CreateButton(label, parent, w, h);
            go.name = name;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin - new Vector2(w / 2160f, h / 3840f / 2f);
            rect.anchorMax = anchorMin + new Vector2(w / 2160f, h / 3840f / 2f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return go;
        }

        public void UpdateCoins(int coins)
        {
            if (CoinsText != null) CoinsText.text = $"🪙 {coins:N0}";
        }

        public void UpdateDiamonds(int diamonds)
        {
            if (DiamondsText != null) DiamondsText.text = $"💎 {diamonds:N0}";
        }

        public void UpdatePlayerLevel(int level, float xpProgress)
        {
            if (PlayerLevelText != null) PlayerLevelText.text = $"LVL {level}";
            if (PlayerLevelProgress != null)
            {
                var max = PlayerLevelProgress.GetComponent<RectTransform>().anchorMax;
                max.x = Mathf.Clamp01(xpProgress);
                PlayerLevelProgress.GetComponent<RectTransform>().anchorMax = max;
            }
        }

        public void Localize(ILocalizationService loc)
        {
            _locService = loc;
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_title", loc);
            if (SubtitleText != null) GameUIResources.LocalizeText(SubtitleText.gameObject, "game_tagline", loc);
            if (_continueBtn != null) GameUIResources.LocalizeButtonText(_continueBtn, "menu_continue", loc);
            if (_playBtn != null) GameUIResources.LocalizeButtonText(_playBtn, "menu_quick_play", loc);
            if (_lvlBtn != null) GameUIResources.LocalizeButtonText(_lvlBtn, "menu_levels", loc);
            if (_dailyBtn != null) GameUIResources.LocalizeButtonText(_dailyBtn, "menu_daily_reward", loc);
            if (_chestBtn != null) GameUIResources.LocalizeButtonText(_chestBtn, "menu_chests", loc);
        }

        public void SetDailyRewardAvailable(bool available)
        {
            if (DailyRewardButton == null) return;
            DailyRewardButton.interactable = available;
            var label = DailyRewardButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                string baseText = _locService?.GetString("menu_daily_reward", "DAILY REWARD") ?? "DAILY REWARD";
                label.text = available ? $"{baseText} ●" : baseText;
                label.color = available ? GameUIResources.AccentColor : GameUIResources.MutedTextDark;
            }
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("CONTINUE")) { _continueBtn = btn.gameObject; ContinueButton = btn; }
                else if (upper.Contains("QUICK PLAY") || upper.Contains("PLAY")) { _playBtn = btn.gameObject; PlayButton = btn; }
                else if (upper.Contains("LEVELS") || upper.Contains("LEVEL SELECT")) { _lvlBtn = btn.gameObject; LevelSelectButton = btn; }
                else if (upper.Contains("DAILY") || upper.Contains("REWARD")) { _dailyBtn = btn.gameObject; DailyRewardButton = btn; }
                else if (upper.Contains("SETTINGS") || upper.Contains("⚙")) { _settingsBtn = btn.gameObject; SettingsButton = btn; }
                else if (upper.Contains("CHEST")) { _chestBtn = btn.gameObject; ChestButton = btn; }
            }
            _allButtons = buttons;

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.transform.parent != transform && txt.transform.parent?.parent?.parent != transform) continue;
                var upper = txt.name.ToUpperInvariant();
                if (txt.fontSize >= 50 || upper.Contains("TITLE")) TitleText = txt;
                else if (txt.fontSize >= 18 && (upper.Contains("SUBTITLE") || upper.Contains("TAG"))) SubtitleText = txt;
                else if (upper.Contains("COIN")) CoinsText = txt;
                else if (upper.Contains("DIAMOND") || upper.Contains("GEM")) DiamondsText = txt;
                else if (upper.Contains("VERSION") || upper.Contains("VER")) VersionLabel = txt;
                else if (upper.Contains("PLAYER LEVEL") || upper.Contains("LVL")) PlayerLevelText = txt;
            }

            var progressBars = GetComponentsInChildren<Image>(true);
            foreach (var img in progressBars)
                if (img.name.Contains("LevelBarFill")) PlayerLevelProgress = img;
        }
    }
}
