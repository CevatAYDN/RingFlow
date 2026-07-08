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
            Subscribe<MoveRingSignal>(_ => View.BuildBoard(_model.Poles));
            Subscribe<UndoSignal>(_ => View.BuildBoard(_model.Poles));
            Subscribe<RevealMysterySignal>(_ => View.BuildBoard(_model.Poles));
            Subscribe<BreakIceSignal>(_ => View.BuildBoard(_model.Poles));
            Subscribe<UnlockPoleSignal>(_ => View.BuildBoard(_model.Poles));
            Subscribe<PaintRingSignal>(_ => View.BuildBoard(_model.Poles));

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
    }
}