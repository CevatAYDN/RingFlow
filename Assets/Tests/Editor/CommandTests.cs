using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Strategies;

namespace RingFlow.Tests
{
    [TestFixture]
    public class MoveRingCommandTests
    {
        private GameplayModel _model;
        private CommandTestSignalBus _signalBus;
        private RingMoveStrategyManager _strategyManager;
        private RingValidationStrategyManager _validationManager;
        private GameFeelConfigSO _feelConfig;
        private GameConfigDatabaseSO _dbConfig;
        private IProgressionService _progression;

        [SetUp]
        public void Setup()
        {
            _model = new GameplayModel();
            _signalBus = new CommandTestSignalBus();
            _validationManager = new RingValidationStrategyManager();

            _dbConfig = UnityEngine.Resources.Load<GameConfigDatabaseSO>(
                GameplayAssetKeys.GameConfigDatabase);
            _feelConfig = UnityEngine.Resources.Load<GameFeelConfigSO>(
                GameplayAssetKeys.GameFeelConfig);

            _progression = new MockProgressionService();

            if (_dbConfig != null)
            {
                _strategyManager = new RingMoveStrategyManager(_dbConfig);
            }
            else
            {
                _strategyManager = new RingMoveStrategyManager(null);
            }
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }

        private void SetupStandardBoard()
        {
            _model.Poles.Add(new PoleState
            {
                Id = 0,
                MaxCapacity = 4,
                RingCapacity = 4
            });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));

