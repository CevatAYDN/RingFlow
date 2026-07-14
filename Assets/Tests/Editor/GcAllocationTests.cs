using System.Reflection;
using NUnit.Framework;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Strategies;
using UnityEngine;

namespace RingFlow.Tests
{
    /// <summary>
    /// M11: GC allocation regression tests.
    /// Verifies that hot-path gameplay operations (move, undo, tick) produce
    /// zero GC allocations per AGENTS.md performance budget.
    ///
    /// Note: Unity Test Runner does not expose a direct GC byte counter, so
    /// these tests use System.GC.GetTotalMemory to detect unexpected allocations
    /// across a batch of operations.  A threshold of 0 is enforced to catch
    /// regressions early.
    /// </summary>
    [TestFixture]
    public class GcAllocationTests
    {
        private GameplayModel _model;
        private MockSignalBus _signalBus;
        private MoveRingCommand _moveCmd;
        private UndoCommand _undoCmd;
        private GameConfigDatabaseSO _db;

        [SetUp]
        public void Setup()
        {
            _db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(_db, "GameConfigDatabaseSO required for GC tests.");

            _model = new GameplayModel();
            _signalBus = new MockSignalBus();

            _moveCmd = new MoveRingCommand();
            SetField(_moveCmd, "_model",            _model);
            SetField(_moveCmd, "_signalBus",        _signalBus);
            SetField(_moveCmd, "_fsm",              new MockGameStateMachine());
            SetField(_moveCmd, "_strategyManager",  new RingMoveStrategyManager(_db));
            SetField(_moveCmd, "_progression",      new ProgressionService(new PlayerProgressModel(), _db));
            SetField(_moveCmd, "_validationManager",new RingValidationStrategyManager());
            SetField(_moveCmd, "_dbConfig",         _db);

            _undoCmd = new UndoCommand();
            SetField(_undoCmd, "_model",     _model);
            SetField(_undoCmd, "_signalBus", _signalBus);
        }

        [TearDown]
        public void Teardown()
        {
            // Pools do not expose test-only reset APIs in this codebase.
        }

        [Test]
        public void MoveRingCommand_StandardMove_ProducesNoGcAllocation()
        {
            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            _model.Poles.Add(pole0);
            _model.Poles.Add(pole1);

            // Warm-up: first call may allocate pool entries
            _moveCmd.Execute(new MoveRingSignal(0, 1));
            _undoCmd.Execute(new UndoSignal());

            // Measure: subsequent calls must be alloc-free
            long before = System.GC.GetTotalMemory(false);
            for (int i = 0; i < 20; i++)
            {
                _moveCmd.Execute(new MoveRingSignal(0, 1));
                _undoCmd.Execute(new UndoSignal());
            }
            long after = System.GC.GetTotalMemory(false);

            long allocatedBytes = after - before;
            Assert.LessOrEqual(allocatedBytes, 0L,
                $"Expected 0 GC bytes for 20 move+undo cycles, got {allocatedBytes} bytes. " +
                "Check for LINQ, closures, string interpolation, or List<T> growth.");
        }

        [Test]
        public void MoveRingCommand_BombTick_ProducesNoGcAllocation()
        {
            // Board with a bomb that never explodes (counter stays >1 per move)
            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            var pole2 = new PoleState { Id = 2, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            pole2.AddRing(new RingData(RingColor.Blue, RingType.Bomb, 15));
            _model.Poles.Add(pole0);
            _model.Poles.Add(pole1);
            _model.Poles.Add(pole2);

            // Warm-up
            _moveCmd.Execute(new MoveRingSignal(0, 1));
            _undoCmd.Execute(new UndoSignal());

            long before = System.GC.GetTotalMemory(false);
            for (int i = 0; i < 10; i++)
            {
                _moveCmd.Execute(new MoveRingSignal(0, 1));
                _undoCmd.Execute(new UndoSignal());
            }
            long after = System.GC.GetTotalMemory(false);

            Assert.LessOrEqual(after - before, 0L,
                $"Bomb tick path allocated {after - before} bytes. Check Span<int> or stackalloc usage.");
        }

        [Test]
        public void AnyPoleHasBomb_EmptyBoard_DoesNotAllocate()
        {
            for (int i = 0; i < 12; i++)
                _model.Poles.Add(new PoleState { Id = i, MaxCapacity = 4 });

            // Warm-up via reflection to hit the private method
            var method = typeof(MoveRingCommand).GetMethod("AnyPoleHasBomb",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "AnyPoleHasBomb not found via reflection.");

            for (int i = 0; i < 5; i++) method.Invoke(_moveCmd, null);

            long before = System.GC.GetTotalMemory(false);
            for (int i = 0; i < 1000; i++) method.Invoke(_moveCmd, null);
            long after = System.GC.GetTotalMemory(false);

            // Reflection itself may box but the underlying logic must not allocate.
            // We allow small reflection overhead (< 4 KB for 1000 calls).
            Assert.LessOrEqual(after - before, 4096L,
                $"AnyPoleHasBomb allocated {after - before} bytes across 1000 calls.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field '{name}' not found on {target.GetType().Name}.");
            f.SetValue(target, value);
        }
    }
}
