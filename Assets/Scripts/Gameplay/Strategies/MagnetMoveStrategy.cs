namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Magnet rings.
    /// When a Magnet ring lands on a target pole, it pulls all same-color rings
    /// from every other pole to the target pole (capacity permitting).
    /// GDD §37 — Magnet rings attract matching colors.
    ///
    /// Migrated from MoveRingCommand.ApplyMagnetPull (was inline, "future migration" stub).
    /// MoveContext.MainRecord must be set before ExecuteSubMoves so this strategy can
    /// append sub-move records for Undo support.
    /// </summary>
    public sealed class MagnetMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Magnet;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            var model = context.Model;
            if (model == null) return;

            int pullCount = 0;

            for (int p = 0; p < model.Poles.Count; p++)
            {
                var pole = model.Poles[p];

                // FIX-A4: Compare against the TARGET POLE'S ID, not its list index p.
                if (pole.Id == context.ToPoleId) continue;

                // FIX-M1: Was `break` — wrong. When the target pole is full we must
                // CONTINUE scanning remaining poles so we still record all pulls that
                // happened before the pole filled up.
                if (context.ToPole.IsFull) continue;

                if (!pole.CanPopRing() || pole.TopRing.Color != context.MovingRing.Color) continue;

                var pulled = pole.PopRing();
                if (!context.ToPole.CanAddRing(pulled))
                {
                    pole.AddRing(pulled);
                    continue;
                }
                context.ToPole.AddRing(pulled);

                if (context.MainRecord != null)
                {
                    var subRecord = MoveRecordPool.Rent();
                    subRecord.FromPoleId = pole.Id;
                    subRecord.ToPoleId = context.ToPoleId;
                    subRecord.Ring = pulled;
                    context.MainRecord.SubMoves.Add(subRecord);
                }
                pullCount++;
            }

            // Fire signal for VFX/SFX (magnetic whoosh + hum) after all pulls.
            if (pullCount > 0)
            {
                context.SignalBus?.Fire(new MagnetPullSignal(context.ToPoleId, pullCount, context.MovingRing.Color));
            }

#if DEVELOPMENT_BUILD
            if (pullCount > 0)
            {
                NexusLog.Info("MagnetMoveStrategy", nameof(PostMoveExecution),
                    context.ToPoleId.ToString(),
                    $"Magnet pulled {pullCount} matching ring(s) to pole {context.ToPoleId}, color={context.MovingRing.Color}.");
            }
#endif
        }
    }
}
