using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RingFlow.Gameplay.Services
{
    /// <summary>
    /// DI-injectable contract for procedural audio clip generation.
    /// Replaces the static ProceduralAudio class to comply with AGENTS.md:
    /// "Never use static mutable state" and "All dependencies must be injected."
    /// 
    /// ProceduralAudio static class remains as a backward-compatible thin wrapper
    /// during migration. New code should inject IProceduralAudioService instead.
    /// </summary>
    public interface IProceduralAudioService
    {
        AudioClip GetOrCreateMoveClip();
        AudioClip GetOrCreateWinClip();
        AudioClip GetOrCreateErrorClip();
        AudioClip GetOrCreateExplosionClip();
        AudioClip GetOrCreatePoleCompleteClip();
        AudioClip GetOrCreateRichPoleCompleteClip(int ringCount);
        AudioClip GetOrCreateFinalPoleClip();
        AudioClip GetOrCreateBgmClip(int worldIndex);
        AudioClip GetOrCreateChainClip();
        AudioClip GetOrCreateMagnetClip();
        AudioClip GetOrCreatePaintClip();
        AudioClip GetOrCreateIceBreakClip();
        AudioClip GetOrCreateStoneImpactClip();
        AudioClip GetOrCreateBombExplosionClip();
        AudioClip GetOrCreatePortalClip();
        void ClearCache();
    }
}
