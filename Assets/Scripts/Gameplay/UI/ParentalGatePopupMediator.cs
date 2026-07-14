using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;
using RingFlow.Gameplay.Services;

namespace RingFlow.Gameplay.UI
{
    public class ParentalGatePopupMediator : Mediator<ParentalGatePopupView>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ISignalBus _signalBus;
        [Inject] private ILocalizationService _loc;
        [Inject] private ILegalConsentService _consent;

        protected override void OnBind()
        {
            if (View == null) return;

            View.EnsureInitialized();
            View.Localize(_loc);

            View.AcceptButton?.onClick.AddListener(HandleAccept);
            View.TermsButton?.onClick.AddListener(HandleOpenTerms);
            View.PrivacyButton?.onClick.AddListener(HandleOpenPrivacy);
        }

        private void HandleAccept()
        {
            if (View == null) return;

            if (View.ValidateAnswer())
            {
                // Save acceptance state locally (device bound) via the Nexus legal consent service.
                _consent?.Accept();

                NexusLog.Info("ParentalGatePopupMediator", "HandleAccept", "", "Parental gate verification passed. GDPR accepted.");
                
                // Hide popup and transition to main menu
                _signalBus?.Fire(new HideScreenSignal(ScreenType.ParentalGate));
                if (_fsm != null)
                {
                    _ = _fsm.ChangeStateAsync<MainMenuState>();
                }
            }
        }

        private void HandleOpenTerms()
        {
            Application.OpenURL("https://fecestudios.com/terms");
        }

        private void HandleOpenPrivacy()
        {
            Application.OpenURL("https://fecestudios.com/privacy");
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.AcceptButton?.onClick.RemoveListener(HandleAccept);
            View.TermsButton?.onClick.RemoveListener(HandleOpenTerms);
            View.PrivacyButton?.onClick.RemoveListener(HandleOpenPrivacy);
        }
    }
}
