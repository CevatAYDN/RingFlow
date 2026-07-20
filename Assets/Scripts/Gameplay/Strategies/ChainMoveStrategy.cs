using System.Collections.Generic;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Move strategy for Chain rings.
    /// When a Chain ring moves to a target pole, its partner ring (same AdditionalData group)
    /// on any other pole is automatically pulled to the same target pole.
    /// GDD §38 — Chain rings always move in pairs.
    ///
    /// Migrated from MoveRingCommand.ApplyChainSubMove (was inline, "future migration" stub).
    /// MoveContext.MainRecord must be set before ExecuteSubMoves so this strategy can
    /// append the sub-move record.
    /// </summary>
    public sealed class ChainMoveStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType) => ringType == RingType.Chain;

        public bool PreMoveValidation(ref MoveContext context) => true;

        public void PostMoveExecution(ref MoveContext context)
        {
            // Find the partner Chain ring (same AdditionalData group, different pole).
            var model = context.Model;
            if (model == null) return;

            for (int i = 0; i < model.Poles.Count; i++)
            {
                var pole = model.Poles[i];
                if (pole.Id == context.FromPole.Id) continue;

                var topR = pole.TopRing;
                if (topR.Type != RingType.Chain || topR.AdditionalData != context.MovingRing.AdditionalData)
                    continue;

                // Found the partner — try to pull it to the target pole.
                pole.PopRing();
                if (!context.ToPole.CanAddRing(topR))
                {
                    // Target full: rollback the partner pop.
                    pole.AddRing(topR);
                    return;
                }
                context.ToPole.AddRing(topR);

                // Fire signal for VFX/SFX (chain-link burst + metallic clink).
                // Fires before sub-record to ensure mediator sees the signal in time.
                context.SignalBus?.Fire(new ChainLinkSignal(pole.Id, context.ToPoleId, topR.Color));

                // Record the sub-move so Undo can reverse it.
                if (context.MainRecord != null)
                {
                    var subRecord = MoveRecordPool.Rent();
                    subRecord.FromPoleId = pole.Id;
                    subRecord.ToPoleId = context.ToPoleId;
                    subRecord.Ring = topR;
                    context.MainRecord.SubMoves.Add(subRecord);
                }

#if DEVELOPMENT_BUILD
                NexusLog.Info("ChainMoveStrategy", nameof(PostMoveExecution),
                    context.ToPoleId.ToString(),
                    $"Chain sub-move: partner from pole {pole.Id} → {context.ToPoleId}, color={topR.Color}.");
#endif
                return; // Only one partner per chain group.
            }
        }
    }
}
