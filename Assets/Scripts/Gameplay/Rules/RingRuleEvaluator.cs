namespace RingFlow.Gameplay.Rules
{
    /// <summary>
    /// Pure placement/pop validation shared by runtime model, commands and solver.
    /// This is intentionally side-effect free; transformations (paint, rainbow, portal,
    /// magnet, chain, bomb tick) are applied by move simulation/execution layers.
    /// </summary>
    public static class RingRuleEvaluator
    {
        public static bool CanAddRing(RingData movingRing, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked)
                return movingRing.Type == RingType.Locked || movingRing.Type == RingType.Key;

            if (isPoleFull)
                return false;

            if (topRing.Color == RingColor.None)
                return true;

            if (topRing.Type == RingType.Stone)
                return topRing.Color == movingRing.Color;

            if (topRing.Type == RingType.Frozen)
                return true;

            if (movingRing.Type == RingType.Rainbow || movingRing.Type == RingType.Paint)
                return true;

            if (topRing.Type == RingType.Rainbow || topRing.Type == RingType.Paint)
                return true;

            return topRing.Color == movingRing.Color;
        }

        public static bool CanPopRing(RingData topRing, bool isPoleEmpty, bool isPoleLocked)
        {
            if (isPoleEmpty) return false;
            if (isPoleLocked) return false;
            if (topRing.Type == RingType.Frozen) return false;
            if (topRing.Type == RingType.Stone) return false;
            return true;
        }
    }
}
