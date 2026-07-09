using Nexus.Core;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Strategy for Paint ring mechanics (GDD §4).
    /// Paint rings change the color of the ring below them to their own color,
    /// then become standard rings (single-use).
    /// </summary>
    public sealed class PaintRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Paint;
        }

        public bool PreMoveValidation(ref MoveContext context)
        {
            // Paint rings don't block moves
            return true;
        }

        public void PostMoveExecution(ref MoveContext context)
        {
            // Check if the moving ring landed on a Paint ring
            if (context.ToPole.Rings.Count >= 2)
            {
                var ringBelowIndex = context.ToPole.Rings.Count - 2;
                var ringBelow = context.ToPole.Rings[ringBelowIndex];
                
                if (ringBelow.Type == RingType.Paint)
                {
                    // Paint the moving ring with the paint ring's color
                    var paintColor = ringBelow.Color;
                    var movingRingIndex = context.ToPole.Rings.Count - 1;
                    
                    // Store original color for undo
                    context.PaintedRingIndex = movingRingIndex;
                    context.PaintedRingOriginalColor = context.ToPole.Rings[movingRingIndex].Color;
                    context.WasPaintApplied = true;
                    
                    // Apply paint
                    var paintedRing = context.ToPole.Rings[movingRingIndex];
                    paintedRing = new RingData(paintColor, RingType.Standard);
                    context.ToPole.Rings[movingRingIndex] = paintedRing;
                    
                    // Consume the paint ring (convert to standard)
                    var consumedPaint = new RingData(paintColor, RingType.Standard);
                    context.ToPole.Rings[ringBelowIndex] = consumedPaint;
                    
                    // Fire signal for UI feedback
                    context.SignalBus?.Fire(new PaintRingSignal(context.ToPoleId, paintColor));
                    
                    UnityEngine.Debug.Log($"[PaintRingStrategy] Paint ring applied {paintColor} to ring at index {movingRingIndex}");
                }
            }
        }
    }
}
