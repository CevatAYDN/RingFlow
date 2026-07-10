using System;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Gameplay
{
    public class MoveRingCommand : ICommand<MoveRingSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IGameStateMachine _fsm;
        [Inject] private RingMoveStrategyManager _strategyManager;
        [Inject] private IProgressionService _progression;

        public void Execute(MoveRingSignal signal)
        {
            if (!TryValidateMove(signal, out var context)) return;
            if (!TryPreMoveValidate(ref context)) return;

            ExecuteCoreMove(ref context);
            var mainRecord = BuildMoveRecord(context);
            ExecuteSubMoves(ref context, mainRecord);

            CompleteMove(context, mainRecord);
        }

        private bool TryValidateMove(MoveRingSignal signal, out MoveContext context)
        {
            context = default;

            var fromPole = _model.Poles.GetPoleById(signal.FromPoleId);
            var toPole = _model.Poles.GetPoleById(signal.ToPoleId);

            if (fromPole == null || toPole == null || !fromPole.CanPopRing())
            {
                NexusLog.Warn("MoveRingCommand", "TryValidateMove", $"{signal.FromPoleId}->{signal.ToPoleId}",
                    $"Blocked. fromPoleNull={fromPole == null}, toPoleNull={toPole == null}, canPop={fromPole?.CanPopRing()}");
                return false;
            }

            var ring = fromPole.TopRing;

            if (!TryReserveChainCapacity(ref ring, fromPole, toPole)) return false;
            if (!toPole.CanAddRing(ring)) return false;

            context = new MoveContext
            {
                FromPoleId = signal.FromPoleId,
                ToPoleId = signal.ToPoleId,
                MovingRing = ring,
                FromPole = fromPole,
                ToPole = toPole,
                Model = _model,
                SignalBus = _signalBus,
                Progression = _progression
            };
            return true;
        }

        private bool TryPreMoveValidate(ref MoveContext context)
        {
            if (!_strategyManager.ExecutePreMoveValidation(context.MovingRing.Type, ref context))
            {
                NexusLog.Info("MoveRingCommand", "TryPreMoveValidate", $"{context.FromPoleId}->{context.ToPoleId}",
                    "Move blocked by strategy validation.");
                return false;
            }
            return true;
        }

        private void ExecuteCoreMove(ref MoveContext context)
        {
            var fromPole = context.FromPole;
            var toPole = context.ToPole;
            var ring = context.MovingRing;

            fromPole.PopRing();

            if (toPole.IsLocked && ring.Type == RingType.Locked)
            {
                toPole.IsLocked = false;
                context.WasPoleUnlocked = true;
                _signalBus.Fire(new UnlockPoleSignal(context.ToPoleId));
                NexusLog.Info("MoveRingCommand", "ExecuteCoreMove", context.ToPoleId.ToString(),
                    $"Locked pole {context.ToPoleId} unlocked with Key ring.");
            }

            toPole.AddRing(ring);

            _strategyManager.ExecutePostMoveExecution(ring.Type, ref context);

            if (toPole.Rings.Count >= 2)
            {
                var targetType = toPole.Rings[toPole.Rings.Count - 2].Type;
                if (targetType == RingType.Paint || targetType == RingType.Rainbow)
                {
                    _strategyManager.ExecutePostMoveExecution(targetType, ref context);
                }
            }

            if (fromPole.Rings.Count > 0 && fromPole.TopRing.Type == RingType.Mystery)
            {
                _strategyManager.ExecutePostMoveExecution(RingType.Mystery, ref context);
            }
        }

        private void ExecuteSubMoves(ref MoveContext context, MoveRecord mainRecord)
        {
            ApplyChainSubMove(ref context, mainRecord);
            ApplyMagnetPull(ref context, mainRecord);
            TryBreakIceOnTarget(ref context, mainRecord);
        }

        private void CompleteMove(MoveContext context, MoveRecord mainRecord)
        {
            SnapshotBombCounters(mainRecord);

            _model.MovesCount.Value++;

            NexusLog.Info("MoveRingCommand", "CompleteMove", $"{context.FromPoleId}->{context.ToPoleId}",
                $"Move {context.FromPoleId}->{context.ToPoleId} OK. Total moves={_model.MovesCount.Value}. Subs={mainRecord.SubMoves?.Count ?? 0}.");

            _signalBus.Fire(new RingMovedSignal(context.FromPoleId, context.ToPoleId));

            TickAllBombsAndCapture(mainRecord);
            bool bombExploded = mainRecord.BombExplodedRings.Count > 0;
            _model.MoveHistory.Push(mainRecord);

            if (bombExploded)
            {
                NexusLog.Warn("MoveRingCommand", "CompleteMove", $"{context.FromPoleId}->{context.ToPoleId}",
                    "Bomb exploded during classic-mode move. Continuing level per GDD §3 — only Challenge mode ends the run.");
            }

            _signalBus.Fire(new CheckWinSignal());
        }

        private MoveRecord BuildMoveRecord(MoveContext context)
        {
            var record = MoveRecordPool.Rent();
            record.FromPoleId = context.FromPoleId;
            record.ToPoleId = context.ToPoleId;
            record.Ring = context.MovingRing;
            record.WasMysteryRevealedOnFrom = context.WasMysteryRevealed;
            record.WasTargetPoleUnlocked = context.WasPoleUnlocked;
            record.WasPainted = context.WasPaintApplied;
            record.PaintedRingIndex = context.PaintedRingIndex;
            record.PaintedRingOriginalColor = context.PaintedRingOriginalColor;
            record.OriginalColor = context.MovingRing.Color;
            record.WasRainbowTargetConverted = context.WasRainbowConverted;
            record.RainbowTargetRingIndex = context.RainbowTargetIndex;
            record.RainbowTargetOriginalColor = context.RainbowTargetOriginalColor;
            return record;
        }

        private bool TryReserveChainCapacity(ref RingData ring, PoleState fromPole, PoleState toPole)
        {
            if (ring.Type != RingType.Chain) return true;

            foreach (var pole in _model.Poles)
            {
                if (pole.Id == fromPole.Id) continue;
                var topR = pole.TopRing;
                if (topR.Type == RingType.Chain && topR.AdditionalData == ring.AdditionalData)
                {
                    if (toPole.Rings.Count + 2 > toPole.MaxCapacity)
                    {
                        NexusLog.Warn("MoveRingCommand", "TryReserveChainCapacity", $"{fromPole.Id}->{toPole.Id}",
                            $"Chain move blocked — target pole {toPole.Id} full (need 2 slots, have {toPole.MaxCapacity - toPole.Rings.Count} free).");
                        return false;
                    }
                    break;
                }
            }
            return true;
        }

        private void TryBreakIceOnTarget(ref MoveContext context, MoveRecord mainRecord)
        {
            if (context.ToPole.Rings.Count < 2) return;

            int checkIndex = context.ToPole.Rings.Count - 2;
            bool anyBroken = false;

            while (checkIndex >= 0)
            {
                var current = context.ToPole.Rings[checkIndex];
                if (current.Type != RingType.Frozen || current.Color != context.MovingRing.Color)
                    break;

                context.ToPole.Rings[checkIndex] = new RingData(current.Color, RingType.Standard);
                mainRecord.IceBrokenRingIndices.Add(checkIndex);
                anyBroken = true;
                checkIndex--;
            }

            if (anyBroken)
            {
                context.WasIceBroken = true;
                context.IceBrokenRingIndices = mainRecord.IceBrokenRingIndices;
                _signalBus.Fire(new BreakIceSignal(context.ToPoleId));
                NexusLog.Info("MoveRingCommand", "TryBreakIceOnTarget", context.ToPoleId.ToString(),
                    $"Ice broken on pole {context.ToPoleId}: {mainRecord.IceBrokenRingIndices.Count} rings melted at indices [{string.Join(",", mainRecord.IceBrokenRingIndices)}].");
            }
        }

        private void ApplyChainSubMove(ref MoveContext context, MoveRecord mainRecord)
        {
            if (context.MovingRing.Type != RingType.Chain) return;

            foreach (var pole in _model.Poles)
            {
                if (pole.Id == context.FromPole.Id) continue;
                var topR = pole.TopRing;
                if (topR.Type != RingType.Chain || topR.AdditionalData != context.MovingRing.AdditionalData) continue;

                pole.PopRing();
                context.ToPole.AddRing(topR);

                var subRecord = MoveRecordPool.Rent();
                subRecord.FromPoleId = pole.Id;
                subRecord.ToPoleId = context.ToPoleId;
                subRecord.Ring = topR;
                mainRecord.SubMoves.Add(subRecord);

                NexusLog.Info("MoveRingCommand", "ApplyChainSubMove", context.ToPoleId.ToString(),
                    $"Chain sub-move: partner from pole {pole.Id} → {context.ToPoleId}, color={topR.Color}.");
                return;
            }
        }

        private void ApplyMagnetPull(ref MoveContext context, MoveRecord mainRecord)
        {
            if (context.MovingRing.Type != RingType.Magnet) return;

            int toIdx = context.ToPoleId;
            int[] candidateIdx = (toIdx % 2 == 0)
                ? new[] { toIdx - 1, toIdx + 1 }
                : new[] { toIdx - 1, toIdx + 1 };

            int pullCount = 0;
            for (int i = 0; i < candidateIdx.Length; i++)
            {
                int p = candidateIdx[i];
                if (p < 0 || p >= _model.Poles.Count) continue;
                if (p == context.ToPoleId) continue;
                if (context.ToPole.IsFull) break;
                var pole = _model.Poles[p];
                if (!pole.CanPopRing() || pole.TopRing.Color != context.MovingRing.Color) continue;

                var pulled = pole.PopRing();
                context.ToPole.AddRing(pulled);

                var subRecord = MoveRecordPool.Rent();
                subRecord.FromPoleId = p;
                subRecord.ToPoleId = context.ToPoleId;
                subRecord.Ring = pulled;
                mainRecord.SubMoves.Add(subRecord);
                pullCount++;
            }

            if (pullCount > 0)
            {
                NexusLog.Info("MoveRingCommand", "ApplyMagnetPull", context.ToPoleId.ToString(),
                    $"Magnet pulled {pullCount} matching ring(s) from adjacent poles to pole {context.ToPoleId}, color={context.MovingRing.Color}.");
            }
        }

        private void TickAllBombsAndCapture(MoveRecord mainRecord)
        {
            if (!AnyPoleHasBomb()) return;

            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];
                int explodedCount = 0;
                Span<int> explodedIdx = stackalloc int[4];

                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    var ring = pole.Rings[r];
                    if (ring.Type != RingType.Bomb) continue;

                    int newCounter = ring.AdditionalData - 1;
                    pole.Rings[r] = new RingData(ring.Color, RingType.Bomb, newCounter);
                    _signalBus.Fire(new BombTickSignal(pole.Id, newCounter));

                    if (newCounter <= 0)
                    {
                        _signalBus.Fire(new BombExplodedSignal(pole.Id));
                        explodedIdx[explodedCount++] = r;
                    }
                }

                if (explodedCount > 0)
                {
                    for (int i = explodedCount - 1; i >= 0; i--)
                    {

                        int idx = explodedIdx[i];
                        mainRecord.BombExplodedRings.Add((pole.Id, idx, pole.Rings[idx]));
                        pole.Rings.RemoveAt(idx);
                    }
                }
            }
        }

        private bool AnyPoleHasBomb()
        {
            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    if (pole.Rings[r].Type == RingType.Bomb) return true;
                }
            }
            return false;
        }

        private void SnapshotBombCounters(MoveRecord mainRecord)
        {
            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];
                for (int i = 0; i < pole.Rings.Count; i++)
                {
                    if (pole.Rings[i].Type != RingType.Bomb) continue;
                    mainRecord.BombCountersBeforeTick.Add((pole.Id, i, pole.Rings[i].AdditionalData));
                }
            }
        }
    }
}