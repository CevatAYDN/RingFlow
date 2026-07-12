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
        private int _maxCapacity = 4;
        public int MaxCapacity
        {
            get => _maxCapacity;
            set
            {
                _maxCapacity = value;
                _ringCapacity = value;
            }
        }
        public bool IsLocked { get; set; }

        /// <summary>
        /// Portal partner pole ID. -1 means this pole is not a portal pole.
        /// When a ring is placed on a portal pole, it immediately teleports to the linked partner pole.
        /// </summary>
        public int PortalPartnerId { get; set; } = -1;

        private int _ringCapacity = 4;
        public int RingCapacity
        {
            get => _ringCapacity;
            set
            {
                _ringCapacity = value;
                _maxCapacity = value;
            }
        }

        public bool IsFull => Rings.Count >= RingCapacity;
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
        /// If validation manager is not set, uses direct validation rules.
        /// </summary>
        public bool CanAddRing(RingData ring)
        {
            if (s_validationManager == null)
            {
                return DirectCanAddRing(ring);
            }

            if (ring.Type == RingType.Rainbow || ring.Type == RingType.Paint)
            {
                return s_validationManager.CanAddUniversalRing(ring, TopRing, IsFull, IsLocked);
            }

            if (!IsEmpty && (TopRing.Type == RingType.Rainbow || TopRing.Type == RingType.Paint))
            {
                return s_validationManager.CanAddUniversalRing(ring, TopRing, IsFull, IsLocked);
            }

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
                // Use direct validation rules if manager not set (editor compatibility)
                return DirectCanPopRing();
            }

            return s_validationManager.CanPopRing(TopRing, IsLocked);
        }

        public void AddRing(RingData ring)
        {
            if (Rings.Count < RingCapacity)
            {
                Rings.Add(ring);
            }
        }

        public void SetCapacity(int capacity)
        {
            RingCapacity = capacity > 0 ? capacity : 4;
            MaxCapacity = RingCapacity;
        }

        public RingData PopRing()
        {
            if (IsEmpty) return new RingData(RingColor.None);
            var ring = Rings[^1];
            Rings.RemoveAt(Rings.Count - 1);
            return ring;
        }

        #region Direct Validation (Editor Compatibility)

        private bool DirectCanAddRing(RingData ring)
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

        private bool DirectCanPopRing()
        {
            if (IsLocked) return false;

            if (TopRing.Type == RingType.Frozen) return false;
            if (TopRing.Type == RingType.Stone) return false;

            return true;
        }

        #endregion
    }
}
