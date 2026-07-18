using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;
using RingFlow.Gameplay.Services;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    public class SplashMediator : Mediator<SplashView>
    {
        [Inject] private IGameStateMachine _fsm;
        [Inject] private ILocalizationService _loc;
        [Inject] private ISignalBus _signalBus;
        [Inject] private ILegalConsentService _consent;
        [Inject] private SettingsModel _settings;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("SplashMediator", "OnBind", "", "View not bound.");
                return;
            }
            View.Localize(_loc);
            AnimateProgress();
        }

        private async void AnimateProgress()
        {
            float duration = 0.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float progress = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                View?.SetProgress(progress);
                await Task.Yield();
            }
            View?.SetProgress(1f);

            try { await Awaitable.WaitForSecondsAsync(0.3f); }
            catch (System.Exception) { return; }

            if (!IsViewValid) return;
            if (_fsm == null) return;

            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;
            if (!reduceMotion && View?.CardGroup != null)
            {
                DOTween.Kill(View.CardGroup);
                DOTween.To(() => View.CardGroup.alpha, v => View.CardGroup.alpha = v, 0f, 0.2f)
                    .SetEase(DG.Tweening.Ease.InCubic)
                    .OnComplete(() => Transition());
            }
            else
            {
                Transition();
            }
        }

        private async void Transition()
        {
            bool gdprAccepted = _consent != null && _consent.IsAccepted;
            if (gdprAccepted)
            {
                try { await _fsm.ChangeStateAsync<MainMenuState>(); }
                catch (System.Exception ex) { NexusLog.Error("SplashMediator", "Transition", "", ex); }
            }
            else
            {
                _signalBus?.Fire(new ShowScreenSignal(ScreenType.ParentalGate));
            }
        }

        protected override void OnUnbind() { }
    }
}
