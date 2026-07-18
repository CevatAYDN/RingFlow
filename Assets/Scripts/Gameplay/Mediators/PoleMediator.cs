using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class PoleMediator : Mediator<PoleView>
    {
        /// <summary>
        /// FIX-M2: Guard flag to prevent HandleClicked from firing after OnUnbind
        /// completes. The delegate View.OnClicked is set to null in OnUnbind, but
        /// a click event could have already been queued on the UI event system before
        /// the null assignment takes effect. The _isBound flag provides a synchronous
        /// check that handles this race condition without allocating additional objects.
        /// </summary>
        private bool _isBound;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("PoleMediator", nameof(OnBind), "",
                    "PoleView not bound; click handling disabled.");
                return;
            }
            _isBound = true;
            NexusLog.Info("PoleMediator", nameof(OnBind), View.PoleId.ToString(), "Binding pole click handler.");
            View.OnClicked = HandleClicked;
        }

        private void HandleClicked()
        {
            // FIX-M2: Double guard: check both _isBound flag AND View reference.
            // The _isBound flag is cleared FIRST in OnUnbind (before View.OnClicked = null),
            // so any already-dispatched click callback that fires between the flag clear
            // and the delegate null will be safely ignored.
            if (!_isBound || View == null)
            {
                NexusLog.Warn("PoleMediator", nameof(HandleClicked), "",
                    "View unbound or missing between Bind and click; ignoring.");
                return;
            }
            NexusLog.Info("PoleMediator", nameof(HandleClicked), View.PoleId.ToString(), "Firing SelectPoleSignal.");
            SignalBus.Fire(new SelectPoleSignal(View.PoleId));
        }

        protected override void OnUnbind()
        {
            // FIX-M2: Clear the bound flag FIRST to prevent any in-flight click
            // callbacks from reaching SignalBus.Fire after unbind.
            _isBound = false;
            if (View != null)
            {
                View.OnClicked = null;
            }
        }
    }
}
