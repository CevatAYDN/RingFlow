using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;
using RingFlow.Gameplay.Services;

namespace RingFlow.Gameplay.UI
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILocalizationService _loc;
        [Inject] private ISignalBus _signalBus;
        [Inject] private ILegalConsentService _consent;

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

            if (_consent == null)
            {
                NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "",
                    "ILegalConsentService is not bound; treating GDPR consent as not accepted.");
            }
            bool gdprAccepted = _consent != null && _consent.IsAccepted;

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
