using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;

namespace RingFlow.Gameplay
{
    public class WinState : IGameState
    {
        public ValueTask OnEnterAsync(object args, CancellationToken ct) => default;
        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
