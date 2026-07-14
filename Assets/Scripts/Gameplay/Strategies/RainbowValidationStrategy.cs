namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Rainbow rings. Rainbow rings act as jokers:
    /// they can be placed on any pole (any color) and accept any ring placed on them.
    /// </summary>
    public sealed class RainbowValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Rainbow;

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked) return ring.Type == RingType.Locked;
            if (isPoleFull) return false;
            if (topRing.Color == RingColor.None) return true;
            return true;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            return !isPoleLocked;
        }
    }
}
