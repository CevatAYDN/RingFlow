using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class GameOverMediator : Mediator<GameOverView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private ILocalizationService _loc;
        [Inject] private IAudioService _audio;

        private ISignalSubscription _showScreenSub;

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

            _showScreenSub = SignalBus.Subscribe<ShowScreenSignal>(OnShowScreen);
        }

        private void OnShowScreen(ShowScreenSignal signal)
        {
            if (signal.Screen == ScreenType.GameOver)
            {
                if (_audio != null)
                {
                    var failClip = ProceduralAudio.GetOrCreateExplosionClip();
                    _audio.PlaySfx(failClip, 0.8f);
                }
            }
        }

        protected override void OnUnbind()
        {
            _showScreenSub?.Dispose();
            _showScreenSub = null;

            if (View == null) return;
            View.RestartButton?.onClick.RemoveAllListeners();
            View.QuitButton?.onClick.RemoveAllListeners();
        }
    }
}
