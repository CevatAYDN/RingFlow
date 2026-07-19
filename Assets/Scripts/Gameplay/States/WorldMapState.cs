using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class WorldMapState : IGameState
    {
        [Inject] private ISignalBus _signalBus;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            NexusLog.Info("WorldMapState", nameof(OnEnterAsync), "",
                "Entered WorldMapState.");
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.WorldMap));
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct)
        {
            NexusLog.Info("WorldMapState", nameof(OnExitAsync), "",
                "Exiting WorldMapState.");
            return default;
        }
        public void OnTick(float deltaTime) {}
    }
}
