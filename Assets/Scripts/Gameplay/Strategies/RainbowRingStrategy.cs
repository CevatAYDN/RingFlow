using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
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
            if (context.MovingRing.Type == RingType.Rainbow)
            {
                if (context.ToPole.Rings.Count >= 2)
                {
                    var ringBelow = context.ToPole.Rings[context.ToPole.Rings.Count - 2];
                    context.RainbowTargetIndex = context.ToPole.Rings.Count - 1;
                    context.RainbowTargetOriginalColor = context.MovingRing.Color;
                    context.WasRainbowConverted = true;

                    var convertedRing = new RingData(ringBelow.Color, RingType.Standard);
                    context.ToPole.Rings[context.ToPole.Rings.Count - 1] = convertedRing;

                    NexusLog.Info("RainbowRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                        $"Rainbow converted to {ringBelow.Color} (from ring below). TargetIndex={context.RainbowTargetIndex}, OriginalColor={context.RainbowTargetOriginalColor}.");
                }
                else
                {
                    // Placed on an empty/otherwise-empty pole: leave the Rainbow unconverted.
                    // This matches BoardState.ResolvePaintAndRainbowSpecial (GDD §35: a Rainbow
                    // converts only when it contacts a ring). The ring stays Rainbow and converts
                    // the moment a ring is stacked on top of it.
                }
            }
        }

    }
}
