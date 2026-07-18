using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class PauseMediator : Mediator<PauseView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progression;
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            if (View == null) return;
            View.Localize(_loc);

            // Show progress
            int level = _progression?.CurrentLevel.Value ?? 1;
            int moves = _model?.MovesCount.Value ?? 0;
            View.SetProgress(level, moves);

            View.ResumeButton?.onClick.AddListener(() => SignalBus.Fire(new ResumeRequestedSignal()));
            View.RestartButton?.onClick.AddListener(() =>
            {
                SignalBus.Fire(new HideScreenSignal(ScreenType.Pause));
                int currentLevel = _progression?.CurrentLevel.Value ?? 1;
                SignalBus.FireAsyncAndForget(new InitLevelSignal(currentLevel));
            });
            View.SettingsButton?.onClick.AddListener(() => SignalBus.Fire(new OpenSettingsSignal()));
            View.QuitButton?.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.ResumeButton?.onClick.RemoveAllListeners();
            View.RestartButton?.onClick.RemoveAllListeners();
            View.SettingsButton?.onClick.RemoveAllListeners();
            View.QuitButton?.onClick.RemoveAllListeners();
        }
    }
}
