using System.Collections.Generic;
using RingFlow.Gameplay.Rules;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Pole state model following Nexus MVCS principles.
    /// Validation rules are self-contained via direct inline logic (DirectCanAddRing /
    /// DirectCanPopRing). The <see cref="Strategies.RingValidationStrategyManager"/> is
    /// injected into Commands (SelectPoleCommand, MoveRingCommand) which is the correct
    /// MVCS boundary — Models must not hold service references.
    /// </summary>
    public class PoleState
    {
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
        /// Validates if a ring can be added to this pole.
        /// Handles all standard and special ring type rules inline.
        /// </summary>
        public bool CanAddRing(RingData ring)
        {
            return DirectCanAddRing(ring);
        }

        /// <summary>
        /// Validates if the top ring can be removed from this pole.
        /// </summary>
        public bool CanPopRing()
        {
            if (IsEmpty) return false;
            return DirectCanPopRing();
        }

        public void AddRing(RingData ring)
        {
            if (Rings.Count >= RingCapacity)
            {
                return;
            }

            // FIX-A2: Ice-breaking side-effect REMOVED from AddRing.
            // MoveRingCommand.ExecuteCoreMove owns the ice-break contract:
            //   1. Checks frozen condition BEFORE calling AddRing.
            //   2. Calls AddRing (pure data append, no side-effects).
            //   3. Fires BreakIceSignal and records WasIceBroken for undo.
            // Duplicating thaw logic here caused double-thaw: ring thawed by
            // AddRing AND signal fired again by the command, producing wrong
            // undo restores and duplicate ice-break VFX.
            // PoleState is a pure Model (Nexus MVCS §74.1) — no rule side-effects.
            Rings.Add(ring);
        }

        public RingData PopRingRaw()
        {
            if (IsEmpty) return new RingData(RingColor.None);
            var ring = Rings[^1];
            Rings.RemoveAt(Rings.Count - 1);
            return ring;
        }

        public void AddRingRaw(RingData ring)
        {
            if (Rings.Count < RingCapacity)
                Rings.Add(ring);
        }

        public void InsertRingRaw(int index, RingData ring)
        {
            if (index < 0) index = 0;
            if (index > Rings.Count) index = Rings.Count;
            if (Rings.Count < RingCapacity)
                Rings.Insert(index, ring);
        }

        public void SetCapacity(int capacity)
        {
            RingCapacity = capacity > 0 ? capacity : GameplayAssetKeys.Tuning.MaxCapacity;
            MaxCapacity = RingCapacity;
        }

        public RingData PopRing()
        {
            if (IsEmpty) return new RingData(RingColor.None);
            var ring = Rings[^1];
            Rings.RemoveAt(Rings.Count - 1);

            // Auto-thaw: if the new top ring is Frozen, thaw it to prevent softlock
            if (Rings.Count > 0 && Rings[^1].Type == RingType.Frozen)
            {
                var newTop = Rings[^1];
                Rings[^1] = new RingData(newTop.Color, RingType.Standard, newTop.AdditionalData);
            }

            return ring;
        }

        #region Validation Rules

        private bool DirectCanAddRing(RingData ring)
        {
            return RingRuleEvaluator.CanAddRing(ring, TopRing, IsFull, IsLocked);
        }

        private bool DirectCanPopRing()
        {
            return RingRuleEvaluator.CanPopRing(TopRing, IsEmpty, IsLocked);
        }

        #endregion
    }
}
