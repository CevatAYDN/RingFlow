using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class BootState : IGameState
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILoggerService _logger;

        [Inject] private Diagnostics.IGameDiagnostics _diag;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _diag?.Checkpoint("BootState");
            _diag?.Log("FSM", "BootState.OnEnterAsync started");
            _logger?.Log("[BootState] Game starting, initializing models...");
            if (_fsm != null)
            {
                _ = DeferTransitionAsync();
            }
            else
            {
                NexusLog.Error("BootState", nameof(OnEnterAsync), "",
                    "IGameStateMachine unbound; cannot transition to SplashState.");
            }
            return default;
        }

        private async Task DeferTransitionAsync()
        {
            await Task.Yield();
            if (_fsm != null)
            {
                await _fsm.ChangeStateAsync<SplashState>();
            }
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
