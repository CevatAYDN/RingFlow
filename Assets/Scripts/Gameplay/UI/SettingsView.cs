using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(SettingsMediator))]
    public class SettingsView : View, IAuthoredView
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
        public Button RemoveAdsButton { get; private set; }
        public Button RestoreButton { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private GameObject _closeBtn, _removeAdsBtn, _restoreBtn;
        private Text _musicLabel, _sfxLabel, _hapticLabel, _motionLabel, _bigLabel, _cbLabel, _langLabel;

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
            // Overlay
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = GameUIResources.OverlayMedium;
                overlay.raycastTarget = true;
            }

            var cardGo = GameUIResources.CreateCard("Card", transform, GameUIResources.SurfaceDark);
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.06f, 0.06f, 0.94f, 0.94f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Title
            var titleGo = GameUIResources.CreateText("SETTINGS", cardGo.transform, 40, TextAnchor.MiddleCenter, GameUIResources.TextOnDark);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.10f, 0.90f, 0.90f, 0.98f);

            // Settings rows
            float rowY = 0.82f;
            float rowStep = 0.065f;

            CreateToggleRow(cardGo.transform, "Music", "MUSIC", ref rowY, ref _musicLabel);
            rowY -= rowStep;
            CreateToggleRow(cardGo.transform, "SFX", "SFX", ref rowY, ref _sfxLabel);
            rowY -= rowStep;
            CreateToggleRow(cardGo.transform, "Haptic", "HAPTIC FEEDBACK", ref rowY, ref _hapticLabel);
            rowY -= rowStep;
            CreateToggleRow(cardGo.transform, "Motion", "REDUCE MOTION", ref rowY, ref _motionLabel);
            rowY -= rowStep;
            CreateToggleRow(cardGo.transform, "Big", "BIG BUTTONS", ref rowY, ref _bigLabel);

            // Color blind slider row
            rowY -= rowStep * 0.8f;
            var cbLabelGo = GameUIResources.CreateText("COLOR BLIND MODE", cardGo.transform, 16, TextAnchor.MiddleLeft, GameUIResources.TextOnDark);
            cbLabelGo.name = "CbLabel";
            _cbLabel = cbLabelGo.GetComponent<Text>();
            GameUIResources.SetAnchors(cbLabelGo.GetComponent<RectTransform>(), 0.08f, rowY - 0.02f, 0.44f, rowY + 0.02f);

            var sliderGo = new GameObject("ColorBlindSlider", typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderGo.transform.SetParent(cardGo.transform, false);
            GameUIResources.SetAnchors(sliderGo.GetComponent<RectTransform>(), 0.48f, rowY - 0.025f, 0.92f, rowY + 0.025f);
            sliderGo.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);
            sliderGo.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            sliderGo.GetComponent<Image>().type = Image.Type.Sliced;
            ColorBlindSlider = sliderGo.GetComponent<Slider>();
            ColorBlindSlider.minValue = 0;
            ColorBlindSlider.maxValue = 3;
            ColorBlindSlider.wholeNumbers = true;

            // Language
            rowY -= rowStep;
            var langLabelGo = GameUIResources.CreateText("LANGUAGE", cardGo.transform, 16, TextAnchor.MiddleLeft, GameUIResources.TextOnDark);
            langLabelGo.name = "LangLabel";
            _langLabel = langLabelGo.GetComponent<Text>();
            GameUIResources.SetAnchors(langLabelGo.GetComponent<RectTransform>(), 0.08f, rowY - 0.02f, 0.40f, rowY + 0.02f);

            var ddGo = new GameObject("LanguageDropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            ddGo.transform.SetParent(cardGo.transform, false);
            GameUIResources.SetAnchors(ddGo.GetComponent<RectTransform>(), 0.44f, rowY - 0.028f, 0.92f, rowY + 0.028f);
            ddGo.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);
            ddGo.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            ddGo.GetComponent<Image>().type = Image.Type.Sliced;
            LanguageDropdown = ddGo.GetComponent<Dropdown>();

            // Remove Ads
            rowY -= rowStep * 1.5f;
            _removeAdsBtn = GameUIResources.CreateButton("REMOVE ADS", cardGo.transform, 220, 48);
            _removeAdsBtn.name = "Btn_REMOVE ADS";
            GameUIResources.ApplyPrimaryStyle(_removeAdsBtn);
            RemoveAdsButton = _removeAdsBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_removeAdsBtn.GetComponent<RectTransform>(), 0.08f, rowY - 0.03f, 0.46f, rowY + 0.03f);

            _restoreBtn = GameUIResources.CreateButton("RESTORE", cardGo.transform, 180, 48);
            _restoreBtn.name = "Btn_RESTORE";
            GameUIResources.ApplyOutlineStyle(_restoreBtn);
            RestoreButton = _restoreBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_restoreBtn.GetComponent<RectTransform>(), 0.52f, rowY - 0.03f, 0.92f, rowY + 0.03f);

            // Close
            _closeBtn = GameUIResources.CreateButton("CLOSE", cardGo.transform, 140, 42);
            _closeBtn.name = "Btn_CLOSE";
            GameUIResources.ApplyOutlineStyle(_closeBtn);
            CloseButton = _closeBtn.GetComponent<Button>();
            GameUIResources.SetAnchors(_closeBtn.GetComponent<RectTransform>(), 0.04f, 0.02f, 0.20f, 0.07f);
        }

        private void CreateToggleRow(Transform parent, string name, string label, ref float anchorY,
            ref Text labelRef)
        {
            var labelGo = GameUIResources.CreateText(label, parent, 16, TextAnchor.MiddleLeft, GameUIResources.TextOnDark);
            labelGo.name = $"{name}Label";
            labelRef = labelGo.GetComponent<Text>();
            GameUIResources.SetAnchors(labelGo.GetComponent<RectTransform>(), 0.08f, anchorY - 0.025f, 0.46f, anchorY + 0.025f);

            var toggleGo = GameUIResources.CreateToggle(parent, 0.72f, anchorY - 0.028f, 0.92f, anchorY + 0.028f, true);
            toggleGo.name = $"{name}Toggle";
        }

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
                if (upper.Contains("CLOSE") || upper.Contains("BACK")) { _closeBtn = btn.gameObject; CloseButton = btn; }
                else if (upper.Contains("REMOVE ADS") || upper.Contains("REMOVEADS")) { _removeAdsBtn = btn.gameObject; RemoveAdsButton = btn; }
                else if (upper.Contains("RESTORE")) { _restoreBtn = btn.gameObject; RestoreButton = btn; }
            }

            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var toggle in toggles)
            {
                var upper = toggle.name.ToUpperInvariant();
                if (upper.Contains("MUSIC")) MusicToggle = toggle;
                else if (upper.Contains("SFX")) SfxToggle = toggle;
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
                else if (upper.Contains("SFX")) _sfxLabel = txt;
                else if (upper.Contains("HAPTIC")) _hapticLabel = txt;
                else if (upper.Contains("MOTION")) _motionLabel = txt;
                else if (upper.Contains("BIG")) _bigLabel = txt;
                else if (upper.Contains("COLOR") || upper.Contains("CB")) _cbLabel = txt;
                else if (upper.Contains("LANG")) _langLabel = txt;
            }
        }
    }
}
