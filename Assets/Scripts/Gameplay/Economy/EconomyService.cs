using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Custom EconomyService bridge that maps IEconomyService (Coins, Diamonds) 
    /// directly to the PlayerProgressModel reactive properties.
    /// This ensures unified state representation and zero discrepancy between model save-game values and service queries.
    /// </summary>
    public sealed class EconomyService : IEconomyService, INexusService, IDisposable
    {
        [Inject] private PlayerProgressModel _progress;

        private readonly Dictionary<string, ObservableProperty<long>> _balances = new();
        private readonly HashSet<string> _updatingCurrencies = new();
        private const long IntOverflowBuffer = 1_000_000_000_000L; // 1T — long'un int kesimine ulaşacağı nokta. Üstünde kazanımlar reddedilir.

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (_progress == null) return default;

            // Initialize Observables
            var coinsObs = GetObservableBalance("Coins");
            var diamondsObs = GetObservableBalance("Diamonds");
            var hintsObs = GetObservableBalance("Hint");

            coinsObs.Value = _progress.Coins.Value;
            diamondsObs.Value = _progress.Diamonds.Value;
            hintsObs.Value = _progress.HintCount.Value;

            // Hook changes: PlayerProgressModel -> EconomyService (with per-currency recursion guard)
            _progress.Coins.OnChanged((_, newVal) =>
            {
                if (TryBeginCurrencySync("Coins")) return;
                try
                {
                    if (coinsObs.Value != newVal) coinsObs.Value = newVal;
                }
                finally { EndCurrencySync("Coins"); }
            });
            _progress.Diamonds.OnChanged((_, newVal) =>
            {
                if (TryBeginCurrencySync("Diamonds")) return;
                try
                {
                    if (diamondsObs.Value != newVal) diamondsObs.Value = newVal;
                }
                finally { EndCurrencySync("Diamonds"); }
            });
            _progress.HintCount.OnChanged((_, newVal) =>
            {
                if (TryBeginCurrencySync("Hint")) return;
                try
                {
                    if (hintsObs.Value != newVal) hintsObs.Value = newVal;
                }
                finally { EndCurrencySync("Hint"); }
            });

            // Hook changes: EconomyService -> PlayerProgressModel (with per-currency recursion guard)
            coinsObs.OnChanged((_, newVal) =>
            {
                if (TryBeginCurrencySync("Coins")) return;
                try
                {
                    if (_progress.Coins.Value != (int)newVal)
                        _progress.Coins.Value = ClampInt(newVal, "Coins");
                }
                finally { EndCurrencySync("Coins"); }
            });
            diamondsObs.OnChanged((_, newVal) =>
            {
                if (TryBeginCurrencySync("Diamonds")) return;
                try
                {
                    if (_progress.Diamonds.Value != (int)newVal)
                        _progress.Diamonds.Value = ClampInt(newVal, "Diamonds");
                }
                finally { EndCurrencySync("Diamonds"); }
            });
            hintsObs.OnChanged((_, newVal) =>
            {
                if (TryBeginCurrencySync("Hint")) return;
                try
                {
                    if (_progress.HintCount.Value != (int)newVal)
                        _progress.HintCount.Value = ClampInt(newVal, "Hint");
                }
                finally { EndCurrencySync("Hint"); }
            });

            return default;
        }

        public void OnDispose() => Dispose();

        /// <summary>
        /// Sadece tek bir currency için recursion guard'ı tetikler. Aynı frame'de birden fazla
        /// currency değişse bile birbirini blok etmez.
        /// </summary>
        private bool TryBeginCurrencySync(string currencyId)
        {
            lock (_updatingCurrencies)
            {
                if (_updatingCurrencies.Contains(currencyId)) return true;
                _updatingCurrencies.Add(currencyId);
                return false;
            }
        }

        private void EndCurrencySync(string currencyId)
        {
            lock (_updatingCurrencies)
            {
                _updatingCurrencies.Remove(currencyId);
            }
        }

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

        public ObservableProperty<long> GetObservableBalance(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                NexusLog.Warn("EconomyService", nameof(GetObservableBalance), "",
                    "Empty currencyId passed — returning null. Use the named ID constant.");
                return null;
            }

            lock (_balances)
            {
                if (!_balances.TryGetValue(currencyId, out var prop))
                {
                    prop = new ObservableProperty<long>(0);
                    _balances[currencyId] = prop;
                }
                return prop;
            }
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

            lock (_balances)
            {
                var prop = GetObservableBalance(currencyId);
                if (prop.Value < amount)
                {
                    NexusLog.Warn("EconomyService", nameof(Spend), currencyId,
                        $"Insufficient balance. Have {prop.Value}, need {amount}. Reason: {reason}");
                    return false;
                }

                prop.Value -= amount;
                NexusLog.Info("EconomyService", nameof(Spend), currencyId,
                    $"Spent {amount}. New balance: {prop.Value}. Reason: {reason}");
                return true;
            }
        }

        public void Earn(string currencyId, long amount, string reason = "")
        {
            if (amount <= 0) return;

            lock (_balances)
            {
                var prop = GetObservableBalance(currencyId);
                prop.Value += amount;
                NexusLog.Info("EconomyService", nameof(Earn), currencyId,
                    $"Earned {amount}. New balance: {prop.Value}. Reason: {reason}");
            }
        }

        public void SetBalance(string currencyId, long amount)
        {
            lock (_balances)
            {
                var prop = GetObservableBalance(currencyId);
                long old = prop.Value;
                prop.Value = amount;
                NexusLog.Info("EconomyService", nameof(SetBalance), currencyId,
                    $"Balance changed: {old} -> {amount}");
            }
        }

        public void Dispose()
        {
            lock (_balances)
            {
                foreach (var pair in _balances.Values)
                {
                    pair.ClearOnChanged();
                }
            }
        }
    }
}
