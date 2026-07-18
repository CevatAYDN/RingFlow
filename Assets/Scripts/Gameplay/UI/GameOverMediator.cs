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
                NexusLog.Error("GameOverMediator", "OnBind", "", "GameOverView not bound.");
                return;
            }
            View.Localize(_loc);

            int currentLevel = _progression?.CurrentLevel.Value ?? 1;
            View.SetLevel(currentLevel);

            View.RestartButton?.onClick.AddListener(() =>
            {
                SignalBus.Fire(new LevelSelectedSignal(currentLevel));
            });

            View.QuitButton?.onClick.AddListener(() =>
            {
                SignalBus.Fire(new QuitToMenuRequestedSignal());
            });

            if (_audio != null)
            {
                var failClip = ProceduralAudio.GetOrCreateExplosionClip();
                _audio.PlaySfx(failClip, 0.8f);
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
