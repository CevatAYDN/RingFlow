using Nexus.Core;
using Nexus.Core.Services;
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
                NexusLog.Warn("VfxPrefabRegistry", nameof(Validate), "", "RingPopPrefab is not assigned. VFX for ring moves will be disabled.");
                isValid = false;
            }

            if (ConfettiPrefab == null)
            {
                NexusLog.Warn("VfxPrefabRegistry", nameof(Validate), "", "ConfettiPrefab is not assigned. Win celebration VFX will be disabled.");
                isValid = false;
            }

            if (MergeEffectPrefab == null)
            {
                NexusLog.Warn("VfxPrefabRegistry", nameof(Validate), "", "MergeEffectPrefab is not assigned. Pole complete merge VFX will fall back to legacy.");
                isValid = false;
            }

            return isValid;
        }

        public GameObject GetRingPopPrefab()
        {
            if (RingPopPrefab == null)
            {
                NexusLog.Warn("VfxPrefabRegistry", nameof(GetRingPopPrefab), "", "RingPopPrefab requested but not assigned.");
                return null;
            }
            return RingPopPrefab;
        }

        public GameObject GetConfettiPrefab()
        {
            if (ConfettiPrefab == null)
            {
                NexusLog.Warn("VfxPrefabRegistry", nameof(GetConfettiPrefab), "", "ConfettiPrefab requested but not assigned.");
                return null;
            }
            return ConfettiPrefab;
        }

        public GameObject GetMergeEffectPrefab()
        {
            if (MergeEffectPrefab == null)
            {
                NexusLog.Warn("VfxPrefabRegistry", nameof(GetMergeEffectPrefab), "", "MergeEffectPrefab requested but not assigned.");
                return null;
            }
            return MergeEffectPrefab;
        }
    }
}
