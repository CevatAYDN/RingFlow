using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class PoleMediator : Mediator<PoleView>
    {
        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("PoleMediator", nameof(OnBind), "",
                    "PoleView not bound; click handling disabled.");
                return;
            }
            NexusLog.Info("PoleMediator", nameof(OnBind), View.PoleId.ToString(), "Binding pole click handler.");
            View.OnClicked = HandleClicked;
        }

        private void HandleClicked()
        {
            if (View == null)
            {
                NexusLog.Warn("PoleMediator", nameof(HandleClicked), "",
                    "View disappeared between Bind and click; ignoring.");
                return;
            }
            NexusLog.Info("PoleMediator", nameof(HandleClicked), View.PoleId.ToString(), "Firing SelectPoleSignal.");
            SignalBus.Fire(new SelectPoleSignal(View.PoleId));
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.OnClicked = null;
            }
        }
    }
}
