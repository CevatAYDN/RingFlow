namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Magnet rings. Magnet rings follow standard movement rules;
    /// their pull behavior is handled inline in MoveRingCommand and BoardState.
    /// </summary>
    public sealed class MagnetValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Magnet;

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
