using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class BoardMediator : Mediator<BoardView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ISignalBus _signalBus;
        [Inject] private ILoggerService _logger;

        private readonly System.Collections.Generic.List<System.IDisposable> _subscriptions = new();

        protected override void OnBind()
        {
            _logger?.Log("[BoardMediator] Binding BoardView to signals...");

            // Listen to level updates
            _subscriptions.Add(_signalBus.Subscribe<InitLevelSignal>(OnLevelInitialized));
            _subscriptions.Add(_signalBus.Subscribe<MoveRingSignal>(OnRingMoved));
            _subscriptions.Add(_signalBus.Subscribe<UndoSignal>(OnUndo));
            _subscriptions.Add(_signalBus.Subscribe<RevealMysterySignal>(OnRevealMystery));
            _subscriptions.Add(_signalBus.Subscribe<BreakIceSignal>(OnBreakIce));
            _subscriptions.Add(_signalBus.Subscribe<UnlockPoleSignal>(OnUnlockPole));
            _subscriptions.Add(_signalBus.Subscribe<PaintRingSignal>(OnPaintRing));

            // Rebuild initial state if already loaded
            if (_model.Poles.Count > 0)
            {
                View.BuildBoard(_model.Poles);
            }
        }

        private void OnLevelInitialized(InitLevelSignal signal)
        {
            _logger?.Log($"[BoardMediator] Level initialized. Rebuilding visual board for {_model.Poles.Count} poles.");
            View.BuildBoard(_model.Poles);
        }

        private void OnRingMoved(MoveRingSignal signal)
        {
            _logger?.Log($"[BoardMediator] Ring moved from {signal.FromPoleId} to {signal.ToPoleId}. Updating visual board.");
            View.BuildBoard(_model.Poles);
        }

        private void OnUndo(UndoSignal signal)
        {
            _logger?.Log("[BoardMediator] Undo requested. Updating visual board.");
            View.BuildBoard(_model.Poles);
        }

        private void OnRevealMystery(RevealMysterySignal signal)
        {
            _logger?.Log("[BoardMediator] Mystery ring revealed. Updating visual board.");
            View.BuildBoard(_model.Poles);
        }

        private void OnBreakIce(BreakIceSignal signal)
        {
            _logger?.Log("[BoardMediator] Ice broken. Updating visual board.");
            View.BuildBoard(_model.Poles);
        }

        private void OnUnlockPole(UnlockPoleSignal signal)
        {
            _logger?.Log("[BoardMediator] Pole unlocked. Updating visual board.");
            View.BuildBoard(_model.Poles);
        }

        private void OnPaintRing(PaintRingSignal signal)
        {
            _logger?.Log("[BoardMediator] Ring painted. Updating visual board.");
            View.BuildBoard(_model.Poles);
        }

        protected override void OnUnbind()
        {
            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}
