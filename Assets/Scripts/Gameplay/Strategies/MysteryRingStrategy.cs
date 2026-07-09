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
            // GDD §4: Mystery rings reveal a random color from the current level's color set
            // For simplicity, we'll use a deterministic hash-based approach
            var colorCount = GameConfigDatabaseSO.Instance.GetColorCountForLevel(
                context.Progression?.CurrentLevel.Value ?? 1);
            
            // Use pole position as seed for deterministic but varied colors
            int seed = (context.FromPoleId * 17) + (context.Model.MovesCount.Value * 31);
            int colorIndex = 1 + (seed % colorCount); // Skip None (index 0)
            
            // Ensure valid color range
            if (colorIndex > 10) colorIndex = 3; // Fallback to basic colors
            
            return (RingColor)colorIndex;
        }
    }
}
