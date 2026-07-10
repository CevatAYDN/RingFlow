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

            if (View.ResumeButton != null)
                View.ResumeButton.onClick.AddListener(() => SignalBus.Fire(new ResumeRequestedSignal()));
            else
                NexusLog.Warn("PauseMediator", nameof(OnBind), "", "ResumeButton is null — cannot bind.");

            if (View.QuitButton != null)
                View.QuitButton.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));
            else
                NexusLog.Warn("PauseMediator", nameof(OnBind), "", "QuitButton is null — cannot bind.");
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
