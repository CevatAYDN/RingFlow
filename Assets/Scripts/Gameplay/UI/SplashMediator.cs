using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay.UI
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("SplashMediator", nameof(OnBind), "", "View not bound.");
                return;
            }
            if (_loc == null)
            {
                NexusLog.Warn("SplashMediator", nameof(OnBind), "",
                    "Localization service unbound; strings will fall back to defaults.");
            }
            else
            {
                View.Localize(_loc);
            }
            TransitionAfterDelay();
        }

        private async void TransitionAfterDelay()
        {
            try
            {
                await Task.Delay(800);
            }
            catch (System.Exception ex)
            {
                NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "", ex);
                return;
            }

            if (_fsm == null)
            {
                NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "",
                    "IGameStateMachine is null — cannot transition to MainMenuState.");
                return;
            }

            try
            {
                await _fsm.ChangeStateAsync<MainMenuState>();
            }
            catch (System.Exception ex)
            {
                NexusLog.Error("SplashMediator", nameof(TransitionAfterDelay), "", ex);
            }
        }

        protected override void OnUnbind() { }
    }
}
