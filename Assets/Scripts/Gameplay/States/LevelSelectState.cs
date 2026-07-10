using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class LevelSelectState : IGameState
    {
        [Inject] private ISignalBus _signalBus;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            NexusLog.Info("LevelSelectState", nameof(OnEnterAsync), "",
                "Entered LevelSelectState.");
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.LevelSelect));
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct)
        {
            NexusLog.Info("LevelSelectState", nameof(OnExitAsync), "",
                "Exiting LevelSelectState.");
            return default;
        }
        public void OnTick(float deltaTime) {}
    }
}
