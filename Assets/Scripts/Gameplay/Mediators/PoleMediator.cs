using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class PoleMediator : Mediator<PoleView>
    {
        protected override void OnBind()
        {
            NexusLog.Info("PoleMediator", nameof(OnBind), View.PoleId.ToString(), "Binding pole click handler.");
            View.OnClicked = HandleClicked;
        }

        private void HandleClicked()
        {
            NexusLog.Info("PoleMediator", nameof(HandleClicked), View.PoleId.ToString(), "Firing SelectPoleSignal.");
            SignalBus.Fire(new SelectPoleSignal(View.PoleId));
        }

        protected override void OnUnbind()
        {
            View.OnClicked = null;
        }
    }
}
