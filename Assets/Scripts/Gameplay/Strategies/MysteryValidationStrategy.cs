namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Mystery rings. Mystery rings follow standard movement rules;
    /// the Mystery reveal (Mystery -> Standard + random color) is handled by MysteryRingStrategy.
    /// </summary>
    public sealed class MysteryValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Mystery;

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked) return false;
            if (isPoleFull) return false;
            if (topRing.Type == RingType.Stone) return topRing.Color == ring.Color;
            if (topRing.Color == RingColor.None) return true;
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            return !isPoleLocked;
        }
    }
}
