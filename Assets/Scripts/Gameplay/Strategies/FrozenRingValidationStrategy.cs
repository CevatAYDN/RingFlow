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
            if (isPoleLocked) return false;
            if (isPoleFull) return false;
            return true;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            return topRing.Type != RingType.Frozen;
        }
    }
}
