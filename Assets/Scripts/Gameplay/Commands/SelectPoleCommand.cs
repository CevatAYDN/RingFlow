using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Gameplay
{
    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progression;
        [Inject] private RingValidationStrategyManager _validationManager;

        public void Execute(SelectPoleSignal signal)
        {
            NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                $"Start. Selected={_model.SelectedPoleId.Value}, Won={_model.IsGameWon.Value}, Poles={_model.Poles.Count}");

            if (_model.IsGameWon.Value) return;

            // NOTE: Tutorial guided-input restriction has been removed.
            // Tutorial guidance is now handled by TutorialService + HintCommand (async),
            // which does not block the main thread. (Resolved: IHL-03)

            int currentSelected = _model.SelectedPoleId.Value;

            if (currentSelected == -1)
            {
                var pole = _model.Poles.GetPoleById(signal.PoleId);
                if (pole != null && CanPopRing(pole))
                {
                    _model.SelectedPoleId.Value = signal.PoleId;
                    TryRevealGhost(pole, signal.PoleId, _signalBus);
                }
            }
            else
            {
                if (currentSelected == signal.PoleId)
                {
                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(), "Deselecting same pole.");
                    _model.SelectedPoleId.Value = -1;
                    _model.PendingGhostRevealPoleId = -1;
                }
                else
                {
                    int fromId = currentSelected;
                    int toId = signal.PoleId;
                    var fromPole = _model.Poles.GetPoleById(fromId);
                    var toPole = _model.Poles.GetPoleById(toId);

                    if (fromPole == null || toPole == null || !CanPopRing(fromPole))
                    {
                        NexusLog.Warn("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                            $"Move blocked. fromNull={fromPole == null}, toNull={toPole == null}, canPop={fromPole?.CanPopRing()}");
                        _model.SelectedPoleId.Value = -1;
                        return;
                    }

                    if (!CanAddRing(toPole, fromPole.TopRing))
                    {
                        if (CanPopRing(toPole))
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

                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                        $"Moving from {fromId} to {toId}.");
                    _model.SelectedPoleId.Value = -1;
                    _signalBus.Fire(new MoveRingSignal(fromId, toId));
                }
            }
        }

        /// <summary>
        /// Ghost reveal: changes the top Ghost ring to Standard on selection.
        /// The move that follows will record this in MoveRecord.WasGhostRevealedOnFrom
        /// so UndoCommand can restore it. Only fires GhostRevealedSignal — no game-state
        /// mutation here beyond the ring type change (which is deliberate game design).
        /// Also stores the revealed pole id so MoveRingCommand can capture it
        /// for undo only when the next move starts from the same pole.
        /// </summary>
        private void TryRevealGhost(PoleState pole, int poleId, ISignalBus signalBus)
        {
            if (pole.TopRing.Type != RingType.Ghost)
            {
                _model.PendingGhostRevealPoleId = -1;
                return;
            }
            var ghostCopy = pole.Rings[^1];
            ghostCopy.Type = RingType.Standard;
            pole.Rings[^1] = ghostCopy;
            _model.PendingGhostRevealPoleId = poleId;
            signalBus?.Fire(new GhostRevealedSignal(poleId, ghostCopy));
        }

        // ── Validation delegation ─────────────────────────────────────────────

        private bool CanPopRing(PoleState pole)
        {
            if (_validationManager != null)
                return _validationManager.CanPopRing(pole.TopRing, pole.IsLocked);
            return pole.CanPopRing();
        }

        private bool CanAddRing(PoleState pole, RingData ring)
        {
            if (_validationManager != null)
            {
                if (ring.Type == RingType.Rainbow || ring.Type == RingType.Paint)
                    return _validationManager.CanAddUniversalRing(ring, pole.TopRing, pole.IsFull, pole.IsLocked);
                if (!pole.IsEmpty && (pole.TopRing.Type == RingType.Rainbow || pole.TopRing.Type == RingType.Paint))
                    return _validationManager.CanAddUniversalRing(ring, pole.TopRing, pole.IsFull, pole.IsLocked);
                return _validationManager.CanAddRing(ring, pole.TopRing, pole.IsFull, pole.IsLocked);
            }
            return pole.CanAddRing(ring);
        }

    }
}