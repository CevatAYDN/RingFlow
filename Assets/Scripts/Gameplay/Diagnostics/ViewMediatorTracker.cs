using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using UnityEngine.Scripting;

namespace RingFlow.Gameplay.Diagnostics
{
    [Preserve]
    public interface IViewMediatorTracker : INexusService
    {
        void TrackViewBound(Type viewType, Type mediatorType);
        void TrackViewUnbound(Type viewType);
        string GetBindingReport();
    }

    [Preserve]
    public class ViewMediatorTracker : IViewMediatorTracker
    {
        [Inject] private IGameDiagnostics _diag;

        private readonly Dictionary<Type, int> _viewCounts = new();

        public ValueTask InitializeAsync(CancellationToken ct)
        {
            _diag?.Log("ViewBinding", "View/Mediator Tracker initialized");
            return default;
        }

        public void TrackViewBound(Type viewType, Type mediatorType)
        {
            lock (_viewCounts)
            {
                _viewCounts.TryGetValue(viewType, out var count);
                _viewCounts[viewType] = count + 1;
            }

            _diag?.Log("ViewBinding", $"View bound: {viewType.Name} → Mediator: {mediatorType.Name}");
        }

        public void TrackViewUnbound(Type viewType)
        {
            lock (_viewCounts)
            {
                _viewCounts.TryGetValue(viewType, out var count);
                if (count > 0)
                {
                    _viewCounts[viewType] = count - 1;
                }
            }

            _diag?.Log("ViewBinding", $"View unbound: {viewType.Name}");
        }

        public string GetBindingReport()
        {
            var sb = new System.Text.StringBuilder("=== View/Mediator Binding Report ===\n");
            lock (_viewCounts)
            {
                foreach (var kvp in _viewCounts)
                {
                    sb.Append("  ").Append(kvp.Key.Name).Append(": ").Append(kvp.Value).AppendLine(" active");
                }
            }
            return sb.ToString();
        }

        public void OnDispose() {}
    }
}
