using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §12 — Central registry for VFX prefabs following Nexus DI principles.
    /// Provides prefab references through dependency injection instead of static fields,
    /// ensuring thread-safety, testability, and proper lifecycle management.
    /// This replaces the previous static GameObject fields in GameplayLifecycle.
    /// </summary>
    public sealed class VfxPrefabRegistry
    {
        public GameObject RingPopPrefab { get; set; }
        public GameObject ConfettiPrefab { get; set; }

        /// <summary>
        /// Validates that required prefabs are assigned for production builds.
        /// Called during editor validation or runtime initialization.
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
            
            return isValid;
        }

        /// <summary>
        /// Gets the ring pop prefab with null-check for graceful degradation.
        /// </summary>
        public GameObject GetRingPopPrefab()
        {
            if (RingPopPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] RingPopPrefab requested but not assigned.");
                return null;
            }
            return RingPopPrefab;
        }

        /// <summary>
        /// Gets the confetti prefab with null-check for graceful degradation.
        /// </summary>
        public GameObject GetConfettiPrefab()
        {
            if (ConfettiPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[VfxPrefabRegistry] ConfettiPrefab requested but not assigned.");
                return null;
            }
            return ConfettiPrefab;
        }
    }
}
