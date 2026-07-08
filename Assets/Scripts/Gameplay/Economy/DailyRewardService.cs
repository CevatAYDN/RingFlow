using System;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §9 — Daily 7-day reward cycle (100 → 150 → 200 → Hint → 300 → Tema → Diamond).
    /// Implemented as a static table + claim method so it survives zero-config and avoids any platform clock dependency for unit tests.
    /// </summary>
    public static class DailyRewardTable
    {
        public const int CycleLength = 7;

        /// <summary>Reward for each day index 0..6.</summary>
        public static CurrencyAmount RewardForDayIndex(int dayIndex)
        {
            // Normalize wraps around at 7 (cycle repeats).
            int d = ((dayIndex % CycleLength) + CycleLength) % CycleLength;
            return d switch
            {
                0 => new CurrencyAmount("Coins", 100),
                1 => new CurrencyAmount("Coins", 150),
                2 => new CurrencyAmount("Coins", 200),
                3 => new CurrencyAmount("Hint", 1),       // virtual currency — gives 1 free undo via UndoCommand
                4 => new CurrencyAmount("Coins", 300),
                5 => new CurrencyAmount("Theme", 1),      // unlocks a random theme (caller picks)
                6 => new CurrencyAmount("Diamonds", 25),
                _ => new CurrencyAmount("Coins", 0)
            };
        }

        /// <summary>True if the local day has changed since last claim given 24h reset semantics.</summary>
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

        public DailyRewardService(PlayerProgressModel progress)
        {
            _progress = progress;
        }

        /// <summary>The day index that the NEXT claim will reward (read-only preview for UI).</summary>
        public int DayIndexPreview => _progress.DailyDayIndex.Value + 1;

        public bool CanClaimNow()
        {
            return DailyRewardTable.IsDailyRewardClaimable(
                _progress.DailyLastClaimUtcTicks.Value,
                DateTime.UtcNow);
        }

        /// <summary>Returns the next day's reward after this claim. Updates stamp + index.</summary>
        public CurrencyAmount Claim()
        {
            if (!CanClaimNow())
            {
                NexusLog.Warn("DailyRewardService", nameof(Claim), _progress.DailyLastClaimUtcTicks.Value.ToString(),
                    "Cannot claim yet (24h reset not elapsed). Returning zero Coins.");
                return new CurrencyAmount("Coins", 0);
            }

            int nextIndex = _progress.DailyDayIndex.Value + 1;
            var reward = DailyRewardTable.RewardForDayIndex(nextIndex);

            if (reward.Amount <= 0)
            {
                NexusLog.Warn("DailyRewardService", nameof(Claim), nextIndex.ToString(),
                    $"Reward table returned zero for day index {nextIndex}.");
            }

            _progress.DailyDayIndex.Value = nextIndex % DailyRewardTable.CycleLength;
            _progress.DailyLastClaimUtcTicks.Value = DateTime.UtcNow.Ticks;

            return reward;
        }
    }
}
