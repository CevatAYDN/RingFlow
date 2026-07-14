using System;
using System.Collections.Generic;
using Nexus.Core.Services;
using RingFlow.Gameplay.Services;

namespace RingFlow.Gameplay
{
    public static class DailyRewardTable
    {
        /// <summary>Reward for each day index from GameBalanceConfig.DailyRewards list.</summary>
        public static CurrencyAmount RewardForDayIndex(List<DailyRewardEntry> dailyRewards, int dayIndex)
        {
            if (dailyRewards == null || dailyRewards.Count == 0)
                return new CurrencyAmount("Coins", 0);
            int d = ((dayIndex % dailyRewards.Count) + dailyRewards.Count) % dailyRewards.Count;
            var entry = dailyRewards[d];
            return new CurrencyAmount(entry.CurrencyId, entry.Amount);
        }

        public static int CycleLength(GameConfigDatabaseSO db)
        {
            return db?.BalanceConfig.DailyRewards?.Count ?? 7;
        }

        public static bool IsDailyRewardClaimable(long lastClaimUtcTicks, DateTime nowUtc)
        {
            if (lastClaimUtcTicks <= 0) return true;
            try
            {
                var last = new DateTime(lastClaimUtcTicks, DateTimeKind.Utc);
                return (nowUtc.Date - last.Date).TotalDays >= 1.0;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                NexusLog.Warn("DailyRewardTable", nameof(IsDailyRewardClaimable), lastClaimUtcTicks.ToString(),
                    $"Stored tick value out of range ({ex.Message}); allowing claim to recover corrupt save.");
                return true;
            }
        }
    }

    public sealed class DailyRewardService
    {
        private readonly PlayerProgressModel _progress;
        private readonly GameConfigDatabaseSO _dbConfig;
        private readonly IGameTimeService _time;

        public DailyRewardService(PlayerProgressModel progress, GameConfigDatabaseSO dbConfig, IGameTimeService time = null)
        {
            _progress = progress;
            _dbConfig = dbConfig;
            _time = time;
        }

        private DateTime UtcNow => _time?.UtcNow ?? DateTime.UtcNow;

        public int DayIndexPreview => _progress.DailyDayIndex.Value + 1;

        private long MinClaimIntervalTicks
        {
            get
            {
                int minutes = _dbConfig != null ? _dbConfig.BalanceConfig.MinClaimIntervalMinutes : 5;
                return TimeSpan.TicksPerMinute * minutes;
            }
        }

        public bool CanClaimNow()
        {
            return CanClaimNow(out _);
        }

        public bool CanClaimNow(out string reason)
        {
            var now = UtcNow;
            var nowTicks = now.Ticks;
            var lastTicks = _progress.DailyLastClaimUtcTicks.Value;

            if (lastTicks > 0 && nowTicks < lastTicks)
            {
                reason = "clock_rollback";
                return false;
            }

            var resetMode = _dbConfig != null
                ? _dbConfig.BalanceConfig.ResetMode
                : DailyRewardResetMode.CalendarDayUtc;

            if (resetMode == DailyRewardResetMode.FixedIntervalMinutes)
            {
                if (lastTicks > 0 && (nowTicks - lastTicks) < MinClaimIntervalTicks)
                {
                    reason = "too_soon";
                    return false;
                }
            }
            else
            {
                if (!DailyRewardTable.IsDailyRewardClaimable(lastTicks, now))
                {
                    reason = "daily_reset_not_elapsed";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public CurrencyAmount Claim()
        {
            if (!CanClaimNow(out var reason))
            {
                NexusLog.Warn("DailyRewardService", nameof(Claim), _progress.DailyLastClaimUtcTicks.Value.ToString(),
                    $"Cannot claim yet ({reason}). Returning zero Coins.");
                return new CurrencyAmount("Coins", 0);
            }

            int nextIndex = _progress.DailyDayIndex.Value + 1;
            var reward = DailyRewardTable.RewardForDayIndex(
                _dbConfig?.BalanceConfig.DailyRewards, nextIndex);

            if (reward.Amount <= 0)
            {
                NexusLog.Warn("DailyRewardService", nameof(Claim), nextIndex.ToString(),
                    $"Reward table returned zero for day index {nextIndex}.");
            }

            int cycle = DailyRewardTable.CycleLength(_dbConfig);
            _progress.DailyDayIndex.Value = nextIndex % cycle;
            _progress.DailyLastClaimUtcTicks.Value = UtcNow.Ticks;

            NexusLog.Info("DailyRewardService", nameof(Claim), nextIndex.ToString(),
                $"Daily reward claimed — day {nextIndex % cycle}, reward: {reward.CurrencyId} x{reward.Amount}.");

            return reward;
        }
    }
}
