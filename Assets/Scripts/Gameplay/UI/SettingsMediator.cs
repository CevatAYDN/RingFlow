using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    public class SettingsMediator : Mediator<SettingsView>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private SettingsModel _settings;
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            View.Localize(_loc);
            View.CloseButton.onClick.AddListener(() => SignalBus.Fire(new CloseSettingsSignal()));

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
        }

        protected override void OnUnbind()
        {
            View.CloseButton.onClick.RemoveAllListeners();
            View.MusicToggle.onValueChanged.RemoveAllListeners();
            View.SfxToggle.onValueChanged.RemoveAllListeners();
            View.HapticToggle.onValueChanged.RemoveAllListeners();
            View.ReduceMotionToggle.onValueChanged.RemoveAllListeners();
            View.BigButtonsToggle.onValueChanged.RemoveAllListeners();
            View.ColorBlindSlider.onValueChanged.RemoveAllListeners();
        }
    }
}
