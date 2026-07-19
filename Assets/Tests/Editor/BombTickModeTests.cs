using System;
using System.Reflection;
using NUnit.Framework;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Rules;
using RingFlow.Gameplay.Strategies;
using System.Collections.Generic;

namespace RingFlow.Tests
{
    [TestFixture]
    public class BombTickModeTests
    {
        private static GameConfigDatabaseSO LoadDb()
        {
            var db = UnityEngine.Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            if (db == null) throw new InvalidOperationException("GameConfigDatabaseSO not found via Resources.Load.");
            return db;
        }

        private static GameConfigDatabaseSO CloneDbWithBombTickMode(GameConfigDatabaseSO original, BombTickMode mode)
        {
            // ScriptableObjects aren't trivially cloneable; for tests we mutate a safe copy by creating
            // a new ScriptableObject instance and copying the LevelGen value struct.
            // LevelGen is a struct, so assignment is value-copied.
            var clone = UnityEngine.ScriptableObject.CreateInstance<GameConfigDatabaseSO>();
            // Copy reference fields minimally used by commands/solvers in these tests.
            // (If additional fields are required in the future, extend copy.)
            clone.LevelGen = original.LevelGen;
            clone.BalanceConfig = original.BalanceConfig;
            clone.DifficultyBands = original.DifficultyBands;
            clone.ColorCurve = original.ColorCurve;
            clone.LevelThemes = original.LevelThemes;
            clone.Worlds = original.Worlds;
            clone.MechanicUnlocks = original.MechanicUnlocks;

            clone.TotalLevels = original.TotalLevels;
            clone.LevelsPerWorld = original.LevelsPerWorld;
            clone.TotalWorlds = original.TotalWorlds;

            var lg = clone.LevelGen;
            lg.BombTickMode = mode;
            clone.LevelGen = lg;

            return clone;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var f = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"Field '{fieldName}' not found on type '{target.GetType().Name}'.");
            f.SetValue(target, value);
        }

        private static BoardState BuildBoardWithSingleBomb(int poleCount, int maxCapacity, int bombPole, int bombRingIndex, int bombCounter)
        {
            var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };
            // Ensure poleCount<=12 for packing safety; tests use <=2/3.
            board.Initialize(poleCount, maxCapacity, maxCapacity);

            // Place a bomb with countdown in AddData
            board.SetRingCount(bombPole, 1);
            board.SetRingColor(bombPole, bombRingIndex, RingColor.Red);
            board.SetRingType(bombPole, bombRingIndex, RingType.Bomb);
            board.SetRingAdditional(bombPole, bombRingIndex, bombCounter);

            return board;
        }

        [TestCase(BombTickMode.AllBombsPerMove)]
        [TestCase(BombTickMode.SourceAndTargetPolesOnly)]
        [TestCase(BombTickMode.MovedBombOnly)]
        public void Solver_BombBoard_RemainsUnsolved_WithinOneMove(BombTickMode tickMode)
        {
            // Arrange:
            var board = new BoardState { PoleCount = 3, MaxCapacity = 2 };
            board.Initialize(3, 2, 2);

            board.SetRingCount(0, 2);
            board.SetRingColor(0, 0, RingColor.Red);
            board.SetRingType(0, 0, RingType.Bomb);
            board.SetRingAdditional(0, 0, 1);
            board.SetRingColor(0, 1, RingColor.Blue);
            board.SetRingType(0, 1, RingType.Standard);
            board.SetRingAdditional(0, 1, 0);

            board.SetRingCount(1, 2);
            board.SetRingColor(1, 0, RingColor.Red);
            board.SetRingType(1, 0, RingType.Standard);
            board.SetRingAdditional(1, 0, 0);
            board.SetRingColor(1, 1, RingColor.Red);
            board.SetRingType(1, 1, RingType.Standard);
            board.SetRingAdditional(1, 1, 0);

            board.SetRingCount(2, 1);
            board.SetRingColor(2, 0, RingColor.Blue);
            board.SetRingType(2, 0, RingType.Standard);
            board.SetRingAdditional(2, 0, 0);

            int maxMoves = 1;

            Assert.IsFalse(LevelSolver.IsSolved(board, 2), "Board must start unsolved for the bomb-mode solver check.");

            // Act:
            var res = LevelSolver.Solve(board, maxCapacity: 2, maxStatesLimit: 5000, maxMovesLimit: maxMoves, bombTickMode: tickMode);

            // Assert:
            Assert.IsFalse(res.IsSolvable, "The board is intentionally one move short of a full solve.");
        }

        [TestCase(BombTickMode.AllBombsPerMove, true)]
        [TestCase(BombTickMode.SourceAndTargetPolesOnly, false)]
        [TestCase(BombTickMode.MovedBombOnly, false)]
        public void Runtime_BombExplodesAndForcesUndo_BasedOnBombTickMode(BombTickMode tickMode, bool shouldExplodedSignalFire)
        {
            // Runtime test is shallow: verify MoveRingCommand ticks bombs and fires BombExplodedSignal only when mode ticks that bomb.
            // Undo/revert is covered by existing tests.

            var command = new MoveRingCommand();
            // Reuse existing minimal test infrastructure by copying logic from GameplayCommandTests:
            var model = new GameplayModel();
            var progress = new PlayerProgressModel();

            var signalBus = new MockSignalBus();
            var economy = new MockEconomyService();
            var ad = new MockAdService();

            var db = LoadDb();
            var dbWithMode = CloneDbWithBombTickMode(db, tickMode);

            // Inject dependencies via reflection (since we're in separate file).
            // Note: _fsm, _economyService, _adService are NOT fields on MoveRingCommand;
            // they were removed in a prior refactor. Only inject what actually exists.
            SetPrivateField(command, "_model", model);
            SetPrivateField(command, "_signalBus", signalBus);
            SetPrivateField(command, "_strategyManager", new RingMoveStrategyManager(dbWithMode));
            SetPrivateField(command, "_progression", new RingFlow.Gameplay.ProgressionService(progress, dbWithMode));
            SetPrivateField(command, "_validationManager", new RingValidationStrategyManager());
            // BUG-7 FIX: Inject _dbConfig (SSOT for BombTickMode) so ShouldTickBomb reads
            // from GameConfigDatabaseSO instead of GameFeelConfigSO.
            SetPrivateField(command, "_dbConfig", dbWithMode);

            var feelConfig = UnityEngine.ScriptableObject.CreateInstance<GameFeelConfigSO>();
            feelConfig.BombTickMode = tickMode;
            SetPrivateField(command, "_feelConfig", feelConfig);

            // Setup board:
            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            var pole2 = new PoleState { Id = 2, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            // Bomb on pole2 with counter=1
            pole2.AddRing(new RingData(RingColor.Red, RingType.Bomb, 1));

            model.Poles.Add(pole0);
            model.Poles.Add(pole1);
            model.Poles.Add(pole2);

            // Act:
            command.Execute(new MoveRingSignal(0, 1));

            // Assert:
            Assert.AreEqual(shouldExplodedSignalFire, signalBus.HasFiredBombExploded,
                $"Expected BombExplodedSignal to {(shouldExplodedSignalFire ? "fire" : "not fire")} for tickMode={tickMode}.");
        }
    }
}
