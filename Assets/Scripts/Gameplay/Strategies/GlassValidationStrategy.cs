namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Glass rings. Glass rings are transparent — they accept
    /// any ring color on top and can be placed on any pole regardless of top color.
    /// </summary>
    public sealed class GlassValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Glass;

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked) return false;
            if (isPoleFull) return false;

            // Glass is transparent: accepts any color, and can be placed anywhere
            // that isn't locked or full. Stone restriction still applies for safety.
            if (topRing.Type == RingType.Stone) return topRing.Color == ring.Color;
            return true;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            return !isPoleLocked;
        }
    }
}
