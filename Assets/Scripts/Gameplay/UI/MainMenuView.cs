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
            // Backdrop
            _backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            _backdrop.transform.SetParent(transform, false);
            var bdRect = _backdrop.GetComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero; bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero; bdRect.offsetMax = Vector2.zero;
            _backdrop.GetComponent<Image>().color = GameUIResources.BgDark;
            _backdrop.GetComponent<Image>().raycastTarget = false;

            // Main content card
            var cardGo = GameUIResources.CreatePanel("Card", transform);
            cardGo.GetComponent<Image>().color = GameUIResources.SurfaceDark;
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.08f, 0.10f, 0.92f, 0.90f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Title
            var titleGo = GameUIResources.CreateDisplayText("RING FLOW", cardGo.transform, 56, GameUIResources.TextOnDark);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.10f, 0.78f, 0.90f, 0.90f);

            // Subtitle
            var subGo = GameUIResources.CreateText("", cardGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            subGo.name = "Subtitle";
            SubtitleText = subGo.GetComponent<Text>();
            GameUIResources.SetAnchors(subGo.GetComponent<RectTransform>(), 0.12f, 0.72f, 0.88f, 0.76f);

            // Player level panel
            var levelPanel = GameUIResources.CreatePanel("PlayerLevelPanel", cardGo.transform);
            levelPanel.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f);
            GameUIResources.SetAnchors(levelPanel.GetComponent<RectTransform>(), 0.10f, 0.64f, 0.90f, 0.70f);

            var levelGo = GameUIResources.CreateText("LVL 1", levelPanel.transform, 16, TextAnchor.MiddleLeft, GameUIResources.AccentColor);
            levelGo.name = "PlayerLevel";
            PlayerLevelText = levelGo.GetComponent<Text>();
            GameUIResources.SetAnchors(levelGo.GetComponent<RectTransform>(), 0.04f, 0f, 0.20f, 1f);

            var levelBarBg = new GameObject("LevelBarBg", typeof(RectTransform), typeof(Image));
            levelBarBg.transform.SetParent(levelPanel.transform, false);
            GameUIResources.SetAnchors(levelBarBg.GetComponent<RectTransform>(), 0.22f, 0.25f, 0.70f, 0.75f);
            levelBarBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);
            levelBarBg.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            levelBarBg.GetComponent<Image>().type = Image.Type.Sliced;

            var levelBarFill = new GameObject("LevelBarFill", typeof(RectTransform), typeof(Image));
            levelBarFill.transform.SetParent(levelBarBg.transform, false);
            levelBarFill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            levelBarFill.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            levelBarFill.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            levelBarFill.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            levelBarFill.GetComponent<Image>().color = GameUIResources.AccentColor;
            levelBarFill.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            levelBarFill.GetComponent<Image>().type = Image.Type.Sliced;
            PlayerLevelProgress = levelBarFill.GetComponent<Image>();

            // Currency row
            var currencyRow = GameUIResources.CreatePanel("CurrencyRow", cardGo.transform);
            currencyRow.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f);
            GameUIResources.SetAnchors(currencyRow.GetComponent<RectTransform>(), 0.10f, 0.56f, 0.90f, 0.62f);

            // Coins
            var coinsLabel = GameUIResources.CreateText("🪙 0", currencyRow.transform, 16, TextAnchor.MiddleLeft, GameUIResources.CoinColor);
            coinsLabel.name = "CoinsText";
            CoinsText = coinsLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(coinsLabel.GetComponent<RectTransform>(), 0.04f, 0f, 0.46f, 1f);
            CoinsText.supportRichText = true;

            // Diamonds
            var gemsLabel = GameUIResources.CreateText("💎 0", currencyRow.transform, 16, TextAnchor.MiddleRight, GameUIResources.DiamondColor);
            gemsLabel.name = "DiamondsText";
            DiamondsText = gemsLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(gemsLabel.GetComponent<RectTransform>(), 0.54f, 0f, 0.96f, 1f);
            DiamondsText.supportRichText = true;

            // Buttons
            float btnW = 320f;
            float btnH = 56f;
            float gap = 8f;
            float startY = 0.50f;

            _continueBtn = CreateMenuButton(cardGo.transform, "Btn_CONTINUE", "CONTINUE", btnW, btnH,
                new Vector2(0.5f, startY), new Vector2(0.5f, startY - 0.055f));
            ContinueButton = _continueBtn.GetComponent<Button>();
            GameUIResources.ApplyPrimaryStyle(_continueBtn);

            _playBtn = CreateMenuButton(cardGo.transform, "Btn_PLAY", "QUICK PLAY", btnW, btnH * 0.9f,
                new Vector2(0.5f, startY - 0.065f), new Vector2(0.5f, startY - 0.115f));
            PlayButton = _playBtn.GetComponent<Button>();
            GameUIResources.ApplyAccentStyle(_playBtn);

            _lvlBtn = CreateMenuButton(cardGo.transform, "Btn_LEVELS", "LEVEL SELECT", btnW, btnH * 0.9f,
                new Vector2(0.5f, startY - 0.125f), new Vector2(0.5f, startY - 0.175f));
            LevelSelectButton = _lvlBtn.GetComponent<Button>();
            GameUIResources.ApplyOutlineStyle(_lvlBtn);

            _dailyBtn = CreateMenuButton(cardGo.transform, "Btn_DAILY", "DAILY REWARD", btnW, btnH * 0.85f,
                new Vector2(0.5f, startY - 0.185f), new Vector2(0.5f, startY - 0.230f));
            DailyRewardButton = _dailyBtn.GetComponent<Button>();
            GameUIResources.ApplyTextButtonStyle(_dailyBtn);

            // Chest button
            _chestBtn = CreateMenuButton(cardGo.transform, "Btn_CHEST", "CHESTS", btnW * 0.45f, btnH * 0.8f,
                new Vector2(0.5f, startY - 0.255f), new Vector2(0.5f, startY - 0.295f));
            ChestButton = _chestBtn.GetComponent<Button>();
            GameUIResources.ApplyAccentStyle(_chestBtn);

            // Settings (icon button top right)
            _settingsBtn = GameUIResources.CreateIconButton("⚙", cardGo.transform, 48f);
            _settingsBtn.name = "Btn_SETTINGS";
            SettingsButton = _settingsBtn.GetComponent<Button>();
            var setRect = _settingsBtn.GetComponent<RectTransform>();
            setRect.anchorMin = new Vector2(0.84f, 0.90f);
            setRect.anchorMax = new Vector2(0.94f, 0.99f);
            setRect.offsetMin = Vector2.zero;
            setRect.offsetMax = Vector2.zero;

            // Version
            var verGo = GameUIResources.CreateText("v1.0", cardGo.transform, 12, TextAnchor.LowerCenter, GameUIResources.MutedTextDark);
            verGo.name = "Version";
            VersionLabel = verGo.GetComponent<Text>();
            GameUIResources.SetAnchors(verGo.GetComponent<RectTransform>(), 0.30f, 0.01f, 0.70f, 0.04f);

            // Cache all buttons for tab navigation
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
