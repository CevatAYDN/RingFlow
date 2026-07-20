using Nexus.Core;
using RingFlow.Gameplay.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Thin static proxy that delegates to the DI-injected IProceduralAudioService.
    /// Allows existing caller code to run without compile-time breakage during migration.
    /// New code should inject IProceduralAudioService directly.
    /// </summary>
    public static class ProceduralAudio
    {
        private static IProceduralAudioService Service
        {
            get
            {
                var context = NexusRuntime.CurrentContext;
                if (context != null)
                {
                    var service = context.TryResolve<IProceduralAudioService>();
                    if (service != null)
                        return service;
                }

                throw new System.InvalidOperationException(
                    "[ProceduralAudio] IProceduralAudioService not available. " +
                    "Ensure it is bound in GameplayLifecycle.OnConfigure() via " +
                    "builder.BindService&lt;IProceduralAudioService, ProceduralAudioService&gt;().");
            }
        }

        /// <summary>
        /// No-op kept for backward compatibility. IProceduralAudioService is initialized
        /// by the DI container automatically.
        /// </summary>
        public static void Initialize(AudioConfigSO config) { }

        public static AudioClip GetOrCreateMoveClip()                        => Service.GetOrCreateMoveClip();
        public static AudioClip GetOrCreateWinClip()                         => Service.GetOrCreateWinClip();
        public static AudioClip GetOrCreateErrorClip()                       => Service.GetOrCreateErrorClip();
        public static AudioClip GetOrCreateExplosionClip()                   => Service.GetOrCreateExplosionClip();
        public static AudioClip GetOrCreatePoleCompleteClip()                => Service.GetOrCreatePoleCompleteClip();
        public static AudioClip GetOrCreateRichPoleCompleteClip(int ringCount)=> Service.GetOrCreateRichPoleCompleteClip(ringCount);
        public static AudioClip GetOrCreateFinalPoleClip()                   => Service.GetOrCreateFinalPoleClip();
        public static AudioClip GetOrCreateBgmClip(int worldIndex)           => Service.GetOrCreateBgmClip(worldIndex);
        public static void ClearCache()                                      => Service.ClearCache();
    }
}
