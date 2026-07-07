using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class GameplayCommandTests
    {
        private GameplayModel _gameplayModel;
        private PlayerProgressModel _progressModel;
        private MockSignalBus _signalBus;
        private MockEconomyService _economyService;
        private MockAdService _adService;

        [SetUp]
        public void Setup()
        {
            _gameplayModel = new GameplayModel();
            _progressModel = new PlayerProgressModel();
            _signalBus = new MockSignalBus();
            _economyService = new MockEconomyService();
            _adService = new MockAdService();

            // Initialize progress fields
            _progressModel.Coins.Value = 100;
            _progressModel.FreeUndosUsedThisSession.Value = 0;
            
            // Register worlds list
            for (int i = 0; i < 40; i++)
            {
                _progressModel.UnlockedWorlds.Add(i == 0); // Unlock first world
            }
        }

        private void InjectDependencies(object target)
        {
            var fields = target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.GetCustomAttribute<InjectAttribute>() != null)
                {
                    if (f.FieldType == typeof(GameplayModel))
                        f.SetValue(target, _gameplayModel);
                    else if (f.FieldType == typeof(PlayerProgressModel))
                        f.SetValue(target, _progressModel);
                    else if (f.FieldType == typeof(ISignalBus))
                        f.SetValue(target, _signalBus);
                    else if (f.FieldType == typeof(IEconomyService))
                        f.SetValue(target, _economyService);
                    else if (f.FieldType == typeof(IAdService))
                        f.SetValue(target, _adService);
                }
            }
        }

        [Test]
        public void SelectPoleCommand_SelectsEmptyOrValidPoles()
        {
            var command = new SelectPoleCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            _gameplayModel.Poles.Add(pole0);

            // First click selects
            command.Execute(new SelectPoleSignal(0));
            Assert.AreEqual(0, _gameplayModel.SelectedPoleId.Value);

            // Clicking again deselects
            command.Execute(new SelectPoleSignal(0));
            Assert.AreEqual(-1, _gameplayModel.SelectedPoleId.Value);
        }

        [Test]
        public void MoveRingCommand_ExecutesStandardMoveAndTendsHistory()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            command.Execute(new MoveRingSignal(0, 1));

            Assert.AreEqual(0, pole0.Rings.Count);
            Assert.AreEqual(1, pole1.Rings.Count);
            Assert.AreEqual(RingColor.Red, pole1.TopRing.Color);
            Assert.AreEqual(1, _gameplayModel.MoveHistory.Count);
            Assert.AreEqual(1, _gameplayModel.MovesCount.Value);
        }

        [Test]
        public void MoveRingCommand_HandlesPaintAndRainbowSpecialMechanics()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            // Set up target with a Paint ring
            pole1.AddRing(new RingData(RingColor.Blue, RingType.Paint));

            // Set up source with a Standard Red ring
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            command.Execute(new MoveRingSignal(0, 1));

            // Standard Red moving onto Paint Blue: Paint changes it to Blue, Paint becomes standard
            Assert.AreEqual(2, pole1.Rings.Count);
            Assert.AreEqual(RingColor.Blue, pole1.Rings[0].Color); // Old paint target
            Assert.AreEqual(RingType.Standard, pole1.Rings[0].Type); // Consumed Paint
            Assert.AreEqual(RingColor.Blue, pole1.TopRing.Color);   // Placed ring painted Blue
        }

        [Test]
        public void MoveRingCommand_HandlesMysteryRevealOnFromPole()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Mystery));
            pole0.AddRing(new RingData(RingColor.Blue, RingType.Standard));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            command.Execute(new MoveRingSignal(0, 1));

            // Popping top Blue ring from pole0 reveals the Mystery Red ring under it
            Assert.AreEqual(RingType.Standard, pole0.TopRing.Type);
            Assert.AreEqual(RingColor.Red, pole0.TopRing.Color);
            Assert.IsTrue(_signalBus.HasFiredRevealMystery);
        }

        [Test]
        public void SelectPoleCommand_GhostRingBecomesStandardWhenSelected()
        {
            var command = new SelectPoleCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Green, RingType.Ghost));
            _gameplayModel.Poles.Add(pole0);

            command.Execute(new SelectPoleSignal(0));

            Assert.AreEqual(RingType.Standard, pole0.TopRing.Type);
        }

        [Test]
        public void UndoRequestedCommand_AppliesAdAndCoinCostsCorrectly()
        {
            var command = new UndoRequestedCommand();
            InjectDependencies(command);

            // First 5 sessions are free
            _progressModel.FreeUndosUsedThisSession.Value = 4;
            _gameplayModel.MoveHistory.Push(new MoveRecord(0, 1, new RingData(RingColor.Red)));

            command.Execute(new UndoRequestedSignal());

            Assert.AreEqual(5, _progressModel.FreeUndosUsedThisSession.Value);
            Assert.IsTrue(_signalBus.HasFiredUndo);

            // Next undo costs 5 coins
            _progressModel.Coins.Value = 10;
            _economyService.CoinsBalance = 10;

            command.Execute(new UndoRequestedSignal());

            Assert.AreEqual(5, _economyService.CoinsBalance);
        }
    }

    // --- Minimal Mocks for Unit Testing ---

    public class MockSignalBus : ISignalBus
    {
        public bool HasFiredUndo { get; private set; }
        public bool HasFiredRevealMystery { get; private set; }

        public System.Collections.Generic.IReadOnlyDictionary<Type, System.Collections.Generic.IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers => null;

        public void Fire<T>(T signal) where T : struct
        {
            if (typeof(T) == typeof(UndoSignal))
                HasFiredUndo = true;
            else if (typeof(T) == typeof(RevealMysterySignal))
                HasFiredRevealMystery = true;
        }

        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct {}
        public void FireNextFrame<T>(T signal) where T : struct {}
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;

        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

    public class MockEconomyService : IEconomyService
    {
        public long CoinsBalance = 100;

        public float MasterVolume { get; set; }
        public float BgmVolume { get; set; }
        public float SfxVolume { get; set; }
        public bool IsMuted { get; set; }

        public ObservableProperty<long> GetObservableBalance(string currencyId) => new(CoinsBalance);
        public long GetBalance(string currencyId) => CoinsBalance;
        public bool CanAfford(string currencyId, long amount) => CoinsBalance >= amount;
        
        public bool Spend(string currencyId, long amount, string reason = "")
        {
            if (CanAfford(currencyId, amount))
            {
                CoinsBalance -= amount;
                return true;
            }
            return false;
        }

        public void Earn(string currencyId, long amount, string reason = "") => CoinsBalance += amount;
        public void SetBalance(string currencyId, long amount) => CoinsBalance = amount;
    }

    public class MockAdService : IAdService
    {
        #pragma warning disable 0067
        public event Action<string, double, string> OnImpressionRecorded;
        #pragma warning restore 0067

        public void SetNetworkAdapter(IAdNetworkAdapter adapter) {}
        public void SetInterstitialCooldown(float seconds) {}
        public bool IsInterstitialAvailable(string placement) => true;
        public bool IsRewardedAvailable(string placement) => true;
        public void ShowInterstitial(string placement, Action onComplete = null) => onComplete?.Invoke();
        public void ShowRewarded(string placement, Action<bool> onComplete) => onComplete?.Invoke(true);
        public void ShowBanner(string placement = "default", string position = "bottom") {}
        public void HideBanner() {}
    }
}
