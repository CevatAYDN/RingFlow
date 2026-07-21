using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(SettingsMediator))]
    public class SettingsView : View, IAuthoredView
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _hapticToggle;
        [SerializeField] private Toggle _reduceMotionToggle;
        [SerializeField] private Toggle _bigButtonsToggle;
        [SerializeField] private Slider _colorBlindSlider;
        [SerializeField] private TMP_Dropdown _languageDropdown;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _removeAdsButton;
        [SerializeField] private Button _restoreButton;
        [SerializeField] private CanvasGroup _cardGroup;
        public Button CloseButton => _closeButton;
        public Toggle MusicToggle => _musicToggle;
        public Toggle SfxToggle => _sfxToggle;
        public Toggle HapticToggle => _hapticToggle;
        public Toggle ReduceMotionToggle => _reduceMotionToggle;
        public Toggle BigButtonsToggle => _bigButtonsToggle;
        public Slider ColorBlindSlider => _colorBlindSlider;
        public TMP_Dropdown LanguageDropdown => _languageDropdown;
        public TextMeshProUGUI TitleText => _titleText;
        public Button RemoveAdsButton => _removeAdsButton;
        public Button RestoreButton => _restoreButton;
        public CanvasGroup CardGroup => _cardGroup;

        private GameObject _closeBtn, _removeAdsBtn, _restoreBtn;
        private TextMeshProUGUI _musicLabel, _sfxLabel, _hapticLabel, _motionLabel, _bigLabel, _cbLabel, _langLabel;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "settings_title", loc);
            if (_musicLabel != null) GameUIResources.LocalizeText(_musicLabel.gameObject, "settings_music", loc);
            if (_sfxLabel != null) GameUIResources.LocalizeText(_sfxLabel.gameObject, "settings_sfx", loc);
            if (_hapticLabel != null) GameUIResources.LocalizeText(_hapticLabel.gameObject, "settings_haptic", loc);
            if (_motionLabel != null) GameUIResources.LocalizeText(_motionLabel.gameObject, "settings_reduce_motion", loc);
            if (_bigLabel != null) GameUIResources.LocalizeText(_bigLabel.gameObject, "settings_big_buttons", loc);
            if (_cbLabel != null) GameUIResources.LocalizeText(_cbLabel.gameObject, "settings_color_blind", loc);
            if (_langLabel != null) GameUIResources.LocalizeText(_langLabel.gameObject, "settings_language", loc);
            if (_removeAdsBtn != null) GameUIResources.LocalizeButtonText(_removeAdsBtn, "settings_remove_ads", loc);
            if (_restoreBtn != null) GameUIResources.LocalizeButtonText(_restoreBtn, "settings_restore", loc);
            if (_closeBtn != null) GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("CLOSE") || upper.Contains("BACK")) { _closeBtn = btn.gameObject; _closeButton = btn; }
                else if (upper.Contains("REMOVE ADS") || upper.Contains("REMOVEADS")) { _removeAdsBtn = btn.gameObject; _removeAdsButton = btn; }
                else if (upper.Contains("RESTORE")) { _restoreBtn = btn.gameObject; _restoreButton = btn; }
            }

            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                var upper = toggle.name.ToUpperInvariant();
                if (upper.Contains("MUSIC")) _musicToggle = toggle;
                else if (upper.Contains("SFX")) _sfxToggle = toggle;
                else if (upper.Contains("HAPTIC")) _hapticToggle = toggle;
                else if (upper.Contains("MOTION")) _reduceMotionToggle = toggle;
                else if (upper.Contains("BIG")) _bigButtonsToggle = toggle;
            }

            _colorBlindSlider = GetComponentInChildren<Slider>(true);
            _languageDropdown = GetComponentInChildren<TMP_Dropdown>(true);

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) _titleText = txt;
                else if (upper.Contains("MUSIC")) _musicLabel = txt;
                else if (upper.Contains("SFX")) _sfxLabel = txt;
                else if (upper.Contains("HAPTIC")) _hapticLabel = txt;
                else if (upper.Contains("MOTION")) _motionLabel = txt;
                else if (upper.Contains("BIG")) _bigLabel = txt;
                else if (upper.Contains("COLOR") || upper.Contains("CB")) _cbLabel = txt;
                else if (upper.Contains("LANG")) _langLabel = txt;
            }

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }
    }
}
