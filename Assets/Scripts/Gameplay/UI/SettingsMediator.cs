using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Localization;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RingFlow.Gameplay.UI
{
    public class SettingsMediator : Mediator<SettingsView>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private SettingsModel _settings;
        [Inject] private ILocalizationService _loc;
        [Inject] private IIapService _iapService;
        [Inject] private IAdService _adService;
        [Inject] private ISignalBus _signalBus;
        [Inject] private LocalizationConfigSO _locConfig;

        protected override void OnBind()
        {
            if (View == null) return;
            View.Localize(_loc);

            if (View.CloseButton != null)
            {
                View.CloseButton.onClick.AddListener(OnCloseClicked);
            }

            if (_settings != null)
            {
                View.MusicToggle.isOn = _settings.MusicEnabled.Value;
                View.SfxToggle.isOn = _settings.SfxEnabled.Value;
                View.HapticToggle.isOn = _settings.HapticEnabled.Value;
                View.ReduceMotionToggle.isOn = _settings.ReduceMotion.Value;
                View.BigButtonsToggle.isOn = _settings.BigButtons.Value;
                View.ColorBlindSlider.value = _settings.ColorBlindMode.Value;

                View.MusicToggle.onValueChanged.AddListener(OnMusicChanged);
                View.SfxToggle.onValueChanged.AddListener(OnSfxChanged);
                View.HapticToggle.onValueChanged.AddListener(OnHapticChanged);
                View.ReduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
                View.BigButtonsToggle.onValueChanged.AddListener(OnBigButtonsChanged);
                View.ColorBlindSlider.onValueChanged.AddListener(OnColorBlindChanged);
            }

            // Populate language dropdown
            if (View.LanguageDropdown != null && _locConfig != null && _locConfig.Languages != null)
            {
                var options = new List<TMP_Dropdown.OptionData>();
                int selectedIndex = 0;
                for (int i = 0; i < _locConfig.Languages.Count; i++)
                {
                    var lang = _locConfig.Languages[i];
                    options.Add(new TMP_Dropdown.OptionData(lang.DisplayName));
                    if (lang.Code == _settings?.LanguageCode.Value)
                        selectedIndex = i;
                }
                View.LanguageDropdown.ClearOptions();
                View.LanguageDropdown.AddOptions(options);
                View.LanguageDropdown.value = selectedIndex;

                View.LanguageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            }

            BindPurchaseButtons();
        }

        private void BindPurchaseButtons()
        {
            if (_progress != null && View.RemoveAdsButton != null)
            {
                View.RemoveAdsButton.gameObject.SetActive(!_progress.RemoveAds.Value);
                View.RemoveAdsButton.onClick.AddListener(OnRemoveAdsClicked);
            }

            if (_progress != null && View.RestoreButton != null)
            {
                View.RestoreButton.onClick.AddListener(OnRestoreClicked);
            }
        }

        private void OnCloseClicked() => SignalBus.Fire(new CloseSettingsSignal());
        private void OnMusicChanged(bool value) => _settings.MusicEnabled.Value = value;
        private void OnSfxChanged(bool value) => _settings.SfxEnabled.Value = value;
        private void OnHapticChanged(bool value) => _settings.HapticEnabled.Value = value;
        private void OnReduceMotionChanged(bool value) => _settings.ReduceMotion.Value = value;
        private void OnBigButtonsChanged(bool value) => _settings.BigButtons.Value = value;
        private void OnColorBlindChanged(float value) => _settings.ColorBlindMode.Value = (int)value;
        private void OnLanguageChanged(int idx)
        {
            if (idx >= 0 && _locConfig != null && _locConfig.Languages != null && idx < _locConfig.Languages.Count)
                _settings.LanguageCode.Value = _locConfig.Languages[idx].Code;
        }

        private void OnRemoveAdsClicked()
        {
            _iapService?.PurchaseProduct("remove_ads", OnRemoveAdsPurchaseResult);
        }

        private void OnRemoveAdsPurchaseResult(bool success, string productId)
        {
            if (success)
            {
                _progress.RemoveAds.Value = true;
                View.RemoveAdsButton.gameObject.SetActive(false);
                _adService?.HideBanner();
                return;
            }

            bool storeUnavailable = string.Equals(productId, "store_unavailable", System.StringComparison.Ordinal);
            _signalBus?.Fire(new PurchaseFailedSignal(productId ?? "remove_ads", storeUnavailable));
        }

        private void OnRestoreClicked()
        {
            _iapService?.RestorePurchases(OnRestoreResult);
        }

        private void OnRestoreResult(bool success)
        {
            if (!success) return;

            bool owned = _iapService != null && _iapService.IsProductOwned("remove_ads");
            _progress.RemoveAds.Value = owned;
            if (owned)
            {
                View.RemoveAdsButton.gameObject.SetActive(false);
                _adService?.HideBanner();
            }
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.CloseButton?.onClick.RemoveAllListeners();
            View.MusicToggle?.onValueChanged.RemoveAllListeners();
            View.SfxToggle?.onValueChanged.RemoveAllListeners();
            View.HapticToggle?.onValueChanged.RemoveAllListeners();
            View.ReduceMotionToggle?.onValueChanged.RemoveAllListeners();
            View.BigButtonsToggle?.onValueChanged.RemoveAllListeners();
            View.ColorBlindSlider?.onValueChanged.RemoveAllListeners();
            View.LanguageDropdown?.onValueChanged.RemoveAllListeners();
            View.RemoveAdsButton?.onClick.RemoveAllListeners();
            View.RestoreButton?.onClick.RemoveAllListeners();
        }
    }
}
