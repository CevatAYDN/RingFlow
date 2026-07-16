using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class GameOverMediator : Mediator<GameOverView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private ILocalizationService _loc;
        [Inject] private IAudioService _audio;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("GameOverMediator", nameof(OnBind), "", "GameOverView not bound.");
                return;
            }

            View.Localize(_loc);

            if (View.RestartButton != null)
            {
                View.RestartButton.onClick.AddListener(() =>
                {
                    int currentLevel = _progression?.CurrentLevel.Value ?? 1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("GameOverMediator", "RestartButton", currentLevel.ToString(),
                        $"Restart requested. Firing LevelSelectedSignal({currentLevel}).");
#endif
                    SignalBus.Fire(new LevelSelectedSignal(currentLevel));
                });
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("GameOverMediator", nameof(OnBind), "", "RestartButton missing — player cannot restart from game over.");
#endif
            }

            if (View.QuitButton != null)
            {
                View.QuitButton.onClick.AddListener(() =>
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("GameOverMediator", "QuitButton", "", "Quit to menu requested.");
#endif
                    SignalBus.Fire(new QuitToMenuRequestedSignal());
                });
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("GameOverMediator", nameof(OnBind), "", "QuitButton missing — player cannot quit to menu from game over.");
#endif
            }

            // Play Game Over Sound procedurally
            if (_audio != null)
            {
                var failClip = ProceduralAudio.GetOrCreateExplosionClip();
                _audio.PlaySfx(failClip, 1.0f);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("GameOverMediator", nameof(OnBind), "", "IAudioService not bound — game over sound will not play.");
            }
#endif
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.RestartButton?.onClick.RemoveAllListeners();
            View.QuitButton?.onClick.RemoveAllListeners();
        }
    }
}
