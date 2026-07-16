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
            // Only act when the MOVING ring is the Paint ring itself (GDD §39).
            // If PostMoveExecution is invoked because the ring below was Paint
            // (MoveRingCommand targetType path), context.MovingRing.Type is NOT Paint
            // and we skip — landing on an existing Paint ring does not trigger painting.
            if (context.MovingRing.Type != RingType.Paint) return;

            // Paint ring must have landed on another ring (not an empty pole).
            if (context.ToPole.Rings.Count < 2) return;

            int paintIndex = context.ToPole.Rings.Count - 1;   // the Paint ring itself
            int belowIndex = context.ToPole.Rings.Count - 2;   // the ring to be painted
            var ringBelow  = context.ToPole.Rings[belowIndex];
            var paintRing  = context.ToPole.Rings[paintIndex];

            // Do not paint another Paint ring — prevent chain reactions not in GDD.
            if (ringBelow.Type == RingType.Paint) return;

            var paintColor = paintRing.Color;

            // Record for undo
            context.PaintedRingIndex          = belowIndex;
            context.PaintedRingOriginalColor  = ringBelow.Color;
            context.PaintConsumedRingIndex    = paintIndex;
            context.PaintConsumedRingData     = new RingData(paintColor, RingType.Paint);
            context.WasPaintApplied           = true;

            // Apply paint: ring below gets paint's color (type stays, only color changes)
            context.ToPole.Rings[belowIndex] = new RingData(paintColor, ringBelow.Type, ringBelow.AdditionalData);

            // Consume paint ring: becomes Standard with same color
            context.ToPole.Rings[paintIndex] = new RingData(paintColor, RingType.Standard);

            context.SignalBus?.Fire(new PaintRingSignal(context.ToPoleId, paintColor));

            NexusLog.Info("PaintRingStrategy", nameof(PostMoveExecution), context.ToPoleId.ToString(),
                $"Paint applied: ring[{belowIndex}] recolored to {paintColor} " +
                $"(was {context.PaintedRingOriginalColor}, type={ringBelow.Type}). " +
                $"Paint ring[{paintIndex}] consumed → Standard/{paintColor}.");
        }
    }
}
