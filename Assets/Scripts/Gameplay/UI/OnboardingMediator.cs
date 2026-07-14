using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Placeholder mediator for OnboardingView. Auto-advances to LevelSelect
    /// until the full tutorial sequence is implemented.
    /// </summary>
    public class OnboardingMediator : Mediator<OnboardingView>
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            if (View == null) return;
            View.Localize(_loc);
            _signalBus?.Fire(new PlayRequestedSignal());
        }

        protected override void OnUnbind() { }
    }
}
