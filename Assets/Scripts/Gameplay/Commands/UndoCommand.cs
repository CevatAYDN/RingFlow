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
                    // MoveRingCommand.CaptureBoardSnapshot always populates BoardBefore, so the
                    // snapshot-restore path above is canonical and fully reverses the move —
                    // including bombs, special rings, portals, ice, ghost, chain and magnet pulls.
                    // The field-by-field undo path was dead code and has been removed.
                    if (lastMove.BoardBefore.Count > 0)
                    {
                        RestoreBoardSnapshot(lastMove);
                        RestoreGhostReveal(lastMove, fromPole);
                        FinishUndo(lastMove);
                        return;
                    }

                    NexusLog.Warn("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                        "Undo failed: no board snapshot captured for this move.");
                    MoveRecordPool.Return(lastMove);
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

        /// <summary>
        /// Re-applies the Ghost type to the restored from-pole top ring. The board snapshot is
        /// captured at move time, after SelectPoleCommand already revealed the ghost (Ghost→Standard),
        /// so the restored ring comes back as Standard. Per the MoveRecord contract a move whose
        /// selection revealed a ghost must undo back to the pre-selection Ghost state.
        /// </summary>
        private void RestoreGhostReveal(MoveRecord record, PoleState fromPole)
        {
            if (!record.WasGhostRevealedOnFrom || fromPole == null || fromPole.Rings.Count == 0)
                return;

            var top = fromPole.TopRing;
            fromPole.Rings[fromPole.Rings.Count - 1] = new RingData(top.Color, RingType.Ghost);
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
            _model.PendingGhostRevealPoleId = -1;
            NexusLog.Info("UndoCommand", "Execute", $"{lastMove.FromPoleId}->{lastMove.ToPoleId}",
                $"Undo complete. Moves now: {_model.MovesCount.Value}");

            MoveRecordPool.Return(lastMove);
            _signalBus.Fire(new CheckWinSignal());
        }


    }
}