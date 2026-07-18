using System;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Diagnostics;

namespace RingFlow.Gameplay.UI
{
    public class MainMenuMediator : Mediator<MainMenuView>
    {
        [Inject] private IProgressionService _progression;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private DailyRewardService _dailyReward;
        [Inject] private ILocalizationService _loc;
        [Inject] private IGameDiagnostics _diag;
        [Inject] private IViewMediatorTracker _tracker;
        [Inject] private GameConfigDatabaseSO _dbConfig;

        private Action<int, int> _coinsHandler;
        private Action<int, int> _diamondsHandler;
        private Action<int, int> _levelHandler;
        private Action<int, int> _xpHandler;

        protected override void OnBind()
        {
            _diag?.Checkpoint("MainMenuMediator.OnBind");
            _tracker?.TrackViewBound(View?.GetType(), GetType());
            if (View == null) return;

            View.Localize(_loc);

            View.ContinueButton?.onClick.AddListener(ContinueGame);
            View.PlayButton?.onClick.AddListener(QuickPlay);
            View.LevelSelectButton?.onClick.AddListener(GoToLevelSelect);
            View.SettingsButton?.onClick.AddListener(OpenSettings);
            View.DailyRewardButton?.onClick.AddListener(OpenDailyReward);
            View.ChestButton?.onClick.AddListener(OpenChests);

            bool canClaim = _dailyReward != null && _dailyReward.CanClaimNow();
            View.SetDailyRewardAvailable(canClaim);

            BindCurrencyUpdates();
            BindPlayerLevel();

            _diag?.Log("MainMenuMediator", $"Bound. DailyRewardAvailable={canClaim}.");
        }

        private void BindCurrencyUpdates()
        {
            if (_progress == null) return;

            _coinsHandler = (_, n) => View?.UpdateCoins(n);
            _diamondsHandler = (_, n) => View?.UpdateDiamonds(n);
            _progress.Coins.OnChanged(_coinsHandler);
            View.UpdateCoins(_progress.Coins.Value);
            _progress.Diamonds.OnChanged(_diamondsHandler);
            View.UpdateDiamonds(_progress.Diamonds.Value);
        }

        private void BindPlayerLevel()
        {
            if (_progress == null || _dbConfig == null) return;

            _levelHandler = (_, n) => RefreshPlayerLevel();
            _xpHandler = (_, n) => RefreshPlayerLevel();
            _progress.PlayerLevel.OnChanged(_levelHandler);
            _progress.Xp.OnChanged(_xpHandler);
            RefreshPlayerLevel();
        }

        private void RefreshPlayerLevel()
        {
            if (_progress == null || _dbConfig == null) return;
            int level = _progress.PlayerLevel.Value;
            int currentXp = _progress.Xp.Value;
            int reqXp = _dbConfig.GetXpRequiredForLevel(level);
            float progress = reqXp > 0 ? Math.Clamp((float)currentXp / reqXp, 0f, 1f) : 0f;
            View?.UpdatePlayerLevel(level, progress);
        }

        private void ContinueGame()
        {
            int level = _progression?.CurrentLevel.Value ?? 1;
            SignalBus.Fire(new LevelSelectedSignal(level));
        }

        private void QuickPlay()
        {
            int level = _progression?.CurrentLevel.Value ?? 1;
            SignalBus.Fire(new LevelSelectedSignal(level));
        }

        private void GoToLevelSelect()
        {
            SignalBus.Fire(new PlayRequestedSignal());
        }

        private void OpenSettings()
        {
            SignalBus.Fire(new OpenSettingsSignal());
        }

        private void OpenDailyReward()
        {
            SignalBus.Fire(new OpenDailyRewardSignal());
        }

        private void OpenChests()
        {
            SignalBus.Fire(new OpenChestPopupSignal());
        }

        protected override void OnUnbind()
        {
            _tracker?.TrackViewUnbound(View?.GetType());
            if (View != null)
            {
                View.ContinueButton?.onClick.RemoveAllListeners();
                View.PlayButton?.onClick.RemoveAllListeners();
                View.LevelSelectButton?.onClick.RemoveAllListeners();
                View.SettingsButton?.onClick.RemoveAllListeners();
                View.DailyRewardButton?.onClick.RemoveAllListeners();
                View.ChestButton?.onClick.RemoveAllListeners();
            }
            if (_progress != null)
            {
                if (_coinsHandler != null) _progress.Coins.RemoveOnChanged(_coinsHandler);
                if (_diamondsHandler != null) _progress.Diamonds.RemoveOnChanged(_diamondsHandler);
                if (_levelHandler != null) _progress.PlayerLevel.RemoveOnChanged(_levelHandler);
                if (_xpHandler != null) _progress.Xp.RemoveOnChanged(_xpHandler);
            }
            _coinsHandler = null;
            _diamondsHandler = null;
            _levelHandler = null;
            _xpHandler = null;
        }
    }
}
