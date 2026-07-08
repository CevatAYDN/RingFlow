using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using RingFlow.Gameplay;
using UnityEngine.Scripting;

namespace RingFlow.Gameplay.Diagnostics
{
    [Preserve]
    public interface ISignalBusMonitor : INexusService {}

    [Preserve]
    public class SignalBusMonitor : ISignalBusMonitor, IDisposable
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private IGameDiagnostics _diag;

        private readonly System.Collections.Generic.List<ISignalSubscription> _subscriptions = new();

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (_signalBus == null || _diag == null) return default;

            _diag.Log("SignalBus", "SignalBus Monitor starting...");

            TrySubscribe<InitLevelSignal>(sig => $"InitLevelSignal: Level={sig.LevelIndex}");
            TrySubscribe<SelectPoleSignal>(sig => $"SelectPoleSignal: PoleId={sig.PoleId}");
            TrySubscribe<MoveRingSignal>(sig => $"MoveRingSignal: From={sig.FromPoleId} To={sig.ToPoleId}");
            TrySubscribe<RingMovedSignal>(sig => $"RingMovedSignal: From={sig.FromPoleId} To={sig.ToPoleId}");
            TrySubscribe<UndoSignal>(_ => "UndoSignal");
            TrySubscribe<UndoRequestedSignal>(_ => "UndoRequestedSignal");
            TrySubscribe<CheckWinSignal>(_ => "CheckWinSignal");
            TrySubscribe<HintRequestedSignal>(_ => "HintRequestedSignal");
            TrySubscribe<DailyRewardClaimSignal>(_ => "DailyRewardClaimSignal");
            TrySubscribe<ShowScreenSignal>(sig => $"ShowScreenSignal: Screen={sig.Screen}");
            TrySubscribe<HideScreenSignal>(sig => $"HideScreenSignal: Screen={sig.Screen}");

            _diag.Log("SignalBus", "SignalBus Monitor successfully registered subscriptions");
            return default;
        }

        private void TrySubscribe<TSignal>(Func<TSignal, string> format) where TSignal : struct
        {
            if (_signalBus == null) return;
            try
            {
                var sub = _signalBus.Subscribe<TSignal>(sig =>
                {
                    string msg = format(sig);
                    _diag?.Log("SignalBus", msg, DiagnosticSeverity.Info);
                });
                if (sub != null) _subscriptions.Add(sub);
            }
            catch (Exception ex)
            {
                _diag?.LogError("SignalBus", $"Failed to subscribe to {typeof(TSignal).Name}: {ex.Message}");
            }
        }

        public void OnDispose()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}
