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

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            if (_progress == null) return default;

            // Initialize Observables
            var coinsObs = GetObservableBalance("Coins");
            var diamondsObs = GetObservableBalance("Diamonds");

            coinsObs.Value = _progress.Coins.Value;
            diamondsObs.Value = _progress.Diamonds.Value;

            // Hook changes: PlayerProgressModel -> EconomyService (with recursion guard)
            _progress.Coins.OnChanged((_, newVal) => 
            {
                if (coinsObs.Value != newVal) coinsObs.Value = newVal;
            });
            _progress.Diamonds.OnChanged((_, newVal) => 
            {
                if (diamondsObs.Value != newVal) diamondsObs.Value = newVal;
            });

            // Hook changes: EconomyService -> PlayerProgressModel (with recursion guard)
            coinsObs.OnChanged((_, newVal) => 
            {
                if (_progress.Coins.Value != newVal) _progress.Coins.Value = (int)newVal;
            });
            diamondsObs.OnChanged((_, newVal) => 
            {
                if (_progress.Diamonds.Value != newVal) _progress.Diamonds.Value = (int)newVal;
            });

            return default;
        }

        public void OnDispose() => Dispose();

        public ObservableProperty<long> GetObservableBalance(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId)) return null;

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
                if (prop.Value < amount) return false;

                prop.Value -= amount;
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
            }
        }

        public void SetBalance(string currencyId, long amount)
        {
            lock (_balances)
            {
                var prop = GetObservableBalance(currencyId);
                prop.Value = amount;
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
