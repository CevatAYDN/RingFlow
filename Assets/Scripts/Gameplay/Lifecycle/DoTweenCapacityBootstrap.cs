using DG.Tweening;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Allocates DOTween's tween/sequence capacity once at startup to avoid
    /// runtime auto-growth warnings and per-call reallocations.
    /// Values are derived from worst-case VFX counts (confetti + merge + ring pop)
    /// with headroom for ring moves, selections, UI fades and ongoing sequences.
    /// </summary>
    public static class DoTweenCapacityBootstrap
    {
        private static bool s_initialized;

        public static void EnsureInitialized(int tweensCapacity = 1500, int sequencesCapacity = 200)
        {
            if (s_initialized) return;
            s_initialized = true;

            DOTween.SetTweensCapacity(tweensCapacity, sequencesCapacity);
        }

        public static void ResetForTests()
        {
            s_initialized = false;
        }
    }
}
