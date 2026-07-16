using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    // PERMANENT FIX: IAsyncCommand<T> instead of ICommand<T>.
    // With ICommand<T>, Execute() returns immediately and the CommandPool clears all
    // [Inject] fields right away. Any async void helper called from Execute() will have
    // its captured [Inject] references null'd before the first await resumes.
    // With IAsyncCommand<T>, the pool only calls Return() after ExecuteAsync() fully
    // completes — so _fsm and all other injected refs remain valid throughout the delay.
    public class LevelWonCommand : IAsyncCommand<LevelWonSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private IEconomyService _economyService;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IGameStateMachine _fsm;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private IAnalyticsService _analyticsService;
        // _ads removed: TryShowInterstitial moved to WinState.OnEnterAsync (ad SDK race fix).
        // _feelConfig removed: WinStateDelayMs moved to WinState.OnEnterAsync (timing is State concern).

        private GameBalanceConfig Cfg
        {
            get
            {
                if (_dbConfig == null) throw new System.InvalidOperationException("[LevelWonCommand] GameConfigDatabaseSO not injected!");
                return _dbConfig.BalanceConfig;
            }
        }

        public async ValueTask ExecuteAsync(LevelWonSignal signal, CancellationToken ct)
        {
            if (_progressionService == null)
            {
                NexusLog.Error("LevelWonCommand", "ExecuteAsync", "",
                    "IProgressionService unbound; cannot advance level even though board was solved.");
            }
            if (_economyService == null)
            {
                NexusLog.Error("LevelWonCommand", "ExecuteAsync", "",
                    "IEconomyService unbound; coin/xp reward dropped.");
            }
            if (_progress == null)
            {
                NexusLog.Error("LevelWonCommand", "ExecuteAsync", "",
                    "PlayerProgressModel unbound; xp/world unlock dropped.");
            }

            int prevLevel  = _progressionService != null ? _progressionService.CurrentLevel.Value : 0;
            int prevMoves  = _model.MovesCount.Value;
            int prevTarget = _model.TargetMovesCount.Value;

            int stars = ComputeStars(prevMoves, prevTarget);

            _progressionService?.CompleteCurrentLevel();
            int newLevel = _progressionService != null ? _progressionService.CurrentLevel.Value : prevLevel;

            GrantRewards(prevLevel, prevMoves, stars, out int coinReward, out int xpEarned);
            _model.LastReward.Value = WinReward.From(prevMoves, prevTarget, coinReward, xpEarned, stars, prevLevel);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("LevelWonCommand", "ExecuteAsync", "",
                $"Level {prevLevel} WON! Moves={prevMoves}, Target={prevTarget}, Stars={stars}, Coins+={coinReward}, XP+={xpEarned}");
#endif

            _analyticsService?.LevelComplete(prevLevel, prevMoves, stars);

            UnlockWorldIfNeeded(newLevel);
            // TryShowInterstitial moved to WinState.OnEnterAsync — ad is shown inside the
            // state transition so the FSM is fully settled before the ad callback fires.
            MaybeDropChests(prevLevel, prevMoves, stars);

            // Delay responsibility moved to WinState.OnEnterAsync (GameFeelConfigSO.WinStateDelayMs).
            // Commands must not contain timing/animation delays — that is View/State concern.
            // Keeping delay here would block the SignalBus async chain for 500ms.
            if (ct.IsCancellationRequested) return;

            if (_fsm == null)
            {
                NexusLog.Error("LevelWonCommand", "ExecuteAsync", "",
                    "IGameStateMachine is NULL — Win screen cannot be shown.");
                return;
            }

            NexusLog.Info("LevelWonCommand", "ExecuteAsync", "",
                "Transitioning to WinState.");

            await _fsm.ChangeStateAsync<WinState>();
        }

        private int ComputeStars(int moves, int target)
        {
            if (target <= 0) return 1;
            if (moves <= target * Cfg.ThreeStarTargetRatioPercent / 100f) return 3;
            if (moves <= target * Cfg.TwoStarTargetRatioPercent / 100f)   return 2;
            return 1;
        }

        private void GrantRewards(int prevLevel, int prevMoves, int stars, out int coinReward, out int xpEarned)
        {
            bool isBoss = GameConfigDatabaseSO.IsBossLevel(_dbConfig, prevLevel);
            coinReward  = isBoss
                ? Cfg.BossCoinReward
                : Cfg.NormalCoinReward + (prevLevel % Cfg.LevelUpBonusDivisor) * Cfg.LevelUpBonusMultiplier;
            xpEarned = isBoss ? Cfg.BossXpReward : Cfg.NormalXpReward;

            _economyService?.Earn(CurrencyIds.Coins, coinReward,
                isBoss ? "Boss Win Reward" : "Level Win Reward");

            ApplyXpAndLevelUps(xpEarned);
        }

        private void ApplyXpAndLevelUps(int xpEarned)
        {
            if (_progress == null) return;

            _progress.Xp.Value += xpEarned;
            int xpRequired = _progress.XpToNextLevel(_dbConfig, _progress.PlayerLevel.Value);

            while (_progress.Xp.Value >= xpRequired)
            {
                int oldLevel = _progress.PlayerLevel.Value;
                _progress.Xp.Value       -= xpRequired;
                _progress.PlayerLevel.Value++;
                _economyService?.Earn(CurrencyIds.Coins, Cfg.LevelUpCoinReward, "Player Level Up Reward");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("LevelWonCommand", "ApplyXpAndLevelUps", "",
                    $"Player leveled up: {oldLevel} → {_progress.PlayerLevel.Value}. XP remaining={_progress.Xp.Value}.");
#endif
                xpRequired = _progress.XpToNextLevel(_dbConfig, _progress.PlayerLevel.Value);
            }
        }

        private void MaybeDropChests(int prevLevel, int prevMoves, int stars)
        {
            if (_progress == null) return;

            var rng = new System.Random(prevLevel * 1000 + prevMoves);
            _progress.ChestBronze.Value++;
            if (rng.NextDouble() < Cfg.SilverChestChance)                  _progress.ChestSilver.Value++;
            if (stars >= 3 && rng.NextDouble() < Cfg.GoldChestChance)     _progress.ChestGold.Value++;
            if (stars >= 3 && rng.NextDouble() < Cfg.DiamondChestChance)  _progress.ChestDiamond.Value++;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("LevelWonCommand", "MaybeDropChests", "",
                $"Chests: Bronze+1={_progress.ChestBronze.Value}, Silver={_progress.ChestSilver.Value}, Gold={_progress.ChestGold.Value}, Diamond={_progress.ChestDiamond.Value}.");
#endif
        }

        private void UnlockWorldIfNeeded(int newLevel)
        {
            if (_progress == null || _dbConfig == null) return;

            int newWorldIndex = _dbConfig.GetWorldForLevel(newLevel);
            if (newWorldIndex < 0 || newWorldIndex >= _progress.UnlockedWorlds.Count) return;

            if (!_progress.UnlockedWorlds[newWorldIndex])
            {
                _progress.UnlockedWorlds[newWorldIndex] = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("LevelWonCommand", "UnlockWorldIfNeeded", "",
                    $"World {newWorldIndex} unlocked (→ level {newLevel}).");
#endif
            }
        }

    }
}
