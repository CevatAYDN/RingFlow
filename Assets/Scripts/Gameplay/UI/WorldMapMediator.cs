using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Placeholder mediator for WorldMapView. Fires PlayRequestedSignal to skip
    /// directly to LevelSelect until the world map feature is implemented.
    /// </summary>
    public class WorldMapMediator : Mediator<WorldMapView>
    {
        [Inject] private ISignalBus _signalBus;

        protected override void OnBind()
        {
            if (View == null) return;

            // Auto-advance to LevelSelect since WorldMap is stubbed
            _signalBus?.Fire(new PlayRequestedSignal());
        }

        protected override void OnUnbind() { }
    }
}
