using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Rainbow rings (GDD §35).
    ///
    /// GDD §35: "Rainbow Ring her renk ile eşleşebilir. Boş veya dolu bir çubuğa
    /// yerleştirildiğinde temas ettiği ilk halkanın rengine dönüşür ve o renkte sabitlenir."
    ///
    /// Conversion rules:
    ///   1. Rainbow placed on a pole with at least one ring → converts to the color of
    ///      the ring directly below it. Type becomes Standard.
    ///   2. Rainbow placed on an empty pole → stays Rainbow until another ring lands
    ///      on top. MoveRingCommand handles case 2: when the ring below the newly placed
    ///      ring is Rainbow, ExecutePostMoveExecution(RingType.Rainbow, ...) is called,
    ///      routing back here with the Rainbow ring now sitting below the new ring.
    ///      In that call context.MovingRing is the NEW ring (not Rainbow), so we convert
    ///      the Rainbow ring that is NOW below by inspecting ToPole.Rings[Count-2].
    ///
    /// ANALYSIS (FIX-R1):
    ///   The original implementation only converted when context.MovingRing.Type == Rainbow.
    ///   That missed the "empty pole" case: when another ring later lands on top of a
    ///   still-unconverted Rainbow ring, MoveRingCommand fires PostMoveExecution(Rainbow)
    ///   but context.MovingRing is the NEW ring, not the Rainbow. The conversion was
    ///   silently skipped, leaving the Rainbow ring unconverted forever.
    ///
    ///   Fix: check whether the ring at [Count-2] (the ring just below the new arrival)
    ///   is Rainbow and convert it to the color of the ring that landed on top ([Count-1]).
    ///   This correctly handles both the direct-placement and delayed-conversion cases.
    /// </summary>
    public sealed class RainbowRingStrategy : IRingMoveStrategy
    {
        private readonly GameConfigDatabaseSO _db;

        public RainbowRingStrategy(GameConfigDatabaseSO db)
        {
            _db = db;
        }

        public bool CanHandle(RingType ringType) => ringType == RingType.Rainbow;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            int count = context.ToPole.Rings.Count;

            // ── Case 1: Rainbow was MOVED and lands on a pole with a ring below it ──
            if (context.MovingRing.Type == RingType.Rainbow && count >= 2)
            {
                var ringBelow = context.ToPole.Rings[count - 2];
                ConvertRainbow(ref context, count - 1, ringBelow.Color,
                    source: $"landed on ring-below (color={ringBelow.Color})");
                return;
            }

            // ── Case 2: Another ring landed ON TOP of a still-unconverted Rainbow ──
            // MoveRingCommand fires ExecutePostMoveExecution(RingType.Rainbow) when
            // the ring at [Count-2] is Rainbow. Convert that Rainbow to the color of
            // the ring that just arrived at [Count-1].
            if (context.MovingRing.Type != RingType.Rainbow && count >= 2)
            {
                var rainbowCandidate = context.ToPole.Rings[count - 2];
                if (rainbowCandidate.Type == RingType.Rainbow)
                {
                    var newTop = context.ToPole.Rings[count - 1];
                    ConvertRainbow(ref context, count - 2, newTop.Color,
                        source: $"ring landed on top (color={newTop.Color})");
                }
            }

            // ── Case 3: Rainbow placed on an empty pole ──
            // No conversion yet — stays Rainbow. Will convert when next ring lands (Case 2).
#if DEVELOPMENT_BUILD
            if (context.MovingRing.Type == RingType.Rainbow && count == 1)
            {
                NexusLog.Info("RainbowRingStrategy", nameof(PostMoveExecution),
                    context.ToPoleId.ToString(),
                    "Rainbow placed on empty pole — conversion deferred until a ring lands on top.");
            }
#endif
        }

        private static void ConvertRainbow(ref MoveContext context, int ringIndex, RingColor targetColor, string source)
        {
            context.RainbowTargetIndex         = ringIndex;
            context.RainbowTargetOriginalColor = context.ToPole.Rings[ringIndex].Color;
            context.WasRainbowConverted        = true;

            context.ToPole.Rings[ringIndex] = new RingData(targetColor, RingType.Standard);

            NexusLog.Info("RainbowRingStrategy", "ConvertRainbow",
                context.ToPoleId.ToString(),
                $"Rainbow[{ringIndex}] converted to {targetColor} ({source}). " +
                $"OriginalColor={context.RainbowTargetOriginalColor}.");
        }
    }
}
