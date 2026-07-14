using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD State Machine LOSE state.
    /// Entered when the player fails a level (bomb explosion, out of moves, etc.).
    /// </summary>
    public class LoseState : IGameState
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private IAudioService _audio;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            string reason = args is LevelLostSignal lost ? lost.Reason : "unknown";
            NexusLog.Info("LoseState", nameof(OnEnterAsync), "", $"Level lost. Reason: {reason}");
            // M3: Reset BGM multiplier so lose/game-over music plays at full volume.
            if (_audio != null)
                _audio.BgmStateMultiplier = 1f;
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.GameOver));
            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}