using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Placeholder mediator for WorldMapView.
    ///
    /// MVCS note: Mediators coordinate presentation flow in response to user
    /// interaction or signals — they must NOT trigger gameplay transitions
    /// from OnBind(). The previous auto-advance (firing PlayRequestedSignal
    /// in OnBind) violated the "Mediators never modify gameplay state from
    /// lifecycle hooks" guidance and caused a "no subscribers" warning
    /// because UIRoot had not subscribed yet.
    ///
    /// Navigation to LevelSelect is driven by the MainMenuMediator's
    /// "Level Select" button (GoToLevelSelect), which fires PlayRequestedSignal
    /// in response to an explicit user click — the correct MVCS flow.
    /// </summary>
    public class WorldMapMediator : Mediator<WorldMapView>
    {
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            if (View == null) return;
            View.Localize(_loc);
            NexusLog.Info("WorldMapMediator", nameof(OnBind), "UI",
                $"Bound to {View.GetType().Name}. Awaiting user navigation.");
        }

        protected override void OnUnbind() { }
    }
}
