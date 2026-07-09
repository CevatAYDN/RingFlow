using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(SettingsMediator))]
    public class SettingsView : View
    {
        public Button CloseButton { get; private set; }
        public Toggle MusicToggle { get; private set; }
        public Toggle SfxToggle { get; private set; }
        public Toggle HapticToggle { get; private set; }
        public Toggle ReduceMotionToggle { get; private set; }
        public Toggle BigButtonsToggle { get; private set; }
        public Slider ColorBlindSlider { get; private set; }
        public Dropdown LanguageDropdown { get; private set; }
        public Text TitleText { get; private set; }
        public Text[] SettingLabels { get; private set; }
        public Button RemoveAdsButton { get; private set; }
        public Button RestoreButton { get; private set; }
        private GameObject _closeBtn, _removeAdsBtn, _restoreBtn;
        private Text _musicLabel, _sfxLabel, _hapticLabel, _motionLabel, _bigLabel, _cbLabel, _langLabel;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        public void Localize(ILocalizationService loc)
        {
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "settings_title", loc);
            if (_musicLabel != null) GameUIResources.LocalizeText(_musicLabel.gameObject, "settings_music", loc);
            if (_sfxLabel != null) GameUIResources.LocalizeText(_sfxLabel.gameObject, "settings_sfx", loc);
            if (_hapticLabel != null) GameUIResources.LocalizeText(_hapticLabel.gameObject, "settings_haptic", loc);
            if (_motionLabel != null) GameUIResources.LocalizeText(_motionLabel.gameObject, "settings_reduce_motion", loc);
            if (_bigLabel != null) GameUIResources.LocalizeText(_bigLabel.gameObject, "settings_big_buttons", loc);
            if (_cbLabel != null) GameUIResources.LocalizeText(_cbLabel.gameObject, "settings_color_blind", loc);
            if (_langLabel != null) GameUIResources.LocalizeText(_langLabel.gameObject, "settings_language", loc);
            GameUIResources.LocalizeButtonText(_removeAdsBtn, "settings_remove_ads", loc);
            GameUIResources.LocalizeButtonText(_restoreBtn, "settings_restore", loc);
            GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
        }

        private Toggle CreateToggle(Transform parent, float anchorX, float anchorY, bool initialValue)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 40);
            GameUIResources.SetAnchors(rect, anchorX, anchorY - 0.02f, anchorX + 0.18f, anchorY + 0.02f);
            go.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = initialValue;
            var checkmarkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkGo.transform.SetParent(go.transform, false);
            var cmRect = checkmarkGo.GetComponent<RectTransform>();
            cmRect.anchorMin = new Vector2(0.5f, 0.5f);
            cmRect.anchorMax = new Vector2(0.5f, 0.5f);
            cmRect.sizeDelta = new Vector2(28, 28);
            checkmarkGo.GetComponent<Image>().color = GameUIResources.AccentColor;
            toggle.targetGraphic = checkmarkGo.GetComponent<Image>();
            toggle.graphic = checkmarkGo.GetComponent<Image>();
            return toggle;
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("CLOSE")) { _closeBtn = btn.gameObject; CloseButton = btn; }
                else if (upper.Contains("REMOVE ADS")) { _removeAdsBtn = btn.gameObject; RemoveAdsButton = btn; }
                else if (upper.Contains("RESTORE")) { _restoreBtn = btn.gameObject; RestoreButton = btn; }
            }

            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                var upper = toggle.name.ToUpperInvariant();
                if (upper.Contains("MUSIC")) MusicToggle = toggle;
                else if (upper.Contains("SFX") || upper.Contains("SOUND")) SfxToggle = toggle;
                else if (upper.Contains("HAPTIC")) HapticToggle = toggle;
                else if (upper.Contains("MOTION")) ReduceMotionToggle = toggle;
                else if (upper.Contains("BIG")) BigButtonsToggle = toggle;
            }

            ColorBlindSlider = GetComponentInChildren<Slider>(true);
            LanguageDropdown = GetComponentInChildren<Dropdown>(true);

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) TitleText = txt;
                else if (upper.Contains("MUSIC")) _musicLabel = txt;
                else if (upper.Contains("SFX") || upper.Contains("SOUND")) _sfxLabel = txt;
                else if (upper.Contains("HAPTIC")) _hapticLabel = txt;
                else if (upper.Contains("MOTION")) _motionLabel = txt;
                else if (upper.Contains("BIG")) _bigLabel = txt;
                else if (upper.Contains("COLOR") || upper.Contains("CB")) _cbLabel = txt;
                else if (upper.Contains("LANG")) _langLabel = txt;
            }
        }


    }
}
