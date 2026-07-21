using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(MainMenuMediator))]
    public class MainMenuView : View, IAuthoredView
    {
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _levelSelectButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _dailyRewardButton;
        [SerializeField] private Button _chestButton;
        [SerializeField] private Button _worldMapButton;
        [SerializeField] private TextMeshProUGUI _versionLabel;
        [SerializeField] private TextMeshProUGUI _coinsText;
        [SerializeField] private TextMeshProUGUI _diamondsText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subtitleText;
        [SerializeField] private TextMeshProUGUI _playerLevelText;
        [SerializeField] private Image _playerLevelProgress;
        [SerializeField] private CanvasGroup _cardGroup;
        public Button ContinueButton => _continueButton;
        public Button PlayButton => _playButton;
        public Button LevelSelectButton => _levelSelectButton;
        public Button SettingsButton => _settingsButton;
        public Button DailyRewardButton => _dailyRewardButton;
        public Button ChestButton => _chestButton;
        public Button WorldMapButton => _worldMapButton;
        public TextMeshProUGUI VersionLabel => _versionLabel;
        public TextMeshProUGUI CoinsText => _coinsText;
        public TextMeshProUGUI DiamondsText => _diamondsText;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI SubtitleText => _subtitleText;
        public TextMeshProUGUI PlayerLevelText => _playerLevelText;
        public Image PlayerLevelProgress => _playerLevelProgress;
        public CanvasGroup CardGroup => _cardGroup;

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
            var label = _continueBtn.GetComponentInChildren<TextMeshProUGUI>();
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
            var label = DailyRewardButton.GetComponentInChildren<TextMeshProUGUI>();
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
                if (upper.Contains("CONTINUE")) { _continueBtn = btn.gameObject; _continueButton = btn; }
                else if (upper.Contains("QUICK PLAY") || upper.Contains("PLAY")) { _playBtn = btn.gameObject; _playButton = btn; }
                else if (upper.Contains("LEVELS") || upper.Contains("LEVEL SELECT")) { _lvlBtn = btn.gameObject; _levelSelectButton = btn; }
                else if (upper.Contains("DAILY") || upper.Contains("REWARD")) { _dailyBtn = btn.gameObject; _dailyRewardButton = btn; }
                else if (upper.Contains("SETTINGS") || upper.Contains("⚙")) { _settingsBtn = btn.gameObject; _settingsButton = btn; }
                else if (upper.Contains("CHEST")) { _chestBtn = btn.gameObject; _chestButton = btn; }
                else if (upper.Contains("WORLD") || upper.Contains("MAP")) { _mapBtn = btn.gameObject; _worldMapButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt.transform.parent != transform && txt.transform.parent?.parent?.parent != transform) continue;
                var upper = txt.name.ToUpperInvariant();
                if (txt.fontSize >= 50 || upper.Contains("TITLE")) _titleText = txt;
                else if (txt.fontSize >= 18 && (upper.Contains("SUBTITLE") || upper.Contains("TAG"))) _subtitleText = txt;
                else if (upper.Contains("COIN")) _coinsText = txt;
                else if (upper.Contains("DIAMOND") || upper.Contains("GEM")) _diamondsText = txt;
                else if (upper.Contains("VERSION") || upper.Contains("VER")) _versionLabel = txt;
                else if (upper.Contains("PLAYER LEVEL") || upper.Contains("LVL")) _playerLevelText = txt;
            }

            var progressBars = GetComponentsInChildren<Image>(true);
            foreach (var img in progressBars)
                if (img.name.Contains("LevelBarFill")) _playerLevelProgress = img;

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }
    }
}
