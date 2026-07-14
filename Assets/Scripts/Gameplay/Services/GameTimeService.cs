using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace RingFlow.Gameplay.Services
{
    public interface IGameTimeService
    {
        DateTime UtcNow { get; }
    }

    public sealed class GameTimeService : IGameTimeService, INexusService
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
