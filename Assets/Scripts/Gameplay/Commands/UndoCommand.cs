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
#if DEVELOPMENT_BUILD
                NexusLog.Info("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                    $"Undoing move. History depth: {depthAfterPop} remaining.");
#endif

                var fromPole = _model.Poles.GetPoleById(lastMove.FromPoleId);
                var toPole   = _model.Poles.GetPoleById(lastMove.ToPoleId);

                if (fromPole != null && toPole != null)
                {
                    // MoveRingCommand.CaptureBoardSnapshot always populates BoardBefore, so the
                    // snapshot-restore path above is canonical and fully reverses the move —
                    // including bombs, special rings, portals, ice, chain and magnet pulls.
                    // The field-by-field undo path was dead code and has been removed.
                    if (lastMove.BoardBefore.Count > 0)
                    {
                        RestoreBoardSnapshot(lastMove);
                        FinishUndo(lastMove);
                        return;
                    }

#if DEVELOPMENT_BUILD
                    NexusLog.Warn("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        "Undo failed: no board snapshot captured for this move.");
#endif
                    MoveRecordPool.Return(lastMove);
                }
                else
                {
#if DEVELOPMENT_BUILD
                    NexusLog.Warn("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        $"Undo failed: pole lookup returned null. fromNull={fromPole == null}, toNull={toPole == null}");
#endif

                    MoveRecordPool.Return(lastMove);
                }
            }
            else
            {
#if DEVELOPMENT_BUILD
                NexusLog.Warn("UndoCommand", "Execute", "", "Undo requested but MoveHistory is empty.");
#endif
            }
        }


        /// <summary>
        /// FIX-H2 documented contract:
        /// RestoreBoardSnapshot fully reverses ALL aspects of the move via the
        /// BoardBefore snapshot. This includes:
        ///   • Standard ring positions (from→to, to→from)
        ///   • Locked/Unlocked pole state
        ///   • Portal partner IDs
        ///   • Ring capacity
        ///   • All ring data (color, type, additional data including Bomb counters)
        ///   • CompletedPoles is cleared and recomputed by CheckWinSignal that FinishUndo fires
        ///
        /// IMPORTANT: Because the snapshot includes ring data with Bomb counters,
        /// BombCountersBeforeTick and BombExplodedRings are NOT used during undo —
        /// the snapshot already contains the correct counter values. These fields
        /// exist primarily for the solver's replay engine and for debug tooling.
        /// </summary>
        private void RestoreBoardSnapshot(MoveRecord record)
        {
            // Restore in place: mutate the existing PoleState objects (matched by Id) instead of
            // recreating them. This preserves external references (views, tests) and avoids
            // per-undo allocations, satisfying the zero-GC-during-gameplay rule.
            for (int i = 0; i < record.BoardBefore.Count; i++)
            {
                var snapshot = record.BoardBefore[i];
                var pole = _model.Poles.GetPoleById(snapshot.Id);
                if (pole == null) continue;

                pole.IsLocked = snapshot.IsLocked;
                pole.PortalPartnerId = snapshot.PortalPartnerId;
                pole.SetCapacity(snapshot.RingCapacity);
                pole.Rings.Clear();
                for (int r = 0; r < snapshot.Rings.Count; r++)
                    pole.AddRingRaw(snapshot.Rings[r].Clone());
            }

            // Completed-pole flags are recomputed by the win-check fired in FinishUndo.
            _model.CompletedPoles.Clear();
        }

        private void FinishUndo(MoveRecord lastMove)
        {
            if (_model.MovesCount.Value > 0)
            {
                _model.MovesCount.Value--;
            }

            if (_model.IsGameWon.Value)
            {
                _model.IsGameWon.Value = false;
            }

            _model.SelectedPoleId.Value = -1;
#if DEVELOPMENT_BUILD
            NexusLog.Info("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                $"Undo complete. Moves now: {_model.MovesCount.Value}");
#endif

            MoveRecordPool.Return(lastMove);
            // CheckWinCommand is IAsyncCommand — Fire() throws at runtime for async handlers.
            _signalBus.FireAsyncAndForget(new CheckWinSignal(),
                ex => NexusLog.Error("UndoCommand", "FinishUndo", "",
                    $"CheckWinSignal handler threw: {ex?.GetType().Name}: {ex?.Message}"));
        }


    }
}
