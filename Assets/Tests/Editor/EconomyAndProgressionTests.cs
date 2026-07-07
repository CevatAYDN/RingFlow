using System;
using System.Threading;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class EconomyAndProgressionTests
    {
        private PlayerProgressModel _progress;
        private RingFlow.Gameplay.EconomyService _economyService;
        private RingFlow.Gameplay.ProgressionService _progressionService;
        private DailyRewardService _dailyRewardService;

        [SetUp]
        public void Setup()
        {
            _progress = new PlayerProgressModel();
            _progress.Coins.Value = 100;
            _progress.Diamonds.Value = 10;
            _progress.CurrentLevel.Value = 1;
            _progress.MaxUnlockedLevel.Value = 1;

            _economyService = new RingFlow.Gameplay.EconomyService();
            // Inject dependency using reflection since we run outside DI container
            typeof(RingFlow.Gameplay.EconomyService).GetField("_progress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_economyService, _progress);
            _economyService.InitializeAsync(CancellationToken.None);

            _progressionService = new RingFlow.Gameplay.ProgressionService(_progress);
            _dailyRewardService = new DailyRewardService(_progress);
        }

        [Test]
        public void EconomyService_EarnAndSpend_SyncsWithPlayerProgressModel()
        {
            Assert.AreEqual(100, _economyService.GetBalance("Coins"));
            Assert.AreEqual(10, _economyService.GetBalance("Diamonds"));

            // Earn Coins
            _economyService.Earn("Coins", 50);
            Assert.AreEqual(150, _progress.Coins.Value);
            Assert.AreEqual(150, _economyService.GetBalance("Coins"));

            // Spend Coins
            bool success = _economyService.Spend("Coins", 80);
            Assert.IsTrue(success);
            Assert.AreEqual(70, _progress.Coins.Value);
            Assert.AreEqual(70, _economyService.GetBalance("Coins"));

            // Spend more than balance (should fail)
            bool fail = _economyService.Spend("Coins", 100);
            Assert.IsFalse(fail);
            Assert.AreEqual(70, _progress.Coins.Value);

            // CanAfford check
            Assert.IsTrue(_economyService.CanAfford("Coins", 50));
            Assert.IsFalse(_economyService.CanAfford("Coins", 100));
        }

        [Test]
        public void EconomyService_ModelUpdate_SyncsWithServiceBalance()
        {
            // Update model directly
            _progress.Coins.Value = 500;
            Assert.AreEqual(500, _economyService.GetBalance("Coins"));

            _progress.Diamonds.Value = 250;
            Assert.AreEqual(250, _economyService.GetBalance("Diamonds"));
        }

        [Test]
        public void ProgressionService_HandlesLevelBoundariesAndProgression()
        {
            // Normal set
            _progressionService.SetLevel(10);
            Assert.AreEqual(10, _progress.CurrentLevel.Value);
            Assert.AreEqual(10, _progress.MaxUnlockedLevel.Value);

            // Lower boundary clamp
            _progressionService.SetLevel(-5);
            Assert.AreEqual(1, _progress.CurrentLevel.Value);

            // Complete level increases level by 1
            _progressionService.SetLevel(5);
            _progress.MaxUnlockedLevel.Value = 5; // Reset to 5 so we test completion increase
            _progressionService.CompleteCurrentLevel();
            Assert.AreEqual(6, _progress.CurrentLevel.Value);
            Assert.AreEqual(6, _progress.MaxUnlockedLevel.Value);
        }

        [Test]
        public void ProgressionService_UpgradeCostCalculations()
        {
            // Linear curve cost test
            long linearCost = _progressionService.CalculateUpgradeCost(100, 3, 1.2f, CurveType.Linear);
            // formula: baseCost * (1 + (level-1) * (multiplier-1)) = 100 * (1 + 2 * 0.2) = 140
            Assert.AreEqual(140, linearCost);

            // Exponential curve cost test
            long expCost = _progressionService.CalculateUpgradeCost(100, 3, 1.5f, CurveType.Exponential);
            // formula: baseCost * multiplier^(level-1) = 100 * 1.5^2 = 225
            Assert.AreEqual(225, expCost);
        }

        [Test]
        public void DailyRewardService_GrantCycleAndResetLogic()
        {
            // Day 0 reward is 100 Coins
            var reward0 = DailyRewardTable.RewardForDayIndex(0);
            Assert.AreEqual("Coins", reward0.CurrencyId);
            Assert.AreEqual(100, reward0.Amount);

            // Day 3 reward is 1 Hint
            var reward3 = DailyRewardTable.RewardForDayIndex(3);
            Assert.AreEqual("Hint", reward3.CurrencyId);
            Assert.AreEqual(1, reward3.Amount);

            // Day 6 reward is 25 Diamonds
            var reward6 = DailyRewardTable.RewardForDayIndex(6);
            Assert.AreEqual("Diamonds", reward6.CurrencyId);
            Assert.AreEqual(25, reward6.Amount);

            // Test claimable timer resets (at least 24 hours required)
            // Use fixed noon DateTime to prevent calendar timezone midnight wrap errors
            DateTime baseTime = new DateTime(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc);
            _progress.DailyLastClaimUtcTicks.Value = baseTime.Ticks;

            // Same day claim should fail
            Assert.IsFalse(DailyRewardTable.IsDailyRewardClaimable(_progress.DailyLastClaimUtcTicks.Value, baseTime.AddHours(2)));

            // Next day claim should succeed
            Assert.IsTrue(DailyRewardTable.IsDailyRewardClaimable(_progress.DailyLastClaimUtcTicks.Value, baseTime.AddHours(25)));
        }
    }
}
