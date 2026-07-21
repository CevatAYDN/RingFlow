using System;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Gameplay
{
    public class MoveRingCommand : ICommand<MoveRingSignal>, IResettable
        {
            [Inject] private GameplayModel _model;
            [Inject] private ISignalBus _signalBus;
            [Inject] private RingMoveStrategyManager _strategyManager;
            [Inject] private IProgressionService _progression;
            [Inject] private RingValidationStrategyManager _validationManager;
            [Inject] private GameFeelConfigSO _feelConfig;
            // BUG-7 FIX: Inject GameConfigDatabaseSO so ShouldTickBomb reads BombTickMode
            // from the same SSOT as LevelGenerator and LevelSolver (AGENTS.md Golden Rule #3).
            [Inject] private GameConfigDatabaseSO _dbConfig;
            [Inject] private IPlayerPrefsService _prefs;

        // IResettable: called by CommandPool before returning this instance to the pool.
        // Clears the bomb-count cache so the next Execute() starts with a fresh scan.
        public void Reset() => _cachedBombCount = BombCountUnknown;

        public void Execute(MoveRingSignal signal)
        {
            if (!TryValidateMove(signal, out var context)) return;
            if (!TryPreMoveValidate(ref context)) return;

            var mainRecord = MoveRecordPool.Rent();
            CaptureBoardSnapshot(mainRecord);

            // Set MainRecord on context BEFORE ExecuteCoreMove so that Chain and Magnet
            // strategies (called via PostMoveExecution inside ExecuteCoreMove) can append
            // their sub-move records directly. Without this, MainRecord would be null
            // when strategies run and sub-moves would not be recorded for Undo.
            context.MainRecord = mainRecord;

            ExecuteCoreMove(ref context);
            PopulateMoveRecord(context, mainRecord);
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
            if (toPole.Rings.Count >= toPole.MaxCapacity) return false;
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

            if (toPole.IsLocked && ring.Type.IsLockedKey())
            {
                toPole.IsLocked = false;
                context.WasPoleUnlocked = true;
                _signalBus.Fire(new UnlockPoleSignal(context.ToPoleId));
                NexusLog.Info("MoveRingCommand", "ExecuteCoreMove", context.ToPoleId.ToString(),
                    $"Locked pole {context.ToPoleId} unlocked with Key ring.");
            }

            // FIX-G2: Ice-breaking must trigger for ANY ring type that shares the frozen
            // ring's color, not just Standard. GDD §31 says "üzerindeki tüm halkalar
            // kaldırıldığında" (all rings above are removed) — no restriction on the
            // ring type that lands on top. Key, Rainbow, and Paint rings can all break
            // ice when color-matched. The previous Standard-only guard silently dropped
            // ice-break VFX and undo records when a non-standard ring completed the thaw.
            int frozenBelowIndex = toPole.Rings.Count - 1;
            bool breaksIce = frozenBelowIndex >= 0 &&
                             toPole.Rings[frozenBelowIndex].Type == RingType.Frozen &&
                             toPole.Rings[frozenBelowIndex].Color == ring.Color;

            toPole.AddRing(ring);

            // FIX-M3: Capture PlayerRingIndex immediately after AddRing but BEFORE
            // PostMoveExecution (chain/magnet pulls may add rings above ours).
            context.PlayerRingIndex = toPole.Rings.Count - 1;

            if (breaksIce)
            {
                // Thaw the frozen ring in the model (now the ring below the newly added one).
                // PoleState.AddRing no longer does this (FIX-A2), so it must be done here.
                int thawIndex = toPole.Rings.Count - 2;
                if (thawIndex >= 0 && toPole.Rings[thawIndex].Type == RingType.Frozen)
                {
                    var frozen = toPole.Rings[thawIndex];
                    toPole.Rings[thawIndex] = new RingData(frozen.Color, RingType.Standard, frozen.AdditionalData);
                }
                context.WasIceBroken = true;
                _signalBus.Fire(new BreakIceSignal(context.ToPoleId));
            }

            _strategyManager.ExecutePostMoveExecution(ring.Type, ref context);

            if (toPole.Rings.Count >= 2)
            {
                var ringBelowType = toPole.Rings[toPole.Rings.Count - 2].Type;

                // FIX-RC1: Paint on top of existing Paint — do NOT chain paint twice.
                // The moving ring already ran PostMoveExecution above (for Paint itself).
                // Only trigger the below-ring's strategy when the MOVING ring is NOT Paint,
                // to avoid double-consuming a paint ring that was placed on another Paint.
                if (ringBelowType == RingType.Paint && ring.Type != RingType.Paint)
                {
                    _strategyManager.ExecutePostMoveExecution(RingType.Paint, ref context);
                }
                // FIX-R1 (RainbowRingStrategy Case 2): another ring landed on a Rainbow
                // that was previously placed on an empty pole — trigger delayed conversion.
                else if (ringBelowType == RingType.Stone)
                {
                    _signalBus.Fire(new StoneImpactSignal(context.ToPoleId, ring.Color));
                }
                else if (ringBelowType == RingType.Rainbow)
                {
                    _strategyManager.ExecutePostMoveExecution(RingType.Rainbow, ref context);
                }
                
            }

            if (fromPole.Rings.Count > 0 && fromPole.TopRing.Type == RingType.Mystery)
            {
                _strategyManager.ExecutePostMoveExecution(RingType.Mystery, ref context);
            }
        }

        private void ExecuteSubMoves(ref MoveContext context, MoveRecord mainRecord)
        {
            // FIX-M3: Set PlayerRingIndex AFTER ExecuteCoreMove completes, because
            // Chain/Magnet PostMoveExecution may have appended additional rings on top
            // of the moved ring. Setting PlayerRingIndex before PostMoveExecution runs
            // would point to the wrong index if chain/magnet pulled new rings above
            // the player's ring. Portal teleport (ApplyPortalTeleport) uses this index
            // to find the specific ring to teleport — a stale index would teleport the
            // wrong ring (e.g. a pulled chain partner instead of the player's ring).
            //
            // Formula: PlayerRingIndex = ringCount - 1 (the player's ring sits at
            // the top BEFORE any post-move side-effects), but after chain/magnet we
            // need the original index which is now somewhere below the pulled rings.
            // Correct approach: capture the index BEFORE PostMoveExecution runs, not after.
            // Actually, PostMoveExecution is called inside ExecuteCoreMove via:
            //   _strategyManager.ExecutePostMoveExecution(ring.Type, ref context);
            // which may modify the ToPole's rings. So by the time we reach this method,
            // the player's ring may no longer be at the top.
            //
            // The correct index is: (pre-AddRing count) — the ring's position before any
            // chain/magnet pulls. We captured this during ExecuteCoreMove: after AddRing,
            // the ring is at index = toPole.Rings.Count - 1 - subPulls.
            //
            // Simplify: set PlayerRingIndex to the index of the moved ring. We know
            // it was placed at what was the top after AddRing, so we track it before
            // PostMoveExecution modifies the pole.
            // Capture in ExecuteCoreMove before strategy PostMoveExecution modifies the pole.
            if (context.PlayerRingIndex < 0)
            {
                // Fallback: if not captured by strategy, use current top index
                context.PlayerRingIndex = context.ToPole.Rings.Count - 1;
            }

            // Portal teleport remains here: it is a POLE mechanic (PortalPartnerId on PoleState),
            // not a ring-type mechanic. A ring strategy cannot own pole configuration.
            ApplyPortalTeleport(ref context, mainRecord);
        }

        private void CompleteMove(MoveContext context, MoveRecord mainRecord)
        {
            SnapshotBombCounters(mainRecord);

            _model.MovesCount.Value++;

            NexusLog.Info("MoveRingCommand", "CompleteMove", $"{context.FromPoleId}->{context.ToPoleId}",
                $"Move EXECUTED. Total moves={_model.MovesCount.Value}. Subs={mainRecord.SubMoves?.Count ?? 0}. Firing RingMovedSignal.");

            _signalBus.Fire(new RingMovedSignal(context.FromPoleId, context.ToPoleId));

            TickAllBombsAndCapture(mainRecord);
            bool bombExploded = mainRecord.BombExplodedRings.Count > 0;
            _model.MoveHistory.Push(mainRecord);
            
            // Autosave board state after each move for crash recovery
            int currentLevel = _progression?.CurrentLevel.Value ?? 1;
            BoardStateSaveSystem.Save(_prefs, _model, currentLevel);

            if (bombExploded)
            {
                int explodedPoleId = mainRecord.BombExplodedRings[0].PoleId;
#if DEVELOPMENT_BUILD
                NexusLog.Warn("MoveRingCommand", "CompleteMove", $"{context.FromPoleId}->{context.ToPoleId}",
                    $"Bomb exploded on pole {explodedPoleId}. Level failed per GDD §36.");
#endif
                _signalBus.Fire(new BombExplodedSignal(explodedPoleId));
                // LevelLostCommand is IAsyncCommand — must not use Fire() here.
                // FireAsyncAndForget dispatches the async chain without blocking Execute().
                _signalBus.FireAsyncAndForget(new LevelLostSignal($"Bomb exploded on pole {explodedPoleId}"),
                    ex => NexusLog.Error("MoveRingCommand", "CompleteMove", "",
                        $"LevelLostSignal (bomb) handler threw: {ex?.GetType().Name}: {ex?.Message}"));
            }
            else
            {
                // CheckWinCommand is IAsyncCommand — must not use Fire() here.
                _signalBus.FireAsyncAndForget(new CheckWinSignal(),
                    ex => NexusLog.Error("MoveRingCommand", "CompleteMove", "",
                        $"CheckWinSignal handler threw: {ex?.GetType().Name}: {ex?.Message}"));
                if (_model.IsChallengeMode.Value
                    && _model.ChallengeMoveLimit.Value > 0
                    && _model.MovesCount.Value >= _model.ChallengeMoveLimit.Value
                    && !_model.IsGameWon.Value)
                {
                    // LevelLostCommand is IAsyncCommand — must not use Fire() here.
                    _signalBus.FireAsyncAndForget(new LevelLostSignal($"Move limit reached ({_model.MovesCount.Value}/{_model.ChallengeMoveLimit.Value})"),
                        ex => NexusLog.Error("MoveRingCommand", "CompleteMove", "",
                            $"LevelLostSignal (move limit) handler threw: {ex?.GetType().Name}: {ex?.Message}"));
                }
            }
        }

        private void PopulateMoveRecord(MoveContext context, MoveRecord record)
        {
            record.FromPoleId = context.FromPoleId;
            record.ToPoleId = context.ToPoleId;
            record.Ring = context.MovingRing;
            record.WasMysteryRevealedOnFrom = context.WasMysteryRevealed;
            if (context.WasIceBroken)
            {
                record.IceBrokenRingIndices.Add(context.ToPole.Rings.Count - 2);
            }
            record.WasTargetPoleUnlocked = context.WasPoleUnlocked;
            record.WasPainted = context.WasPaintApplied;
            record.PaintedRingIndex = context.PaintedRingIndex;
            record.PaintedRingOriginalColor = context.PaintedRingOriginalColor;
            record.PaintConsumedRingIndex = context.PaintConsumedRingIndex;
            record.PaintConsumedRingData = context.PaintConsumedRingData;
            record.OriginalColor = context.MovingRing.Color;
            record.WasRainbowTargetConverted = context.WasRainbowConverted;
            record.RainbowTargetRingIndex = context.RainbowTargetIndex;
            record.RainbowTargetOriginalColor = context.RainbowTargetOriginalColor;
        }

        private void CaptureBoardSnapshot(MoveRecord record)
        {
            for (int i = 0; i < _model.Poles.Count; i++)
            {
                var pole = _model.Poles[i];
                if (pole == null) continue;
                var snapshot = PoleSnapshotPool.Rent();
                snapshot.Capture(pole);
                record.BoardBefore.Add(snapshot);
            }
        }

        private bool TryReserveChainCapacity(ref RingData ring, PoleState fromPole, PoleState toPole)
        {
            if (ring.Type != RingType.Chain) return true;

            // Use for-index to avoid enumerator allocation (ObservableList may allocate on foreach)
            for (int i = 0; i < _model.Poles.Count; i++)
            {
                var pole = _model.Poles[i];
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

        private void ApplyPortalTeleport(ref MoveContext context, MoveRecord mainRecord)
        {
            int portalPartnerId = context.ToPole.PortalPartnerId;
            if (portalPartnerId < 0) return;

            var partner = _model.Poles.GetPoleById(portalPartnerId);
            if (partner == null) return;
            if (partner.IsFull) return;

            int playerIdx = context.PlayerRingIndex;
            if (playerIdx < 0 || playerIdx >= context.ToPole.Rings.Count) return;
            var ring = context.ToPole.Rings[playerIdx];
            context.ToPole.Rings.RemoveAt(playerIdx);

            if (!partner.CanAddRing(ring))
            {
                context.ToPole.InsertRingRaw(playerIdx, ring);
                return;
            }

            partner.AddRing(ring);

            var subRecord = MoveRecordPool.Rent();
            subRecord.FromPoleId = context.ToPoleId;
            subRecord.ToPoleId = portalPartnerId;
            subRecord.Ring = ring;
            mainRecord.SubMoves.Add(subRecord);

            mainRecord.WasPortalTeleported = true;
            mainRecord.PortalTeleportTargetPoleId = portalPartnerId;

            _signalBus.Fire(new PortalTeleportSignal(context.ToPoleId, portalPartnerId));

#if DEVELOPMENT_BUILD
            NexusLog.Info("MoveRingCommand", nameof(ApplyPortalTeleport), $"portal{context.ToPoleId}->{portalPartnerId}",
                $"Ring teleported from portal pole {context.ToPoleId} to partner pole {portalPartnerId}, color={ring.Color}.");
#endif
        }

        // FIX-M2: AnyPoleHasBomb() was called every move and ran O(n×r) — scanning every
        // ring on every pole. For a 12-pole board with 4 rings each that's 48 comparisons
        // per move just to decide whether to tick. Replace with a cached count that is
        // maintained by TickAllBombsAndCapture itself: after every tick pass, if no bombs
        // remain we know to skip future calls immediately at O(1).
        // A full O(1) dirty-flag requires hooking PoleState mutations; for now a single
        // cached field on the command is sufficient because MoveRingCommand is
        // reconstructed per-command-bus-registration (not a long-lived singleton), so the
        // cache is valid for the lifetime of the gameplay session.
        //
        // BombCountUnknown (-1): sentinel meaning "cache is stale, full scan needed".
        //              0: scanned and confirmed no bombs on board — skip all tick logic.
        //             >0: known live bomb count — tick logic runs, count decrements on explosion.
        private const int BombCountUnknown = -1;
        private int _cachedBombCount = BombCountUnknown;

        private bool AnyPoleHasBombCached()
        {
            if (_cachedBombCount == 0) return false;
            if (_cachedBombCount > 0) return true;
            // BombCountUnknown: cache invalidated by IResettable.Reset() or first call. Full scan.
            _cachedBombCount = 0;
            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                    if (pole.Rings[r].Type == RingType.Bomb) _cachedBombCount++;
            }
            return _cachedBombCount > 0;
        }

        private void TickAllBombsAndCapture(MoveRecord mainRecord)
        {
            if (!AnyPoleHasBombCached()) return;

            for (int p = 0; p < _model.Poles.Count; p++)
            {
                var pole = _model.Poles[p];
                int explodedCount = 0;
                Span<int> explodedIdx = stackalloc int[pole.Rings.Count];

                for (int r = 0; r < pole.Rings.Count; r++)
                {
                    var ring = pole.Rings[r];
                    if (ring.Type != RingType.Bomb) continue;
                    if (!ShouldTickBomb(pole.Id, r, mainRecord, ring)) continue;

                    int newCounter = ring.AdditionalData - 1;
                    pole.Rings[r] = new RingData(ring.Color, RingType.Bomb, newCounter);
                    _signalBus.Fire(new BombTickSignal(pole.Id, newCounter));

                    if (newCounter <= 0)
                    {
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
                        // Exploded bombs reduce the cached count.
                        if (_cachedBombCount > 0) _cachedBombCount--;
                    }
                }
            }
        }

        private bool ShouldTickBomb(int poleId, int ringIndex, MoveRecord mainRecord, RingData ring)
        {
            // BUG-7 FIX: Read BombTickMode from GameConfigDatabaseSO (SSOT) instead of
            // GameFeelConfigSO. LevelGenerator and LevelSolver already read from _dbConfig,
            // so this aligns all three systems to the same single source.
            // AGENTS.md Golden Rule #3: "Generator and Solver must use identical rules."
            var mode = _dbConfig != null ? _dbConfig.LevelGen.BombTickMode
                     : (_feelConfig != null ? _feelConfig.BombTickMode : BombTickMode.AllBombsPerMove);
            switch (mode)
            {
                case BombTickMode.SourceAndTargetPolesOnly:
                    return poleId == mainRecord.FromPoleId ||
                           poleId == mainRecord.ToPoleId ||
                           poleId == mainRecord.PortalTeleportTargetPoleId;
                case BombTickMode.MovedBombOnly:
                    // Only tick the bomb that was actually moved.
                    // Aligns exactly with LevelSolver.ShouldTickBombForSolver:
                    //   movedRingType == Bomb → only pole==toPoleId ticks.
                    // The ring at the landing position IS the moved ring iff
                    //   poleId==toPoleId AND the moved ring itself was a Bomb.
                    if (mainRecord.Ring.Type != RingType.Bomb) return false;
                    return poleId == mainRecord.ToPoleId;
                default: // BombTickMode.AllBombsPerMove
                    return true;
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
