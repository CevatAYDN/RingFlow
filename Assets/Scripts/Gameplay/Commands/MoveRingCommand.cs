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
            var fromPole = _model.Poles.GetPoleById(signal.FromPoleId);
            var toPole = _model.Poles.GetPoleById(signal.ToPoleId);

            if (fromPole == null || toPole == null || !fromPole.CanPopRing())
            {
                NexusLog.Warn("MoveRingCommand", "Execute", $"{signal.FromPoleId}->{signal.ToPoleId}",
                    $"Blocked. fromPoleNull={fromPole == null}, toPoleNull={toPole == null}, canPop={fromPole?.CanPopRing()}");
                return;
            }

            var ring = fromPole.TopRing;

            if (!TryReserveChainCapacity(ref ring, fromPole, toPole)) return;
            if (!toPole.CanAddRing(ring)) return;

            var context = new MoveContext
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

            if (!_strategyManager.ExecutePreMoveValidation(ring.Type, ref context))
            {
                NexusLog.Info("MoveRingCommand", "Execute", $"{signal.FromPoleId}->{signal.ToPoleId}",
                    "Move blocked by strategy validation.");
                return;
            }

            fromPole.PopRing();

            if (toPole.IsLocked && ring.Type == RingType.Locked)
            {
                toPole.IsLocked = false;
                context.WasPoleUnlocked = true;
                _signalBus.Fire(new UnlockPoleSignal(signal.ToPoleId));
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

            var mainRecord = BuildMoveRecord(context);

            ApplyChainSubMove(ref context, mainRecord);
            ApplyMagnetPull(ref context, mainRecord);
            TryBreakIceOnTarget(ref context, mainRecord);

            SnapshotBombCounters(mainRecord);

            _model.MovesCount.Value++;

            NexusLog.Info("MoveRingCommand", "Execute", $"{signal.FromPoleId}->{signal.ToPoleId}",
                $"Move {signal.FromPoleId}->{signal.ToPoleId} OK. Total moves={_model.MovesCount.Value}. Subs={mainRecord.SubMoves?.Count ?? 0}.");

            _signalBus.Fire(new RingMovedSignal(signal.FromPoleId, signal.ToPoleId));

            TickAllBombsAndCapture(mainRecord);
            bool bombExploded = mainRecord.BombExplodedRings.Count > 0;
            _model.MoveHistory.Push(mainRecord);

            if (bombExploded)
            {
                NexusLog.Warn("MoveRingCommand", "Execute", $"{signal.FromPoleId}->{signal.ToPoleId}",
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
                    if (toPole.Rings.Count + 2 > toPole.MaxCapacity) return false;
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
                mainRecord.IceBrokenRingIndices.Sort();
                context.WasIceBroken = true;
                context.IceBrokenRingIndices = mainRecord.IceBrokenRingIndices;
                _signalBus.Fire(new BreakIceSignal(context.ToPoleId));
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
            }
        }

        private void TickAllBombsAndCapture(MoveRecord mainRecord)
        {
            bool hasBombs = false;
            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    if (pole.Rings[r].Type == RingType.Bomb)
                    {
                        hasBombs = true;
                        break;
                    }
                    if (hasBombs) break;
                }
            }
            if (!hasBombs) return;

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