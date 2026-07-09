using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class PauseMediator : Mediator<PauseView>
    {
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            if (View == null) return;
            View.Localize(_loc);
            View.ResumeButton.onClick.AddListener(() => SignalBus.Fire(new ResumeRequestedSignal()));
            View.QuitButton.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));
        }

        protected override void OnUnbind()
        {
            if (View != null)
            {
                View.ResumeButton?.onClick.RemoveAllListeners();
                View.QuitButton?.onClick.RemoveAllListeners();
            }
        }
    }
}
