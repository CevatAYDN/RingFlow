using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            View.Localize(_loc);
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
