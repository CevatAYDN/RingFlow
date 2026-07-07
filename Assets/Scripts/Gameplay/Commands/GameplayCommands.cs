using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class InitLevelCommand : ICommand<InitLevelSignal>
    {
        [Inject] private GameplayModel _model;

        public void Execute(InitLevelSignal signal)
        {
            _model.Reset();

            // Create Pole 0 with Red/Blue/Red/Blue
            var p0 = new PoleState { Id = 0 };
            p0.AddRing(RingColor.Red);
            p0.AddRing(RingColor.Blue);
            p0.AddRing(RingColor.Red);
            p0.AddRing(RingColor.Blue);

            // Create Pole 1 with Blue/Red/Blue/Red
            var p1 = new PoleState { Id = 1 };
            p1.AddRing(RingColor.Blue);
            p1.AddRing(RingColor.Red);
            p1.AddRing(RingColor.Blue);
            p1.AddRing(RingColor.Red);

            // Create Pole 2 (Empty)
            var p2 = new PoleState { Id = 2 };

            _model.Poles.Add(p0);
            _model.Poles.Add(p1);
            _model.Poles.Add(p2);
        }
    }

    public class SelectPoleCommand : ICommand<SelectPoleSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(SelectPoleSignal signal)
        {
            if (_model.IsGameWon.Value) return;

            int currentSelected = _model.SelectedPoleId.Value;

            if (currentSelected == -1)
            {
                // No pole selected, select this one if it has rings
                var pole = _model.Poles.Find(p => p.Id == signal.PoleId);
                if (pole != null && !pole.IsEmpty)
                {
                    _model.SelectedPoleId.Value = signal.PoleId;
                }
            }
            else
            {
                if (currentSelected == signal.PoleId)
                {
                    // Clicked the same pole, deselect
                    _model.SelectedPoleId.Value = -1;
                }
                else
                {
                    // Clicked a different pole, try to move
                    int fromId = currentSelected;
                    int toId = signal.PoleId;
                    
                    // Reset selection first
                    _model.SelectedPoleId.Value = -1;

                    // Fire move signal
                    _signalBus.Fire(new MoveRingSignal(fromId, toId));
                }
            }
        }
    }

    public class MoveRingCommand : ICommand<MoveRingSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;

        public void Execute(MoveRingSignal signal)
        {
            var fromPole = _model.Poles.Find(p => p.Id == signal.FromPoleId);
            var toPole = _model.Poles.Find(p => p.Id == signal.ToPoleId);

            if (fromPole == null || toPole == null || fromPole.IsEmpty) return;

            var color = fromPole.TopRing;
            if (toPole.CanAddRing(color))
            {
                // Execute move
                fromPole.PopRing();
                toPole.AddRing(color);

                // Record move for Undo
                _model.MoveHistory.Push(new MoveRecord(signal.FromPoleId, signal.ToPoleId, color));

                // Update moves count
                _model.MovesCount.Value++;

                // Check win condition
                _signalBus.Fire(new CheckWinSignal());
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
                
                var fromPole = _model.Poles.Find(p => p.Id == lastMove.FromPoleId);
                var toPole = _model.Poles.Find(p => p.Id == lastMove.ToPoleId);

                if (fromPole != null && toPole != null && toPole.TopRing == lastMove.Color)
                {
                    // Revert move
                    toPole.PopRing();
                    fromPole.AddRing(lastMove.Color);

                    // Reduce moves count
                    if (_model.MovesCount.Value > 0)
                    {
                        _model.MovesCount.Value--;
                    }

                    // Reset selection
                    _model.SelectedPoleId.Value = -1;

                    // Re-check win condition
                    _signalBus.Fire(new CheckWinSignal());
                }
            }
        }
    }

    public class CheckWinCommand : ICommand<CheckWinSignal>
    {
        [Inject] private GameplayModel _model;

        public void Execute(CheckWinSignal signal)
        {
            bool won = true;
            int totalRingsCount = 0;

            foreach (var pole in _model.Poles)
            {
                if (pole.IsEmpty) continue;

                totalRingsCount += pole.Rings.Count;

                // A non-empty pole must be full to be complete (capacity = 4)
                if (!pole.IsFull)
                {
                    won = false;
                    break;
                }

                // All rings in the pole must be of the same color
                var firstColor = pole.Rings[0];
                for (int i = 1; i < pole.Rings.Count; i++)
                {
                    if (pole.Rings[i] != firstColor)
                    {
                        won = false;
                        break;
                    }
                }

                if (!won) break;
            }

            if (totalRingsCount == 0) won = false;

            _model.IsGameWon.Value = won;
        }
    }
}
