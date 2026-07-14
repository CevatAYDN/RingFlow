using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using RingFlow.Gameplay.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// INexusService implementation of IProceduralAudioService.
    /// Delegates to the static ProceduralAudio class during the migration period.
    /// Once all callers inject this service instead of calling ProceduralAudio directly,
    /// the static class can be internalized or removed.
    ///
    /// Bound in GameplayLifecycle.OnConfigure() via:
    ///   builder.BindService&lt;IProceduralAudioService, ProceduralAudioService&gt;();
    /// </summary>
    public class ProceduralAudioService : IProceduralAudioService, INexusService
    {
        private readonly AudioConfigSO _config;

        public ProceduralAudioService(AudioConfigSO config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config),
                "[ProceduralAudioService] AudioConfigSO is required.");
        }

        // INexusService lifecycle
        public ValueTask InitializeAsync(CancellationToken ct)
        {
            // Ensure the static wrapper is also initialized for backward-compatible call-sites.
            ProceduralAudio.Initialize(_config);
            return default;
        }

        public void OnDispose()
        {
            ProceduralAudio.ClearCache();
        }

        // ── Delegation to static cache (zero extra allocation) ───────────────
        public AudioClip GetOrCreateMoveClip()                        => ProceduralAudio.GetOrCreateMoveClip();
        public AudioClip GetOrCreateWinClip()                         => ProceduralAudio.GetOrCreateWinClip();
        public AudioClip GetOrCreateErrorClip()                       => ProceduralAudio.GetOrCreateErrorClip();
        public AudioClip GetOrCreateExplosionClip()                   => ProceduralAudio.GetOrCreateExplosionClip();
        public AudioClip GetOrCreatePoleCompleteClip()                => ProceduralAudio.GetOrCreatePoleCompleteClip();
        public AudioClip GetOrCreateRichPoleCompleteClip(int ringCount)=> ProceduralAudio.GetOrCreateRichPoleCompleteClip(ringCount);
        public AudioClip GetOrCreateFinalPoleClip()                   => ProceduralAudio.GetOrCreateFinalPoleClip();
        public AudioClip GetOrCreateBgmClip(int worldIndex)           => ProceduralAudio.GetOrCreateBgmClip(worldIndex);
        public void ClearCache()                                      => ProceduralAudio.ClearCache();
    }
}
