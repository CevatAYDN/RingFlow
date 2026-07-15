using System;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class BoardMediator : Mediator<BoardView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ILoggerService _logger;
        [Inject] private IProgressionService _progression;

        private int _tutorialFromPole = -1;
        private int _tutorialToPole = -1;
        private System.Threading.CancellationTokenSource _tutorialSolveCts;

        [Inject] private Diagnostics.IGameDiagnostics _diag;
        [Inject] private Diagnostics.IViewMediatorTracker _tracker;
        [Inject] private GameFeelConfigSO _feelConfig;
        [Inject] private GameConfigDatabaseSO _dbConfig;

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
        private Action<PoleCompletedSignal> _poleCompletedHandler;
        private Action<UndoRequestedSignal> _undoRequestedHandler;

        private readonly System.Collections.Generic.Stack<(int From, int To)> _recentMoves = new(16);

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
            _undoHandler ??= OnUndoVisualRestore;
            _undoRequestedHandler ??= OnUndoRequested;
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
            Subscribe<UndoRequestedSignal>(_undoRequestedHandler);
            Subscribe<RevealMysterySignal>(_revealMysteryHandler);
            Subscribe<BreakIceSignal>(_breakIceHandler);
            Subscribe<UnlockPoleSignal>(_unlockPoleHandler);
            Subscribe<PaintRingSignal>(_paintRingHandler);
            Subscribe<BombExplodedSignal>(_bombExplodedHandler);
            Subscribe<HintResolvedSignal>(_hintResolvedHandler);
            Subscribe<MoveBlockedSignal>(_moveBlockedHandler);

            _poleCompletedHandler ??= OnPoleCompleted;
            Subscribe<PoleCompletedSignal>(_poleCompletedHandler);

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
            _recentMoves.Push((signal.FromPoleId, signal.ToPoleId));
            if (View != null && _model != null)
                View.AnimateRingMove(signal.FromPoleId, signal.ToPoleId, _model.Poles);
            View?.SetSelectedPole(_model?.SelectedPoleId.Value ?? -1);
            UpdateTutorialStateAsync();
        }

        private void OnUndoRequested(UndoRequestedSignal _)
        {
            // With the corrected execution order (commands before subscriptions),
            // UndoCommand has already reverted the model by the time this runs.
            // No flag needed — OnUndoVisualRestore detects success via count comparison.
        }

        private void OnUndoVisualRestore(UndoSignal _)
        {
            if (View == null || _model == null) return;

            // Detect if undo actually succeeded: _recentMoves tracks visual moves,
            // MovesCount is already decremented by UndoCommand (commands run first).
            // If _recentMoves has more entries than MovesCount, a move was undone.
            if (_recentMoves.Count > 0 && _recentMoves.Count > _model.MovesCount.Value)
            {
                var (fromPoleId, toPoleId) = _recentMoves.Pop();
                // AnimateRingUndo handles BuildBoard + animation internally
                // (same self-contained pattern as AnimateRingMove).
                View.AnimateRingUndo(fromPoleId, toPoleId, _model.Poles);
            }
            else
            {
                // No successful undo or complex state — fallback to full rebuild
                RebuildBoard();
            }

            View?.SetSelectedPole(_model?.SelectedPoleId.Value ?? -1);
            UpdateTutorialStateAsync();
        }

        private void OnPoleCompleted(PoleCompletedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Pole {signal.PoleId} completed! Celebrating.");
            if (View != null && _model != null)
            {
                int poleId = signal.PoleId;
                int ringCount = 0;
                if (poleId >= 0 && poleId < _model.Poles.Count)
                    ringCount = _model.Poles[poleId].Rings.Count;

                int completedCount = _model.CompletedPoles.Count;
                bool isFinalPole = completedCount >= _model.Poles.Count - 1;

                View.CelebratePoleComplete(poleId, ringCount, completedCount, isFinalPole);
            }
        }

        private void OnMoveBlocked(MoveBlockedSignal signal)
        {
            _logger?.Log($"[BoardMediator] Move blocked: {signal.FromPoleId}->{signal.ToPoleId} ({signal.Reason}).");
            View?.FlashPoleError(signal.ToPoleId);
            var f = _feelConfig;
            if (f == null) throw new System.InvalidOperationException("[BoardMediator] GameFeelConfigSO not injected!");
            View?.ShakeCamera(f.ShakeErrorIntensity, f.ShakeErrorDuration);
        }

        private void ApplySelection(int poleId)
        {
            View?.SetSelectedPole(poleId);
            if (poleId >= 0)
            {
                _logger?.Log($"[BoardMediator] Pole {poleId} selected.");
            }
            UpdateTutorialStateAsync();
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
            _logger?.Log($"[BoardMediator] Bomb exploded on pole {signal.PoleId}. Level lost per GDD §36.");
            RebuildBoard();
            var f = _feelConfig;
            if (f == null) throw new System.InvalidOperationException("[BoardMediator] GameFeelConfigSO not injected!");
            View?.ShakeCamera(f.ShakeExplosionIntensity, f.ShakeExplosionDuration);
        }

        protected override void OnUnbind()
        {
            _tutorialSolveCts?.Cancel();
            _tutorialSolveCts?.Dispose();
            _tutorialSolveCts = null;

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
            _recentMoves.Clear();
            RebuildBoard();
            View?.FitCameraToBoard(_model.Poles.Count);
            UpdateTutorialStateAsync();
        }

        private bool _rebuildPending;
        private bool _rebuildRequestedThisFrame;

        private void RebuildBoard()
        {
            if (View == null || _model == null)
            {
                return;
            }

            // Coalesce rebuilds triggered in the same frame (undo/redo/animation chains).
            // AGENTS.md performance budget: 16.6 ms / frame, 0 GC during gameplay.
            if (_rebuildPending)
            {
                _rebuildRequestedThisFrame = true;
                return;
            }

            DoRebuildBoard();
        }

        private void DoRebuildBoard()
        {
            _rebuildPending = true;

            _diag?.Checkpoint("BoardRebuild");
            View.BuildBoard(_model.Poles);
            View.SetSelectedPole(_model.SelectedPoleId.Value);
            var elapsed = _diag != null ? _diag.GetElapsedSinceCheckpoint("BoardRebuild") : System.TimeSpan.Zero;
            _diag?.Log("Performance", $"Board rebuilt. Time: {elapsed.TotalMilliseconds}ms");
            UpdateTutorialStateAsync();

            _rebuildPending = false;
            if (_rebuildRequestedThisFrame)
            {
                _rebuildRequestedThisFrame = false;
                if (View != null && _model != null)
                {
                    DoRebuildBoard();
                }
            }
        }

        private async void UpdateTutorialStateAsync()
        {
            if (View == null || _model == null || _progression == null) return;

            try
            {
                _tutorialSolveCts?.Cancel();
                _tutorialSolveCts?.Dispose();
                _tutorialSolveCts = new System.Threading.CancellationTokenSource();

                int currentLevel = _progression.CurrentLevel.Value;
                if (currentLevel > 3)
                {
                    View.HideTutorialArrow();
                    _tutorialFromPole = -1;
                    _tutorialToPole = -1;
                    return;
                }

                if (_model.IsGameWon.Value || _model.Poles.Count == 0)
                {
                    View.HideTutorialArrow();
                    return;
                }

                var (board, maxCapacity, portalTargets) = BuildBoardStateFromModel(_model);
                var ct = _tutorialSolveCts.Token;

                var result = await LevelSolver.SolveAsync(board, maxCapacity, portalTargets: portalTargets, cancellationToken: ct,
                    bombTickMode: _dbConfig?.LevelGen.BombTickMode ?? BombTickMode.AllBombsPerMove);
                if (ct.IsCancellationRequested) return;

                if (result.IsSolvable && result.MoveCount > 0 && result.Moves != null && result.Moves.Count > 0)
                {
                    var firstMove = result.Moves[0];
                    _tutorialFromPole = firstMove.FromPoleId;
                    _tutorialToPole = firstMove.ToPoleId;

                    int selected = _model.SelectedPoleId.Value;
                    if (selected == -1)
                    {
                        View.ShowTutorialArrow(_tutorialFromPole, "SELECT");
                    }
                    else if (selected == _tutorialFromPole)
                    {
                        View.ShowTutorialArrow(_tutorialToPole, "PLACE");
                    }
                    else
                    {
                        View.ShowTutorialArrow(_tutorialFromPole, "SELECT");
                    }
                }
                else
                {
                    View.HideTutorialArrow();
                    _tutorialFromPole = -1;
                    _tutorialToPole = -1;
                }
            }
            catch (System.Exception)
            {
                View?.HideTutorialArrow();
            }
        }

        private static (BoardState Board, int MaxCapacity, int[] PortalTargets) BuildBoardStateFromModel(GameplayModel m)
        {
            var board = new BoardState { PoleCount = m.Poles.Count };
            int maxCapacity = 0;
            int poleCount = Math.Min(m.Poles.Count, 12);
            for (int p = 0; p < poleCount; p++)
            {
                var pole = m.Poles[p];
                if (pole == null) continue;
                if (pole.RingCapacity > maxCapacity) maxCapacity = pole.RingCapacity;
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
            return (board, maxCapacity, GameplayHelpers.BuildPortalTargets(m.Poles));
        }
    }
}
