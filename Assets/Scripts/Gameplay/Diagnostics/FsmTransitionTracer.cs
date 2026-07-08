using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using UnityEngine.Scripting;

namespace RingFlow.Gameplay.Diagnostics
{
    [Preserve]
    public interface IFsmTransitionTracer : INexusService, ITickable {}

    [Preserve]
    public class FsmTransitionTracer : IFsmTransitionTracer
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private IGameDiagnostics _diag;

        private IGameState _lastState;
        private int _transitionCount;

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (_fsm != null)
            {
                _lastState = _fsm.CurrentState;
            }
            _diag?.Log("FSM", $"FSM Tracing started. Initial state: {_lastState?.GetType().Name ?? "None"}");
            return default;
        }

        public void Tick(float deltaTime)
        {
            if (_fsm == null || _diag == null) return;

            var current = _fsm.CurrentState;
            if (current != _lastState)
            {
                _transitionCount++;
                _diag.Log("FSM", $"State transition #{_transitionCount}: {_lastState?.GetType().Name ?? "None"} → {current?.GetType().Name ?? "None"}");
                _lastState = current;
            }
        }

        public void OnDispose() {}
    }
}
