namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Bomb rings. Bomb rings follow standard movement rules;
    /// they can be placed and moved freely. The countdown/ticking is handled by MoveRingCommand.
    /// </summary>
    public sealed class BombValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Bomb;

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
