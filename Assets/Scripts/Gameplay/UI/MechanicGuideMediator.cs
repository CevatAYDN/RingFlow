using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Mediator for the Mechanic Guide popup.
    /// Binds localization, close button, and handles the guide lifecycle.
    /// </summary>
    public class MechanicGuideMediator : Mediator<MechanicGuideView>
    {
        [Inject] private ILocalizationService _loc;
        [Inject] private IGameDiagnostics _diag;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("MechanicGuideMediator", nameof(OnBind), "", "MechanicGuideView not bound.");
                return;
            }

            _diag?.Log("MechanicGuideMediator", "Mechanic Guide popup bound.");
            _diag?.Checkpoint("MechanicGuideMediator.OnBind");

            View.Localize(_loc);

            if (View.CloseButton != null)
                View.CloseButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            _diag?.Log("MechanicGuideMediator", "Close clicked.");
            SignalBus.Fire(new CloseMechanicGuideSignal());
        }

        protected override void OnUnbind()
        {
            if (View?.CloseButton != null)
                View.CloseButton.onClick.RemoveAllListeners();
        }
    }
}
