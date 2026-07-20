using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Gameplay
{
    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;
        [Inject] private RingValidationStrategyManager _validationManager;

        public void Execute(SelectPoleSignal signal)
        {
            NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                $"Start. Selected={_model.SelectedPoleId.Value}, Won={_model.IsGameWon.Value}, Poles={_model.Poles.Count}");

            if (_model.IsGameWon.Value) return;

            int currentSelected = _model.SelectedPoleId.Value;

            if (currentSelected == -1)
            {
                var pole = _model.Poles.GetPoleById(signal.PoleId);
                if (pole != null && CanPopRing(pole))
                {
                    _model.SelectedPoleId.Value = signal.PoleId;
                }
            }
            else
            {
                if (currentSelected == signal.PoleId)
                {
                    _model.SelectedPoleId.Value = -1;
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

        // ── Validation delegation ─────────────────────────────────────────────

        private bool CanPopRing(PoleState pole)
        {
            if (_validationManager != null)
                return _validationManager.CanPopRing(pole.TopRing, pole.IsLocked, pole.Rings.Count);
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