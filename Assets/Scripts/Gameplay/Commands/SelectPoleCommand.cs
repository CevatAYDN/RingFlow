using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(SelectPoleSignal signal)
        {
            NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                $"Start. Selected={_model.SelectedPoleId.Value}, Won={_model.IsGameWon.Value}, Poles={_model.Poles.Count}");

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

                    NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                        $"Moving from {fromId} to {toId}.");
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
}