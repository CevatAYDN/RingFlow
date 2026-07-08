using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class BoardMediator : Mediator<BoardView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ILoggerService _logger;

        protected override void OnBind()
        {
            if (_model == null || View == null) return;

            _logger?.Log("[BoardMediator] Binding BoardView to signals...");

            Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            Subscribe<MoveRingSignal>(OnRingMoved);
            Subscribe<UndoSignal>(OnUndo);
            Subscribe<RevealMysterySignal>(OnRevealMystery);
            Subscribe<BreakIceSignal>(OnBreakIce);
            Subscribe<UnlockPoleSignal>(OnUnlockPole);
            Subscribe<PaintRingSignal>(OnPaintRing);

            if (_model.Poles.Count > 0)
            {
                View.BuildBoard(_model.Poles);
            }
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Level {signal.LevelIndex} loaded. Rebuilding visual board for {_model.Poles.Count} poles.");
            View.BuildBoard(_model.Poles);
        }

        private void OnRingMoved(MoveRingSignal signal)
        {
            View.BuildBoard(_model.Poles);
        }

        private void OnUndo(UndoSignal signal)
        {
            View.BuildBoard(_model.Poles);
        }

        private void OnRevealMystery(RevealMysterySignal signal)
        {
            View.BuildBoard(_model.Poles);
        }

        private void OnBreakIce(BreakIceSignal signal)
        {
            View.BuildBoard(_model.Poles);
        }

        private void OnUnlockPole(UnlockPoleSignal signal)
        {
            View.BuildBoard(_model.Poles);
        }

        private void OnPaintRing(PaintRingSignal signal)
        {
            View.BuildBoard(_model.Poles);
        }
    }
}
