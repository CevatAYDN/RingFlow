using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

using UnityEngine;

namespace RingFlow.Gameplay
{
    public class LevelWonCommand : ICommand<LevelWonSignal>
    {
        [Inject] private GameplayModel _model;
        [Inject] private IProgressionService _progressionService;
        [Inject] private IEconomyService _economyService;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IGameStateMachine _fsm;
        [Inject] private IAdService _ads;
        [Inject] private GameConfigDatabaseSO _dbConfig;
        [Inject] private IAnalyticsService _analyticsService;

        private GameBalanceConfig Cfg
        {
            get
            {
                if (_dbConfig == null) throw new System.InvalidOperationException("[LevelWonCommand] GameConfigDatabaseSO not injected!");
                return _dbConfig.BalanceConfig;
            }
        }

        public void Execute(LevelWonSignal signal)
        {
            if (_progressionService == null)
            {
                NexusLog.Error("LevelWonCommand", "Execute", "",
                    "IProgressionService unbound; cannot advance level even though board was solved.");
            }
            if (_economyService == null)
            {
                NexusLog.Error("LevelWonCommand", "Execute", "",
                    "IEconomyService unbound; coin/xp reward dropped.");
            }
            if (_progress == null)
            {
                NexusLog.Error("LevelWonCommand", "Execute", "",
                    "PlayerProgressModel unbound; xp/world unlock dropped.");
            }

            int prevLevel = _progressionService != null ? _progressionService.CurrentLevel.Value : 0;
            int prevMoves = _model.MovesCount.Value;
            int prevTarget = _model.TargetMovesCount.Value;

            int stars = 1;
            if (prevTarget > 0)
            {
                if (prevMoves <= prevTarget * Cfg.ThreeStarTargetRatioPercent / 100f) stars = 3;
                else if (prevMoves <= prevTarget * Cfg.TwoStarTargetRatioPercent / 100f) stars = 2;
            }

            if (_progressionService != null)
            {
                _progressionService.CompleteCurrentLevel();
            }

            int newLevel = _progressionService != null ? _progressionService.CurrentLevel.Value : prevLevel;

            int newWorldIndex = _dbConfig.GetWorldForLevel(newLevel);
            bool isBoss = WorldConfigSO.IsBossLevel(prevLevel);
            int coinReward = isBoss ? Cfg.BossCoinReward : Cfg.NormalCoinReward + (prevLevel % 11) * 10;
            _economyService?.Earn(CurrencyIds.Coins, coinReward, isBoss ? "Boss Win Reward" : "Level Win Reward");

            int xpEarned = isBoss ? Cfg.BossXpReward : Cfg.NormalXpReward;
            if (_progress != null)
            {
                _progress.Xp.Value += xpEarned;

                int xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
                while (_progress.Xp.Value >= xpRequired)
                {
                    int oldLevel = _progress.PlayerLevel.Value;
                    _progress.Xp.Value -= xpRequired;
                    _progress.PlayerLevel.Value++;
                    _economyService?.Earn(CurrencyIds.Coins, Cfg.LevelUpCoinReward, "Player Level Up Reward");
                    NexusLog.Info("LevelWonCommand", "Execute", "",
                        $"Player leveled up: {oldLevel} → {_progress.PlayerLevel.Value}. XP remaining={_progress.Xp.Value}.");
                    xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
                }
            }

            _model.LastReward.Value = WinReward.From(prevMoves, prevTarget, coinReward, xpEarned, stars);

            NexusLog.Info("LevelWonCommand", "Execute", "",
                $"Level {prevLevel} WON! Moves={prevMoves}, Target={prevTarget}, Stars={stars}, Coins+={coinReward}, XP+={xpEarned}");

            if (_analyticsService != null)
            {
                _analyticsService.LevelComplete(prevLevel, prevMoves, stars);
            }

            if (_progress != null)
            {
                _progress.ChestBronze.Value++;
                if (UnityEngine.Random.value < Cfg.SilverChestChance) _progress.ChestSilver.Value++;
                if (stars >= 3 && UnityEngine.Random.value < Cfg.GoldChestChance) _progress.ChestGold.Value++;
                if (stars >= 3 && UnityEngine.Random.value < Cfg.DiamondChestChance) _progress.ChestDiamond.Value++;

                NexusLog.Info("LevelWonCommand", "Execute", "",
                    $"Chests awarded: Bronze+1={_progress.ChestBronze.Value}, Silver={_progress.ChestSilver.Value}, Gold={_progress.ChestGold.Value}, Diamond={_progress.ChestDiamond.Value}.");
            }

            if (_progress != null && newWorldIndex >= 0 && newWorldIndex < _progress.UnlockedWorlds.Count)
            {
                bool wasUnlocked = _progress.UnlockedWorlds[newWorldIndex];
                if (!wasUnlocked)
                {
                    _progress.UnlockedWorlds[newWorldIndex] = true;
                    NexusLog.Info("LevelWonCommand", "Execute", "",
                        $"World {newWorldIndex} unlocked (level {prevLevel} → {newLevel}).");
                }
            }

            // Move the world-unlock actual mutation AFTER the log so we don't double-set.
            // The first set (original) is removed — the logic above covers it.

            if (_progress != null && _ads != null && !_progress.RemoveAds.Value)
            {
                _progress.LevelsSinceLastInterstitial++;
                if (_progress.LevelsSinceLastInterstitial >= Cfg.InterstitialAdInterval)
                {
                    _progress.LevelsSinceLastInterstitial = 0;
                    if (_ads.IsInterstitialAvailable("LevelComplete"))
                    {
                        NexusLog.Info("LevelWonCommand", "Execute", "",
                            $"Showing interstitial (interval={Cfg.InterstitialAdInterval}).");
                        _ads.ShowInterstitial("LevelComplete");
                        if (_analyticsService != null)
                        {
                            _analyticsService.InterstitialAd("LevelComplete");
                        }
                    }
                }
            }

            _ = _fsm?.ChangeStateAsync<WinState>();
        }
    }
}