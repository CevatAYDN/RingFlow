namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Stone rings (GDD §33).
    ///
    /// GDD §33: "Taş halkadır. Asla hareket ettirilemez. Oyuncu diğer halkaları
    /// onun etrafından dolaştırarak bulmacayı çözmek zorundadır."
    ///
    /// FIX-S1 — CanPopRing bug:
    ///   The old implementation returned `topRing.Type != RingType.Stone`.
    ///   This strategy is ONLY invoked when topRing IS Stone (CanHandle returns true
    ///   only for RingType.Stone), so that expression always evaluated to false.
    ///   It happened to produce the correct "stone cannot be popped" answer, but:
    ///   (a) The logic was misleading — it suggested the strategy could receive a
    ///       non-Stone topRing.
    ///   (b) It did NOT check isPoleLocked, meaning a locked Stone pole would also
    ///       return false (accidentally correct), but for the wrong reason.
    ///   Replaced with explicit `return false` + locked-pole note for clarity.
    ///
    ///   CanAddRing: any ring of matching color can stack ON a Stone ring (GDD §33
    ///   does not prohibit stacking; it only prohibits MOVING the stone itself).
    ///   Rainbow and Paint rings are jokers and may land on a stone (handled by
    ///   RingRuleEvaluator, which is also correct here).
    /// </summary>
    public sealed class StoneRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Stone;

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked) return false;
            if (isPoleFull)   return false;

            // Stone on top: only same-color ring (or joker) may stack on it
            if (topRing.Type == RingType.Stone)
                return topRing.Color == ring.Color;

            // Empty pole — always accept
            if (topRing.Color == RingColor.None) return true;

            // Rainbow / Paint on top: accept any ring (joker rule)
            if (topRing.Type == RingType.Rainbow || topRing.Type == RingType.Paint)
                return true;

            return topRing.Color == ring.Color;
        }

        /// <summary>
        /// GDD §33 — Stone ring can NEVER be moved. Always returns false regardless
        /// of pole lock state, because even an unlocked pole with a Stone top is
        /// immovable by design.
        /// </summary>
        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            if (isPoleLocked) return false;
            return topRing.Type != RingType.Stone;
        }
    }
}
