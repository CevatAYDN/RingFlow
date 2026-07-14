namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Chain rings. Chain rings follow standard movement rules
    /// for basic validation. The chain partner capacity check is handled inline in
    /// BoardState and MoveRingCommand.
    /// </summary>
    public sealed class ChainValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Chain;

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
