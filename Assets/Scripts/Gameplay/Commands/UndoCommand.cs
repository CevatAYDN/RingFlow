using System;
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
                var toPole = _model.Poles.GetPoleById(lastMove.ToPoleId);

                if (fromPole != null && toPole != null)
                {
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

                    RestoreBombCounters(lastMove.BombCountersBeforeTick);

                    if (lastMove.SubMoves.Count > 0)
                    {
                        for (int i = lastMove.SubMoves.Count - 1; i >= 0; i--)
                        {
                            var sub = lastMove.SubMoves[i];
                            var subFrom = _model.Poles.GetPoleById(sub.FromPoleId);
                            var subTo = _model.Poles.GetPoleById(sub.ToPoleId);

                            if (subFrom != null && subTo != null)
                            {
                                subTo.PopRing();
                                subFrom.AddRing(sub.Ring);
                            }
                        }
                    }

                    if (lastMove.WasTargetPoleUnlocked)
                    {
                        toPole.IsLocked = true;
                    }

                    if (lastMove.WasMysteryRevealedOnFrom && !fromPole.IsEmpty)
                    {
                        var topM = fromPole.TopRing;
                        fromPole.Rings[^1] = new RingData(topM.Color, RingType.Mystery);
                    }

                    var movedRing = toPole.PopRing();

                    if (lastMove.WasPainted)
                    {
                        movedRing.Color = lastMove.OriginalColor;

                        int paintTargetIndex = lastMove.PaintedRingIndex - 1;
                        if (paintTargetIndex >= 0 && paintTargetIndex < toPole.Rings.Count)
                        {
                            var painted = toPole.Rings[paintTargetIndex];
                            painted.Type = RingType.Paint;
                            toPole.Rings[paintTargetIndex] = painted;
                        }
                    }

                    if (lastMove.WasRainbowTargetConverted)
                    {
                        movedRing.Type = RingType.Rainbow;
                        movedRing.Color = lastMove.RainbowTargetOriginalColor;
                    }

                    fromPole.AddRing(movedRing);

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
                _signalBus.Fire(new BombTickSignal(entry.PoleId, entry.Counter));
            }
        }
    }
}