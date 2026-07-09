using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private ISignalBus _signalBus;

        public void Execute(InitLevelSignal signal)
        {
            if (_model == null)
            {
                NexusLog.Error("InitLevelCommand", "Execute", signal.LevelIndex.ToString(),
                    "GameplayModel is null — cannot initialize level.");
                return;
            }

            _model.Reset();

            int currentLevel = signal.LevelIndex > 0
                ? signal.LevelIndex
                : _progressionService?.CurrentLevel.Value ?? 1;

            LevelData levelData = null;
            var savedLevel = UnityEngine.Resources.Load<LevelDataSO>($"Levels/Level_{currentLevel}");
            if (savedLevel != null && savedLevel.Data != null)
            {
                levelData = savedLevel.Data;
            }
            else
            {
                if (_progressionService == null && signal.LevelIndex <= 0)
                {
                    NexusLog.Warn("InitLevelCommand", "Execute", currentLevel.ToString(),
                        "Progression service not bound and no level index specified — defaulting to level 1.");
                }

                int poleCount = DifficultyCurve.PoleCountForLevel(currentLevel);
                int colorCount = DifficultyCurve.ColorCountForLevel(currentLevel);
                int maxCapacity = DifficultyCurve.MaxCapacityForLevel(currentLevel);

                if (poleCount < colorCount + 1) poleCount = colorCount + 1;
                if (poleCount > 12)
                {
                    NexusLog.Warn("InitLevelCommand", "Execute", currentLevel.ToString(),
                        $"Computed pole count exceeded 12; clamping from {poleCount}.");
                    poleCount = 12;
                }

                levelData = LevelGenerator.GenerateLevel(
                    currentLevel, currentLevel * 12345, poleCount, colorCount, maxCapacity);
            }

            if (levelData != null)
            {
                for (int i = 0; i < levelData.Poles.Count; i++)
                {
                    var pData = levelData.Poles[i];
                    var poleState = new PoleState
                    {
                        Id = i,
                        MaxCapacity = pData.MaxCapacity,
                        IsLocked = pData.IsLocked
                    };

                    for (int r = 0; r < pData.Rings.Count; r++)
                    {
                        poleState.AddRing(pData.Rings[r]);
                    }

                    _model.Poles.Add(poleState);
                }

                _model.TargetMovesCount.Value = levelData.TargetMoves;
            }
            else
            {
                NexusLog.Error("InitLevelCommand", "Execute", currentLevel.ToString(),
                    "LevelGenerator returned null — fallback to hardcoded 3-pole tutorial. Likely cause: solver hit search limits or seed exhausted.");

                var p0 = new PoleState { Id = 0 };
                p0.AddRing(new RingData(RingColor.Red));
                p0.AddRing(new RingData(RingColor.Blue));
                var p1 = new PoleState { Id = 1 };
                p1.AddRing(new RingData(RingColor.Blue));
                p1.AddRing(new RingData(RingColor.Red));
                var p2 = new PoleState { Id = 2 };

                _model.Poles.Add(p0);
                _model.Poles.Add(p1);
                _model.Poles.Add(p2);
                _model.TargetMovesCount.Value = 2;
            }

            NexusLog.Info("InitLevelCommand", "Execute", currentLevel.ToString(),
                $"Initialized level {currentLevel} with {_model.Poles.Count} poles. Target moves: {_model.TargetMovesCount.Value}.");

            int worldIndex = WorldConfigSO.WorldFromAbsoluteLevel(currentLevel);

            // Glass rings are handled as Standard (visual-only, no special mechanics).
            // Guard against null levelData (fallback tutorial path) to avoid NRE.
            if (levelData != null)
            {
                int glassCount = 0;
                foreach (var p in levelData.Poles)
                    foreach (var r in p.Rings)
                        if (r.Type == RingType.Glass) glassCount++;
                if (glassCount > 0)
                    NexusLog.Info("InitLevelCommand", "Execute", currentLevel.ToString(),
                        $"Level has {glassCount} Glass ring(s) — treated as Standard (visual-only).");
            }

            AnalyticsEvents.LevelStart(currentLevel, worldIndex);

            // Fire AFTER the model is populated so subscribers (e.g. BoardMediator)
            // see a consistent state. This avoids the subscriber-before-command
            // ordering problem in Nexus's SignalBus.
            _signalBus?.Fire(new LevelLoadedSignal(currentLevel));
        }
    }

    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(SelectPoleSignal signal)
        {
            NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), $"Start. Selected={_model.SelectedPoleId.Value}, Won={_model.IsGameWon.Value}, Poles={_model.Poles.Count}");

            if (_model.IsGameWon.Value) return;

            int currentSelected = _model.SelectedPoleId.Value;

            if (currentSelected == -1)
            {
                var pole = _model.Poles.GetPoleById(signal.PoleId);
                if (pole != null && pole.CanPopRing())
                {
                    _model.SelectedPoleId.Value = signal.PoleId;
                    TryRevealGhost(pole, signal.PoleId, _signalBus);
                }
            }
            else
            {
                if (currentSelected == signal.PoleId)
                {
                    // Aynı direğe tekrar basıldıysa seçimi iptal et
                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), "Deselecting same pole.");
                    _model.SelectedPoleId.Value = -1;
                }
                else
                {
                    int fromId = currentSelected;
                    int toId = signal.PoleId;
                    var fromPole = _model.Poles.GetPoleById(fromId);
                    var toPole = _model.Poles.GetPoleById(toId);

                    if (fromPole == null || toPole == null || !fromPole.CanPopRing())
                    {
                        NexusLog.Warn("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                            $"Move blocked. fromNull={fromPole == null}, toNull={toPole == null}, canPop={fromPole?.CanPopRing()}");
                        _model.SelectedPoleId.Value = -1;
                        return;
                    }

                    if (!toPole.CanAddRing(fromPole.TopRing))
                    {
                        // Hedefe hareket ettiremiyoruz. Ancak hedef direk kendisi halka çıkartılabilecek (seçilebilecek) durumdaysa, seçimi oraya kaydır.
                        if (toPole.CanPopRing())
                        {
                            _model.SelectedPoleId.Value = toId;
                            TryRevealGhost(toPole, toId, _signalBus);
                            return;
                        }

                        string reason = GameplayHelpers.DescribeBlockReason(fromPole, toPole);
                        NexusLog.Warn("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                            $"Move blocked. target cannot accept ring. from={fromId}, to={toId} reason={reason}");
                        _signalBus?.Fire(new MoveBlockedSignal(fromId, toId, reason));
                        return;
                    }

                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), $"Moving from {fromId} to {toId}.");
                    _model.SelectedPoleId.Value = -1;
                    _signalBus.Fire(new MoveRingSignal(fromId, toId));
                }
            }
        }

        private static void TryRevealGhost(PoleState pole, int poleId, ISignalBus signalBus)
        {
            if (pole.TopRing.Type != RingType.Ghost) return;
            var ghostCopy = pole.Rings[^1];
            ghostCopy.Type = RingType.Standard;
            pole.Rings[^1] = ghostCopy;
            signalBus?.Fire(new RevealMysterySignal(poleId, ghostCopy));
        }
    }

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

            // Build strategy context (Nexus pattern: struct for 0-GC)
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

            // Execute pre-move strategies (Mystery, Paint, Rainbow)
            if (!_strategyManager.ExecutePreMoveValidation(ring.Type, ref context))
            {
                NexusLog.Info("MoveRingCommand", "Execute", $"{signal.FromPoleId}->{signal.ToPoleId}",
                    "Move blocked by strategy validation.");
                return;
            }

            // Execute the main move
            fromPole.PopRing();

            // Handle locked pole unlock (Key ring logic)
            if (toPole.IsLocked && ring.Type == RingType.Locked)
            {
                toPole.IsLocked = false;
                context.WasPoleUnlocked = true;
                _signalBus.Fire(new UnlockPoleSignal(signal.ToPoleId));
            }

            toPole.AddRing(ring);

            // Execute post-move strategies (Paint application, Rainbow conversion, etc.)
            _strategyManager.ExecutePostMoveExecution(ring.Type, ref context);

            // Paint/Rainbow targets: If standard ring lands on top of Paint or Rainbow ring, run their strategy
            if (toPole.Rings.Count >= 2)
            {
                var targetType = toPole.Rings[toPole.Rings.Count - 2].Type;
                if (targetType == RingType.Paint || targetType == RingType.Rainbow)
                {
                    _strategyManager.ExecutePostMoveExecution(targetType, ref context);
                }
            }

            // Mystery reveal: If the new top ring on the source pole (FromPole) is Mystery, reveal it
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
            if (bombExploded)
            {
                _model.IsGameWon.Value = false;
                _model.MoveHistory.Push(mainRecord);
                FireGameOverTransition();
                return;
            }

            _model.MoveHistory.Push(mainRecord);
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

        private void FireGameOverTransition()
        {
            if (_fsm == null)
            {
                NexusLog.Warn("MoveRingCommand", nameof(FireGameOverTransition), "",
                    "IGameStateMachine unbound; cannot transition to GameOverState.");
                return;
            }
            try
            {
                _ = _fsm.ChangeStateAsync<GameOverState>();
            }
            catch (System.Exception ex)
            {
                NexusLog.Error("MoveRingCommand", nameof(FireGameOverTransition), "", ex);
            }
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

            foreach (var p in _model.Poles)
            {
                if (p.Id == context.ToPoleId) continue;
                if (context.ToPole.IsFull) break;
                if (!p.CanPopRing() || p.TopRing.Color != context.MovingRing.Color) continue;

                var pulled = p.PopRing();
                context.ToPole.AddRing(pulled);

                var subRecord = MoveRecordPool.Rent();
                subRecord.FromPoleId = p.Id;
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
                }
                if (hasBombs) break;
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
                    // C4: Restore exploded bomb rings BEFORE any other undo logic.
                    // Insert the captured rings at their original positions (highest index first
                    // to preserve ordering during insertion).
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

                    // Restore bomb counters AFTER restoring exploded bombs so that
                    // snapshot RingIndex entries point to the correct position.
                    RestoreBombCounters(lastMove.BombCountersBeforeTick);

                    // SubMoves (magnet, chain) reversed before main ring because
                    // they were applied after the main ring in the original move
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

                    // Win bayrağını sıfırla: undo, kazanılan bir seviyeyi geri alıyorsa
                    // CheckWinCommand erken dönüş yapmasın ve tahta yeniden değerlendirilsin.
                    if (_model.IsGameWon.Value)
                    {
                        _model.IsGameWon.Value = false;
                    }

                    _model.SelectedPoleId.Value = -1;
                    NexusLog.Info("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        $"Undo complete. Moves now: {_model.MovesCount.Value}");

                    // Return to pool after we're done using it
                    MoveRecordPool.Return(lastMove);

                    _signalBus.Fire(new CheckWinSignal());
                }
                else
                {
                    NexusLog.Warn("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        $"Undo failed: pole lookup returned null. fromNull={fromPole == null}, toNull={toPole == null}");
                    
                    // Even if failed, we must return to pool to avoid leaking the record
                    MoveRecordPool.Return(lastMove);
                }
            }
            else
            {
                NexusLog.Warn("UndoCommand", "Execute", "", "Undo requested but MoveHistory is empty.");
            }
        }

        private void RestoreBombCounters(List<(int PoleId, int RingIndex, int Counter)> snapshot)
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

    public class CheckWinCommand : ICommand<CheckWinSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(CheckWinSignal signal)
        {
            if (_model.IsGameWon.Value) return;
            if (_model == null || _model.Poles.Count == 0)
            {
                NexusLog.Warn("CheckWinCommand", "Execute", "",
                    "Model or poles empty — cannot evaluate win.");
                return;
            }

            bool won = true;
            int nonEmptyPoleCount = 0;

            foreach (var pole in _model.Poles)
            {
                if (pole.IsEmpty) continue;

                nonEmptyPoleCount++;

                bool poleSolved = LevelSolver.IsSolved(pole, pole.MaxCapacity);
                if (!poleSolved)
                {
                    won = false;
                    break;
                }
            }

            if (nonEmptyPoleCount == 0)
            {
                won = false;
            }

            if (won)
            {
                _model.IsGameWon.Value = true;
                _signalBus.Fire(new LevelWonSignal());
            }
        }
    }

    public class LevelWonCommand : ICommand<LevelWonSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private IEconomyService _economyService;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IGameStateMachine _fsm;
        [Inject] private IAdService _ads;

        private const int InterstitialInterval = 3;

        public void Execute(LevelWonSignal signal)
        {
            if (_progressionService == null)
            {
                NexusLog.Error("LevelWonCommand", "Execute", "",
                    "IProgressionService unbound; cannot advance level even though board was solved.");
            }
            if (_economyService == null)
            {
                NexusLog.Error("LevelWonCommand", "Execute", "",
                    "IEconomyService unbound; coin/xp reward dropped.");
            }
            if (_progress == null)
            {
                NexusLog.Error("LevelWonCommand", "Execute", "",
                    "PlayerProgressModel unbound; xp/world unlock dropped.");
            }

            int prevLevel = _progressionService != null ? _progressionService.CurrentLevel.Value : 0;
            int prevMoves = _model.MovesCount.Value;
            int prevTarget = _model.TargetMovesCount.Value;

            int stars = 1;
            if (prevTarget > 0)
            {
                if (prevMoves <= prevTarget) stars = 3;
                else if (prevMoves <= prevTarget * 1.3) stars = 2;
            }

            if (_progressionService != null)
            {
                _progressionService.CompleteCurrentLevel();
            }

            int newLevel = _progressionService != null ? _progressionService.CurrentLevel.Value : prevLevel;

            int newWorldIndex = WorldConfigSO.WorldFromAbsoluteLevel(newLevel);
            if (_progress != null &&
                newWorldIndex >= 0 &&
                newWorldIndex < _progress.UnlockedWorlds.Count)
            {
                _progress.UnlockedWorlds[newWorldIndex] = true;
            }

            bool isBoss = WorldConfigSO.IsBossLevel(prevLevel);
            int coinReward = isBoss ? 500 : 50 + (prevLevel % 11) * 10;
            _economyService?.Earn("Coins", coinReward, isBoss ? "Boss Win Reward" : "Level Win Reward");

            int xpEarned = isBoss ? 50 : 10;
            if (_progress != null)
            {
                _progress.Xp.Value += xpEarned;

                int xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
                while (_progress.Xp.Value >= xpRequired)
                {
                    _progress.Xp.Value -= xpRequired;
                    _progress.PlayerLevel.Value++;
                    _economyService?.Earn("Coins", 100, "Player Level Up Reward");
                    xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
                }
            }

            _model.LastReward.Value = WinReward.From(prevMoves, prevTarget, coinReward, xpEarned, stars);

            NexusLog.Info("LevelWonCommand", "Execute", "",
                $"Level {prevLevel} WON! Moves={prevMoves}, Target={prevTarget}, Stars={stars}, Coins+={coinReward}, XP+={xpEarned}");

            AnalyticsEvents.LevelComplete(prevLevel, prevMoves, stars);

            // Award chests on win (GDD §9)
            if (_progress != null)
            {
                _progress.ChestBronze.Value++;
                if (UnityEngine.Random.value < 0.40f) _progress.ChestSilver.Value++;
                if (stars >= 3 && UnityEngine.Random.value < 0.10f) _progress.ChestGold.Value++;
                if (stars >= 3 && UnityEngine.Random.value < 0.01f) _progress.ChestDiamond.Value++;
            }

            // Interstitial ad every 3 levels (GDD §10)
            if (_progress != null && _ads != null && !_progress.RemoveAds.Value)
            {
                _progress.LevelsSinceLastInterstitial++;
                if (_progress.LevelsSinceLastInterstitial >= InterstitialInterval)
                {
                    _progress.LevelsSinceLastInterstitial = 0;
                    if (_ads.IsInterstitialAvailable("LevelComplete"))
                    {
                        NexusLog.Info("LevelWonCommand", "Execute", "",
                            $"Showing interstitial ad (interval={InterstitialInterval}).");
                        _ads.ShowInterstitial("LevelComplete");
                        AnalyticsEvents.InterstitialAd("LevelComplete");
                    }
                }
            }

            _ = _fsm?.ChangeStateAsync<WinState>();
        }
    }

    public class UndoRequestedCommand : ICommand<UndoRequestedSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progressionService;

        public void Execute(UndoRequestedSignal signal)
        {
            if (_model.MoveHistory.Count == 0)
            {
                NexusLog.Warn("UndoRequestedCommand", "Execute", "", "No moves to undo.");
                return;
            }

            int level = _progressionService?.CurrentLevel.Value ?? 0;

            if (_progress.FreeUndosUsedThisSession.Value < 5)
            {
                _progress.FreeUndosUsedThisSession.Value++;
                NexusLog.Info("UndoRequestedCommand", "Execute", "",
                    $"Free undo used ({_progress.FreeUndosUsedThisSession.Value}/5 this session).");
                AnalyticsEvents.UndoUse(level, wasFree: true);
                _signalBus.Fire(new UndoSignal());
            }
            else if (_economy.CanAfford("Coins", 5))
            {
                if (_economy.Spend("Coins", 5, "Undo"))
                {
                    NexusLog.Info("UndoRequestedCommand", "Execute", "",
                        "Paid undo with 5 coins.");
                    AnalyticsEvents.UndoUse(level, wasFree: false);
                    _signalBus.Fire(new UndoSignal());
                }
            }
            else if (_ads != null && _ads.IsRewardedAvailable("Undo"))
            {
                NexusLog.Info("UndoRequestedCommand", "Execute", "",
                    "No coins for undo; showing rewarded ad.");
                _ads.ShowRewarded("Undo", success =>
                {
                    if (success)
                    {
                        NexusLog.Info("UndoRequestedCommand", "Execute", "",
                            "Rewarded ad completed; applying undo.");
                        AnalyticsEvents.UndoUse(level, wasFree: false);
                        AnalyticsEvents.RewardedAd("Undo", true);
                        _signalBus.Fire(new UndoSignal());
                    }
                    else
                    {
                        NexusLog.Warn("UndoRequestedCommand", "Execute", "",
                            "Rewarded ad not completed; undo skipped.");
                    }
                });
            }
            else
            {
                NexusLog.Warn("UndoRequestedCommand", "Execute", "",
                    "Cannot afford undo (no coins, no ad).");
            }
        }
    }

    public static class GameplayHelpers
    {
        public static GameObject FindRootGameObject(string name)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i] != null && rootObjects[i].name == name)
                {
                    return rootObjects[i];
                }
            }
            return null;
        }

        public static PoleState GetPoleById(this List<PoleState> poles, int id)
        {
            if (poles == null) return null;
            if (id >= 0 && id < poles.Count)
            {
                var pole = poles[id];
                if (pole != null && pole.Id == id) return pole;
            }
            for (int i = 0; i < poles.Count; i++)
            {
                if (poles[i] != null && poles[i].Id == id) return poles[i];
            }
            return null;
        }

        public static string DescribeBlockReason(PoleState fromPole, PoleState toPole)
        {
            if (toPole.IsLocked) return "Locked";
            if (toPole.IsFull) return "Pole full";
            if (toPole.IsEmpty) return "Color mismatch";
            if (toPole.TopRing.Type == RingType.Stone) return "Stone blocks";
            return "Color mismatch";
        }
    }
}



