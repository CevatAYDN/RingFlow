using System;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class HUDMediator : Mediator<HUDView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progression;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private ILocalizationService _loc;
        [Inject] private IAnalyticsService _analytics;

        private Action<int, int> _movesHandler;
        private Action<int, int> _levelHandler;
        private Action<int, int> _coinsHandler;
        private Action<int, int> _diamondsHandler;
        private Action<bool, bool> _isGameWonHandler;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("HUDMediator", nameof(OnBind), "", "HUDView not bound.");
                return;
            }

            View.Localize(_loc);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("HUDMediator", nameof(OnBind), "",
                $"HUD binding. model={_model != null}, progression={_progression != null}, progress={_progress != null}.");
#endif

            if (View.UndoButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "UndoButton missing.");
            else View.UndoButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("HUDMediator", "UndoButton", "", "Undo button pressed.");
#endif
                SignalBus.Fire(new UndoRequestedSignal());
            });

            if (View.RestartButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "RestartButton missing.");
            else View.RestartButton.onClick.AddListener(() =>
            {
                int level = _progression?.CurrentLevel.Value ?? 1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("HUDMediator", "RestartButton", level.ToString(), $"Restart pressed for level {level}.");
#endif
                if (_analytics != null)
                {
                    _analytics.RestartUse(level);
                }
                SignalBus.FireAsyncAndForget(new InitLevelSignal(level));
            });

            if (View.PauseButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "PauseButton missing.");
            else View.PauseButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("HUDMediator", "PauseButton", "", "Pause button pressed.");
#endif
                SignalBus.Fire(new PauseRequestedSignal());
            });

            if (View.HintButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "HintButton missing.");
            else View.HintButton.onClick.AddListener(() =>
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("HUDMediator", "HintButton", "", "Hint button pressed.");
#endif
                SignalBus.FireAsyncAndForget(new HintRequestedSignal());
            });

            if (_model != null)
            {
                _movesHandler = (_, n) =>
                {
                    View?.UpdateMoves(n, _loc);
                    if (View?.UndoButton != null)
                    {
                        View.UndoButton.interactable = n > 0 && !_model.IsGameWon.Value;
                    }
                };
                _model.MovesCount.OnChanged(_movesHandler);
                View.UpdateMoves(_model.MovesCount.Value, _loc);

                _isGameWonHandler = (_, won) =>
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("HUDMediator", "IsGameWonChanged", won.ToString(),
                        $"IsGameWon changed to {won}. Setting button interactable states.");
#endif
                    if (View?.UndoButton != null) View.UndoButton.interactable = !won && _model.MovesCount.Value > 0;
                    if (View?.RestartButton != null) View.RestartButton.interactable = !won;
                    if (View?.HintButton != null) View.HintButton.interactable = !won;
                };
                _model.IsGameWon.OnChanged(_isGameWonHandler);

                if (View.UndoButton != null) View.UndoButton.interactable = !_model.IsGameWon.Value && _model.MovesCount.Value > 0;
                if (View.RestartButton != null) View.RestartButton.interactable = !_model.IsGameWon.Value;
                if (View.HintButton != null) View.HintButton.interactable = !_model.IsGameWon.Value;
            }
            else
            {
                NexusLog.Warn("HUDMediator", nameof(OnBind), "", "GameplayModel not bound; HUD will not show moves.");
            }

            if (_progression != null)
            {
                _levelHandler = (_, n) =>
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("HUDMediator", "LevelChanged", n.ToString(), $"Level changed to {n}. Updating HUD.");
#endif
                    View?.UpdateLevel(n, _loc);
                };
                _progression.CurrentLevel.OnChanged(_levelHandler);
                View.UpdateLevel(_progression.CurrentLevel.Value, _loc);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("HUDMediator", nameof(OnBind), "", "IProgressionService not bound — HUD level display disabled.");
            }
#endif

            if (_progress != null)
            {
                _coinsHandler = (_, n) => View?.UpdateCoins(n);
                _diamondsHandler = (_, n) => View?.UpdateDiamonds(n);
                _progress.Coins.OnChanged(_coinsHandler);
                View.UpdateCoins(_progress.Coins.Value);
                _progress.Diamonds.OnChanged(_diamondsHandler);
                View.UpdateDiamonds(_progress.Diamonds.Value);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("HUDMediator", nameof(OnBind), "", "PlayerProgressModel not bound — coins/diamonds display disabled.");
            }
#endif
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.UndoButton?.onClick.RemoveAllListeners();
            View.RestartButton?.onClick.RemoveAllListeners();
            View.PauseButton?.onClick.RemoveAllListeners();
            View.HintButton?.onClick.RemoveAllListeners();

            if (_model != null)
            {
                if (_movesHandler != null) _model.MovesCount.RemoveOnChanged(_movesHandler);
                if (_isGameWonHandler != null) _model.IsGameWon.RemoveOnChanged(_isGameWonHandler);
            }
            if (_progression != null && _levelHandler != null)
                _progression.CurrentLevel.RemoveOnChanged(_levelHandler);
            if (_progress != null)
            {
                if (_coinsHandler != null) _progress.Coins.RemoveOnChanged(_coinsHandler);
                if (_diamondsHandler != null) _progress.Diamonds.RemoveOnChanged(_diamondsHandler);
            }
            _movesHandler = null;
            _isGameWonHandler = null;
            _levelHandler = null;
            _coinsHandler = null;
            _diamondsHandler = null;
        }
    }
}
