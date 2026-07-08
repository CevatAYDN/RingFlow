using Nexus.Core;

namespace RingFlow.Gameplay.UI
{
    public class PauseMediator : Mediator<PauseView>
    {
        protected override void OnBind()
        {
            View.ResumeButton.onClick.AddListener(() => SignalBus.Fire(new ResumeRequestedSignal()));
            View.QuitButton.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));
        }

        protected override void OnUnbind()
        {
            View.ResumeButton.onClick.RemoveAllListeners();
            View.QuitButton.onClick.RemoveAllListeners();
        }
    }
}
