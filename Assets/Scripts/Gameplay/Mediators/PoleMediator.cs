using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class PoleMediator : Mediator<PoleView>
    {
        protected override void OnBind()
        {
            View.OnClicked = HandleClicked;
        }

        private void HandleClicked()
        {
            SignalBus.Fire(new SelectPoleSignal(View.PoleId));
        }

        protected override void OnUnbind()
        {
            View.OnClicked = null;
        }
    }
}
