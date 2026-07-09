namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Frozen rings following GDD §4.
    /// Frozen rings cannot be moved until the ice is broken by matching color rings.
    /// </summary>
    public sealed class FrozenRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Frozen;
        }

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            // Frozen rings cannot be placed on locked poles
            if (isPoleLocked) return false;
            
            // Cannot add if pole is full
            if (isPoleFull) return false;
            
            // Frozen rings can be placed on empty poles or matching color poles
            if (topRing.Color == RingColor.None) return true;
            
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            // Frozen rings cannot be moved until ice is broken (GDD §4)
            return false;
        }
    }
}
