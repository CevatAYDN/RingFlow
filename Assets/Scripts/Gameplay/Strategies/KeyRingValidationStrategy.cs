namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Key (Locked) rings following GDD §4.
    /// Key rings can unlock locked poles and can be placed on any pole.
    /// </summary>
    public sealed class KeyRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Locked;
        }

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            // Key rings can be placed on locked poles (to unlock them)
            if (isPoleLocked) return true;
            
            // Cannot add if pole is full
            if (isPoleFull) return false;
            
            // Key rings can be placed on any pole (empty or with matching color)
            if (topRing.Color == RingColor.None) return true;
            
            // Key rings follow color matching when not unlocking
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            // Key rings cannot be popped from locked poles
            if (isPoleLocked) return false;
            
            // Key rings can be popped normally
            return true;
        }
    }
}
