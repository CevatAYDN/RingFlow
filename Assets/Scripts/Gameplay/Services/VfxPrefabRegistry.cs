using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §12 — Central registry for VFX prefabs following Nexus DI principles.
    /// Provides prefab references through dependency injection instead of static fields,
    /// ensuring thread-safety, testability, and proper lifecycle management.
    /// </summary>
    public sealed class VfxPrefabRegistry
    {
        public GameObject RingPopPrefab { get; set; }
        public GameObject ConfettiPrefab { get; set; }
        public GameObject MergeEffectPrefab { get; set; }

        /// <summary>
        /// Validates that required prefabs are assigned for production builds.
        /// </summary>
        public bool Validate()
        {
            bool isValid = true;

            if (RingPopPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] RingPopPrefab is not assigned. VFX for ring moves will be disabled.");
                isValid = false;
            }

            if (ConfettiPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] ConfettiPrefab is not assigned. Win celebration VFX will be disabled.");
                isValid = false;
            }

            if (MergeEffectPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] MergeEffectPrefab is not assigned. Pole complete merge VFX will fall back to legacy.");
                isValid = false;
            }

            return isValid;
        }

        public GameObject GetRingPopPrefab()
        {
            if (RingPopPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] RingPopPrefab requested but not assigned.");
                return null;
            }
            return RingPopPrefab;
        }

        public GameObject GetConfettiPrefab()
        {
            if (ConfettiPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] ConfettiPrefab requested but not assigned.");
                return null;
            }
            return ConfettiPrefab;
        }

        public GameObject GetMergeEffectPrefab()
        {
            if (MergeEffectPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] MergeEffectPrefab requested but not assigned.");
                return null;
            }
            return MergeEffectPrefab;
        }
    }
}
