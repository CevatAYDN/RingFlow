namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Stone rings following GDD §4.
    /// Stone rings cannot be moved once placed and no rings can be placed on top of them.
    /// </summary>
    public sealed class StoneRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Stone;
        }

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            // Stone rings cannot be placed on locked poles
            if (isPoleLocked) return false;
            
            // Cannot add if pole is full
            if (isPoleFull) return false;
            
            // Stone rings can be placed on empty poles or matching color poles
            if (topRing.Color == RingColor.None) return true;
            
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            // Stone rings can never be moved (GDD §4)
            return false;
        }
    }
}
