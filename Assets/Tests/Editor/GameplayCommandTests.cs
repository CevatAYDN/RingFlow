using System;
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
    public class GameplayCommandTests
    {
        private GameplayModel _gameplayModel;
        private PlayerProgressModel _progressModel;
        private MockSignalBus _signalBus;
        private MockEconomyService _economyService;
        private MockAdService _adService;
        private RingFlow.Gameplay.ProgressionService _progressionService;

        [SetUp]
        public void Setup()
        {
            _gameplayModel = new GameplayModel();
            _progressModel = new PlayerProgressModel();
            _signalBus = new MockSignalBus();
            _economyService = new MockEconomyService();
            _adService = new MockAdService();
            _progressionService = new RingFlow.Gameplay.ProgressionService(_progressModel);

            _progressModel.Coins.Value = 100;
            _progressModel.FreeUndosUsedThisSession.Value = 0;
            _progressModel.Xp.Value = 0;
            _progressModel.PlayerLevel.Value = 1;

            for (int i = 0; i < 40; i++)
            {
                _progressModel.UnlockedWorlds.Add(i == 0);
            }

            var levelWonCommand = new LevelWonCommand();
            InjectDependencies(levelWonCommand);
            _signalBus.RegisterHandler<LevelWonSignal>(levelWonCommand.Execute);
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
                    else if (f.FieldType == typeof(IProgressionService))
                        f.SetValue(target, _progressionService);
                    else if (f.FieldType == typeof(RingMoveStrategyManager))
                        f.SetValue(target, new RingMoveStrategyManager());
                    else if (f.FieldType == typeof(Nexus.Core.FSM.IGameStateMachine))
                        f.SetValue(target, new MockGameStateMachine());
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

            // Reveal sonrası Top ring Standard olmalı ve Mystery'nin gerçek rengini almalı
            Assert.AreEqual(RingType.Standard, pole0.TopRing.Type);
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

        [Test]
        public void MoveRingCommand_HandlesChainRingsTogether()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            var pole2 = new PoleState { Id = 2, MaxCapacity = 4 };

            // Create two linked Chain rings (AdditionalData = 99) on separate poles
            pole0.AddRing(new RingData(RingColor.Yellow, RingType.Chain, 99));
            pole1.AddRing(new RingData(RingColor.Yellow, RingType.Chain, 99));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);
            _gameplayModel.Poles.Add(pole2);

            // Moving chain ring on pole0 to pole2 should automatically pull the other chain ring on pole1
            command.Execute(new MoveRingSignal(0, 2));

            Assert.AreEqual(0, pole0.Rings.Count);
            Assert.AreEqual(0, pole1.Rings.Count);
            Assert.AreEqual(2, pole2.Rings.Count);
            Assert.AreEqual(RingColor.Yellow, pole2.Rings[0].Color);
            Assert.AreEqual(RingColor.Yellow, pole2.Rings[1].Color);
        }

        [Test]
        public void MoveRingCommand_HandlesMagnetPullingSameColors()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            var pole2 = new PoleState { Id = 2, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Magnet));
            pole1.AddRing(new RingData(RingColor.Red, RingType.Standard));
            // Magnet pulls Red to pole2 (which must start empty)

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);
            _gameplayModel.Poles.Add(pole2);

            // Move Magnet Red to empty pole2
            command.Execute(new MoveRingSignal(0, 2));

            // It should pull the other Red ring from pole1 to pole2
            Assert.AreEqual(0, pole1.Rings.Count);
            Assert.AreEqual(2, pole2.Rings.Count); // Magnet + pulled Standard
            Assert.AreEqual(RingColor.Red, pole2.Rings[0].Color);
            Assert.AreEqual(RingColor.Red, pole2.Rings[1].Color);
        }

        [Test]
        public void MoveRingCommand_BombExplosionDoesNotEndClassicMode()
        {
            // FIX P1.Bomb — classic-mode level no longer transitions to GameOver when a bomb
            // detonates. Per GDD §3, GameOver is reserved for Challenge mode. The bomb ticks
            // down, the explosion signal still fires (HUD/analytics reaction), but the move
            // history is preserved so the player can undo the move (paying coins) and recover
            // the lost ring.
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            // Bomb with counter = 1 (must be Red color so stack is valid)
            pole1.AddRing(new RingData(RingColor.Red, RingType.Bomb, 1));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Snapshot move-count so we can assert nothing got reset to 0.
            int movesBefore = _gameplayModel.MovesCount.Value;

            // Execute move
            command.Execute(new MoveRingSignal(0, 1));

            // Bomb exploded signal still fires (UI + analytics), but IsGameWon is not flipped.
            Assert.IsTrue(_signalBus.HasFiredBombExploded);
            Assert.IsFalse(_gameplayModel.IsGameWon.Value);

            // Move history was pushed (so the player can pay coins to undo and recover the bomb).
            Assert.AreEqual(1, _gameplayModel.MoveHistory.Count);
            Assert.AreEqual(movesBefore + 1, _gameplayModel.MovesCount.Value);
        }

        [Test]
        public void UndoCommand_RestoresBombCountersFromSnapshot()
        {
            var moveCommand = new MoveRingCommand();
            InjectDependencies(moveCommand);

            var undoCommand = new UndoCommand();
            InjectDependencies(undoCommand);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            // Bomb with counter = 3
            pole1.AddRing(new RingData(RingColor.Red, RingType.Bomb, 3));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Execute move - bomb should tick from 3 to 2
            moveCommand.Execute(new MoveRingSignal(0, 1));
            Assert.AreEqual(2, pole1.Rings[0].AdditionalData);
            Assert.AreEqual(1, _gameplayModel.MoveHistory.Count);

            // Undo - bomb counter should restore to 3
            undoCommand.Execute(new UndoSignal());
            Assert.AreEqual(3, pole1.Rings[0].AdditionalData);
            Assert.AreEqual(0, _gameplayModel.MoveHistory.Count);
        }

        [Test]
        public void UndoCommand_SnapshotOnlyCapturesExistingBombs()
        {
            var moveCommand = new MoveRingCommand();
            InjectDependencies(moveCommand);

            var undoCommand = new UndoCommand();
            InjectDependencies(undoCommand);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            moveCommand.Execute(new MoveRingSignal(0, 1));
            var lastRecord = _gameplayModel.MoveHistory.Pop();
            Assert.IsNotNull(lastRecord, "Move should have been recorded.");
            Assert.AreEqual(0, lastRecord.BombCountersBeforeTick.Count);

            // Undo should work without error
            _gameplayModel.MoveHistory.Push(new MoveRecord(0, 1, new RingData(RingColor.Red)));
            undoCommand.Execute(new UndoSignal());
            Assert.AreEqual(0, _gameplayModel.MoveHistory.Count);
        }

        [Test]
        public void MoveRingCommand_HandlesFrozenIceBreaking()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Green, RingType.Standard));
            pole1.AddRing(new RingData(RingColor.Green, RingType.Frozen));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Popping Frozen directly should fail
            Assert.IsFalse(pole1.CanPopRing());

            // Placing standard Green on top of Frozen Green should break ice (turn it Standard)
            command.Execute(new MoveRingSignal(0, 1));

            Assert.AreEqual(2, pole1.Rings.Count);
            Assert.AreEqual(RingType.Standard, pole1.Rings[0].Type); // Ice broken!
            Assert.AreEqual(RingType.Standard, pole1.Rings[1].Type);
            Assert.IsTrue(_signalBus.HasFiredBreakIce);
        }

        [Test]
        public void MoveRingCommand_HandlesLockedPoleAndKey()
        {
            var command = new MoveRingCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4, IsLocked = true };

            // Source has Key (Locked) Ring
            pole0.AddRing(new RingData(RingColor.Yellow, RingType.Locked));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Try placing key on locked pole
            Assert.IsTrue(pole1.CanAddRing(pole0.TopRing));

            command.Execute(new MoveRingSignal(0, 1));

            // Target pole should unlock
            Assert.IsFalse(pole1.IsLocked);
            Assert.IsTrue(_signalBus.HasFiredUnlockPole);
        }

        [Test]
        public void MoveRingCommand_HandlesStoneBlocksPopAndPlacement()
        {
            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            pole0.AddRing(new RingData(RingColor.Red, RingType.Stone));
            pole1.AddRing(new RingData(RingColor.Red, RingType.Standard));

            // Stone rings cannot be popped
            Assert.IsFalse(pole0.CanPopRing());

            // Placing standard on top of Stone should be invalid
            Assert.IsFalse(pole0.CanAddRing(pole1.TopRing));
        }

        [Test]
        public void CheckWinCommand_SavesProgressionAndGrantsCoinAndXpReward()
        {
            var command = new CheckWinCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            // Fill pole0 with same colors
            for (int i = 0; i < 4; i++)
            {
                pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            }

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            _progressModel.CurrentLevel.Value = 1;
            _progressModel.PlayerLevel.Value = 1;
            _progressModel.Xp.Value = 90; // 10 XP away from level up
            _progressModel.Coins.Value = 100;
            _economyService.CoinsBalance = 100;

            command.Execute(new CheckWinSignal());

            // Check Win
            Assert.IsTrue(_gameplayModel.IsGameWon.Value);
            
            // Check Progression level completed (+1)
            Assert.AreEqual(2, _progressModel.CurrentLevel.Value);

            // Check World unlock (Level 2 is World 0, still unlocked)
            Assert.IsTrue(_progressModel.UnlockedWorlds[0]);

            // Check XP and Player Level Up (earned 10 XP -> XP becomes 100 -> levels up -> level becomes 2 -> XP reset to 0 -> earns 100 coins)
            Assert.AreEqual(2, _progressModel.PlayerLevel.Value);
            Assert.AreEqual(0, _progressModel.Xp.Value);

            // Coins reward: level completion reward (50 + 1*10 = 60) + level up reward (100) = 160 coins earned
            // total: 100 + 160 = 260
            Assert.AreEqual(260, _economyService.CoinsBalance);
        }

        [Test]
        public void CheckWinCommand_HandlesMultipleLevelUpsAndXpOverflow()
        {
            var command = new CheckWinCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            // Fill pole0 with same colors to trigger win
            for (int i = 0; i < 4; i++)
            {
                pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));
            }

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            _progressModel.CurrentLevel.Value = 1;
            _progressModel.PlayerLevel.Value = 1;
            
            // Level 1: 100 XP to next level
            // Level 2: 250 XP to next level
            // Level 3: 500 XP to next level
            // Set initial XP to 340. We earn 10 XP on level complete. Total = 350 XP.
            // 350 >= 100 -> level up to 2 (remains 250 XP).
            // 250 >= 250 -> level up to 3 (remains 0 XP).
            // So we should end at PlayerLevel 3, XP 0, and earn two level-up coin rewards (+200 coins).
            _progressModel.Xp.Value = 340;
            _progressModel.Coins.Value = 100;
            _economyService.CoinsBalance = 100;

            command.Execute(new CheckWinSignal());

            Assert.IsTrue(_gameplayModel.IsGameWon.Value);
            Assert.AreEqual(3, _progressModel.PlayerLevel.Value);
            Assert.AreEqual(0, _progressModel.Xp.Value);

            // Coins reward: Level completion (60) + 2 level up rewards (200) = 260
            // total: 100 + 260 = 360
            Assert.AreEqual(360, _economyService.CoinsBalance);
        }

        [Test]
        public void SelectPoleCommand_SwitchesSelectionToValidClickedPoleIfMoveInvalid()
        {
            var command = new SelectPoleCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));

            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            pole1.AddRing(new RingData(RingColor.Blue, RingType.Standard)); // Color mismatch

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Select pole 0 first
            command.Execute(new SelectPoleSignal(0));
            Assert.AreEqual(0, _gameplayModel.SelectedPoleId.Value);

            // Click pole 1. Move is invalid, but pole 1 can pop a ring, so selection should switch to pole 1
            command.Execute(new SelectPoleSignal(1));
            Assert.AreEqual(1, _gameplayModel.SelectedPoleId.Value);
        }

        [Test]
        public void SelectPoleCommand_GhostRingSolidificationFiresRevealSignal()
        {
            var command = new SelectPoleCommand();
            InjectDependencies(command);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Green, RingType.Ghost));
            _gameplayModel.Poles.Add(pole0);

            command.Execute(new SelectPoleSignal(0));

            Assert.AreEqual(RingType.Standard, pole0.TopRing.Type);
            Assert.IsTrue(_signalBus.HasFiredRevealMystery);
        }

        [Test]
        public void MoveRingCommand_PaintRingCanBePlacedOnDifferentColor()
        {
            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            pole0.AddRing(new RingData(RingColor.Red, RingType.Paint)); // Paint ring

            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };
            pole1.AddRing(new RingData(RingColor.Blue, RingType.Standard)); // Different color

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Should be allowed in PoleState
            Assert.IsTrue(pole1.CanAddRing(pole0.TopRing));
        }

        [Test]
        public void UndoCommand_CorrectlyRestoresMovedBombCounter()
        {
            var moveCommand = new MoveRingCommand();
            InjectDependencies(moveCommand);

            var undoCommand = new UndoCommand();
            InjectDependencies(undoCommand);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            // Bomb on pole0
            pole0.AddRing(new RingData(RingColor.Red, RingType.Bomb, 3));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Move the Bomb to pole1
            moveCommand.Execute(new MoveRingSignal(0, 1));
            
            // Bomb should tick from 3 to 2 on pole1
            Assert.AreEqual(2, pole1.TopRing.AdditionalData);

            // Undo the move
            undoCommand.Execute(new UndoSignal());

            // Bomb should be back on pole0, with counter restored to 3
            Assert.AreEqual(1, pole0.Rings.Count);
            Assert.AreEqual(0, pole1.Rings.Count);
            Assert.AreEqual(RingType.Bomb, pole0.TopRing.Type);
            Assert.AreEqual(3, pole0.TopRing.AdditionalData);
        }

        [Test]
        public void UndoCommand_RestoresExplodedBombRingsAndCounters()
        {
            var moveCommand = new MoveRingCommand();
            InjectDependencies(moveCommand);

            var undoCommand = new UndoCommand();
            InjectDependencies(undoCommand);

            var pole0 = new PoleState { Id = 0, MaxCapacity = 4 };
            var pole1 = new PoleState { Id = 1, MaxCapacity = 4 };

            // Bomb on pole1 with counter = 1 (will explode on next move)
            pole1.AddRing(new RingData(RingColor.Red, RingType.Bomb, 1));
            // A standard ring on pole0 to move
            pole0.AddRing(new RingData(RingColor.Red, RingType.Standard));

            _gameplayModel.Poles.Add(pole0);
            _gameplayModel.Poles.Add(pole1);

            // Execute move - bomb should tick from 1 to 0 and explode (removing it)
            moveCommand.Execute(new MoveRingSignal(0, 1));

            // Verify that the bomb exploded and is gone from the pole, and signal was fired
            Assert.AreEqual(1, pole1.Rings.Count); // only the moved red standard ring is there, bomb is gone
            Assert.IsTrue(_signalBus.HasFiredBombExploded);

            // Undo - the bomb should be restored to index 0 on pole1, with its counter set back to 1
            undoCommand.Execute(new UndoSignal());

            // pole0 should have 1 ring (the standard red ring).
            // pole1 should have 1 ring (the restored bomb).
            Assert.AreEqual(1, pole0.Rings.Count);
            Assert.AreEqual(1, pole1.Rings.Count);
            Assert.AreEqual(RingType.Bomb, pole1.Rings[0].Type);
            Assert.AreEqual(1, pole1.Rings[0].AdditionalData); // restored counter
        }

        [Test]
        public void BoardState_EnforcesChainRingCapacityConstraint()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 4 };
            
            // Set up pole 0 with 3 rings (1 slot left)
            board.AddRingSimple(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRingSimple(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRingSimple(0, new RingData(RingColor.Red, RingType.Standard));

            // Set up pole 1 with a Chain ring of color Red (partner)
            board.AddRingSimple(1, new RingData(RingColor.Red, RingType.Chain, 10));

            // Set up pole 2 with the moving Chain ring of color Red (group 10)
            board.AddRingSimple(2, new RingData(RingColor.Red, RingType.Chain, 10));

            // Can we add the chain ring to pole 0? It has only 1 slot left.
            // Chain moving requires 2 slots because it pulls its partner.
            // So CanAddRing should return false!
            bool canAdd = board.CanAddRing(0, RingColor.Red, RingType.Chain, 4, 10);
            Assert.IsFalse(canAdd);
        }
    }

    // --- Minimal Mocks for Unit Testing ---

    public class MockSignalBus : ISignalBus
    {
        public bool HasFiredUndo { get; private set; }
        public bool HasFiredRevealMystery { get; private set; }
        public bool HasFiredBombExploded { get; private set; }
        public bool HasFiredBreakIce { get; private set; }
        public bool HasFiredUnlockPole { get; private set; }

        private readonly System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<Delegate>> _handlers
            = new System.Collections.Generic.Dictionary<Type, System.Collections.Generic.List<Delegate>>();

        public System.Collections.Generic.IReadOnlyDictionary<Type, System.Collections.Generic.IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers => null;

        public void RegisterHandler<T>(Action<T> handler) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new System.Collections.Generic.List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            list.Add(handler);
        }

        public void Fire<T>(T signal) where T : struct
        {
            if (typeof(T) == typeof(UndoSignal))
                HasFiredUndo = true;
            else if (typeof(T) == typeof(RevealMysterySignal))
                HasFiredRevealMystery = true;
            else if (typeof(T) == typeof(BombExplodedSignal))
                HasFiredBombExploded = true;
            else if (typeof(T) == typeof(BreakIceSignal))
                HasFiredBreakIce = true;
            else if (typeof(T) == typeof(UnlockPoleSignal))
                HasFiredUnlockPole = true;

            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ((Action<T>)list[i])?.Invoke(signal);
                }
            }
        }

        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct {}
        public void FireNextFrame<T>(T signal) where T : struct {}
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;

        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct
        {
            RegisterHandler(handler);
            return null;
        }
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

    public class MockEconomyService : IEconomyService
    {
        public long CoinsBalance { get => GetBalance("Coins"); set => SetBalance("Coins", value); }

        public float MasterVolume { get; set; }
        public float BgmVolume { get; set; }
        public float SfxVolume { get; set; }
        public bool IsMuted { get; set; }

        private readonly System.Collections.Generic.Dictionary<string, ObservableProperty<long>> _mockBalances = new();

        public ObservableProperty<long> GetObservableBalance(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId)) return null;
            if (!_mockBalances.TryGetValue(currencyId, out var prop))
            {
                long initialValue = currencyId == "Coins" ? 100 : 0;
                prop = new ObservableProperty<long>(initialValue);
                _mockBalances[currencyId] = prop;
            }
            return prop;
        }

        public long GetBalance(string currencyId)
        {
            return GetObservableBalance(currencyId)?.Value ?? 0L;
        }

        public bool CanAfford(string currencyId, long amount)
        {
            return GetBalance(currencyId) >= amount;
        }
        
        public bool Spend(string currencyId, long amount, string reason = "")
        {
            var prop = GetObservableBalance(currencyId);
            if (prop != null && prop.Value >= amount)
            {
                prop.Value -= amount;
                return true;
            }
            return false;
        }

        public void Earn(string currencyId, long amount, string reason = "")
        {
            var prop = GetObservableBalance(currencyId);
            if (prop != null)
            {
                prop.Value += amount;
            }
        }

        public void SetBalance(string currencyId, long amount)
        {
            var prop = GetObservableBalance(currencyId);
            if (prop != null)
            {
                prop.Value = amount;
            }
        }
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

    public class MockGameStateMachine : Nexus.Core.FSM.IGameStateMachine
    {
        public Nexus.Core.FSM.IGameState CurrentState => null;
        public void RegisterState<TState>(TState state) where TState : class, Nexus.Core.FSM.IGameState {}
        public Task ChangeStateAsync<TState>(object args = null) where TState : class, Nexus.Core.FSM.IGameState => Task.CompletedTask;
        public Task ChangeStateAsync(Type stateType, object args = null) => Task.CompletedTask;
        public Task ChangeStateAsync(Type stateType, CancellationToken ct, object args = null) => Task.CompletedTask;
    }
}
