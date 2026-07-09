using Nexus.Core;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Strategy for Mystery ring mechanics (GDD §4).
    /// Mystery rings reveal their true color when the ring above them is moved.
    /// This strategy handles the reveal logic and signal firing.
    /// </summary>
    public sealed class MysteryRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Mystery;
        }

        public bool PreMoveValidation(ref MoveContext context)
        {
            // Mystery rings don't block moves
            return true;
        }

        public void PostMoveExecution(ref MoveContext context)
        {
            // Check if the ring below the moved ring (now on top) is Mystery
            if (context.FromPole.Rings.Count > 0)
            {
                var topRing = context.FromPole.TopRing;
                if (topRing.Type == RingType.Mystery)
                {
                    // Reveal the mystery ring
                    var revealedColor = DetermineMysteryColor(context);
                    topRing = new RingData(revealedColor, RingType.Standard);
                    
                    // Update the pole state
                    context.FromPole.Rings[^1] = topRing;
                    
                    context.WasMysteryRevealed = true;
                    
                    // Fire signal for UI feedback
                    context.SignalBus?.Fire(new RevealMysterySignal(context.FromPoleId, topRing));
                    
                    UnityEngine.Debug.Log($"[MysteryRingStrategy] Mystery ring revealed as {revealedColor}");
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
