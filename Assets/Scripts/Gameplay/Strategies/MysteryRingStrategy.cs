using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    public sealed class MysteryRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Mystery;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            if (context.FromPole.Rings.Count > 0)
            {
                var topRing = context.FromPole.TopRing;
                if (topRing.Type == RingType.Mystery)
                {
                    var revealedColor = DetermineMysteryColor(context);
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
            int colorCount = GameConfigDatabaseSO.Instance.GetColorCountForLevel(
                context.Progression?.CurrentLevel.Value ?? 1);

            // FIX P2.MysteryDeterminism — derive the seed purely from level+pole identity.
            // Cast to long first so the multiply doesn't overflow int; XOR back into int
            // before the modulo so downstream arithmetic stays inside the 32-bit range.
            int level = context.Progression?.CurrentLevel.Value ?? 1;
            int seed = unchecked((int)((long)level * 2654435761L) ^ (context.FromPoleId * 31));
            int colorIndex = 1 + (System.Math.Abs(seed) % colorCount); // Skip None (index 0)

            // Clamp into the palette — palette size is colour-count, but we degrade gracefully.
            if (colorIndex > 10) colorIndex = 3; // Fallback to basic colours

            return (RingColor)colorIndex;
        }
    }
}
