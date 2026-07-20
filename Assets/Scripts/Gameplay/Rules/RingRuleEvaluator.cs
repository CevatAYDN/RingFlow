using Nexus.Core.Services;

namespace RingFlow.Gameplay.Rules
{
    /// <summary>
    /// Pure placement/pop validation shared by runtime model, commands and solver.
    /// This is intentionally side-effect free; transformations (paint, rainbow, portal,
    /// magnet, chain, bomb tick) are applied by move simulation/execution layers.
    ///
    /// GDD §17, §29-§43 — all special-ring placement rules live here as the single
    /// source of truth. Strategy classes delegate to this for their core logic.
    /// </summary>
    public static class RingRuleEvaluator
    {
        /// <summary>
        /// Returns true if <paramref name="movingRing"/> can be placed on top of
        /// <paramref name="topRing"/> on a pole with the given state flags.
        ///
        /// GDD §17 — a move is valid when:
        ///   • Target pole is not full and not locked (unless unlocked by Key).
        ///   • Empty pole (topRing.Color == None) always accepts any ring.
        ///   • Stone top: only same-color ring may land on it.
        ///   • Frozen top: any ring may land on it (color-matching breaks ice in MoveRingCommand).
        ///   • Rainbow / Paint moving: joker — lands anywhere.
        ///   • Rainbow / Paint on top: accepts any incoming ring.
        ///   • Glass moving: joker — lands anywhere (transparent, accepts any color match).
        ///   • Glass on top: accepts any incoming ring (transparent, acts as empty).
        ///   • Otherwise: color must match.
        ///
        /// Sequential joker conditions ordered by priority:
        ///   1. Locked/Full guards
        ///   2. Empty acceptance
        ///   3. Stone restriction
        ///   4. Frozen acceptance
        ///   5. Moving Rainbow/Paint/Glass joker
        ///   6. Top Rainbow/Paint/Ghost/Glass joker acceptance
        ///   7. Standard color match
        /// </summary>
        public static bool CanAddRing(RingData movingRing, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked)
                return movingRing.Type == RingType.Locked || movingRing.Type == RingType.Key;

            if (isPoleFull)
                return false;

            // Empty pole — always accept
            if (topRing.Color == RingColor.None)
                return true;

            // GDD §33 — Stone: only same-color ring may stack on stone
            if (topRing.Type == RingType.Stone)
                return topRing.Color == movingRing.Color;

            // GDD §31 — Frozen top: any ring can land (color-matching breaks ice in MoveRingCommand)
            if (topRing.Type == RingType.Frozen)
                return true;

            // GDD §35, §39 — Rainbow/Paint as moving ring: joker placement
            if (movingRing.Type == RingType.Rainbow || movingRing.Type == RingType.Paint)
                return true;

            // GDD §35, §39 — Rainbow/Paint on top: accepts any incoming ring
            if (topRing.Type == RingType.Rainbow || topRing.Type == RingType.Paint)
                return true;

            // Glass moving: joker — can be placed anywhere (transparent, no color)
            if (movingRing.Type == RingType.Glass)
                return true;

            // Glass on top: accepts any incoming ring (transparent, acts as empty)
            if (topRing.Type == RingType.Glass)
                return true;

            // Standard color-match rule
            return topRing.Color == movingRing.Color;
        }

        /// <summary>
        /// Returns true if the top ring of a pole can be picked up (popped).
        ///
        /// GDD §17, §31, §33:
        ///   • Empty pole: cannot pop (nothing there).
        ///   • Locked pole: cannot pop anything.
        ///   • Frozen ring: cannot be moved until ice is broken (GDD §31).
        ///   • Stone ring: never moves (GDD §33).
        ///   • All other ring types: can be popped.
        /// </summary>
        public static bool CanPopRing(RingData topRing, bool isPoleEmpty, bool isPoleLocked)
        {
            if (isPoleEmpty)  return false;
            if (isPoleLocked) return false;
            if (topRing.Type == RingType.Frozen) return false;
            if (topRing.Type == RingType.Stone)  return false;
            return true;
        }

#if DEVELOPMENT_BUILD
        /// <summary>
        /// Dev-only helper: logs the reason a CanAddRing call returned false.
        /// </summary>
        public static string DescribeCannotAddReason(RingData movingRing, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked && movingRing.Type != RingType.Locked && movingRing.Type != RingType.Key)
                return "Pole locked and ring is not a Key/Locked type";
            if (isPoleFull)
                return "Pole full";
            if (topRing.Type == RingType.Stone && topRing.Color != movingRing.Color)
                return $"Stone color mismatch: stone={topRing.Color}, moving={movingRing.Color}";
            if (topRing.Color != RingColor.None && topRing.Color != movingRing.Color
                && movingRing.Type != RingType.Rainbow && movingRing.Type != RingType.Paint && movingRing.Type != RingType.Glass
                && topRing.Type  != RingType.Rainbow && topRing.Type  != RingType.Paint
                && topRing.Type  != RingType.Frozen
                && topRing.Type  != RingType.Glass)
                return $"Color mismatch: top={topRing.Color}, moving={movingRing.Color}";
            return "Unknown";
        }
#endif
    }
}
