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
            if (View != null && _loc != null)
            {
                View.Localize(_loc);
            }
            TransitionAfterDelay();
        }

        private async void TransitionAfterDelay()
        {
            await Task.Delay(800);

            if (_fsm != null)
            {
                try
                {
                    await _fsm.ChangeStateAsync<MainMenuState>();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SplashMediator] FSM transition to MainMenuState failed: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError("[SplashMediator] _fsm is null! Cannot transition to MainMenuState.");
            }
        }

        protected override void OnUnbind() { }
    }
}
