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
            // Only a Golden Key (Locked) or an editor-authored Key ring may unlock a locked pole (GDD §32).
            if (isPoleLocked) return ring.Type.IsLockedKey();
            if (isPoleFull) return false;
            if (topRing.Color == RingColor.None) return true;
            if (topRing.Type == RingType.Stone) return topRing.Color == ring.Color;
            if (topRing.Type == RingType.Rainbow || topRing.Type == RingType.Paint) return true;
            return topRing.Color == ring.Color;
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
