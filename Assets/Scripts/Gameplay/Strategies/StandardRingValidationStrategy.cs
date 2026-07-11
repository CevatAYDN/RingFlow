namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for standard rings following GDD §3 movement rules.
    /// Standard rings can be placed on empty poles or on poles with matching color top rings.
    /// </summary>
    public sealed class StandardRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Standard;
        }

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            // Locked poles can only accept Key rings (handled by KeyRingValidationStrategy)
            if (isPoleLocked) return false;
            
            // Cannot add if pole is full
            if (isPoleFull) return false;
            
            // Stone rings cannot have any rings placed on top of them (GDD §4) unless they share the same color
            if (topRing.Type == RingType.Stone) return topRing.Color == ring.Color;
            
            // Can always add to empty pole
            if (topRing.Color == RingColor.None) return true;
            
            // Can only add if colors match
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            // Cannot pop from locked pole
            if (isPoleLocked) return false;
            
            // Standard rings can always be popped (no restrictions)
            return true;
        }
    }
}
