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
        [Inject] private Services.IGameTimeService _time;

        private Action<int, int> _movesHandler;
        private Action<int, int> _levelHandler;
        private Action<int, int> _coinsHandler;
        private Action<int, int> _diamondsHandler;
        private Action<bool, bool> _isGameWonHandler;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("HUDMediator", "OnBind", "", "HUDView not bound.");
                return;
            }
            View.Localize(_loc);

            BindButtons();
            BindModel();
            BindProgression();
            BindCurrency();
        }

        private void BindButtons()
        {
            View.UndoButton?.onClick.AddListener(() => SignalBus.Fire(new UndoRequestedSignal()));
            View.RestartButton?.onClick.AddListener(OnRestart);
            View.PauseButton?.onClick.AddListener(() => SignalBus.Fire(new PauseRequestedSignal()));
            View.HintButton?.onClick.AddListener(() => SignalBus.FireAsyncAndForget(new HintRequestedSignal()));
        }

        private void OnRestart()
        {
            int level = _progression?.CurrentLevel.Value ?? 1;
            _analytics?.RestartUse(level);
            SignalBus.FireAsyncAndForget(new InitLevelSignal(level));
        }

        private void BindModel()
        {
            if (_model == null) return;

            _movesHandler = (_, n) =>
            {
                View?.UpdateMoves(n, _loc);
                if (View?.UndoButton != null)
                    View.UndoButton.interactable = n > 0 && !_model.IsGameWon.Value;
            };
            _model.MovesCount.OnChanged(_movesHandler);
            View.UpdateMoves(_model.MovesCount.Value, _loc);

            _isGameWonHandler = (_, won) =>
            {
                if (View?.UndoButton != null) View.UndoButton.interactable = !won && _model.MovesCount.Value > 0;
                if (View?.RestartButton != null) View.RestartButton.interactable = !won;
                if (View?.HintButton != null) View.HintButton.interactable = !won;
            };
            _model.IsGameWon.OnChanged(_isGameWonHandler);
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool won = _model != null && _model.IsGameWon.Value;
            if (View?.UndoButton != null)
                View.UndoButton.interactable = !won && (_model?.MovesCount.Value ?? 0) > 0;
            if (View?.RestartButton != null) View.RestartButton.interactable = !won;
            if (View?.HintButton != null) View.HintButton.interactable = !won;
        }

        private void BindProgression()
        {
            if (_progression == null) return;
            _levelHandler = (_, n) => View?.UpdateLevel(n, _loc);
            _progression.CurrentLevel.OnChanged(_levelHandler);
            View.UpdateLevel(_progression.CurrentLevel.Value, _loc);
        }

        private void BindCurrency()
        {
            if (_progress == null) return;
            _coinsHandler = (_, n) => View?.UpdateCoins(n);
            _diamondsHandler = (_, n) => View?.UpdateDiamonds(n);
            _progress.Coins.OnChanged(_coinsHandler);
            View.UpdateCoins(_progress.Coins.Value);
            _progress.Diamonds.OnChanged(_diamondsHandler);
            View.UpdateDiamonds(_progress.Diamonds.Value);
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
