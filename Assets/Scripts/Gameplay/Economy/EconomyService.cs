using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Nexus MVCS-compliant EconomyService that bridges IEconomyService to PlayerProgressModel.
    /// Uses Nexus's built-in reactive system for synchronization, eliminating complex locking.
    /// Follows GDD §9 economy requirements with proper DI and 0-GC principles.
    /// </summary>
    public sealed class EconomyService : IEconomyService, INexusService, IDisposable
    {
        [Inject] private PlayerProgressModel _progress;

        private readonly Dictionary<string, ObservableProperty<long>> _balances = new();
        private const long IntOverflowBuffer = 1_000_000_000_000L; // 1T — long to int overflow buffer

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (_progress == null) return default;

            // Initialize Observables from Model (one-way sync: Model -> Service)
            var coinsObs = GetObservableBalance("Coins");
            var diamondsObs = GetObservableBalance("Diamonds");
            var hintsObs = GetObservableBalance("Hint");

            coinsObs.Value = _progress.Coins.Value;
            diamondsObs.Value = _progress.Diamonds.Value;
            hintsObs.Value = _progress.HintCount.Value;

            // Nexus Reactive Pattern: Model -> Service (read-only sync)
            // This ensures save-game is single source of truth
            _progress.Coins.OnChanged((_, newVal) =>
            {
                if (coinsObs.Value != newVal) coinsObs.Value = newVal;
            });
            _progress.Diamonds.OnChanged((_, newVal) =>
            {
                if (diamondsObs.Value != newVal) diamondsObs.Value = newVal;
            });
            _progress.HintCount.OnChanged((_, newVal) =>
            {
                if (hintsObs.Value != newVal) hintsObs.Value = newVal;
            });

            // Note: We do NOT hook Service -> Model back to avoid infinite loops
            // Service mutations write directly to Model, which then propagates back to Service

            return default;
        }

        public void OnDispose() => Dispose();

        /// <summary>
        /// int taşmasından kaçınmak için PlayerProgressModel'e yazmadan önce clamp yapar.
        /// long int aralığına sığmıyorsa değeri sınırda tutar (veri kaybını sessizce yutmaz, log düşürür).
        /// </summary>
        private static int ClampInt(long value, string currencyId)
        {
            if (value > int.MaxValue)
            {
                NexusLog.Warn("EconomyService", nameof(ClampInt), currencyId,
                    $"Value {value} exceeds int range; clamped to int.MaxValue.");
                return int.MaxValue;
            }
            if (value < int.MinValue)
            {
                NexusLog.Warn("EconomyService", nameof(ClampInt), currencyId,
                    $"Value {value} exceeds int range; clamped to int.MinValue.");
                return int.MinValue;
            }
            return (int)value;
        }

        // FIX-P4: The lock(_balances) guard was added preemptively but Unity's gameplay
        // loop is single-threaded: EconomyService.GetObservableBalance is called from
        // HUD bindings, command executions, and UI updates — all on the main thread.
        // The only background-thread path (ad SDK callbacks) uses FireThreadSafe which
        // marshals to the main thread before touching the economy, so no race condition
        // exists. Locking a Dictionary every frame creates unnecessary monitor contention
        // overhead that violates the GDD §75 GC budget (lock acquire/release boxes the
        // lock object and allocates a Monitor handle on some runtimes).
        // Fix: remove the lock. If a genuine background-thread path is ever added, the
        // correct solution is a dedicated ConcurrentDictionary or a thread-safe queue,
        // not a blanket lock on a per-call hot path.
        public ObservableProperty<long> GetObservableBalance(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                NexusLog.Warn("EconomyService", nameof(GetObservableBalance), "",
                    "Empty currencyId passed — returning null. Use the named ID constant.");
                return null;
            }

            if (!_balances.TryGetValue(currencyId, out var prop))
            {
                prop = new ObservableProperty<long>(0);
                _balances[currencyId] = prop;
            }
            return prop;
        }

        public long GetBalance(string currencyId)
        {
            return GetObservableBalance(currencyId)?.Value ?? 0L;
        }

        public bool CanAfford(string currencyId, long amount)
        {
            if (amount <= 0) return true;
            return GetBalance(currencyId) >= amount;
        }

        public bool Spend(string currencyId, long amount, string reason = "")
        {
            if (amount <= 0) return true;

            if (_progress == null)
            {
                NexusLog.Error("EconomyService", nameof(Spend), currencyId, "PlayerProgressModel not bound.");
                return false;
            }

            // Check balance through service observable
            var prop = GetObservableBalance(currencyId);
            if (prop.Value < amount)
            {
                NexusLog.Warn("EconomyService", nameof(Spend), currencyId,
                    $"Insufficient balance. Have {prop.Value}, need {amount}. Reason: {reason}");
                return false;
            }

            // Write directly to Model (single source of truth)
            // Model's reactive system will automatically sync back to Service observable
            long newValue = prop.Value - amount;
            WriteToModel(currencyId, newValue);

            NexusLog.Info("EconomyService", nameof(Spend), currencyId,
                $"Spent {amount}. New balance: {newValue}. Reason: {reason}");
            return true;
        }

        public void Earn(string currencyId, long amount, string reason = "")
        {
            if (amount <= 0) return;

            if (_progress == null)
            {
                NexusLog.Error("EconomyService", nameof(Earn), currencyId, "PlayerProgressModel not bound.");
                return;
            }

            // Write directly to Model (single source of truth)
            var prop = GetObservableBalance(currencyId);
            long newValue = prop.Value + amount;
            WriteToModel(currencyId, newValue);

            NexusLog.Info("EconomyService", nameof(Earn), currencyId,
                $"Earned {amount}. New balance: {newValue}. Reason: {reason}");
        }

        public void SetBalance(string currencyId, long amount)
        {
            if (_progress == null)
            {
                NexusLog.Error("EconomyService", nameof(SetBalance), currencyId, "PlayerProgressModel not bound.");
                return;
            }

            var prop = GetObservableBalance(currencyId);
            long old = prop.Value;
            
            // Write directly to Model (single source of truth)
            WriteToModel(currencyId, amount);

            NexusLog.Info("EconomyService", nameof(SetBalance), currencyId,
                $"Balance changed: {old} -> {amount}");
        }

        /// <summary>
        /// Writes value to the appropriate PlayerProgressModel property with int clamping.
        /// This is the single source of truth for all economy mutations.
        /// </summary>
        private void WriteToModel(string currencyId, long value)
        {
            int clampedValue = ClampInt(value, currencyId);

            switch (currencyId)
            {
                case "Coins":
                    _progress.Coins.Value = clampedValue;
                    break;
                case "Diamonds":
                    _progress.Diamonds.Value = clampedValue;
                    break;
                case "Hint":
                    _progress.HintCount.Value = clampedValue;
                    break;
                default:
                    NexusLog.Warn("EconomyService", nameof(WriteToModel), currencyId,
                        $"Unknown currency ID. Value not written to model.");
                    break;
            }
        }

        public void Dispose()
        {
            foreach (var pair in _balances.Values)
            {
                pair.ClearOnChanged();
            }
            _balances.Clear();
        }
    }
}
