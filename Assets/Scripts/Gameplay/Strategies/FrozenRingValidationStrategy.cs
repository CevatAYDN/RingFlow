using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Validation strategy for Frozen rings (GDD §31).
    ///
    /// GDD §31: "Donmuş halka doğrudan taşınamaz. Kilidinin açılması için üzerindeki
    /// tüm halkaların kaldırılması gerekir. Kaldırıldığında buz çatırtısı efektiyle
    /// normal halkaya dönüşür."
    ///
    /// ANALYSIS (FIX-F1):
    ///   This strategy is registered for RingType.Frozen and is invoked only when
    ///   the TOP ring of a pole is Frozen.
    ///
    ///   CanPopRing:
    ///     The old implementation returned `topRing.Type != RingType.Frozen`.
    ///     Since this strategy is ONLY called when topRing IS Frozen, that expression
    ///     always evaluates to false — which happens to be the correct answer (a frozen
    ///     ring on top cannot be picked up). However the expression is misleading:
    ///     it reads as "the ring is not frozen → can pop", implying the strategy
    ///     could receive a non-frozen topRing, which it cannot by contract.
    ///     Replaced with an explicit `return false` + comment for clarity.
    ///
    ///   CanAddRing:
    ///     Any ring can land on a Frozen top (GDD §31 — ice-break is triggered by
    ///     matching color, handled in MoveRingCommand). No additional restriction.
    ///     The locked-pole and full-pole guards are still applied first.
    ///
    ///   Stack-middle Frozen rings:
    ///     A Frozen ring that is NOT on top is unreachable by CanPopRing because
    ///     the ring above it would be the top ring — and that top ring IS poppable
    ///     (unless also Frozen/Stone/Locked). Once all rings above a Frozen ring are
    ///     removed, the Frozen ring becomes the new top and CanPopRing correctly
    ///     returns false, preventing the player from moving it until ice is broken
    ///     (MoveRingCommand.ExecuteCoreMove: `breaksIce` when same-color ring lands on it).
    /// </summary>
    public sealed class FrozenRingValidationStrategy : IRingValidationStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Frozen;

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked) return false;
            if (isPoleFull)   return false;

            // Empty pole — always accept
            if (topRing.Color == RingColor.None) return true;

            // Stone beneath: only same-color ring may land (stone rule takes precedence)
            if (topRing.Type == RingType.Stone)
                return topRing.Color == ring.Color;

            // Frozen top: any ring can land — ice-break logic in MoveRingCommand
            // checks if the landing ring's color matches the frozen ring's color.
            if (topRing.Type == RingType.Frozen) return true;

            // Rainbow / Paint on top: accept any ring
            if (topRing.Type == RingType.Rainbow || topRing.Type == RingType.Paint) return true;

            // Standard color-match
            return topRing.Color == ring.Color;
        }

        /// <summary>
        /// GDD §31 — a Frozen ring on top CANNOT be popped.
        /// This strategy is only invoked when topRing.Type == Frozen (see CanHandle),
        /// so the answer is always false: frozen rings cannot be moved until thawed.
        /// The locked-pole check is handled by RingRuleEvaluator before strategies run.
        /// </summary>
        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            // GDD §31: Frozen ring is immovable until ice is broken by a same-color ring
            // landing on it. Even if the pole is not locked, a frozen top cannot be selected.
#if DEVELOPMENT_BUILD
            if (isPoleLocked)
            {
                NexusLog.Info("FrozenRingValidationStrategy", nameof(CanPopRing),
                    topRing.Color.ToString(),
                    "CanPopRing=false (locked pole + frozen top).");
            }
#endif
            return false; // Frozen ring on top can never be popped directly
        }
    }
}
