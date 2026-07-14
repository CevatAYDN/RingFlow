namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Paint rings. Paint rings act as jokers:
    /// they can be placed on any pole (any color) and accept any ring placed on them.
    /// The paint consumption logic is handled by PaintRingStrategy and BoardState.
    /// </summary>
    public sealed class PaintValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Paint;

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
