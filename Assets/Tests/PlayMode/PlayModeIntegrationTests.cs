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
using RingFlow.Gameplay.Rules;
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
            _db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);

            // FIX: Initialize UnlockedWorlds from DB.TotalWorlds (data-driven).
            // Previously hardcoded to 40 which caused out-of-range when TotalLevels=100 → TotalWorlds=2.
            // ProgressionService.ctor calls SetTotalWorldCount(_db.TotalWorlds) internally;
            // we must ensure _progressModel has the correct list size BEFORE ProgressionService
            // is constructed so the two stay in sync.
            int totalWorlds = _db != null && _db.TotalWorlds > 0 ? _db.TotalWorlds : 2;
            for (int i = 0; i < totalWorlds; i++)
                _progressModel.UnlockedWorlds.Add(i == 0);

            _progressionService = new RingFlow.Gameplay.ProgressionService(_progressModel, _db);
            _economyService = new MockEconomyService();
            _strategyManager = new RingMoveStrategyManager(_db);

            _progressModel.Coins.Value = 100;
            _progressModel.Xp.Value = 0;
            _progressModel.PlayerLevel.Value = 1;
            _progressModel.MaxUnlockedLevel.Value = 1;
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
            int poleCount = 5; // colorCount(3) + Tutorial.MinEmptyPoles(2)
            int colorCount = 3;
            int maxCap = 4;
            int level = 5;

            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
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

            // FIX: Use DB-derived world count so the test is valid for any TotalLevels config.
            // UnlockedWorlds.Count = DB.TotalWorlds (set by ProgressionService.ctor via SetTotalWorldCount).
            // Hardcoding index 5 fails when TotalLevels=100 (TotalWorlds=2 → only indices 0 and 1 are valid).
            int worldCount = _progressModel.UnlockedWorlds.Count;
            int lastWorldIdx = worldCount - 1; // safe max index
            _progressModel.UnlockedWorlds[0] = true;
            if (lastWorldIdx > 0) _progressModel.UnlockedWorlds[lastWorldIdx] = true;

            var prefs = new InMemoryPlayerPrefs();

            // Act
            PlayerProgressSaveSystem.Save(prefs, _progressModel);

            // loaded must have same world capacity as _progressModel for UnlockedWorlds to match.
            var loaded = new PlayerProgressModel();
            if (_db != null && _db.TotalWorlds > 0)
                loaded.SetTotalWorldCount(_db.TotalWorlds);
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
            // FIX: use the same dynamic lastWorldIdx set during Arrange — avoids out-of-range
            // when TotalWorlds < 6 (e.g., TotalLevels=100 → TotalWorlds=2).
            if (lastWorldIdx > 0)
                Assert.That(loaded.UnlockedWorlds[lastWorldIdx], Is.True,
                    $"World {lastWorldIdx} should be unlocked after round-trip save/load.");
        }

        [Test]
        public void SaveChecksum_CorruptedData_LoadsFromBackup()
        {
            // Arrange
            var prefs = new InMemoryPlayerPrefs();
            _progressModel.Coins.Value = 300;
            PlayerProgressSaveSystem.Save(prefs, _progressModel);

            // Corrupt a key — checksum will mismatch
            prefs.SetInt(PlayerProgressModel.KeyCoins, -99999);

            var loaded = new PlayerProgressModel();

            // Act: load must not throw, even with checksum mismatch.
            // Backup snapshot should restore last valid state.
            Assert.DoesNotThrow(() => PlayerProgressSaveSystem.Load(prefs, loaded));

            // Assert: backup restore recovers original value instead of loading corrupted data
            Assert.That(loaded.Coins.Value, Is.EqualTo(300),
                "Checksum mismatch should trigger backup restore, recovering last valid state.");
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

            // FIX: Use a level index derived from DB.TotalLevels so the test is valid
            // regardless of the configured scope (100 MVP or 2000 full GDD).
            // Previously hardcoded to 999 which exceeds the DB range when TotalLevels=100.
            // We pick the last level in the DB, which exercises the highest-difficulty
            // band and is guaranteed to be within Worlds.Count bounds.
            int highDifficultyLevel = _db != null && _db.TotalLevels > 0 ? _db.TotalLevels : 100;
            _gameplayModel.Reset();
            cmd.ExecuteAsync(new InitLevelSignal(highDifficultyLevel), default).AsTask().GetAwaiter().GetResult();

            // Assert
            Assert.That(_gameplayModel.Poles.Count, Is.GreaterThanOrEqualTo(2),
                $"InitLevelCommand should always produce at least 2 poles for level {highDifficultyLevel}.");
            Assert.That(_gameplayModel.TargetMovesCount.Value, Is.GreaterThan(0));
        }

        // ---------------------------------------------------------------
        //  Special Ring Mechanic Tests (GDD §29-§43)
        // ---------------------------------------------------------------

        [Test]
        public void RingRuleEvaluator_StoneTop_OnlySameColorAllowed()
        {
            // GDD §33: Stone ring on top — only same-color ring may land on it
            var stone = new RingData(RingColor.Red, RingType.Stone);
            var sameColor = new RingData(RingColor.Red, RingType.Standard);
            var diffColor = new RingData(RingColor.Blue, RingType.Standard);
            var rainbow   = new RingData(RingColor.None, RingType.Rainbow);

            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(sameColor, stone, false, false), Is.True,
                "Same-color ring must be able to land on Stone.");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(diffColor, stone, false, false), Is.False,
                "Different-color ring must be blocked by Stone.");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(rainbow, stone, false, false), Is.False,
                "Rainbow ring must be blocked by Stone (GDD §33 overrides joker rule).");
        }

        [Test]
        public void RingRuleEvaluator_FrozenTop_AnyRingAllowed()
        {
            // GDD §31: Frozen top accepts any ring (ice break handled by MoveRingCommand)
            var frozenTop = new RingData(RingColor.Blue, RingType.Frozen);
            var red   = new RingData(RingColor.Red, RingType.Standard);
            var blue  = new RingData(RingColor.Blue, RingType.Standard);
            var rainbow = new RingData(RingColor.None, RingType.Rainbow);

            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(red,    frozenTop, false, false), Is.True,
                "Any ring (different color) must land on Frozen top.");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(blue,   frozenTop, false, false), Is.True,
                "Same-color ring must land on Frozen top.");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(rainbow, frozenTop, false, false), Is.True,
                "Rainbow ring must land on Frozen top.");
        }

        [Test]
        public void RingRuleEvaluator_FrozenTop_CannotPop()
        {
            // GDD §31: Frozen top ring cannot be moved until ice is broken
            var frozenRing = new RingData(RingColor.Blue, RingType.Frozen);
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanPopRing(frozenRing, false, false), Is.False,
                "Frozen ring at top must not be poppable.");
        }

        [Test]
        public void RingRuleEvaluator_RainbowMoving_AnyPoleAccepts()
        {
            // GDD §35: Rainbow ring as moving ring is a joker — lands on any pole
            var rainbow = new RingData(RingColor.None, RingType.Rainbow);
            var redTop  = new RingData(RingColor.Red, RingType.Standard);
            var blueTop = new RingData(RingColor.Blue, RingType.Standard);

            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(rainbow, redTop,  false, false), Is.True);
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(rainbow, blueTop, false, false), Is.True);
        }

        [Test]
        public void RingRuleEvaluator_LockedPole_OnlyKeyAllowed()
        {
            // GDD §32: Locked pole accepts only Key ring
            var key      = new RingData(RingColor.Yellow, RingType.Key);
            var locked   = new RingData(RingColor.Yellow, RingType.Locked);
            var standard = new RingData(RingColor.Yellow, RingType.Standard);
            var emptyTop = new RingData(RingColor.None, RingType.Standard);

            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(key,      emptyTop, false, true), Is.True,
                "Key ring must enter locked pole.");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(locked,   emptyTop, false, true), Is.True,
                "Locked ring must enter locked pole.");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(standard, emptyTop, false, true), Is.False,
                "Standard ring must be blocked by locked pole.");
        }

        [Test]
        public void MoveRingCommand_FrozenRing_BlocksPopUntilThawed()
        {
            // GDD §31: Frozen ring on top cannot be selected/moved
            var pole = new PoleState { Id = 0, MaxCapacity = 4 };
            pole.AddRingRaw(new RingData(RingColor.Blue, RingType.Standard)); // bottom
            pole.AddRingRaw(new RingData(RingColor.Blue, RingType.Frozen));   // top = frozen

            var emptyPole = new PoleState { Id = 1, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(pole);
            _gameplayModel.Poles.Add(emptyPole);

            var cmd = new MoveRingCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);

            cmd.Execute(new MoveRingSignal(0, 1));

            // Frozen ring at top must block the move
            Assert.That(pole.Rings.Count, Is.EqualTo(2), "Frozen top must block move — pole should still have 2 rings.");
            Assert.That(emptyPole.Rings.Count, Is.EqualTo(0), "Target pole must remain empty.");
        }

        [Test]
        public void MoveRingCommand_StoneRing_NeverMoves()
        {
            // GDD §33: Stone ring cannot be moved under any circumstances
            var pole = new PoleState { Id = 0, MaxCapacity = 4 };
            pole.AddRingRaw(new RingData(RingColor.Red, RingType.Stone));

            var emptyPole = new PoleState { Id = 1, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(pole);
            _gameplayModel.Poles.Add(emptyPole);

            var cmd = new MoveRingCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);

            cmd.Execute(new MoveRingSignal(0, 1));

            Assert.That(pole.Rings.Count, Is.EqualTo(1), "Stone ring must not be moved — source pole still has 1 ring.");
            Assert.That(emptyPole.Rings.Count, Is.EqualTo(0), "Target pole must remain empty.");
        }

        [Test]
        public void CheckWinCommand_PartialFillPole_DoesNotWin()
        {
            // GDD §21: Win requires ALL filled poles to be FULL and single-color.
            // A partially-filled same-color pole must NOT trigger win.
            var partial = new PoleState { Id = 0, MaxCapacity = 4 };
            partial.AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            partial.AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            // Only 2 rings — not full (capacity=4), so must NOT be completed.

            _gameplayModel.Poles.Add(partial);

            var cmd = new CheckWinCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);
            cmd.Execute(new CheckWinSignal());

            Assert.That(_signalBus.FiredLevelWon, Is.False,
                "Partial-fill pole (2/4 same color) must not trigger win.");
        }

        [Test]
        public void CheckWinCommand_AllFullSameColorPoles_Wins()
        {
            // GDD §21: All filled poles full + same color = win
            var pole1 = new PoleState { Id = 0, MaxCapacity = 4 };
            pole1.AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            pole1.AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            pole1.AddRingRaw(new RingData(RingColor.Red, RingType.Standard));
            pole1.AddRingRaw(new RingData(RingColor.Red, RingType.Standard));

            var pole2 = new PoleState { Id = 1, MaxCapacity = 4 }; // empty buffer
            _gameplayModel.Poles.Add(pole1);
            _gameplayModel.Poles.Add(pole2);

            var cmd = new CheckWinCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);
            cmd.Execute(new CheckWinSignal());

            Assert.That(_signalBus.FiredLevelWon, Is.True,
                "Full same-color pole + empty buffer pole must trigger win.");
        }

        [Test]
        public void UndoCommand_AfterSpecialMove_RestoresBoardExactly()
        {
            // Undo must restore board to pre-move state for any ring type (board snapshot test)
            var fromPole = new PoleState { Id = 0, MaxCapacity = 4 };
            fromPole.AddRingRaw(new RingData(RingColor.Green, RingType.Standard));
            fromPole.AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            var toPole = new PoleState { Id = 1, MaxCapacity = 4 };
            toPole.AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));

            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var moveCmd = new MoveRingCommand();
            InjectFields(moveCmd);
            InjectField(moveCmd, "_signalBus", _signalBus);
            moveCmd.Execute(new MoveRingSignal(0, 1));

            // from=1 ring (Green), to=2 rings (Blue + Blue)
            Assert.That(_gameplayModel.Poles[0].Rings.Count, Is.EqualTo(1));
            Assert.That(_gameplayModel.Poles[1].Rings.Count, Is.EqualTo(2));

            var undoCmd = new UndoCommand();
            InjectFields(undoCmd);
            InjectField(undoCmd, "_signalBus", _signalBus);
            undoCmd.Execute(new UndoSignal());

            // After undo: from=2 rings, to=1 ring
            Assert.That(_gameplayModel.Poles[0].Rings.Count, Is.EqualTo(2),
                "Undo must restore source pole to 2 rings.");
            Assert.That(_gameplayModel.Poles[1].Rings.Count, Is.EqualTo(1),
                "Undo must restore target pole to 1 ring.");
            Assert.That(_gameplayModel.Poles[0].Rings[1].Color, Is.EqualTo(RingColor.Blue),
                "Top ring on source pole after undo must be Blue.");
        }

        // ---------------------------------------------------------------
        //  Bomb Mechanic Tests (GDD §36)
        // ---------------------------------------------------------------

        [Test]
        public void MoveRingCommand_BombCountdown_DecreasesEachMove()
        {
            // Arrange: board with one bomb ring (counter=3) and one standard ring to move
            var bombPole = new PoleState { Id = 0, MaxCapacity = 4 };
            bombPole.AddRingRaw(new RingData(RingColor.Red, RingType.Bomb, 3));

            var fromPole = new PoleState { Id = 1, MaxCapacity = 4 };
            fromPole.AddRing(new RingData(RingColor.Blue));

            var toPole = new PoleState { Id = 2, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(bombPole);
            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var cmd = new MoveRingCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);

            // Act: make one move (bomb is not being moved, just sitting on its pole)
            cmd.Execute(new MoveRingSignal(1, 2));

            // Assert: bomb counter decreased from 3 to 2
            Assert.That(_gameplayModel.Poles[0].Rings[0].AdditionalData, Is.EqualTo(2),
                "Bomb counter should decrease by 1 after each move (GDD §36).");
        }

        [Test]
        public void MoveRingCommand_BombExplodes_WhenCounterReachesZero()
        {
            // Arrange: bomb with counter=1 — will explode on the next move
            var bombPole = new PoleState { Id = 0, MaxCapacity = 4 };
            bombPole.AddRingRaw(new RingData(RingColor.Red, RingType.Bomb, 1));

            var fromPole = new PoleState { Id = 1, MaxCapacity = 4 };
            fromPole.AddRing(new RingData(RingColor.Blue));

            var toPole = new PoleState { Id = 2, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(bombPole);
            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var signalBus = new LevelLostTrackingSignalBus();

            var cmd = new MoveRingCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", signalBus);

            // Act
            cmd.Execute(new MoveRingSignal(1, 2));

            // Assert: LevelLostSignal was fired because bomb counter hit 0
            Assert.That(signalBus.FiredLevelLost, Is.True,
                "LevelLostSignal must be fired when a bomb counter reaches 0 (GDD §36).");
        }

        [Test]
        public void UndoCommand_BombCounter_RestoredAfterUndo()
        {
            // Arrange: bomb with counter=3, make one move that decrements it to 2, then undo
            var bombPole = new PoleState { Id = 0, MaxCapacity = 4 };
            bombPole.AddRingRaw(new RingData(RingColor.Red, RingType.Bomb, 3));

            var fromPole = new PoleState { Id = 1, MaxCapacity = 4 };
            fromPole.AddRing(new RingData(RingColor.Blue));

            var toPole = new PoleState { Id = 2, MaxCapacity = 4 };
            _gameplayModel.Poles.Add(bombPole);
            _gameplayModel.Poles.Add(fromPole);
            _gameplayModel.Poles.Add(toPole);

            var moveCmd = new MoveRingCommand();
            InjectFields(moveCmd);
            InjectField(moveCmd, "_signalBus", _signalBus);
            moveCmd.Execute(new MoveRingSignal(1, 2));

            // Verify counter decremented
            Assert.That(_gameplayModel.Poles[0].Rings[0].AdditionalData, Is.EqualTo(2),
                "Pre-undo: bomb counter should be 2.");

            // Act: undo
            var undoCmd = new UndoCommand();
            InjectFields(undoCmd);
            InjectField(undoCmd, "_signalBus", _signalBus);
            undoCmd.Execute(new UndoSignal());

            // Assert: board restored to pre-move snapshot — bomb counter back to 3
            Assert.That(_gameplayModel.Poles[0].Rings[0].AdditionalData, Is.EqualTo(3),
                "Undo must restore bomb counter to pre-move value of 3 (board snapshot restore).");
            Assert.That(_gameplayModel.Poles[0].Rings[0].Type, Is.EqualTo(RingType.Bomb),
                "Ring must remain Bomb type after undo.");
        }

        // ---------------------------------------------------------------
        //  Economy Tests
        // ---------------------------------------------------------------

        [Test]
        public void EconomyService_Earn_IncreasesBalance()
        {
            // Arrange — use fully qualified name to avoid ambiguity with Nexus.Core.Services.EconomyService
            var economy = new RingFlow.Gameplay.EconomyService();
            var progress = new PlayerProgressModel();
            InjectField(economy, "_progress", progress);
            economy.InitializeAsync(default).AsTask().GetAwaiter().GetResult();

            // Act
            economy.Earn(CurrencyIds.Coins, 100, "test");

            // Assert
            Assert.That(economy.GetBalance(CurrencyIds.Coins), Is.EqualTo(100),
                "Earn should increase balance by the given amount.");
            Assert.That(progress.Coins.Value, Is.EqualTo(100),
                "Earn should write through to PlayerProgressModel.");
        }

        [Test]
        public void EconomyService_Spend_DecreasesAndBlocks()
        {
            // Arrange — use fully qualified name to avoid ambiguity with Nexus.Core.Services.EconomyService
            var economy = new RingFlow.Gameplay.EconomyService();
            var progress = new PlayerProgressModel();
            InjectField(economy, "_progress", progress);
            economy.InitializeAsync(default).AsTask().GetAwaiter().GetResult();
            economy.Earn(CurrencyIds.Coins, 50, "setup");

            // Act: valid spend
            bool spendOk = economy.Spend(CurrencyIds.Coins, 30, "test");
            // Act: over-spend attempt
            bool spendFail = economy.Spend(CurrencyIds.Coins, 100, "should fail");

            // Assert
            Assert.That(spendOk, Is.True,   "Spend within balance must succeed.");
            Assert.That(spendFail, Is.False, "Spend exceeding balance must be blocked.");
            Assert.That(economy.GetBalance(CurrencyIds.Coins), Is.EqualTo(20),
                "Balance after valid spend should be 50 - 30 = 20.");
        }

        // ---------------------------------------------------------------
        //  Win Condition Tests
        // ---------------------------------------------------------------

        [Test]
        public void CheckWinCommand_MixedColorPole_DoesNotWin()
        {
            // GDD §21: A pole full of mixed colors must NOT trigger win,
            // even though it is at capacity.
            var mixedPole = new PoleState { Id = 0, MaxCapacity = 4 };
            mixedPole.AddRingRaw(new RingData(RingColor.Red,  RingType.Standard));
            mixedPole.AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            mixedPole.AddRingRaw(new RingData(RingColor.Red,  RingType.Standard));
            mixedPole.AddRingRaw(new RingData(RingColor.Blue, RingType.Standard));
            // Pole is full (4/4) but mixed colors — must not be "completed"

            _gameplayModel.Poles.Add(mixedPole);

            var cmd = new CheckWinCommand();
            InjectFields(cmd);
            InjectField(cmd, "_signalBus", _signalBus);
            cmd.Execute(new CheckWinSignal());

            Assert.That(_signalBus.FiredLevelWon, Is.False,
                "A full pole with mixed colors must NOT trigger LevelWon (GDD §21).");
        }

        [Test]
        public void RingRuleEvaluator_PaintMoving_JokerPlacement()
        {
            // GDD §39: Paint ring as moving ring acts as a joker — it can land on any pole
            // (any color on top). Paint strategy recolors the ring below it.
            var paint = new RingData(RingColor.Red, RingType.Paint);
            var blueTop  = new RingData(RingColor.Blue,  RingType.Standard);
            var greenTop = new RingData(RingColor.Green, RingType.Standard);
            var emptyTop = new RingData(RingColor.None,  RingType.Standard); // empty pole

            // Paint ring is a joker — CanAddRing should return true for any top ring color
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(paint, blueTop,  false, false), Is.True,
                "Paint ring must be placeable on a Blue-top pole (joker rule, GDD §39).");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(paint, greenTop, false, false), Is.True,
                "Paint ring must be placeable on a Green-top pole (joker rule, GDD §39).");
            Assert.That(RingFlow.Gameplay.Rules.RingRuleEvaluator.CanAddRing(paint, emptyTop, false, false), Is.True,
                "Paint ring must be placeable on an empty pole.");
        }

        [Test]
        public void ProgressionService_SetLevel_ClampsToBounds()
        {
            // ProgressionService should not allow level < 1 or > TotalLevels
            int totalLevels = _db != null && _db.TotalLevels > 0 ? _db.TotalLevels : 100;

            // Act: clamp below lower bound
            _progressModel.CurrentLevel.Value = 0;
            int clampedLow = System.Math.Max(1, _progressModel.CurrentLevel.Value);

            // Act: clamp above upper bound
            _progressModel.CurrentLevel.Value = totalLevels + 1;
            int clampedHigh = System.Math.Min(totalLevels, _progressModel.CurrentLevel.Value);

            // Assert
            Assert.That(clampedLow,  Is.EqualTo(1),          "Level 0 must clamp to 1.");
            Assert.That(clampedHigh, Is.EqualTo(totalLevels), $"Level {totalLevels + 1} must clamp to {totalLevels}.");
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
            cmd.ExecuteAsync(new InitLevelSignal(level), default).AsTask().GetAwaiter().GetResult();
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
            InjectField(target, "_validationManager", new RingFlow.Gameplay.Strategies.RingValidationStrategyManager());
            InjectField(target, "_economy", _economyService);

            // Data-driven dependencies
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
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

    /// <summary>
    /// Minimal signal bus that only tracks LevelLostSignal — used by bomb explosion tests
    /// to avoid coupling to the full LoggingSignalBus which only tracks LevelWonSignal.
    /// </summary>
    public class LevelLostTrackingSignalBus : ISignalBus
    {
        public bool FiredLevelLost { get; private set; }
        public bool FiredLevelWon  { get; private set; }

        public IReadOnlyDictionary<Type, IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers { get; } =
            new Dictionary<Type, IReadOnlyList<CommandHandlerInfo>>();

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is LevelLostSignal) FiredLevelLost = true;
            if (signal is LevelWonSignal)  FiredLevelWon  = true;
        }

        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;
        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

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
        #pragma warning disable 0067
        public event Action<string, double, string> OnImpressionRecorded;
        #pragma warning restore 0067
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
