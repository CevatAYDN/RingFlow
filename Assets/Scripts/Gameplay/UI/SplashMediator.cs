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
                NexusLog.Error("SplashMediator", nameof(OnBind), "UI",
                    "View not bound.");
                return;
            }
            View.Localize(_loc);
            NexusLog.Info("SplashMediator", nameof(OnBind), "UI",
                $"Bound to {View.GetType().Name}. Starting splash animation.");

            // Fire-and-forget: AnimateProgress() runs asynchronously and
            // eventually triggers FSM transition or ParentalGate popup.
            // Wrapped in ContinueWith to log any unhandled exceptions.
            AnimateProgress().ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                    NexusLog.Error("SplashMediator", "AnimateProgress", "UI",
                        $"Splash animation failed: {t.Exception.InnerException?.Message}");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Animates the splash progress bar. After animation completes,
        /// either transitions to MainMenuState or shows the ParentalGate
        /// popup depending on GDPR consent status.
        /// </summary>
        private async Task AnimateProgress()
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

            if (!IsViewValid)
            {
                NexusLog.Warn("SplashMediator", nameof(AnimateProgress), "UI",
                    "View became invalid during animation; aborting.");
                return;
            }
            if (_fsm == null)
            {
                NexusLog.Error("SplashMediator", nameof(AnimateProgress), "FSM",
                    "IGameStateMachine is null; cannot transition.");
                return;
            }

            NexusLog.Info("SplashMediator", nameof(AnimateProgress), "UI",
                "Splash animation complete. Transitioning...");

            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;
            if (!reduceMotion && View?.CardGroup != null)
            {
                DOTween.Kill(View.CardGroup);
                DOTween.To(() => View.CardGroup.alpha, v => View.CardGroup.alpha = v, 0f, 0.2f)
                    .SetEase(DG.Tweening.Ease.InCubic)
                    .OnComplete(async () => await TransitionAsync()).SetTarget(View.CardGroup);
            }
            else
            {
                await TransitionAsync();
            }
        }

        /// <summary>
        /// Transitions out of the splash screen. If the user has accepted
        /// legal consent (GDPR), go directly to MainMenuState. Otherwise
        /// show the ParentalGate popup screen via ShowScreenSignal.
        /// </summary>
        private async Task TransitionAsync()
        {
            bool gdprAccepted = _consent != null && _consent.IsAccepted;
            if (gdprAccepted)
            {
                NexusLog.Info("SplashMediator", nameof(TransitionAsync), "FSM",
                    "GDPR accepted — transitioning to MainMenuState.");
                try { await _fsm.ChangeStateAsync<MainMenuState>(); }
                catch (System.Exception ex)
                {
                    NexusLog.Error("SplashMediator", nameof(TransitionAsync), "FSM", ex);
                }
            }
            else
            {
                NexusLog.Info("SplashMediator", nameof(TransitionAsync), "UI",
                    "GDPR not accepted — showing ParentalGate popup.");
                _signalBus?.Fire(new ShowScreenSignal(ScreenType.ParentalGate));
            }
        }

        protected override void OnUnbind() { }
    }
}