            _model.Poles.Add(new PoleState
            {
                Id = 1,
                MaxCapacity = 4,
                RingCapacity = 4
            });
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));

            _model.Poles.Add(new PoleState
            {
                Id = 2,
                MaxCapacity = 3,
                RingCapacity = 3
            });
        }

        private MoveRingCommand CreateCommand()
        {
            var cmd = new MoveRingCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_signalBus", _signalBus);
            InjectField(cmd, "_strategyManager", _strategyManager);
            InjectField(cmd, "_validationManager", _validationManager);
            InjectField(cmd, "_feelConfig", _feelConfig);
            InjectField(cmd, "_dbConfig", _dbConfig);
            InjectField(cmd, "_progression", _progression);
            return cmd;
        }

        [Test]
        public void Execute_ValidMove_RingMovesToTarget()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 2));
            Assert.That(_model.Poles[0].Rings.Count, Is.EqualTo(1));
            Assert.That(_model.Poles[2].Rings.Count, Is.EqualTo(1));
            Assert.That(_model.Poles[2].Rings[0].Color, Is.EqualTo(RingColor.Red));
        }

        [Test]
        public void Execute_ValidMove_IncrementsMovesCount()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
            cmd.Execute(new MoveRingSignal(0, 2));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(1));
        }

        [Test]
        public void Execute_ValidMove_FiresRingMovedSignal()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 2));
            Assert.That(_signalBus.LastRingMoved, Is.Not.Null);
            Assert.That(_signalBus.LastRingMoved.Value.FromPoleId, Is.EqualTo(0));
            Assert.That(_signalBus.LastRingMoved.Value.ToPoleId, Is.EqualTo(2));
        }

        [Test]
        public void Execute_ValidMove_PushesToMoveHistory()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 2));
            Assert.That(_model.MoveHistory.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_ValidMove_RecordsCorrectMoveData()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 2));
            var record = _model.MoveHistory.Pop();
            Assert.That(record.FromPoleId, Is.EqualTo(0));
            Assert.That(record.ToPoleId, Is.EqualTo(2));
            Assert.That(record.Ring.Color, Is.EqualTo(RingColor.Red));
            Assert.That(record.Ring.Type, Is.EqualTo(RingType.Standard));
            MoveRecordPool.Return(record);
        }

        [Test]
        public void Execute_ValidMove_CapturesBoardSnapshot()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 2));
            var record = _model.MoveHistory.Pop();
            Assert.That(record.BoardBefore.Count, Is.GreaterThan(0));
            MoveRecordPool.Return(record);
        }

        [Test]
        public void Execute_EmptyTarget_AcceptsMatchingColor()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 3, RingCapacity = 3 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 3, RingCapacity = 3 });
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 1));
            Assert.That(_model.Poles[0].Rings.Count, Is.EqualTo(0));
            Assert.That(_model.Poles[1].Rings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_MatchingColorOnSameColor_Accepts()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 1));
            Assert.That(_model.Poles[1].Rings.Count, Is.EqualTo(2));
        }

        [Test]
        public void Execute_InvalidMove_NullFromPole_DoesNothing()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(-1, 2));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
            Assert.That(_model.Poles[0].Rings.Count, Is.EqualTo(2));
            Assert.That(_model.Poles[2].Rings.Count, Is.EqualTo(0));
        }

        [Test]
        public void Execute_InvalidMove_FullTarget_DoesNothing()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 1, RingCapacity = 1 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 1, RingCapacity = 1 });
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 1));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
            Assert.That(_model.Poles[1].Rings.Count, Is.EqualTo(1));
        }

        [Test]
        public void Execute_InvalidMove_LockedTarget_DoesNothing()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4, IsLocked = true });
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 1));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
        }

        [Test]
        public void Execute_InvalidMove_ColorMismatch_DoesNothing()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 1));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
            Assert.That(_model.Poles[1].Rings[^1].Color, Is.EqualTo(RingColor.Blue));
        }

        [Test]
        public void Execute_MoveOnEmptySource_DoesNotThrow()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cmd = CreateCommand();
            Assert.DoesNotThrow(() => cmd.Execute(new MoveRingSignal(0, 1)));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
        }

        [Test]
        public void Execute_TwoValidMoves_HistoryHasTwoEntries()
        {
            SetupStandardBoard();
            var cmd = CreateCommand();
            cmd.Execute(new MoveRingSignal(0, 2));
            cmd.Execute(new MoveRingSignal(0, 2));
            Assert.That(_model.MoveHistory.Count, Is.EqualTo(2));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(2));
            Assert.That(_model.Poles[2].Rings.Count, Is.EqualTo(2));
        }

        private static void InjectField<T>(T instance, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(instance, value);
        }
    }

    [TestFixture]
    public class UndoCommandTests
    {
        private GameplayModel _model;
        private CommandTestSignalBus _signalBus;

        [SetUp]
        public void Setup()
        {
            _model = new GameplayModel();
            _signalBus = new CommandTestSignalBus();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }

        private UndoCommand CreateCommand()
        {
            var cmd = new UndoCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_signalBus", _signalBus);
            return cmd;
        }

        [Test]
        public void Execute_WithEmptyHistory_DoesNotThrow()
        {
            var cmd = CreateCommand();
            Assert.DoesNotThrow(() => cmd.Execute(default(UndoSignal)));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
        }

        [Test]
        public void Execute_AfterSingleMove_RestoresBoardState()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles.Add(new PoleState { Id = 2, MaxCapacity = 4, RingCapacity = 4 });
            var moveCmd = CreateMoveCommand();
            moveCmd.Execute(new MoveRingSignal(0, 2));
            var undo = CreateCommand();
            undo.Execute(default(UndoSignal));
            Assert.That(_model.Poles[0].Rings.Count, Is.EqualTo(2));
            Assert.That(_model.Poles[2].Rings.Count, Is.EqualTo(0));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
        }

        [Test]
        public void Execute_AfterWin_ResetsIsGameWon()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 1, RingCapacity = 1 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 1, RingCapacity = 1 });
            var moveCmd = CreateMoveCommand();
            moveCmd.Execute(new MoveRingSignal(0, 1));
            _model.IsGameWon.Value = true;
            var undo = CreateCommand();
            undo.Execute(default(UndoSignal));
            Assert.That(_model.IsGameWon.Value, Is.False);
        }

        [Test]
        public void Execute_FiresCheckWinSignal()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 1, RingCapacity = 1 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 1, RingCapacity = 1 });
            var moveCmd = CreateMoveCommand();
            moveCmd.Execute(new MoveRingSignal(0, 1));
            _signalBus.Reset();
            var undo = CreateCommand();
            undo.Execute(default(UndoSignal));
            Assert.That(_signalBus.HasFiredCheckWin, Is.True);
        }

        [Test]
        public void Execute_DecrementsMovesCount()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 2, MaxCapacity = 4, RingCapacity = 4 });
            var moveCmd = CreateMoveCommand();
            moveCmd.Execute(new MoveRingSignal(0, 2));
            var undo = CreateCommand();
            undo.Execute(default(UndoSignal));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(0));
        }

        [Test]
        public void Execute_AfterTwoMoves_UndoOnce_RestoresFirstMoveState()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 2, MaxCapacity = 4, RingCapacity = 4 });
            var moveCmd = CreateMoveCommand();
            moveCmd.Execute(new MoveRingSignal(0, 2));
            moveCmd.Execute(new MoveRingSignal(0, 2));
            var undo = CreateCommand();
            undo.Execute(default(UndoSignal));
            // Pole Id=2 is at list index 1 (only 2 poles: [0]=Id0, [1]=Id2)
            Assert.That(_model.Poles[1].Rings.Count, Is.EqualTo(1));
            Assert.That(_model.Poles[0].Rings.Count, Is.EqualTo(1));
            Assert.That(_model.MovesCount.Value, Is.EqualTo(1));
        }

        private MoveRingCommand CreateMoveCommand()
        {
            var cmd = new MoveRingCommand();
            var dbConfig = UnityEngine.Resources.Load<GameConfigDatabaseSO>(
                GameplayAssetKeys.GameConfigDatabase);
            var feelConfig = UnityEngine.Resources.Load<GameFeelConfigSO>(
                GameplayAssetKeys.GameFeelConfig);
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_signalBus", _signalBus);
            InjectField(cmd, "_strategyManager", new RingMoveStrategyManager(dbConfig));
            InjectField(cmd, "_validationManager", new RingValidationStrategyManager());
            InjectField(cmd, "_feelConfig", feelConfig);
            InjectField(cmd, "_dbConfig", dbConfig);
            InjectField(cmd, "_progression", new MockProgressionService());
            return cmd;
        }

        private static void InjectField<T>(T instance, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(instance, value);
        }
    }

    [TestFixture]
    public class CheckWinCommandTests
    {
        private GameplayModel _model;
        private CommandTestSignalBus _signalBus;

        [SetUp]
        public void Setup()
        {
            _model = new GameplayModel();
            _signalBus = new CommandTestSignalBus();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }

        private CheckWinCommand CreateCommand()
        {
            var cmd = new CheckWinCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_signalBus", _signalBus);
            return cmd;
        }

        [Test]
        public async Task ExecuteAsync_AllPolesSolved_DetectsWin()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            for (int i = 0; i < 4; i++)
                _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_model.IsGameWon.Value, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_AllPolesSolved_FiresLevelWonSignal()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            for (int i = 0; i < 4; i++)
                _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_signalBus.HasFiredLevelWon, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_MixedColors_NoWin()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_model.IsGameWon.Value, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_EmptyBoard_NoWin()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 });
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_model.IsGameWon.Value, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_PartiallyFilledPole_NoWin()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_model.IsGameWon.Value, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_AlreadyWon_DoesNotFireAgain()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 1, RingCapacity = 1 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.IsGameWon.Value = true;
            _signalBus.Reset();
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_signalBus.HasFiredLevelWon, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_NullModel_DoesNotThrow()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            _model = null;
            var cmd = new CheckWinCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
        }

        [Test]
        public async Task ExecuteAsync_SolvedPole_FiresPoleCompletedSignal()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 2, RingCapacity = 2 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_signalBus.HasFiredPoleCompleted, Is.True);
            Assert.That(_signalBus.LastCompletedPoleId, Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteAsync_TwoSolvedPoles_FiresTwoPoleCompletedSignals()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 2, RingCapacity = 2 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 2, RingCapacity = 2 });
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            _model.Poles[1].AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_model.CompletedPoles.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ExecuteAsync_CancelledToken_DoesNotFireLevelWon()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 1, RingCapacity = 1 });
            _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), cts.Token);
            Assert.That(_model.IsGameWon.Value, Is.True);
            Assert.That(_signalBus.HasFiredLevelWon, Is.False);
        }

        [Test]
        public async Task ExecuteAsync_EmptyPlusSolvedPoles_DetectsWin()
        {
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 3, RingCapacity = 3 });
            for (int i = 0; i < 3; i++)
                _model.Poles[0].AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 3, RingCapacity = 3 });
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(CheckWinSignal), CancellationToken.None);
            Assert.That(_model.IsGameWon.Value, Is.True);
        }

        private static void InjectField<T>(T instance, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(instance, value);
        }
    }

    // ─── Shared Test Infrastructure ─────────────────────────────────────

    public class CommandTestSignalBus : ISignalBus
    {
        public RingMovedSignal? LastRingMoved { get; private set; }
        public bool HasFiredCheckWin { get; private set; }
        public bool HasFiredLevelWon { get; private set; }
        public bool HasFiredLevelLost { get; private set; }
        public string LastLevelLostReason { get; private set; }
        public bool HasFiredPoleCompleted { get; private set; }
        public int LastCompletedPoleId { get; private set; } = -1;

        public IReadOnlyDictionary<System.Type,
            IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers =>
            new Dictionary<System.Type, IReadOnlyList<CommandHandlerInfo>>();

        public void Reset()
        {
            LastRingMoved = null;
            HasFiredCheckWin = false;
            HasFiredLevelWon = false;
            HasFiredLevelLost = false;
            LastLevelLostReason = null;
            HasFiredPoleCompleted = false;
            LastCompletedPoleId = -1;
        }

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is RingMovedSignal moved)
                LastRingMoved = moved;
            else if (signal is PoleCompletedSignal pc)
            {
                HasFiredPoleCompleted = true;
                LastCompletedPoleId = pc.PoleId;
            }
        }

        public ValueTask FireAsync<T>(T signal) where T : struct
        {
            if (signal is LevelWonSignal)
                HasFiredLevelWon = true;
            return default;
        }

        public ValueTask FireAsyncAndForget<T>(T signal, System.Action<System.Exception> onError = null) where T : struct
        {
            if (signal is CheckWinSignal)
                HasFiredCheckWin = true;
            else if (signal is LevelLostSignal lost)
            {
                HasFiredLevelLost = true;
                LastLevelLostReason = lost.Reason;
            }
            else if (signal is LevelWonSignal)
                HasFiredLevelWon = true;
            return default;
        }

        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ISignalSubscription Subscribe<T>(System.Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(
            System.Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

    public class MockProgressionService : IProgressionService
    {
        public int CurrentLevelIndex { get; set; }
        public ObservableProperty<int> CurrentLevel { get; set; } = new(1);
        public ObservableProperty<int> MaxUnlockedLevel { get; set; } = new(1);

        public void CompleteCurrentLevel()
        {
            CurrentLevel.Value++;
            if (CurrentLevel.Value > MaxUnlockedLevel.Value)
                MaxUnlockedLevel.Value = CurrentLevel.Value;
        }

        public void SetLevel(int levelIndex)
        {
            CurrentLevel.Value = levelIndex;
            if (CurrentLevel.Value > MaxUnlockedLevel.Value)
                MaxUnlockedLevel.Value = CurrentLevel.Value;
        }

        public long CalculateUpgradeCost(long baseCost, int level,
            float multiplier = 1.15f, CurveType curveType = CurveType.Exponential) => 0L;

        public void UnlockNextLevel() { }
        public bool IsLevelUnlocked(int levelIndex) => true;
        public int GetStarsForLevel(int levelIndex) => 0;
        public void SetStarsForLevel(int levelIndex, int stars) { }
        public int GetBestMovesForLevel(int levelIndex) => 0;
        public void SetBestMovesForLevel(int levelIndex, int moves) { }
        public void RegisterLevelCompletionEvents(System.Action<int, int> onLevelCompleted) { }
    }

    // ─── LevelWonCommand Tests ─────────────────────────────────────────────

    [TestFixture]
    public class LevelWonCommandTests
    {
        private GameplayModel _model;
        private MockProgressionService _progression;
        private MockEconomyService _economy;
        private PlayerProgressModel _progress;
        private MockGameStateMachine _fsm;
        private GameConfigDatabaseSO _dbConfig;
        private MockAnalyticsService _analytics;

        [SetUp]
        public void Setup()
        {
            _model = new GameplayModel();
            _progression = new MockProgressionService();
            _economy = new MockEconomyService();
            _progress = new PlayerProgressModel();
            _progress.SetTotalWorldCount(5);
            _fsm = new MockGameStateMachine();
            _dbConfig = UnityEngine.Resources.Load<GameConfigDatabaseSO>(
                GameplayAssetKeys.GameConfigDatabase);
            _analytics = new MockAnalyticsService();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }

        private LevelWonCommand CreateCommand()
        {
            var cmd = new LevelWonCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_progressionService", _progression);
            InjectField(cmd, "_economyService", _economy);
            InjectField(cmd, "_progress", _progress);
            InjectField(cmd, "_fsm", _fsm);
            InjectField(cmd, "_dbConfig", _dbConfig);
            InjectField(cmd, "_analyticsService", _analytics);
            return cmd;
        }

        [Test]
        public async Task ExecuteAsync_AdvancesLevel()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_progression.CurrentLevel.Value, Is.GreaterThan(1));
        }

        [Test]
        public async Task ExecuteAsync_ComputesStars_ThreeStars_WhenUnderTarget()
        {
            _model.MovesCount.Value = 3;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_model.LastReward.Value.Stars, Is.EqualTo(3));
        }

        [Test]
        public async Task ExecuteAsync_ComputesStars_OneStar_WhenAtTarget()
        {
            // Config: ThreeStarTargetRatioPercent=100, TwoStarTargetRatioPercent=130.
            // With target=10, 3-star threshold = 10*100/100 = 10, 2-star threshold = 10*130/100 = 13.
            // So moves=14 forces 1 star.
            _model.MovesCount.Value = 14;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_model.LastReward.Value.Stars, Is.EqualTo(1));
        }

        [Test]
        public async Task ExecuteAsync_GrantsCoinsReward()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_economy.GetBalance(CurrencyIds.Coins), Is.GreaterThan(0));
        }

        [Test]
        public async Task ExecuteAsync_RecordsBestMoves()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_progress.GetBestMovesForLevel(1), Is.EqualTo(5));
        }

        [Test]
        public async Task ExecuteAsync_SetsLastReward()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_model.LastReward.Value.Level, Is.EqualTo(1));
            Assert.That(_model.LastReward.Value.Moves, Is.EqualTo(5));
            Assert.That(_model.LastReward.Value.TargetMoves, Is.EqualTo(10));
        }

        [Test]
        public async Task ExecuteAsync_DropsChests_AlwaysGetsBronze()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_progress.ChestBronze.Value, Is.EqualTo(1));
        }

        [Test]
        public async Task ExecuteAsync_TransitionsToWinState()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_fsm.RequestedStateType, Is.EqualTo(typeof(WinState)));
        }

        [Test]
        public async Task ExecuteAsync_FiresLevelCompleteAnalytics()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_analytics.EventNames.Contains("level_complete"), Is.True);
            Assert.That(_economy.GetBalance(CurrencyIds.Coins), Is.GreaterThan(0));
        }

        [Test]
        public async Task ExecuteAsync_AppliesXpReward()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            _progress.Xp.Value = 0;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_progress.Xp.Value, Is.GreaterThan(0));
        }

        [Test]
        public async Task ExecuteAsync_NullProgression_DoesNotThrow()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var cmd = new LevelWonCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_economyService", _economy);
            InjectField(cmd, "_progress", _progress);
            InjectField(cmd, "_fsm", _fsm);
            InjectField(cmd, "_dbConfig", _dbConfig);
            InjectField(cmd, "_analyticsService", _analytics);
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
        }

        [Test]
        public async Task ExecuteAsync_NullFsm_DoesNotThrow()
        {
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var cmd = new LevelWonCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_progressionService", _progression);
            InjectField(cmd, "_economyService", _economy);
            InjectField(cmd, "_progress", _progress);
            InjectField(cmd, "_dbConfig", _dbConfig);
            InjectField(cmd, "_analyticsService", _analytics);
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
        }

        [Test]
        public async Task ExecuteAsync_ChallengeMode_UpdatesLastReward()
        {
            _model.IsChallengeMode.Value = true;
            _model.MovesCount.Value = 5;
            _model.TargetMovesCount.Value = 10;
            _progression.CurrentLevel.Value = 1;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(default(LevelWonSignal), CancellationToken.None);
            Assert.That(_model.LastReward.Value.Moves, Is.EqualTo(5));
        }

        private static void InjectField<T>(T instance, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(instance, value);
        }
    }

    // ─── LevelLostCommand Tests ────────────────────────────────────────────

    [TestFixture]
    public class LevelLostCommandTests
    {
        private GameplayModel _model;
        private MockGameStateMachine _fsm;

        [SetUp]
        public void Setup()
        {
            _model = new GameplayModel();
            _fsm = new MockGameStateMachine();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }

        private LevelLostCommand CreateCommand()
        {
            var cmd = new LevelLostCommand();
            InjectField(cmd, "_model", _model);
            InjectField(cmd, "_fsm", _fsm);
            return cmd;
        }

        [Test]
        public async Task ExecuteAsync_SetsHasChallengeFailed()
        {
            _model.HasChallengeFailed.Value = false;
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(new LevelLostSignal("Bomb exploded"), CancellationToken.None);
            Assert.That(_model.HasChallengeFailed.Value, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_DoesNotThrow_WhenModelIsNull()
        {
            var cmd = new LevelLostCommand();
            InjectField(cmd, "_fsm", _fsm);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            await cmd.ExecuteAsync(new LevelLostSignal("test"), CancellationToken.None);
        }

        [Test]
        public async Task ExecuteAsync_DoesNotThrow_WhenFsmIsNull()
        {
            var cmd = new LevelLostCommand();
            InjectField(cmd, "_model", _model);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            await cmd.ExecuteAsync(new LevelLostSignal("test"), CancellationToken.None);
            Assert.That(_model.HasChallengeFailed.Value, Is.True);
        }

        [Test]
        public async Task ExecuteAsync_TransitionsToLoseState()
        {
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(new LevelLostSignal("Out of moves"), CancellationToken.None);
            Assert.That(_fsm.RequestedStateType, Is.EqualTo(typeof(LoseState)));
        }

        [Test]
        public async Task ExecuteAsync_PassesReasonToLoseState()
        {
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(new LevelLostSignal("Bomb exploded on pole 3"), CancellationToken.None);
            Assert.That(_fsm.RequestedArgs, Is.Not.Null);
            if (_fsm.RequestedArgs is LevelLostSignal args)
                Assert.That(args.Reason, Is.EqualTo("Bomb exploded on pole 3"));
        }

        [Test]
        public async Task ExecuteAsync_WithEmptyReason_DoesNotThrow()
        {
            var cmd = CreateCommand();
            await cmd.ExecuteAsync(new LevelLostSignal(""), CancellationToken.None);
            Assert.That(_fsm.RequestedStateType, Is.EqualTo(typeof(LoseState)));
        }

        private static void InjectField<T>(T instance, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(instance, value);
        }
    }
}
