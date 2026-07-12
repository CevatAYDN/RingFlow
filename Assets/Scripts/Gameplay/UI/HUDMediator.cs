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

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("HUDMediator", nameof(OnBind), "", "HUDView not bound.");
                return;
            }

            View.Localize(_loc);

            if (View.UndoButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "UndoButton missing.");
            else View.UndoButton.onClick.AddListener(() => SignalBus.Fire(new UndoRequestedSignal()));

            if (View.RestartButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "RestartButton missing.");
            else View.RestartButton.onClick.AddListener(() =>
            {
                int level = _progression?.CurrentLevel.Value ?? 1;
                if (_analytics != null)
                {
                    _analytics.RestartUse(level);
                }
                SignalBus.Fire(new InitLevelSignal(level));
            });

            if (View.PauseButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "PauseButton missing.");
            else View.PauseButton.onClick.AddListener(() => SignalBus.Fire(new PauseRequestedSignal()));

            if (View.HintButton == null) NexusLog.Warn("HUDMediator", nameof(OnBind), "", "HintButton missing.");
            else View.HintButton.onClick.AddListener(() => SignalBus.FireAsyncAndForget(new HintRequestedSignal()));

            if (_model != null)
            {
                _movesHandler = (_, n) => View?.UpdateMoves(n, _loc);
                _model.MovesCount.OnChanged(_movesHandler);
                View.UpdateMoves(_model.MovesCount.Value, _loc);
            }
            else
            {
                NexusLog.Warn("HUDMediator", nameof(OnBind), "", "GameplayModel not bound; HUD will not show moves.");
            }

            if (_progression != null)
            {
                _levelHandler = (_, n) => View?.UpdateLevel(n, _loc);
                _progression.CurrentLevel.OnChanged(_levelHandler);
                View.UpdateLevel(_progression.CurrentLevel.Value, _loc);
            }

            if (_progress != null)
            {
                _coinsHandler = (_, n) => View?.UpdateCoins(n);
                _diamondsHandler = (_, n) => View?.UpdateDiamonds(n);
                _progress.Coins.OnChanged(_coinsHandler);
                View.UpdateCoins(_progress.Coins.Value);
                _progress.Diamonds.OnChanged(_diamondsHandler);
                View.UpdateDiamonds(_progress.Diamonds.Value);
            }
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.UndoButton?.onClick.RemoveAllListeners();
            View.RestartButton?.onClick.RemoveAllListeners();
            View.PauseButton?.onClick.RemoveAllListeners();
            View.HintButton?.onClick.RemoveAllListeners();

            if (_model != null && _movesHandler != null)
                _model.MovesCount.RemoveOnChanged(_movesHandler);
            if (_progression != null && _levelHandler != null)
                _progression.CurrentLevel.RemoveOnChanged(_levelHandler);
            if (_progress != null)
            {
                if (_coinsHandler != null) _progress.Coins.RemoveOnChanged(_coinsHandler);
                if (_diamondsHandler != null) _progress.Diamonds.RemoveOnChanged(_diamondsHandler);
            }
            _movesHandler = null;
            _levelHandler = null;
            _coinsHandler = null;
            _diamondsHandler = null;
        }
    }
}
