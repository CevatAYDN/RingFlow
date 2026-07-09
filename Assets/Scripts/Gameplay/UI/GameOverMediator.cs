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
                    SignalBus.Fire(new LevelSelectedSignal(currentLevel));
                });
            }

            if (View.QuitButton != null)
            {
                View.QuitButton.onClick.AddListener(() =>
                {
                    SignalBus.Fire(new QuitToMenuRequestedSignal());
                });
            }

            // Play Game Over Sound procedurally
            if (_audio != null)
            {
                var failClip = ProceduralAudio.GetOrCreateExplosionClip();
                _audio.PlaySfx(failClip, 1.0f);
            }
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.RestartButton?.onClick.RemoveAllListeners();
            View.QuitButton?.onClick.RemoveAllListeners();
        }
    }
}
