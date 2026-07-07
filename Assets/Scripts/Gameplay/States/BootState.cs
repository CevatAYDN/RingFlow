using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class BootState : IGameState
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILoggerService _logger;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _logger?.Log("[BootState] Game starting, initializing models...");
            
            // Transition directly to Main Menu State once booted
            _ = _fsm.ChangeStateAsync<MainMenuState>();
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
