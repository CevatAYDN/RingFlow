using System;
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

        private Action<int, int> _selectedPoleHandler;
        private Action<int, int> _movesCountHandler;

        // Cache delegate instances to ensure zero garbage collection allocation when binding/unbinding
        private Action<LevelLoadedSignal> _levelLoadedHandler;
        private Action<RingMovedSignal> _ringMovedHandler;
        private Action<UndoSignal> _undoHandler;
        private Action<RevealMysterySignal> _revealMysteryHandler;
        private Action<BreakIceSignal> _breakIceHandler;
        private Action<UnlockPoleSignal> _unlockPoleHandler;
        private Action<PaintRingSignal> _paintRingHandler;
        private Action<BombExplodedSignal> _bombExplodedHandler;
        private Action<HintResolvedSignal> _hintResolvedHandler;
        private Action<MoveBlockedSignal> _moveBlockedHandler;

        protected override void OnBind()
        {
            if (_model == null || View == null)
            {
                NexusLog.Error("BoardMediator", nameof(OnBind), "",
                    "GameplayModel or View not bound; BoardMediator disabled.");
                return;
            }

            _tracker?.TrackViewBound(typeof(BoardView), typeof(BoardMediator));
            _diag?.Log("Mediator", $"BoardMediator bound. View: {View.GetType().Name}");
            _diag?.Checkpoint("BoardMediator.OnBind");

            _logger?.Log("[BoardMediator] Binding BoardView to signals...");

            // Initialize and cache delegate handlers to avoid closure allocation garbage
            _levelLoadedHandler ??= OnLevelLoaded;
            _ringMovedHandler ??= OnRingMoved;
            _undoHandler ??= _ => RebuildBoard();
            _revealMysteryHandler ??= _ => RebuildBoard();
            _breakIceHandler ??= _ => RebuildBoard();
            _unlockPoleHandler ??= _ => RebuildBoard();
            _paintRingHandler ??= _ => RebuildBoard();
            _bombExplodedHandler ??= OnBombExploded;
            _hintResolvedHandler ??= OnHintResolved;
            _moveBlockedHandler ??= OnMoveBlocked;

            // Note: The base Nexus Mediator automatically disposes and unsubscribes all 
            // subscriptions added via Subscribe<T>() inside the base Unbind() method.
            Subscribe<LevelLoadedSignal>(_levelLoadedHandler);
            Subscribe<RingMovedSignal>(_ringMovedHandler);
            Subscribe<UndoSignal>(_undoHandler);
            Subscribe<RevealMysterySignal>(_revealMysteryHandler);
            Subscribe<BreakIceSignal>(_breakIceHandler);
            Subscribe<UnlockPoleSignal>(_unlockPoleHandler);
            Subscribe<PaintRingSignal>(_paintRingHandler);
            Subscribe<BombExplodedSignal>(_bombExplodedHandler);
            Subscribe<HintResolvedSignal>(_hintResolvedHandler);
            Subscribe<MoveBlockedSignal>(_moveBlockedHandler);

            _selectedPoleHandler = (_, id) => ApplySelection(id);
            _movesCountHandler = (_, _) => _logger?.Log($"[BoardMediator] Moves now {_model.MovesCount.Value}");
            _model.SelectedPoleId.OnChanged(_selectedPoleHandler);
            _model.MovesCount.OnChanged(_movesCountHandler);

            if (_model.Poles.Count > 0)
            {
                RebuildBoard();
            }
        }

        private void OnRingMoved(RingMovedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Move {signal.FromPoleId} -> {signal.ToPoleId} completed. Animating.");
            if (View != null && _model != null)
                View.AnimateRingMove(signal.FromPoleId, signal.ToPoleId, _model.Poles);
            View?.SetSelectedPole(_model?.SelectedPoleId.Value ?? -1);
        }

        private void OnMoveBlocked(MoveBlockedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Move blocked: {signal.FromPoleId}->{signal.ToPoleId} ({signal.Reason}).");
            View?.FlashPoleError(signal.ToPoleId);
        }

        private void ApplySelection(int poleId)
        {
            View?.SetSelectedPole(poleId);
            if (poleId >= 0)
            {
                _logger?.Log($"[BoardMediator] Pole {poleId} selected.");
            }
        }

        private void OnHintResolved(HintResolvedSignal signal)
        {
            if (!signal.HasHint)
            {
                _logger?.Log("[BoardMediator] Hint resolver returned empty (solver found nothing).");
            }
            else
            {
                _logger?.Log($"[BoardMediator] Hint: {signal.FromPoleId} -> {signal.ToPoleId}.");
            }
        }

        private void OnBombExploded(BombExplodedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Bomb exploded on pole {signal.PoleId}.");
            RebuildBoard();
        }

        protected override void OnUnbind()
        {
            if (_model != null)
            {
                if (_selectedPoleHandler != null) _model.SelectedPoleId.RemoveOnChanged(_selectedPoleHandler);
                if (_movesCountHandler != null) _model.MovesCount.RemoveOnChanged(_movesCountHandler);
            }
            _selectedPoleHandler = null;
            _movesCountHandler = null;
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
                View.SetSelectedPole(_model.SelectedPoleId.Value);
                var elapsed = _diag != null ? _diag.GetElapsedSinceCheckpoint("BoardRebuild") : System.TimeSpan.Zero;
                _diag?.Log("Performance", $"Board rebuilt. Time: {elapsed.TotalMilliseconds}ms");
            }
        }
    }
}