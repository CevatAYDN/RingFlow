using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §9 — Chest claim popup mediator.
    /// Shows accumulated chest counts and lets the player claim all in one tap.
    /// </summary>
    public class ChestPopupMediator : Mediator<ChestPopupView>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private ILocalizationService _loc;
        [Inject] private IGameDiagnostics _diag;
        [Inject] private IViewMediatorTracker _tracker;

        protected override void OnBind()
        {
            _diag?.Checkpoint("ChestPopupMediator.OnBind");
            if (View == null)
            {
                NexusLog.Error("ChestPopupMediator", nameof(OnBind), "", "ChestPopupView not bound.");
                return;
            }
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            View.Localize(_loc);
            if (View.ClaimButton != null) View.ClaimButton.onClick.AddListener(OnClaimClicked);
            if (View.CloseButton != null) View.CloseButton.onClick.AddListener(OnCloseClicked);

            // Listen for chest award/claim signals
            Subscribe<ChestAwardedSignal>(_ => OnCloseClicked());

            RefreshDisplay();
            _diag?.Log("ChestPopupMediator", $"Bound. Chests: B={_progress?.ChestBronze.Value} S={_progress?.ChestSilver.Value} G={_progress?.ChestGold.Value} D={_progress?.ChestDiamond.Value}.");
        }

        private void RefreshDisplay()
        {
            if (_progress == null) return;
            if (_dbConfig == null)
            {
                NexusLog.Error("ChestPopupMediator", nameof(RefreshDisplay), "",
                    "GameConfigDatabaseSO not bound.");
                View.ShowChestCounts(0, 0, 0, 0);
                return;
            }
            var cfg = _dbConfig.BalanceConfig;
            View.ShowChestCounts(
                _progress.ChestBronze.Value,
                _progress.ChestSilver.Value,
                _progress.ChestGold.Value,
                _progress.ChestDiamond.Value,
                cfg.ChestXpBronze,
                cfg.ChestXpSilver,
                cfg.ChestXpGold,
                cfg.ChestXpDiamond);
        }

        private void OnClaimClicked()
        {
            _diag?.Log("ChestPopupMediator", "Claim clicked.");
            SignalBus.Fire(new ChestClaimAllSignal());
        }

        private void OnCloseClicked()
        {
            _diag?.Log("ChestPopupMediator", "Close clicked.");
            SignalBus.Fire(new HideScreenSignal(ScreenType.ChestPopup));
        }

        protected override void OnUnbind()
        {
            _tracker?.TrackViewUnbound(View?.GetType());
            if (View.ClaimButton != null) View.ClaimButton.onClick.RemoveAllListeners();
            if (View.CloseButton != null) View.CloseButton.onClick.RemoveAllListeners();
        }
    }
}
