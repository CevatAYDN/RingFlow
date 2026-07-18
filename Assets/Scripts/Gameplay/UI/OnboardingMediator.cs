using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Drives the first-launch onboarding flow. Steps forward through OnboardingView's
    /// 4 stages, persists completion to SettingsModel, then transitions to gameplay
    /// via PlayRequestedSignal. Onboarding only opens once per install because the
    /// boot state machine checks SettingsModel.OnboardingCompleted before showing it.
    /// </summary>
    public class OnboardingMediator : Mediator<OnboardingView>
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private ILocalizationService _loc;
        [Inject] private SettingsModel _settings;
        [Inject] private IPlayerPrefsService _prefs;

        private int _stepIndex;

        protected override void OnBind()
        {
            if (View == null) return;

            bool reduceMotion = _settings?.ReduceMotion.Value ?? false;
            View.Configure(reduceMotion);
            View.Localize(_loc);
            View.ShowStep(0, _loc);

            View.NextClicked += OnNext;
            View.SkipClicked += OnSkip;
        }

        private void OnNext()
        {
            _stepIndex++;
            if (_stepIndex >= 4)
            {
                Complete();
                return;
            }
            View?.ShowStep(_stepIndex, _loc);
            if (View != null) View.Localize(_loc);
        }

        private void OnSkip()
        {
            Complete();
        }

        private void Complete()
        {
            if (_settings != null)
            {
                _settings.OnboardingCompleted.Value = true;
                if (_prefs != null)
                    SettingsSaveSystem.Save(_prefs, _settings);
            }
            _signalBus?.Fire(new PlayRequestedSignal());
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.NextClicked -= OnNext;
                View.SkipClicked -= OnSkip;
            }
        }
    }
}
