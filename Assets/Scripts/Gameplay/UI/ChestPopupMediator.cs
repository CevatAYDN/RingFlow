using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §9 — Chest claim popup mediator.
    /// Shows accumulated chest counts and lets the player claim all in one tap.
    /// </summary>
    public class ChestPopupMediator : Mediator<ChestPopupView>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private ILocalizationService _loc;

        protected override void OnBind()
        {
            View.Localize(_loc);
            if (View.ClaimButton != null) View.ClaimButton.onClick.AddListener(OnClaimClicked);
            if (View.CloseButton != null) View.CloseButton.onClick.AddListener(OnCloseClicked);

            // Listen for chest award/claim signals
            Subscribe<ChestAwardedSignal>(_ => OnCloseClicked());

            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_progress == null) return;
            View.ShowChestCounts(
                _progress.ChestBronze.Value,
                _progress.ChestSilver.Value,
                _progress.ChestGold.Value,
                _progress.ChestDiamond.Value);
        }

        private void OnClaimClicked()
        {
            SignalBus.Fire(new ChestClaimAllSignal());
        }

        private void OnCloseClicked()
        {
            SignalBus.Fire(new HideScreenSignal(ScreenType.ChestPopup));
        }

        protected override void OnUnbind()
        {
            if (View.ClaimButton != null) View.ClaimButton.onClick.RemoveAllListeners();
            if (View.CloseButton != null) View.CloseButton.onClick.RemoveAllListeners();
        }
    }
}
