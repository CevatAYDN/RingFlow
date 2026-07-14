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
                    var randomColor = GetRandomColorForLevel(context);
                    var convertedRing = new RingData(randomColor, RingType.Standard);
                    context.ToPole.Rings[context.ToPole.Rings.Count - 1] = convertedRing;
                    context.RainbowTargetIndex = context.ToPole.Rings.Count - 1;
                    context.WasRainbowConverted = true;

                    int level = context.Progression?.CurrentLevel.Value ?? 1;
                    int seed = unchecked((int)((long)level * 2654435761L) ^ (context.ToPoleId * 31));
                    NexusLog.Info("RainbowRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                        $"Rainbow on empty pole became {randomColor}. Level={level}, ToPole={context.ToPoleId}, Seed={seed}.");
                }
            }
        }

        private RingColor GetRandomColorForLevel(MoveContext context)
        {
            // P0 fix: derive seed from level + pole identity (deterministic), NOT from
            // MovesCount. Same as MysteryRingStrategy — guarantees reproducible color
            // for QA replay and undo consistency.
            if (_db == null) throw new System.InvalidOperationException("[RainbowRingStrategy] GameConfigDatabaseSO not injected!");
            var colorCount = _db.GetColorCountForLevel(
                context.Progression?.CurrentLevel.Value ?? 1);

            int level = context.Progression?.CurrentLevel.Value ?? 1;
            int seed = unchecked((int)((long)level * 2654435761L) ^ (context.ToPoleId * 31));
            int safeSeed = seed == int.MinValue ? 0 : System.Math.Abs(seed);
            int colorIndex = 1 + (safeSeed % colorCount);

            if (colorIndex > GameplayAssetKeys.Tuning.ColorIndexMax) colorIndex = GameplayAssetKeys.Tuning.ColorIndexFallback;

            return (RingColor)colorIndex;
        }
    }
}
