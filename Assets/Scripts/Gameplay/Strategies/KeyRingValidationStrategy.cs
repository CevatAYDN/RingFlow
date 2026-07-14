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
            // Key rings can only be placed on locked poles (to unlock them)
            if (isPoleLocked) return true;

            // Key rings can be placed on unlocked poles — engine/solver handle color matching
            return true;
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
