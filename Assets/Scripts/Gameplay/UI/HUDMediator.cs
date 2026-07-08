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

        protected override void OnBind()
        {
            View.Localize(_loc);
            View.UndoButton.onClick.AddListener(() => SignalBus.Fire(new UndoRequestedSignal()));
            View.RestartButton.onClick.AddListener(() =>
            {
                int level = _progression?.CurrentLevel.Value ?? 1;
                AnalyticsEvents.RestartUse(level);
                SignalBus.Fire(new InitLevelSignal(level));
            });
            View.PauseButton.onClick.AddListener(() => SignalBus.Fire(new PauseRequestedSignal()));
            View.HintButton.onClick.AddListener(() => SignalBus.Fire(new HintRequestedSignal()));

            if (_model != null)
            {
                _model.MovesCount.OnChanged((_, n) => View.UpdateMoves(n));
                View.UpdateMoves(_model.MovesCount.Value);
            }

            if (_progression != null)
            {
                _progression.CurrentLevel.OnChanged((_, n) => View.UpdateLevel(n));
                View.UpdateLevel(_progression.CurrentLevel.Value);
            }

            if (_progress != null)
            {
                _progress.Coins.OnChanged((_, n) => View.UpdateCoins(n));
                View.UpdateCoins(_progress.Coins.Value);
                _progress.Diamonds.OnChanged((_, n) => View.UpdateDiamonds(n));
                View.UpdateDiamonds(_progress.Diamonds.Value);
            }
        }

        protected override void OnUnbind()
        {
            View.UndoButton.onClick.RemoveAllListeners();
            View.RestartButton.onClick.RemoveAllListeners();
            View.PauseButton.onClick.RemoveAllListeners();
            View.HintButton.onClick.RemoveAllListeners();
        }
    }
}
