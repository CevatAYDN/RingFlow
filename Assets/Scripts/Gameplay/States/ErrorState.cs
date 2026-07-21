using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Safe fallback state for FSM when state initialization fails.
    /// Prevents null reference crashes in OnTick.
    /// </summary>
    public class ErrorState : IGameState
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILoggerService _logger;
        
        private bool _recoveryAttempted;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            if (args is Exception exception)
            {
                _logger?.LogError($"[ErrorState] Entered due to exception: {exception.Message}");
                NexusLog.Error("ErrorState", nameof(OnEnterAsync), "", 
                    $"FSM entered error state due to: {exception.Message}\n{exception.StackTrace}");
            }
            else if (args != null)
            {
                _logger?.LogError($"[ErrorState] Entered with args: {args}");
                NexusLog.Error("ErrorState", nameof(OnEnterAsync), "", 
                    $"FSM entered error state with args: {args}");
            }

            // Attempt safe recovery after a delay
            _ = AttemptRecoveryAsync(ct);
            
            return default;
        }

        private async Task AttemptRecoveryAsync(CancellationToken ct)
        {
            if (_recoveryAttempted) return;
            _recoveryAttempted = true;

            try
            {
                // Wait a moment to allow error logging and user notification
                await Task.Delay(2000, ct);
                
                // Attempt to transition to MainMenuState as a safe recovery
                if (_fsm != null)
                {
                    _logger?.Log("[ErrorState] Attempting recovery to MainMenuState");
                    NexusLog.Info("ErrorState", nameof(AttemptRecoveryAsync), "", 
                        "Attempting recovery to MainMenuState");
                    
                    await _fsm.ChangeStateAsync<MainMenuState>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[ErrorState] Recovery failed: {ex.Message}");
                NexusLog.Error("ErrorState", nameof(AttemptRecoveryAsync), "", 
                    $"Recovery failed: {ex.Message}. Staying in ErrorState.");
                // Stay in ErrorState - OnTick is safe and prevents crashes
            }
        }

        public ValueTask OnExitAsync(CancellationToken ct)
        {
            _logger?.Log("[ErrorState] Exiting error state");
            return default;
        }

        public void OnTick(float deltaTime)
        {
            // Safe no-op - prevents crashes when FSM is in error state
        }
    }
}