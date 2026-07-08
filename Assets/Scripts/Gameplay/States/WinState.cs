using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class WinState : IGameState
    {
        [Inject] private ISignalBus _signalBus;
        [Inject] private IAudioService _audio;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Win));

            // Play win sound procedurally
            if (_audio != null)
            {
                var winClip = ProceduralAudio.GetOrCreateWinClip();
                _audio.PlaySfx(winClip, 1.0f);
            }

            // Spawn Confetti VFX
            var pool = NexusRuntime.CurrentContext?.TryResolve<IObjectPoolService>();
            if (pool != null && GameplayLifecycle.ConfettiPrefab != null)
            {
                var confettiGo = pool.Spawn(GameplayLifecycle.ConfettiPrefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                var vfx = confettiGo.GetComponent<ConfettiVfx>();
                if (vfx != null)
                {
                    vfx.Initialize();
                }
            }

            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
