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
        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private VfxPrefabRegistry _vfxRegistry;

        public ValueTask OnEnterAsync(object args, CancellationToken ct)
        {
            NexusLog.Info("WinState", nameof(OnEnterAsync), "",
                "Entered WinState.");
            _signalBus?.Fire(new ShowScreenSignal(ScreenType.Win));

            // M3: BGM multiplier should not persist across states — reset to full on win screen.
            if (_audio != null)
                _audio.BgmStateMultiplier = 1f;

            // Play win sound procedurally
            if (_audio != null)
            {
                var winClip = ProceduralAudio.GetOrCreateWinClip();
                _audio.PlaySfx(winClip, 1.0f);
            }

            // Spawn Confetti VFX through proper DI (Nexus pattern)
            if (_vfxRegistry != null && _objectPoolService != null)
            {
                var prefab = _vfxRegistry.GetConfettiPrefab();
                if (prefab != null)
                {
                    var confettiGo = _objectPoolService.Spawn(prefab, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                    var vfx = confettiGo.GetComponent<ConfettiVfx>();
                    if (vfx != null)
                    {
                        vfx.Initialize();
                    }
                }
            }

            return default;
        }

        public ValueTask OnExitAsync(CancellationToken ct) => default;
        public void OnTick(float deltaTime) {}
    }
}
