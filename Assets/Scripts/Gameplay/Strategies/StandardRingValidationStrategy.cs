namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for standard rings (GDD §17 movement rules).
    /// Standard rings can be placed on empty poles or on poles with matching color top rings.
    ///
    /// This strategy is also aliased for Glass, Ghost, and Mystery ring types in
    /// RingValidationStrategyManager, since all three follow identical placement rules.
    /// </summary>
    public sealed class StandardRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Standard;

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            // Locked poles can only accept Key rings (handled by KeyRingValidationStrategy)
            if (isPoleLocked) return false;

            // Cannot add if pole is full
            if (isPoleFull) return false;

            // GDD §33 — Stone: only same-color ring (or joker) may stack
            if (topRing.Type == RingType.Stone)
                return topRing.Color == ring.Color;

            // Empty pole — always accept
            if (topRing.Color == RingColor.None) return true;

            // GDD §31 — Frozen top: any ring can land (ice-break handled in MoveRingCommand)
            if (topRing.Type == RingType.Frozen) return true;

            // GDD §35, §39 — Rainbow/Paint on top: accept any ring (joker rule)
            if (topRing.Type == RingType.Rainbow || topRing.Type == RingType.Paint) return true;

            // Standard color-match rule
            return topRing.Color == ring.Color;
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            if (isPoleLocked) return false;
            // Standard rings can always be popped when the pole is not locked.
            // (Frozen and Stone are handled by their own strategies; this strategy
            //  is never called with those types as topRing because CanHandle = false.)
            return true;
        }
    }
}
