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
        public Button WorldMapButton { get; private set; }
        public Text VersionLabel { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Text TitleText { get; private set; }
        public Text SubtitleText { get; private set; }
        public Text PlayerLevelText { get; private set; }
        public Image PlayerLevelProgress { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private GameObject _continueBtn, _playBtn, _lvlBtn, _dailyBtn, _settingsBtn, _chestBtn, _mapBtn;
        private ILocalizationService _locService;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void UpdateCoins(int coins)
        {
            if (CoinsText != null) CoinsText.text = $"{coins:N0}";
        }

        public void UpdateDiamonds(int diamonds)
        {
            if (DiamondsText != null) DiamondsText.text = $"{diamonds:N0}";
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

        public void UpdateContinueButtonText(int level)
        {
            if (_continueBtn == null) return;
            var label = _continueBtn.GetComponentInChildren<Text>();
            if (label != null)
            {
                string baseText = _locService != null ? _locService.GetString("menu_continue", "PLAY") : "PLAY";
                label.text = $"{baseText} (LVL {level})";
            }
        }

        public void Localize(ILocalizationService loc)
        {
            _locService = loc;
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "game_title", loc);
            if (SubtitleText != null) GameUIResources.LocalizeText(SubtitleText.gameObject, "menu_subtitle", loc);
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
                else if (upper.Contains("WORLD") || upper.Contains("MAP")) { _mapBtn = btn.gameObject; WorldMapButton = btn; }
            }

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
