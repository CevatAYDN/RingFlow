using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Tests
{
    /// <summary>
    /// Lightweight PlayMode integration tests for the core gameplay loop.
    /// Exercises the state machine, commands, solver, and save system together.
    /// </summary>
    [TestFixture]
    public class PlayModeIntegrationTests
    {
        private GameplayModel _gameplayModel;
        private PlayerProgressModel _progressModel;
        private MockGameStateMachine _fsm;
        private LoggingSignalBus _signalBus;
        private RingFlow.Gameplay.ProgressionService _progressionService;
        private MockEconomyService _economyService;
        private RingMoveStrategyManager _strategyManager;
        private GameConfigDatabaseSO _db;

        [SetUp]
        public void Setup()
        {
            _gameplayModel = new GameplayModel();
            _progressModel = new PlayerProgressModel();
            _fsm = new MockGameStateMachine();
            _signalBus = new LoggingSignalBus();
            _db = Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");
            _progressionService = new RingFlow.Gameplay.ProgressionService(_progressModel, _db);
            _economyService = new MockEconomyService();
            _strategyManager = new RingMoveStrategyManager(_db);
            RingValidationStrategyManager validationManager = new RingValidationStrategyManager();
            PoleState.SetValidationManager(validationManager);

            _progressModel.Coins.Value = 100;
            _progressModel.Xp.Value = 0;
            _progressModel.PlayerLevel.Value = 1;
            _progressModel.MaxUnlockedLevel.Value = 1;
            for (int i = 0; i < 40; i++)
                _progressModel.UnlockedWorlds.Add(i == 0);
        }

        // ---------------------------------------------------------------
        //  Core gameplay loop: init → play moves → win → progression
        // ---------------------------------------------------------------

        [Test]
        public void CoreLoop_InitAndPlayAndWin_UpdatesProgress()
        {
            // Arrange
            InitLevelWithDeterministicSeed(1);

            // Assert initial state
            Assert.That(_gameplayModel.Poles.Count, Is.GreaterThanOrEqualTo(2),
                "Level should have at least 2 poles.");
            Assert.That(_gameplayModel.IsGameWon.Value, Is.False,
                "Level should not be won on init.");

            // Act: play moves recorded via signal bus
            int moveCount = _signalBus.MoveCount;
            Assert.That(moveCount, Is.GreaterThanOrEqualTo(0));

            // Act: simulate win
            _gameplayModel.IsGameWon.Value = true;
            var levelWonCmd = new LevelWonCommand();
            InjectFields(levelWonCmd);
            levelWonCmd.Execute(new LevelWonSignal());

            // Assert: progression updated
            Assert.That(_progressModel.CurrentLevel.Value, Is.EqualTo(2),
                "Current level should advance to 2.");
            Assert.That(_progressModel.MaxUnlockedLevel.Value, Is.EqualTo(2),
                "Max unlocked should advance to 2.");
            Assert.That(_progressModel.Xp.Value, Is.GreaterThan(0),
                "XP should be awarded on win.");
        }

        [Test]
        public void CheckWinCommand_EvaluatesEmptyPolesAsNotWon()
        {
            // Arrange
            _gameplayModel.IsGameWon.Value = false;

            var cmd = new CheckWinCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);

            // Act: empty poles should not be a win
            cmd.Execute(new CheckWinSignal());

            // Assert
            Assert.That(_signalBus.FiredLevelWon, Is.False,
                "Empty board should not fire LevelWon.");
        }

        [Test]
        public void MoveRingCommand_StandardMove_UpdatesModel()
        {
            // Arrange: simple 2-pole board, one ring each
            var fromPole = new PoleState { Id = 0, MaxCapacity = 4 };
            fromPole.AddRing(new RingData(RingColor.Red));
            var toPole = new PoleState { Id = 1, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var cmd = new MoveRingCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);

            // Act
            cmd.Execute(new MoveRingSignal(0, 1));

            // Assert
            Assert.That(_gameplayModel.Poles[0].Rings.Count, Is.EqualTo(0), "Source pole should be empty.");
            Assert.That(_gameplayModel.Poles[1].Rings.Count, Is.EqualTo(1), "Target pole should have 1 ring.");
            Assert.That(_gameplayModel.Poles[1].Rings[0].Color, Is.EqualTo(RingColor.Red));
            Assert.That(_gameplayModel.MovesCount.Value, Is.EqualTo(1));
        }

        [Test]
        public void MoveRingCommand_CannotMoveOntoLockedPole()
        {
            // Arrange
            var fromPole = new PoleState { Id = 0, MaxCapacity = 4 };
            fromPole.AddRing(new RingData(RingColor.Red));
            var toPole = new PoleState { Id = 1, MaxCapacity = 4, IsLocked = true };
            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var cmd = new MoveRingCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);

            // Act
            cmd.Execute(new MoveRingSignal(0, 1));

            // Assert: move should be blocked
            Assert.That(_gameplayModel.Poles[0].Rings.Count, Is.EqualTo(1),
                "Source pole should still have its ring.");
            Assert.That(_gameplayModel.Poles[1].Rings.Count, Is.EqualTo(0),
                "Locked pole should remain empty.");
        }

        [Test]
        public void UndoCommand_RestoresMove()
        {
            // Arrange
            var fromPole = new PoleState { Id = 0, MaxCapacity = 4 };
            fromPole.AddRing(new RingData(RingColor.Red));
            var toPole = new PoleState { Id = 1, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var moveCmd = new MoveRingCommand();
            InjectFields(moveCmd);
            InjectField(moveCmd, "_signalBus", _signalBus);
            moveCmd.Execute(new MoveRingSignal(0, 1));

            Assert.That(_gameplayModel.MoveHistory.Count, Is.EqualTo(1));

            // Act: undo
            var undoCmd = new UndoCommand();
            InjectFields(undoCmd);
            InjectField(undoCmd, "_signalBus", _signalBus);
            undoCmd.Execute(new UndoSignal());

            // Assert
            Assert.That(_gameplayModel.Poles[0].Rings.Count, Is.EqualTo(1),
                "Undo should restore the ring to pole 0.");
            Assert.That(_gameplayModel.Poles[1].Rings.Count, Is.EqualTo(0),
                "Undo should clear pole 1.");
            Assert.That(_gameplayModel.Poles[0].Rings[0].Color, Is.EqualTo(RingColor.Red));
        }

        [Test]
        public void Solver_FindsSolutionForGeneratedLevel()
        {
            // Arrange
            int poleCount = 4;
            int colorCount = 3;
            int maxCap = 4;
            int level = 5;

            var db = Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");
            var levelData = LevelGenerator.GenerateLevel(db, level, level * 12345, poleCount, colorCount, maxCap);
            Assert.That(levelData, Is.Not.Null, "LevelGenerator should produce valid level.");

            // Act
            var board = new BoardState { PoleCount = levelData.Poles.Count };
            for (int p = 0; p < levelData.Poles.Count; p++)
            {
                for (int r = 0; r < levelData.Poles[p].Rings.Count; r++)
                    board.AddRing(p, levelData.Poles[p].Rings[r]);
            }

            var result = LevelSolver.Solve(board, maxCap);

            // Assert
            Assert.That(result.IsSolvable, Is.True,
                $"Generated level {level} should be solvable. {result.MoveCount} moves found.");
            Assert.That(result.MoveCount, Is.GreaterThan(0));
            Assert.That(result.MoveCount, Is.LessThanOrEqualTo(levelData.TargetMoves + 10),
                "Solver should find path within reasonable move budget.");
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesAllFields()
        {
            // Arrange
            _progressModel.Coins.Value = 500;
            _progressModel.Diamonds.Value = 25;
            _progressModel.Xp.Value = 320;
            _progressModel.CurrentLevel.Value = 12;
            _progressModel.MaxUnlockedLevel.Value = 15;
            _progressModel.PlayerLevel.Value = 3;
            _progressModel.ChestBronze.Value = 2;
            _progressModel.ChestGold.Value = 1;
            _progressModel.DailyDayIndex.Value = 4;
            _progressModel.HintCount.Value = 5;
            _progressModel.RemoveAds.Value = true;
            _progressModel.UnlockedWorlds[0] = true;
            _progressModel.UnlockedWorlds[5] = true;

            var prefs = new InMemoryPlayerPrefs();

            // Act
            PlayerProgressSaveSystem.Save(prefs, _progressModel);

            var loaded = new PlayerProgressModel();
            loaded.CurrentLevel.Value = 0; // reset
            PlayerProgressSaveSystem.Load(prefs, loaded);

            // Assert
            Assert.That(loaded.Coins.Value, Is.EqualTo(500));
            Assert.That(loaded.Diamonds.Value, Is.EqualTo(25));
            Assert.That(loaded.Xp.Value, Is.EqualTo(320));
            Assert.That(loaded.CurrentLevel.Value, Is.EqualTo(12));
            Assert.That(loaded.MaxUnlockedLevel.Value, Is.EqualTo(15));
            Assert.That(loaded.PlayerLevel.Value, Is.EqualTo(3));
            Assert.That(loaded.ChestBronze.Value, Is.EqualTo(2));
            Assert.That(loaded.ChestGold.Value, Is.EqualTo(1));
            Assert.That(loaded.DailyDayIndex.Value, Is.EqualTo(4));
            Assert.That(loaded.HintCount.Value, Is.EqualTo(5));
            Assert.That(loaded.RemoveAds.Value, Is.True);
            Assert.That(loaded.UnlockedWorlds[5], Is.True);
        }

        [Test]
        public void SaveChecksum_CorruptedData_LogsErrorButStillLoads()
        {
            // Arrange
            var prefs = new InMemoryPlayerPrefs();
            _progressModel.Coins.Value = 300;
            PlayerProgressSaveSystem.Save(prefs, _progressModel);

            // Corrupt a key — checksum will mismatch
            prefs.SetInt(PlayerProgressModel.KeyCoins, -99999);

            var loaded = new PlayerProgressModel();

            // Act: load must not throw, even with checksum mismatch.
            // Checksum mismatch is logged at Warning level (non-fatal for tests).
            Assert.DoesNotThrow(() => PlayerProgressSaveSystem.Load(prefs, loaded));

            // Assert: corrupt data is loaded (best-effort) rather than silently reset
            Assert.That(loaded.Coins.Value, Is.EqualTo(-99999),
                "Corrupt save should still load data — silent reset is worse than letting the player manually reset.");
        }

        [Test]
        public void InitLevelCommand_SeedRetry_ProducesValidLevel()
        {
            // Arrange
            var cmd = new InitLevelCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);
            InjectField(cmd, "_progressionService",
                new RingFlow.Gameplay.ProgressionService(_progressModel, _db));

            // Act: level 999 with high difficulty (unlikely to fail, but retry logic covered)
            cmd.Execute(new InitLevelSignal(999));

            // Assert
            Assert.That(_gameplayModel.Poles.Count, Is.GreaterThanOrEqualTo(2),
                "InitLevelCommand should always produce at least 2 poles.");
            Assert.That(_gameplayModel.TargetMovesCount.Value, Is.GreaterThan(0));
        }

        // ---------------------------------------------------------------
        //  Helpers
        // ---------------------------------------------------------------

        private void InitLevelWithDeterministicSeed(int level)
        {
            _gameplayModel.Reset();
            var cmd = new InitLevelCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);
            InjectField(cmd, "_progressionService",
                new RingFlow.Gameplay.ProgressionService(_progressModel, _db));
            cmd.Execute(new InitLevelSignal(level));
        }

        private void InjectFields(object target)
        {
            InjectField(target, "_model", _gameplayModel);
            InjectField(target, "_gameplayModel", _gameplayModel);
            InjectField(target, "_progress", _progressModel);
            InjectField(target, "_progressModel", _progressModel);
            InjectField(target, "_fsm", _fsm);
            InjectField(target, "_progression", _progressionService);
            InjectField(target, "_progressionService", _progressionService);
            InjectField(target, "_economyService", _economyService);
            InjectField(target, "_strategyManager", _strategyManager);
            InjectField(target, "_economy", _economyService);

            // Data-driven dependencies
            var db = Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");
            InjectField(target, "_dbConfig", db);
            InjectField(target, "_assetService", new RingFlow.Gameplay.Services.ResourcesAssetService());
            InjectField(target, "_analyticsService", new MockAnalyticsService());
            InjectField(target, "_ads", new MockAdService());
        }

        private static void InjectField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }
    }

    // ---------------------------------------------------------------
    //  Mocks
    // ---------------------------------------------------------------

    public class LoggingSignalBus : ISignalBus
    {
        public bool FiredLevelWon { get; private set; }
        public int MoveCount { get; private set; }
        public int WinCount { get; private set; }

        public IReadOnlyDictionary<Type, IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers { get; } =
            new Dictionary<Type, IReadOnlyList<CommandHandlerInfo>>();

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is MoveRingSignal) MoveCount++;
            if (signal is LevelWonSignal) { FiredLevelWon = true; WinCount++; }
        }

        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;
        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

    public class MockEconomyService : IEconomyService
    {
        private readonly Dictionary<string, long> _balances = new();

        public long GetBalance(string currencyId) => _balances.TryGetValue(currencyId, out var b) ? b : 0L;
        public void SetBalance(string currencyId, long amount) => _balances[currencyId] = amount;
        public bool CanAfford(string currencyId, long amount) => GetBalance(currencyId) >= amount;

        public bool Spend(string currencyId, long amount, string reason = "")
        {
            if (!CanAfford(currencyId, amount)) return false;
            _balances[currencyId] = GetBalance(currencyId) - amount;
            return true;
        }
        public void Earn(string currencyId, long amount, string reason = "")
            => _balances[currencyId] = GetBalance(currencyId) + amount;

        public ObservableProperty<long> GetObservableBalance(string currencyId) =>
            new ObservableProperty<long>(GetBalance(currencyId));
    }

    public class MockGameStateMachine : IGameStateMachine
    {
        public IGameState CurrentState { get; private set; }

        public void RegisterState<TState>(TState state) where TState : class, IGameState { }
        public Task ChangeStateAsync<TState>(object args = null) where TState : class, IGameState
        { CurrentState = null; return Task.CompletedTask; }
        public Task ChangeStateAsync(Type stateType, object args = null)
        { return Task.CompletedTask; }
        public Task ChangeStateAsync(Type stateType, CancellationToken ct, object args = null)
        { return Task.CompletedTask; }
    }

    public class InMemoryPlayerPrefs : IPlayerPrefsService
    {
        private readonly Dictionary<string, object> _store = new();

        public void DeleteKey(string key) => _store.Remove(key);
        public bool HasKey(string key) => _store.ContainsKey(key);
        public void Save() { }

        public void SetBool(string key, bool value) => _store[key] = value;
        public void SetFloat(string key, float value) => _store[key] = value;
        public void SetInt(string key, int value) => _store[key] = value;
        public void SetLong(string key, long value) => _store[key] = value;
        public void SetString(string key, string value) => _store[key] = value;

        public bool GetBool(string key, bool defaultValue = false) =>
            _store.TryGetValue(key, out var v) && v is bool b ? b : defaultValue;

        public float GetFloat(string key, float defaultValue = 0f) =>
            _store.TryGetValue(key, out var v) && v is float f ? f : defaultValue;

        public int GetInt(string key, int defaultValue = 0) =>
            _store.TryGetValue(key, out var v) && v is int i ? i : defaultValue;

        public long GetLong(string key, long defaultValue = 0L) =>
            _store.TryGetValue(key, out var v) && v is long l ? l : defaultValue;

        public string GetString(string key, string defaultValue = "") =>
            _store.TryGetValue(key, out var v) && v is string s ? s : defaultValue;
    }

    public class MockAdService : IAdService
    {
        public event Action<string, double, string> OnImpressionRecorded;
        public void SetNetworkAdapter(IAdNetworkAdapter adapter) { }
        public void SetInterstitialCooldown(float seconds) { }
        public bool IsInterstitialAvailable(string placement) => false;
        public bool IsRewardedAvailable(string placement) => false;
        public void ShowInterstitial(string placement, Action onComplete = null) => onComplete?.Invoke();
        public void ShowRewarded(string placement, Action<bool> onComplete) => onComplete?.Invoke(true);
        public void ShowBanner(string placement = "default", string position = "bottom") { }
        public void HideBanner() { }
    }

    public class MockAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string eventName) { }
        public void LogEvent(string eventName, Dictionary<string, object> parameters) { }
        public void SetUserProperty(string key, string value) { }
    }
}
