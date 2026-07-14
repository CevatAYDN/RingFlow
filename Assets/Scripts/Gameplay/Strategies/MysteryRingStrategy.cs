using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    public sealed class MysteryRingStrategy : IRingMoveStrategy
    {
        private readonly GameConfigDatabaseSO _db;
        private static readonly RingColor[] s_colors = (RingColor[])System.Enum.GetValues(typeof(RingColor));

        public MysteryRingStrategy(GameConfigDatabaseSO db)
        {
            _db = db;
        }
        public bool CanHandle(RingType ringType) => ringType == RingType.Mystery;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            if (context.FromPole.Rings.Count > 0)
            {
                var topRing = context.FromPole.TopRing;
                if (topRing.Type == RingType.Mystery)
                {
                    // Use pre-assigned color from generation; fall back to deterministic compute
                    var revealedColor = topRing.Color;
                    if (revealedColor == RingColor.None)
                    {
                        revealedColor = DetermineMysteryColor(context);
                    }
                    topRing = new RingData(revealedColor, RingType.Standard);
                    context.FromPole.Rings[^1] = topRing;
                    context.WasMysteryRevealed = true;
                    context.SignalBus?.Fire(new RevealMysterySignal(context.FromPoleId, topRing));

                    int level = context.Progression?.CurrentLevel.Value ?? 1;
                    int seed = unchecked((int)((long)level * 2654435761L) ^ (context.FromPoleId * 31));
                    NexusLog.Info("MysteryRingStrategy", nameof(PostMoveExecution), context.FromPoleId.ToString(),
                        $"Mystery revealed as {revealedColor}. Level={level}, FromPole={context.FromPoleId}, Seed={seed}.");
                }
            }
        }

        private RingColor DetermineMysteryColor(MoveContext context)
        {
            // GDD §4 + §8: Mystery rings reveal a colour from the current level's colour set
            // in a way that is *reproducible per level/pole*, independent of the player's move
            // count. The previous implementation included MovesCount.Value in the seed, which
            // broke the QA reproducibility invariant ("same seed → same level").
            if (_db == null) throw new System.InvalidOperationException("[MysteryRingStrategy] GameConfigDatabaseSO not injected!");
            int colorCount = _db.GetColorCountForLevel(
                context.Progression?.CurrentLevel.Value ?? 1);

            // FIX P2.MysteryDeterminism — derive the seed purely from level+pole identity.
            // Cast to long first so the multiply doesn't overflow int; XOR back into int
            // before the modulo so downstream arithmetic stays inside the 32-bit range.
            int level = context.Progression?.CurrentLevel.Value ?? 1;
            int seed = unchecked((int)((long)level * 2654435761L) ^ (context.FromPoleId * 31));
            int safeSeed = seed == int.MinValue ? 0 : System.Math.Abs(seed);
            int colorIndex = 1 + (safeSeed % colorCount); // Skip None (index 0)

            // Clamp into the palette — use RingColor enum count (minus None) as the upper bound.
            int maxColorIndex = s_colors.Length - 1; // Skip None (index 0)
            if (colorIndex > maxColorIndex) colorIndex = 1 + (colorIndex % maxColorIndex);

            return (RingColor)colorIndex;
        }
    }
}
