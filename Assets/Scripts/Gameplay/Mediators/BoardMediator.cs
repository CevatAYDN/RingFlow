using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class BoardMediator : Mediator<BoardView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ILoggerService _logger;

        [Inject] private Diagnostics.IGameDiagnostics _diag;
        [Inject] private Diagnostics.IViewMediatorTracker _tracker;

        protected override void OnBind()
        {
            if (_model == null || View == null) return;

            _tracker?.TrackViewBound(typeof(BoardView), typeof(BoardMediator));
            _diag?.Log("Mediator", $"BoardMediator bound. View: {View.GetType().Name}");
            _diag?.Checkpoint("BoardMediator.OnBind");

            _logger?.Log("[BoardMediator] Binding BoardView to signals...");

            Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            Subscribe<MoveRingSignal>(_ => RebuildBoard());
            Subscribe<UndoSignal>(_ => RebuildBoard());
            Subscribe<RevealMysterySignal>(_ => RebuildBoard());
            Subscribe<BreakIceSignal>(_ => RebuildBoard());
            Subscribe<UnlockPoleSignal>(_ => RebuildBoard());
            Subscribe<PaintRingSignal>(_ => RebuildBoard());

            if (_model.Poles.Count > 0)
            {
                RebuildBoard();
            }
        }

        protected override void OnUnbind()
        {
            _tracker?.TrackViewUnbound(typeof(BoardView));
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Level {signal.LevelIndex} loaded. Rebuilding visual board for {_model.Poles.Count} poles.");
            RebuildBoard();
        }

        private void RebuildBoard()
        {
            if (View != null && _model != null)
            {
                _diag?.Checkpoint("BoardRebuild");
                View.BuildBoard(_model.Poles);
                var elapsed = _diag != null ? _diag.GetElapsedSinceCheckpoint("BoardRebuild") : System.TimeSpan.Zero;
                _diag?.Log("Performance", $"Board rebuilt. Time: {elapsed.TotalMilliseconds}ms");
            }
        }
    }
}