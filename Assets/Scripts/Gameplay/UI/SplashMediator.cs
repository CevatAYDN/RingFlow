using Nexus.Core;
using Nexus.Core.FSM;

namespace RingFlow.Gameplay.UI
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] private IGameStateMachine _fsm;

        protected override void OnBind()
        {
            if (View != null) View.TaglineText.text = "Loading...";
            _ = TransitionAfterDelay();
        }

        private async System.Threading.Tasks.ValueTask TransitionAfterDelay()
        {
            await System.Threading.Tasks.Task.Delay(800);
            if (_fsm != null) _ = _fsm.ChangeStateAsync<MainMenuState>();
        }

        protected override void OnUnbind() { }
    }
}
