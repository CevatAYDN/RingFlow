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
            if (transform.childCount > 0) return;

            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0, 0, 0, 0.85f);
            }

            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.08f, 0.10f, 0.92f, 0.90f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            var title = GameUIResources.CreateText("SETTINGS", transform, 40, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = title.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(title.GetComponent<RectTransform>(), 0.2f, 0.84f, 0.8f, 0.92f);

            var musicLabel = GameUIResources.CreateText("Music", transform, 22, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _musicLabel = musicLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(musicLabel.GetComponent<RectTransform>(), 0.10f, 0.74f, 0.55f, 0.80f);
            MusicToggle = CreateToggle(card.transform, 0.55f, 0.76f, true);

            var sfxLabel = GameUIResources.CreateText("Sound Effects", transform, 22, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _sfxLabel = sfxLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(sfxLabel.GetComponent<RectTransform>(), 0.10f, 0.66f, 0.55f, 0.72f);
            SfxToggle = CreateToggle(card.transform, 0.55f, 0.68f, true);

            var hapticLabel = GameUIResources.CreateText("Haptic Feedback", transform, 22, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _hapticLabel = hapticLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(hapticLabel.GetComponent<RectTransform>(), 0.10f, 0.58f, 0.55f, 0.64f);
            HapticToggle = CreateToggle(card.transform, 0.55f, 0.60f, true);

            var motionLabel = GameUIResources.CreateText("Reduce Motion", transform, 22, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _motionLabel = motionLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(motionLabel.GetComponent<RectTransform>(), 0.10f, 0.50f, 0.55f, 0.56f);
            ReduceMotionToggle = CreateToggle(card.transform, 0.55f, 0.52f, false);

            var bigLabel = GameUIResources.CreateText("Big Buttons", transform, 22, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _bigLabel = bigLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(bigLabel.GetComponent<RectTransform>(), 0.10f, 0.42f, 0.55f, 0.48f);
            BigButtonsToggle = CreateToggle(card.transform, 0.55f, 0.44f, false);

            var cbLabel = GameUIResources.CreateText("Color Blind Mode", transform, 20, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _cbLabel = cbLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(cbLabel.GetComponent<RectTransform>(), 0.10f, 0.32f, 0.50f, 0.38f);

            var cbBg = GameUIResources.CreatePanel("SliderBg", card.transform);
            GameUIResources.SetAnchors(cbBg.GetComponent<RectTransform>(), 0.50f, 0.34f, 0.90f, 0.38f);
            cbBg.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            var cbSlider = cbBg.AddComponent<Slider>();
            cbSlider.direction = Slider.Direction.LeftToRight;
            cbSlider.minValue = 0;
            cbSlider.maxValue = 3;
            cbSlider.wholeNumbers = true;
            cbSlider.value = 0;
            ColorBlindSlider = cbSlider;

            var langLabel = GameUIResources.CreateText("Language", transform, 20, TextAnchor.MiddleLeft, GameUIResources.TextColor);
            _langLabel = langLabel.GetComponent<Text>();
            GameUIResources.SetAnchors(langLabel.GetComponent<RectTransform>(), 0.10f, 0.22f, 0.50f, 0.28f);

            var langDdGo = new GameObject("LangDD", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            langDdGo.transform.SetParent(card.transform, false);
            var langRect = langDdGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(langRect, 0.50f, 0.22f, 0.90f, 0.30f);
            langDdGo.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            LanguageDropdown = langDdGo.GetComponent<Dropdown>();
            string[] langCodes = { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
            foreach (var code in langCodes)
            {
                LanguageDropdown.options.Add(new Dropdown.OptionData(code));
            }

            _removeAdsBtn = GameUIResources.CreateButton("REMOVE ADS", transform, 180, 48);
            GameUIResources.SetAnchors(_removeAdsBtn.GetComponent<RectTransform>(), 0.10f, 0.12f, 0.48f, 0.19f);
            RemoveAdsButton = _removeAdsBtn.GetComponent<Button>();
            GameUIResources.ApplyPrimaryStyle(_removeAdsBtn);

            _restoreBtn = GameUIResources.CreateButton("RESTORE", transform, 180, 48);
            GameUIResources.SetAnchors(_restoreBtn.GetComponent<RectTransform>(), 0.52f, 0.12f, 0.90f, 0.19f);
            RestoreButton = _restoreBtn.GetComponent<Button>();
            GameUIResources.ApplyOutlineStyle(_restoreBtn);

            _closeBtn = GameUIResources.CreateButton("CLOSE", transform, 200, 48);
            GameUIResources.SetAnchors(_closeBtn.GetComponent<RectTransform>(), 0.35f, 0.03f, 0.65f, 0.10f);
            GameUIResources.ApplyPrimaryStyle(_closeBtn);
            CloseButton = _closeBtn.GetComponent<Button>();
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(TitleText.gameObject, "settings_title", loc);
            GameUIResources.LocalizeText(_musicLabel.gameObject, "settings_music", loc);
            GameUIResources.LocalizeText(_sfxLabel.gameObject, "settings_sfx", loc);
            GameUIResources.LocalizeText(_hapticLabel.gameObject, "settings_haptic", loc);
            GameUIResources.LocalizeText(_motionLabel.gameObject, "settings_reduce_motion", loc);
            GameUIResources.LocalizeText(_bigLabel.gameObject, "settings_big_buttons", loc);
            GameUIResources.LocalizeText(_cbLabel.gameObject, "settings_color_blind", loc);
            GameUIResources.LocalizeText(_langLabel.gameObject, "settings_language", loc);
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


    }
}
