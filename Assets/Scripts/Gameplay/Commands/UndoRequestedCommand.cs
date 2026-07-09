using Nexus.Core;
using Nexus.Core.Services;


namespace RingFlow.Gameplay
{
    public class UndoRequestedCommand : ICommand<UndoRequestedSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IEconomyService _economy;
        [Inject] private IAdService _ads;
        [Inject] private ISignalBus _signalBus;
        [Inject] private IProgressionService _progressionService;

        private static GameBalanceConfig Cfg => GameConfigDatabaseSO.Instance.BalanceConfig;

        public void Execute(UndoRequestedSignal signal)
        {
            if (_model.MoveHistory.Count == 0)
            {
                NexusLog.Warn("UndoRequestedCommand", "Execute", "", "No moves to undo.");
                return;
            }

            int level = _progressionService?.CurrentLevel.Value ?? 0;

            if (_progress.FreeUndosUsedThisSession.Value < Cfg.FreeUndosPerSession)
            {
                _progress.FreeUndosUsedThisSession.Value++;
                NexusLog.Info("UndoRequestedCommand", "Execute", "",
                    $"Free undo used ({_progress.FreeUndosUsedThisSession.Value}/{Cfg.FreeUndosPerSession} this session).");
                AnalyticsEvents.UndoUse(level, wasFree: true);
                _signalBus.Fire(new UndoSignal());
            }
            else if (_economy.CanAfford("Coins", Cfg.UndoCoinCost))
            {
                if (_economy.Spend("Coins", Cfg.UndoCoinCost, "Undo"))
                {
                    NexusLog.Info("UndoRequestedCommand", "Execute", "",
                        $"Paid undo with {Cfg.UndoCoinCost} coins.");
                    AnalyticsEvents.UndoUse(level, wasFree: false);
                    _signalBus.Fire(new UndoSignal());
                }
            }
            else if (_ads != null && _ads.IsRewardedAvailable("Undo"))
            {
                NexusLog.Info("UndoRequestedCommand", "Execute", "",
                    "No coins for undo; showing rewarded ad.");
                _ads.ShowRewarded("Undo", success =>
                {
                    if (success)
                    {
                        NexusLog.Info("UndoRequestedCommand", "Execute", "",
                            "Rewarded ad completed; applying undo.");
                        AnalyticsEvents.UndoUse(level, wasFree: false);
                        AnalyticsEvents.RewardedAd("Undo", true);
                        _signalBus.Fire(new UndoSignal());
                    }
                    else
                    {
                        NexusLog.Warn("UndoRequestedCommand", "Execute", "",
                            "Rewarded ad not completed; undo skipped.");
                    }
                });
            }
            else
            {
                NexusLog.Warn("UndoRequestedCommand", "Execute", "",
                    "Cannot afford undo (no coins, no ad).");
            }
        }
    }
}