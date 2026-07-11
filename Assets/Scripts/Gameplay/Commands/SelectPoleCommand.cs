using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progression;

        public void Execute(SelectPoleSignal signal)
        {
            NexusLog.Info("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                $"Start. Selected={_model.SelectedPoleId.Value}, Won={_model.IsGameWon.Value}, Poles={_model.Poles.Count}");

            if (_model.IsGameWon.Value) return;

            // --- Tutorial Guided Move Input Restriction ---
            if (_progression != null && _progression.CurrentLevel.Value <= 3 && _model.Poles.Count > 0)
            {
                var (board, maxCapacity) = BuildBoardStateFromModel(_model);
                var solveResult = LevelSolver.Solve(board, maxCapacity);
                if (solveResult.IsSolvable && solveResult.MoveCount > 0 && solveResult.Moves != null && solveResult.Moves.Count > 0)
                {
                    var recommendedMove = solveResult.Moves[0];
                    int reqFrom = recommendedMove.FromPoleId;
                    int reqTo = recommendedMove.ToPoleId;

                    int currentSel = _model.SelectedPoleId.Value;
                    if (currentSel == -1)
                    {
                        if (signal.PoleId != reqFrom)
                        {
                            NexusLog.Warn("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                                $"Tutorial input blocked: must select pole {reqFrom}.");
                            _signalBus?.Fire(new MoveBlockedSignal(signal.PoleId, reqFrom, "tutorial_select"));
                            return;
                        }
                    }
                    else
                    {
                        if (signal.PoleId != currentSel && signal.PoleId != reqTo)
                        {
                            NexusLog.Warn("SelectPoleCommand", "Execute", signal.PoleId.ToString(),
                                $"Tutorial input blocked: must move to pole {reqTo}.");
                            _signalBus?.Fire(new MoveBlockedSignal(currentSel, signal.PoleId, "tutorial_place"));
                            return;
                        }
                    }
                }
            }

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

        private static (BoardState Board, int MaxCapacity) BuildBoardStateFromModel(GameplayModel m)
        {
            var board = new BoardState { PoleCount = m.Poles.Count };
            int maxCapacity = 0;
            for (int p = 0; p < m.Poles.Count && p < 12; p++)
            {
                var pole = m.Poles[p];
                if (pole == null) continue;
                if (pole.MaxCapacity > maxCapacity) maxCapacity = pole.MaxCapacity;
                board.SetPoleLocked(p, pole.IsLocked);
                int count = pole.Rings.Count;
                board.SetRingCount(p, count);
                for (int r = 0; r < count; r++)
                {
                    var ringData = pole.Rings[r];
                    board.SetRingColor(p, r, ringData.Color);
                    board.SetRingType(p, r, ringData.Type);
                    board.SetRingAdditional(p, r, ringData.AdditionalData);
                }
                if (count > 0)
                {
                    var top = pole.TopRing;
                    board.SetTopRingFrozen(p, top.Type == RingType.Frozen);
                }
            }
            return (board, maxCapacity);
        }
    }
}