using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    public sealed class PaintRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Paint;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            if (context.ToPole.Rings.Count >= 2)
            {
                var ringBelowIndex = context.ToPole.Rings.Count - 2;
                var ringBelow = context.ToPole.Rings[ringBelowIndex];
                var movingRingIndex = context.ToPole.Rings.Count - 1;
                var movingRing = context.ToPole.Rings[movingRingIndex];

                if (movingRing.Type == RingType.Paint && ringBelow.Type != RingType.Paint)
                {
                    // Paint ring placed on another ring: ring below gets painted, Paint consumed
                    var paintColor = movingRing.Color;
                    context.PaintedRingIndex = ringBelowIndex;
                    context.PaintedRingOriginalColor = ringBelow.Color;
                    context.PaintConsumedRingIndex = movingRingIndex;
                    context.PaintConsumedRingData = new RingData(movingRing.Color, RingType.Paint);
                    context.WasPaintApplied = true;

                    var paintedRing = new RingData(paintColor, RingType.Standard);
                    context.ToPole.Rings[ringBelowIndex] = paintedRing;

                    var consumedPaint = new RingData(paintColor, RingType.Standard);
                    context.ToPole.Rings[movingRingIndex] = consumedPaint;

                    context.SignalBus?.Fire(new PaintRingSignal(context.ToPoleId, paintColor));

                    NexusLog.Info("PaintRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                        $"Paint applied by moving Paint ring: ring[{ringBelowIndex}] colored {paintColor} (was {context.PaintedRingOriginalColor}). Paint consumed at index {movingRingIndex}.");
                }
                else if (ringBelow.Type == RingType.Paint)
                {
                    // Ring placed on existing Paint ring: moving ring gets painted, Paint consumed
                    var paintColor = ringBelow.Color;

                    context.PaintedRingIndex = movingRingIndex;
                    context.PaintedRingOriginalColor = movingRing.Color;
                    context.PaintConsumedRingIndex = ringBelowIndex;
                    context.PaintConsumedRingData = new RingData(ringBelow.Color, RingType.Paint);
                    context.WasPaintApplied = true;

                    var paintedRing = new RingData(paintColor, RingType.Standard);
                    context.ToPole.Rings[movingRingIndex] = paintedRing;

                    var consumedPaint = new RingData(paintColor, RingType.Standard);
                    context.ToPole.Rings[ringBelowIndex] = consumedPaint;

                    context.SignalBus?.Fire(new PaintRingSignal(context.ToPoleId, paintColor));

                    NexusLog.Info("PaintRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                        $"Paint applied: ring[{movingRingIndex}] colored {paintColor} (was {context.PaintedRingOriginalColor}). Paint ring consumed at index {ringBelowIndex}.");
                }
            }
        }
    }
}
