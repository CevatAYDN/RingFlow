namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Ghost rings. Ghost rings follow standard movement rules;
    /// the Ghost reveal (Ghost -> Standard) is handled by SelectPoleCommand and BoardState.PopRing.
    /// </summary>
    public sealed class GhostValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Ghost;

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
