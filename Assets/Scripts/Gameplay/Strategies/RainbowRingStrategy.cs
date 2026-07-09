using Nexus.Core;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Strategy for Rainbow ring mechanics (GDD §4).
    /// Rainbow rings take the color of the first ring they land on,
    /// then become standard rings with that color.
    /// </summary>
    public sealed class RainbowRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Rainbow;
        }

        public bool PreMoveValidation(ref MoveContext context)
        {
            // Rainbow rings can land on any ring (GDD §4)
            return true;
        }

        public void PostMoveExecution(ref MoveContext context)
        {
            // Check if the moved ring is a Rainbow ring
            if (context.MovingRing.Type == RingType.Rainbow)
            {
                // Rainbow rings take the color of the ring below them (if any)
                if (context.ToPole.Rings.Count >= 2)
                {
                    var ringBelowIndex = context.ToPole.Rings.Count - 2;
                    var ringBelow = context.ToPole.Rings[ringBelowIndex];
                    
                    // Store original state for undo
                    context.RainbowTargetIndex = context.ToPole.Rings.Count - 1;
                    context.RainbowTargetOriginalColor = context.MovingRing.Color;
                    context.WasRainbowConverted = true;
                    
                    // Convert rainbow to the color below
                    var convertedRing = new RingData(ringBelow.Color, RingType.Standard);
                    context.ToPole.Rings[context.ToPole.Rings.Count - 1] = convertedRing;
                    
                    UnityEngine.Debug.Log($"[RainbowRingStrategy] Rainbow ring converted to {ringBelow.Color}");
                }
                else
                {
                    // If landing on empty pole, rainbow becomes a random color
                    var randomColor = GetRandomColorForLevel(context);
                    var convertedRing = new RingData(randomColor, RingType.Standard);
                    context.ToPole.Rings[context.ToPole.Rings.Count - 1] = convertedRing;
                    
                    UnityEngine.Debug.Log($"[RainbowRingStrategy] Rainbow ring on empty pole became {randomColor}");
                }
            }
        }

        private RingColor GetRandomColorForLevel(MoveContext context)
        {
            // Similar to Mystery strategy, use deterministic approach
            var colorCount = GameConfigDatabaseSO.Instance.GetColorCountForLevel(
                context.Progression?.CurrentLevel.Value ?? 1);
            
            int seed = (context.ToPoleId * 23) + (context.Model.MovesCount.Value * 47);
            int colorIndex = 1 + (seed % colorCount);
            
            if (colorIndex > 10) colorIndex = 3;
            
            return (RingColor)colorIndex;
        }
    }
}
