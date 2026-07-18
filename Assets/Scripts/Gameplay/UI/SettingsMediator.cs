using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Localization;
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

            View.CloseButton?.onClick.AddListener(() => SignalBus.Fire(new CloseSettingsSignal()));

            if (_settings != null)
            {
                View.MusicToggle.isOn = _settings.MusicEnabled.Value;
                View.SfxToggle.isOn = _settings.SfxEnabled.Value;
                View.HapticToggle.isOn = _settings.HapticEnabled.Value;
                View.ReduceMotionToggle.isOn = _settings.ReduceMotion.Value;
                View.BigButtonsToggle.isOn = _settings.BigButtons.Value;
                View.ColorBlindSlider.value = _settings.ColorBlindMode.Value;

                View.MusicToggle.onValueChanged.AddListener(v => _settings.MusicEnabled.Value = v);
                View.SfxToggle.onValueChanged.AddListener(v => _settings.SfxEnabled.Value = v);
                View.HapticToggle.onValueChanged.AddListener(v => _settings.HapticEnabled.Value = v);
                View.ReduceMotionToggle.onValueChanged.AddListener(v => _settings.ReduceMotion.Value = v);
                View.BigButtonsToggle.onValueChanged.AddListener(v => _settings.BigButtons.Value = v);
                View.ColorBlindSlider.onValueChanged.AddListener(v => _settings.ColorBlindMode.Value = (int)v);
            }

            // Populate language dropdown
            if (View.LanguageDropdown != null && _locConfig != null && _locConfig.Languages != null)
            {
                var options = new List<Dropdown.OptionData>();
                int selectedIndex = 0;
                for (int i = 0; i < _locConfig.Languages.Count; i++)
                {
                    var lang = _locConfig.Languages[i];
                    options.Add(new Dropdown.OptionData(lang.DisplayName));
                    if (lang.Code == _settings?.LanguageCode.Value)
                        selectedIndex = i;
                }
                View.LanguageDropdown.ClearOptions();
                View.LanguageDropdown.AddOptions(options);
                View.LanguageDropdown.value = selectedIndex;

                View.LanguageDropdown.onValueChanged.AddListener(idx =>
                {
                    if (idx >= 0 && idx < _locConfig.Languages.Count)
                        _settings.LanguageCode.Value = _locConfig.Languages[idx].Code;
                });
            }

            BindPurchaseButtons();
        }

        private void BindPurchaseButtons()
        {
            if (_progress != null && View.RemoveAdsButton != null)
            {
                View.RemoveAdsButton.gameObject.SetActive(!_progress.RemoveAds.Value);
                View.RemoveAdsButton.onClick.AddListener(() =>
                {
                    _iapService?.PurchaseProduct("remove_ads", (success, productId) =>
                    {
                        if (success)
                        {
                            _progress.RemoveAds.Value = true;
                            View.RemoveAdsButton.gameObject.SetActive(false);
                            _adService?.HideBanner();
                        }
                        else
                        {
                            bool storeUnavailable = string.Equals(productId, "store_unavailable", System.StringComparison.Ordinal);
                            _signalBus?.Fire(new PurchaseFailedSignal(productId ?? "remove_ads", storeUnavailable));
                        }
                    });
                });
            }

            if (_progress != null && View.RestoreButton != null)
            {
                View.RestoreButton.onClick.AddListener(() =>
                {
                    _iapService?.RestorePurchases(success =>
                    {
                        if (success)
                        {
                            bool owned = _iapService != null && _iapService.IsProductOwned("remove_ads");
                            _progress.RemoveAds.Value = owned;
                            if (owned)
                            {
                                View.RemoveAdsButton.gameObject.SetActive(false);
                                _adService?.HideBanner();
                            }
                        }
                    });
                });
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
