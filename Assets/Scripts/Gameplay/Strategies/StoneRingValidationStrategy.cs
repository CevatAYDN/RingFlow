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
            if (isPoleLocked) return false;
            if (isPoleFull) return false;
            
            // Cannot place any ring on top of a Stone ring (GDD §4) unless they share the same color
            if (topRing.Type == RingType.Stone) return topRing.Color == ring.Color;
            
            if (topRing.Color == RingColor.None) return true;
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            return topRing.Type != RingType.Stone;
        }
    }
}
