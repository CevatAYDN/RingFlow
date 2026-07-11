using System.Collections.Generic;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Pole state model following Nexus MVCS principles.
    /// Uses Strategy pattern for ring validation rules, enabling Open/Closed Principle compliance.
    /// Validation is delegated to RingValidationStrategyManager for extensibility.
    /// </summary>
    public class PoleState
    {
        private static RingValidationStrategyManager s_validationManager;

        public int Id { get; set; }
        public List<RingData> Rings { get; } = new(4);
        public int MaxCapacity { get; set; } = 4;
        public bool IsLocked { get; set; }

        public bool IsFull => Rings.Count >= MaxCapacity;
        public bool IsEmpty => Rings.Count == 0;

        public RingData TopRing => IsEmpty ? new RingData(RingColor.None) : Rings[^1];

        /// <summary>
        /// Sets the validation strategy manager (called during gameplay initialization).
        /// This follows Dependency Injection principles while keeping PoleState as a simple model.
        /// </summary>
        public static void SetValidationManager(RingValidationStrategyManager manager)
        {
            s_validationManager = manager;
        }

        /// <summary>
        /// Validates if a ring can be added to this pole using Strategy pattern.
        /// Delegates to RingValidationStrategyManager for rule execution.
        /// </summary>
        public bool CanAddRing(RingData ring)
        {
            if (s_validationManager == null)
            {
                // Fallback to legacy logic if manager not set (editor compatibility)
                return LegacyCanAddRing(ring);
            }

            // Special handling for universal rings (Rainbow, Paint)
            if (ring.Type == RingType.Rainbow || ring.Type == RingType.Paint)
            {
                return s_validationManager.CanAddUniversalRing(ring, TopRing, IsFull, IsLocked);
            }

            // Special handling for target pole having universal rings
            if (!IsEmpty && (TopRing.Type == RingType.Rainbow || TopRing.Type == RingType.Paint))
            {
                return s_validationManager.CanAddUniversalRing(ring, TopRing, IsFull, IsLocked);
            }

            // Standard validation through strategy manager
            return s_validationManager.CanAddRing(ring, TopRing, IsFull, IsLocked);
        }

        /// <summary>
        /// Validates if the top ring can be removed from this pole using Strategy pattern.
        /// Delegates to RingValidationStrategyManager for rule execution.
        /// </summary>
        public bool CanPopRing()
        {
            if (IsEmpty) return false;

            if (s_validationManager == null)
            {
                // Fallback to legacy logic if manager not set (editor compatibility)
                return LegacyCanPopRing();
            }

            return s_validationManager.CanPopRing(TopRing, IsLocked);
        }

        public void AddRing(RingData ring)
        {
            if (Rings.Count < MaxCapacity)
            {
                Rings.Add(ring);
            }
        }

        public RingData PopRing()
        {
            if (IsEmpty) return new RingData(RingColor.None);
            var ring = Rings[^1];
            Rings.RemoveAt(Rings.Count - 1);
            return ring;
        }

        #region Legacy Validation (Fallback for Editor Compatibility)

        private bool LegacyCanAddRing(RingData ring)
        {
            if (IsLocked)
            {
                return ring.Type == RingType.Locked;
            }
            if (IsFull) return false;
            if (IsEmpty) return true;

            if (TopRing.Type == RingType.Stone) return TopRing.Color == ring.Color;

            if (ring.Type == RingType.Rainbow || ring.Type == RingType.Paint) return true;

            if (TopRing.Type == RingType.Rainbow || TopRing.Type == RingType.Paint) return true;

            return TopRing.Color == ring.Color;
        }

        private bool LegacyCanPopRing()
        {
            if (IsLocked) return false;

            if (TopRing.Type == RingType.Frozen) return false;
            if (TopRing.Type == RingType.Stone) return false;

            return true;
        }

        #endregion
    }
}
