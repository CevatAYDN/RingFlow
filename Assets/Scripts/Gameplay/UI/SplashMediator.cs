using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay.UI
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILocalizationService _loc;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IPlayerPrefsService _prefs;

        protected override void OnBind()
        {
            NexusLog.Info("SplashMediator", nameof(OnBind), "", "OnBind called");
            if (View == null)
            {
                NexusLog.Error("SplashMediator", nameof(OnBind), "", "View not bound.");
                return;
            }
            if (_loc == null)
            {
                NexusLog.Warn("SplashMediator", nameof(OnBind), "",
                    "Localization service unbound; strings will fall back to defaults.");
            }
            else
            {
                View.Localize(_loc);
            }
            TransitionAfterDelay();
        }

        private async void TransitionAfterDelay()
        {
            NexusLog.Info("SplashMediator", nameof(TransitionAfterDelay), "", "TransitionAfterDelay started");
            try
            {
                await Awaitable.WaitForSecondsAsync(0.8f);
                NexusLog.Info("SplashMediator", nameof(TransitionAfterDelay), "", "TransitionAfterDelay delay completed");
            }
            catch (System.Exception ex)
            {
                NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "", ex);
                return;
            }

            if (!IsViewValid)
            {
                NexusLog.Info("SplashMediator", nameof(TransitionAfterDelay), "", "TransitionAfterDelay aborted: IsViewValid is false");
                return; // View was disabled/unbound before the delay completed
            }

            if (_fsm == null)
            {
                NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "",
                    "IGameStateMachine is null — cannot transition to MainMenuState.");
                return;
            }

            bool gdprAccepted = _prefs != null
                ? _prefs.GetInt(GameplayAssetKeys.PlayerPrefs.GdprAccepted, 0) == 1
                : PlayerPrefs.GetInt(GameplayAssetKeys.PlayerPrefs.GdprAccepted, 0) == 1;

            if (gdprAccepted)
            {
                try
                {
                    await _fsm.ChangeStateAsync<MainMenuState>();
                }
                catch (System.Exception ex)
                {
                    NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "", ex);
                }
            }
            else
            {
                NexusLog.Info("SplashMediator", "TransitionAfterDelay", "", "GDPR not accepted. Opening Parental Gate popup.");
                _signalBus?.Fire(new ShowScreenSignal(ScreenType.ParentalGate));
            }
        }

        protected override void OnUnbind() { }
    }
}
