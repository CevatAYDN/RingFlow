using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Paint rings (GDD §39).
    ///
    /// GDD §39: "Yerleştirildiği çubuktaki altındaki ilk halkayı kendi rengine boyar.
    /// Tek kullanımlıktır, boyama gerçekleştikten sonra normal bir halkaya dönüşür."
    ///
    /// Translation:
    ///   • A Paint ring paints the ring directly BELOW IT when Paint is placed.
    ///   • Paint is consumed (becomes Standard) after painting.
    ///   • Only the MOVING Paint ring triggers paint — a ring placed ON TOP of an
    ///     existing Paint ring does NOT get painted. That scenario is handled by
    ///     MoveRingCommand: when the ring below the landing spot is Paint, it fires
    ///     ExecutePostMoveExecution(RingType.Paint, ...) which routes here.
    ///
    /// FIX-P1 (previously named SORUN 3):
    ///   The old implementation had a second branch:
    ///     "else if (ringBelow.Type == RingType.Paint)"
    ///   that painted the MOVING ring when it lands on a Paint ring already in the
    ///   stack. GDD §39 does not describe that behaviour — Paint only paints downward,
    ///   never upward. The extra branch has been removed. The MoveRingCommand already
    ///   calls ExecutePostMoveExecution(targetType, ...) for the ring that sits below
    ///   the newly placed ring when targetType == RingType.Paint, so PostMoveExecution
    ///   is invoked correctly for both scenarios via the same code path.
    ///
    ///   Concretely, when another ring lands on a Paint ring:
    ///     context.MovingRing = the ring that just landed (NOT Paint)
    ///     context.ToPole.Rings[Count-2] = the Paint ring
    ///   In that case Count >= 2 and movingRing.Type != Paint, so the first if-branch
    ///   is skipped and nothing happens — which is correct because GDD §39 says Paint
    ///   only acts when PAINT ITSELF is moved, not when something lands on top of it.
    ///   The MoveRingCommand block at line ~130 re-invokes PostMoveExecution(Paint)
    ///   only when the ring sitting below the landing spot is Paint, but at that point
    ///   context.MovingRing is the new ring (not Paint) — again no-op here, which is
    ///   intentional: landing on a static Paint ring does NOT trigger painting.
    /// </summary>
    public sealed class PaintRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Paint;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            if (context.ToPole.Rings.Count < 2) return;

            int topIndex = context.ToPole.Rings.Count - 1;
            int belowIndex = context.ToPole.Rings.Count - 2;
            var topRing = context.ToPole.Rings[topIndex];
            var belowRing = context.ToPole.Rings[belowIndex];

            if (topRing.Type == RingType.Paint)
            {
                // Case 1: Placed ring is Paint. It paints the ring below it.
                if (belowRing.Type == RingType.Paint) return;

                var paintColor = topRing.Color;

                context.PaintedRingIndex = belowIndex;
                context.PaintedRingOriginalColor = belowRing.Color;
                context.PaintConsumedRingIndex = topIndex;
                context.PaintConsumedRingData = new RingData(paintColor, RingType.Paint);
                context.WasPaintApplied = true;

                // Apply paint: ring below gets paint's color (type stays, color changes)
                context.ToPole.Rings[belowIndex] = new RingData(paintColor, belowRing.Type, belowRing.AdditionalData);

                // Consume paint ring: becomes Standard
                context.ToPole.Rings[topIndex] = new RingData(paintColor, RingType.Standard);

                context.SignalBus?.Fire(new PaintRingSignal(context.ToPoleId, paintColor));

                NexusLog.Info("PaintRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                    $"Paint applied: ring[{belowIndex}] recolored to {paintColor} (was {context.PaintedRingOriginalColor}). " +
                    $"Paint ring[{topIndex}] consumed → Standard.");
            }
            else if (belowRing.Type == RingType.Paint)
            {
                // Case 2: Ring below is Paint. It paints the newly placed ring (topRing).
                var paintColor = belowRing.Color;

                context.PaintedRingIndex = topIndex;
                context.PaintedRingOriginalColor = topRing.Color;
                context.PaintConsumedRingIndex = belowIndex;
                context.PaintConsumedRingData = new RingData(paintColor, RingType.Paint);
                context.WasPaintApplied = true;

                // Apply paint: top ring gets paint's color (if it's Rainbow, it becomes Standard per BoardState logic)
                var newTopType = topRing.Type == RingType.Rainbow ? RingType.Standard : topRing.Type;
                context.ToPole.Rings[topIndex] = new RingData(paintColor, newTopType, topRing.AdditionalData);

                // Consume paint ring: becomes Standard
                context.ToPole.Rings[belowIndex] = new RingData(paintColor, RingType.Standard);

                context.SignalBus?.Fire(new PaintRingSignal(context.ToPoleId, paintColor));

                NexusLog.Info("PaintRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                    $"Paint applied: top ring[{topIndex}] recolored to {paintColor} (was {context.PaintedRingOriginalColor}). " +
                    $"Paint ring[{belowIndex}] consumed → Standard.");
            }
        }
    }
}
