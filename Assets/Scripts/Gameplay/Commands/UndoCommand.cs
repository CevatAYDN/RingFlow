using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class UndoCommand : ICommand<UndoSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(UndoSignal signal)
        {
            if (_model.MoveHistory.Count > 0)
            {
                var lastMove = _model.MoveHistory.Pop();
                int depthAfterPop = _model.MoveHistory.Count;
                NexusLog.Info("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                    $"Undoing move. History depth: {depthAfterPop} remaining.");

                var fromPole = _model.Poles.GetPoleById(lastMove.FromPoleId);
                var toPole   = _model.Poles.GetPoleById(lastMove.ToPoleId);

                if (fromPole != null && toPole != null)
                {
                    // ── 1. Restore exploded bomb rings (insert back in original index order, descending) ──
                    if (lastMove.BombExplodedRings.Count > 0)
                    {
                        lastMove.BombExplodedRings.Sort(static (a, b) => b.RingIndex.CompareTo(a.RingIndex));
                        for (int i = 0; i < lastMove.BombExplodedRings.Count; i++)
                        {
                            var entry = lastMove.BombExplodedRings[i];
                            var pole = _model.Poles.GetPoleById(entry.PoleId);
                            if (pole == null) continue;
                            int insertIdx = entry.RingIndex < pole.Rings.Count ? entry.RingIndex : pole.Rings.Count;
                            pole.Rings.Insert(insertIdx, entry.Ring);
                        }
                    }

                    // ── 2. Restore bomb counters to their pre-move values ──
                    RestoreBombCounters(lastMove.BombCountersBeforeTick);

                    // ── 3. Undo sub-moves in reverse order (chain, magnet, portal) ──
                    if (lastMove.SubMoves.Count > 0)
                    {
                        for (int i = lastMove.SubMoves.Count - 1; i >= 0; i--)
                        {
                            var sub    = lastMove.SubMoves[i];
                            var subFrom = _model.Poles.GetPoleById(sub.FromPoleId);
                            var subTo   = _model.Poles.GetPoleById(sub.ToPoleId);

                            if (subFrom != null && subTo != null)
                            {
                                subTo.PopRing();
                                subFrom.AddRing(sub.Ring);
                            }
                        }
                    }

                    // ── 4. Restore locked pole state ──
                    if (lastMove.WasTargetPoleUnlocked)
                    {
                        toPole.IsLocked = true;
                    }

                    // ── 5. Restore Mystery reveal on FROM pole ──
                    if (lastMove.WasMysteryRevealedOnFrom && !fromPole.IsEmpty)
                    {
                        var topM = fromPole.TopRing;
                        fromPole.Rings[^1] = new RingData(topM.Color, RingType.Mystery);
                    }

                    // ── 6. Undo main ring move (portal-aware) ──
                    //   If the move included a portal teleport, the ring was actually
                    //   placed on ToPole first and then teleported to PortalTeleportTargetPoleId.
                    //   Sub-moves already reversed the teleport (step 3), so we just pop ToPole here.
                    var movedRing = toPole.PopRing();

                    // ── 7. Undo paint effect ──
                    if (lastMove.WasPainted)
                    {
                        movedRing.Color = lastMove.OriginalColor;

                        // Restore the consumed Paint ring (Standard → back to Paint)
                        if (lastMove.PaintConsumedRingIndex >= 0 &&
                            lastMove.PaintConsumedRingIndex < toPole.Rings.Count)
                        {
                            toPole.Rings[lastMove.PaintConsumedRingIndex] = lastMove.PaintConsumedRingData;
                        }

                        // Restore the painted ring's original color
                        if (lastMove.PaintedRingIndex >= 0 &&
                            lastMove.PaintedRingIndex < toPole.Rings.Count)
                        {
                            var painted = toPole.Rings[lastMove.PaintedRingIndex];
                            painted.Color = lastMove.PaintedRingOriginalColor;
                            toPole.Rings[lastMove.PaintedRingIndex] = painted;
                        }
                    }

                    // ── 8. Undo rainbow conversion ──
                    //   The rainbow WAS the movedRing (already popped from toPole in step 6).
                    //   We restore its original Type and Color directly.
                    if (lastMove.WasRainbowTargetConverted)
                    {
                        movedRing.Type  = RingType.Rainbow;
                        movedRing.Color = lastMove.RainbowTargetOriginalColor;
                    }

                    // ── 9. Return moving ring to FROM pole ──
                    fromPole.AddRing(movedRing);

                    // ── 10. Restore Ghost state if ring was revealed on FROM pole selection ──
                    //   SelectPoleCommand changes Ghost→Standard before firing MoveRingSignal.
                    //   We must revert that change so the ring appears Ghost again.
                    if (lastMove.WasGhostRevealedOnFrom && !fromPole.IsEmpty)
                    {
                        var topG = fromPole.Rings[^1];
                        topG.Type = RingType.Ghost;
                        fromPole.Rings[^1] = topG;
                        _signalBus?.Fire(new GhostRestoredSignal(lastMove.FromPoleId));
                        NexusLog.Info("UndoCommand", "Execute", lastMove.FromPoleId.ToString(),
                            "Ghost ring restored on FROM pole (undo of reveal).");
                    }

                    // ── 11. Restore frozen rings on TO pole ──
                    if (lastMove.IceBrokenRingIndices.Count > 0)
                    {
                        for (int i = 0; i < lastMove.IceBrokenRingIndices.Count; i++)
                        {
                            int idx = lastMove.IceBrokenRingIndices[i];
                            if (idx >= 0 && idx < toPole.Rings.Count)
                            {
                                var ringAtIdx = toPole.Rings[idx];
                                toPole.Rings[idx] = new RingData(ringAtIdx.Color, RingType.Frozen);
                            }
                        }
                    }

                    // ── 12. Restore move counter ──
                    if (_model.MovesCount.Value > 0)
                    {
                        _model.MovesCount.Value--;
                    }

                    if (_model.IsGameWon.Value)
                    {
                        _model.IsGameWon.Value = false;
                    }

                    _model.SelectedPoleId.Value = -1;
                    NexusLog.Info("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        $"Undo complete. Moves now: {_model.MovesCount.Value}");

                    MoveRecordPool.Return(lastMove);

                    _signalBus.Fire(new CheckWinSignal());
                }
                else
                {
                    NexusLog.Warn("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        $"Undo failed: pole lookup returned null. fromNull={fromPole == null}, toNull={toPole == null}");

                    MoveRecordPool.Return(lastMove);
                }
            }
            else
            {
                NexusLog.Warn("UndoCommand", "Execute", "", "Undo requested but MoveHistory is empty.");
            }
        }

        /// <summary>
        /// Restores bomb counters to their pre-move snapshot values.
        /// Uses <see cref="BombCounterRestoredSignal"/> (NOT <see cref="BombTickSignal"/>)
        /// so that View listeners can distinguish between a normal tick and an undo restore.
        /// Signals must not modify state — only update visuals.
        /// </summary>
        private void RestoreBombCounters(System.Collections.Generic.List<(int PoleId, int RingIndex, int Counter)> snapshot)
        {
            if (snapshot == null || snapshot.Count == 0) return;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var entry = snapshot[i];
                var pole = _model.Poles.GetPoleById(entry.PoleId);
                if (pole == null || entry.RingIndex >= pole.Rings.Count) continue;
                var r = pole.Rings[entry.RingIndex];
                if (r.Type != RingType.Bomb) continue;
                pole.Rings[entry.RingIndex] = new RingData(r.Color, RingType.Bomb, entry.Counter);
                // Use BombCounterRestoredSignal (not BombTickSignal) — undo context is different from gameplay tick.
                _signalBus.Fire(new BombCounterRestoredSignal(entry.PoleId, entry.RingIndex, entry.Counter));
            }
        }
    }
}