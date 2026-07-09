using NUnit.Framework;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class ModelTests
    {
        // ── GameplayModel Tests ──────────────────────────────────────────────

        [Test]
        public void GameplayModel_NewInstance_HasDefaultValues()
        {
            var model = new GameplayModel();
            Assert.AreEqual(-1, model.SelectedPoleId.Value);
            Assert.AreEqual(0, model.MovesCount.Value);
            Assert.AreEqual(0, model.TargetMovesCount.Value);
            Assert.IsFalse(model.IsGameWon.Value);
            Assert.AreEqual(0, model.Poles.Count);
        }

        [Test]
        public void GameplayModel_Reset_RestoresDefaults()
        {
            var model = new GameplayModel();
            model.Poles.Add(new PoleState { Id = 0 });
            model.SelectedPoleId.Value = 2;
            model.MovesCount.Value = 5;
            model.TargetMovesCount.Value = 10;
            model.IsGameWon.Value = true;
            model.MoveHistory.Push(new MoveRecord(0, 1, new RingData(RingColor.Red)));

            model.Reset();

            Assert.AreEqual(0, model.Poles.Count);
            Assert.AreEqual(-1, model.SelectedPoleId.Value);
            Assert.AreEqual(0, model.MovesCount.Value);
            Assert.AreEqual(0, model.TargetMovesCount.Value);
            Assert.IsFalse(model.IsGameWon.Value);
            Assert.AreEqual(0, model.MoveHistory.Count);
        }

        [Test]
        public void GameplayModel_Poles_CanAddAndAccess()
        {
            var model = new GameplayModel();
            var pole = new PoleState { Id = 1, MaxCapacity = 4 };
            model.Poles.Add(pole);
            Assert.AreEqual(1, model.Poles.Count);
            Assert.AreEqual(1, model.Poles[0].Id);
            Assert.AreEqual(4, model.Poles[0].MaxCapacity);
        }

        // ── PlayerProgressModel Tests ────────────────────────────────────────

        [Test]
        public void PlayerProgressModel_NewInstance_HasDefaultValues()
        {
            var model = new PlayerProgressModel();
            Assert.AreEqual(0, model.Coins.Value);
            Assert.AreEqual(0, model.Diamonds.Value);
            Assert.AreEqual(0, model.Xp.Value);
            Assert.AreEqual(1, model.PlayerLevel.Value);
            Assert.AreEqual(1, model.CurrentLevel.Value);
            Assert.AreEqual(1, model.MaxUnlockedLevel.Value);
            Assert.AreEqual(0, model.HintCount.Value);
            Assert.IsFalse(model.RemoveAds.Value);
        }

        [Test]
        public void PlayerProgressModel_Reset_RestoresDefaults()
        {
            var model = new PlayerProgressModel();
            model.Coins.Value = 999;
            model.Diamonds.Value = 50;
            model.Xp.Value = 5000;
            model.PlayerLevel.Value = 10;
            model.CurrentLevel.Value = 42;
            model.MaxUnlockedLevel.Value = 50;
            model.HintCount.Value = 3;
            model.RemoveAds.Value = true;
            model.FreeUndosUsedThisSession.Value = 2;

            model.Reset();

            Assert.AreEqual(0, model.Coins.Value);
            Assert.AreEqual(0, model.Diamonds.Value);
            Assert.AreEqual(0, model.Xp.Value);
            Assert.AreEqual(1, model.PlayerLevel.Value);
            Assert.AreEqual(1, model.CurrentLevel.Value);
            Assert.AreEqual(1, model.MaxUnlockedLevel.Value);
            Assert.AreEqual(0, model.HintCount.Value);
            Assert.IsFalse(model.RemoveAds.Value);
            Assert.AreEqual(0, model.FreeUndosUsedThisSession.Value);
        }

        [Test]
        public void PlayerProgressModel_Reset_ResetsWorldsAndThemes()
        {
            var model = new PlayerProgressModel();
            model.OwnedThemes.Add("summer");
            model.Achievements.Add("first_win");

            model.Reset();

            Assert.AreEqual(0, model.OwnedThemes.Count);
            Assert.AreEqual(0, model.Achievements.Count);
            // Only world 0 should be unlocked after reset
            Assert.AreEqual(0, model.UnlockedWorlds.FindIndex(w => w)); // Index of first true
        }

        [Test]
        public void PlayerProgressModel_XpToNextLevel_ReturnsCorrectValues()
        {
            var model = new PlayerProgressModel();
            Assert.AreEqual(100, model.XpToNextLevel(1));
            Assert.AreEqual(250, model.XpToNextLevel(2));
            Assert.AreEqual(500, model.XpToNextLevel(3));
            Assert.AreEqual(1000, model.XpToNextLevel(4));
            Assert.AreEqual(1000, model.XpToNextLevel(10));
        }

        [Test]
        public void PlayerProgressModel_OnBind_InitializesWorlds()
        {
            var model = new PlayerProgressModel();
            model.OnBind(default);
            Assert.AreEqual(40, model.UnlockedWorlds.Count);
            Assert.IsTrue(model.UnlockedWorlds[0]);
            for (int i = 1; i < 40; i++)
                Assert.IsFalse(model.UnlockedWorlds[i]);
        }

        [Test]
        public void PlayerProgressModel_OnBind_DoesNotOverrideExisting()
        {
            var model = new PlayerProgressModel();
            model.UnlockedWorlds.Add(true); // Already initialized
            model.OnBind(default);
            Assert.AreEqual(1, model.UnlockedWorlds.Count);
        }

        [Test]
        public void PlayerProgressModel_LevelsSinceLastInterstitial_DefaultsToZero()
        {
            var model = new PlayerProgressModel();
            Assert.AreEqual(0, model.LevelsSinceLastInterstitial);
        }

        // ── PoleState Tests ──────────────────────────────────────────────────

        [Test]
        public void PoleState_NewInstance_HasDefaults()
        {
            var pole = new PoleState();
            Assert.AreEqual(4, pole.MaxCapacity);
            Assert.AreEqual(0, pole.Rings.Count);
            Assert.IsFalse(pole.IsLocked);
            Assert.IsFalse(pole.IsFull);
            Assert.IsTrue(pole.IsEmpty);
            Assert.AreEqual(RingColor.None, pole.TopRing.Color);
        }

        [Test]
        public void PoleState_AddRing_PushesToRings()
        {
            var pole = new PoleState();
            pole.AddRing(new RingData(RingColor.Red, RingType.Standard));
            Assert.AreEqual(1, pole.Rings.Count);
            Assert.AreEqual(RingColor.Red, pole.Rings[0].Color);
        }

        [Test]
        public void PoleState_AddRing_ToFull_DoesNotAdd()
        {
            var pole = new PoleState { MaxCapacity = 2 };
            pole.AddRing(new RingData(RingColor.Red, RingType.Standard));
            pole.AddRing(new RingData(RingColor.Blue, RingType.Standard));
            pole.AddRing(new RingData(RingColor.Green, RingType.Standard)); // Should be ignored
            Assert.AreEqual(2, pole.Rings.Count);
        }

        [Test]
        public void PoleState_PopRing_RemovesTop()
        {
            var pole = new PoleState();
            pole.AddRing(new RingData(RingColor.Red, RingType.Standard));
            pole.AddRing(new RingData(RingColor.Blue, RingType.Standard));
            var popped = pole.PopRing();
            Assert.AreEqual(RingColor.Blue, popped.Color);
            Assert.AreEqual(1, pole.Rings.Count);
            Assert.AreEqual(RingColor.Red, pole.Rings[0].Color);
        }

        [Test]
        public void PoleState_PopRing_Empty_ReturnsNone()
        {
            var pole = new PoleState();
            var popped = pole.PopRing();
            Assert.AreEqual(RingColor.None, popped.Color);
        }

        [Test]
        public void PoleState_IsFull_TrueWhenAtCapacity()
        {
            var pole = new PoleState { MaxCapacity = 2 };
            pole.AddRing(new RingData(RingColor.Red, RingType.Standard));
            Assert.IsFalse(pole.IsFull);
            pole.AddRing(new RingData(RingColor.Red, RingType.Standard));
            Assert.IsTrue(pole.IsFull);
        }

        [Test]
        public void PoleState_TopRing_ReturnsLast()
        {
            var pole = new PoleState();
            pole.AddRing(new RingData(RingColor.Red, RingType.Standard));
            pole.AddRing(new RingData(RingColor.Blue, RingType.Standard));
            Assert.AreEqual(RingColor.Blue, pole.TopRing.Color);
        }

        [Test]
        public void PoleState_TopRing_Empty_ReturnsNone()
        {
            var pole = new PoleState();
            Assert.AreEqual(RingColor.None, pole.TopRing.Color);
        }

        // ── WinReward Tests ──────────────────────────────────────────────────

        [Test]
        public void WinReward_From_CreatesCorrectStruct()
        {
            var reward = WinReward.From(moves: 10, targetMoves: 12, coins: 50, xp: 100, stars: 3);
            Assert.AreEqual(10, reward.Moves);
            Assert.AreEqual(12, reward.TargetMoves);
            Assert.AreEqual(50, reward.Coins);
            Assert.AreEqual(100, reward.Xp);
            Assert.AreEqual(3, reward.Stars);
        }

        // ── UndoStack Tests ──────────────────────────────────────────────────

        [Test]
        public void UndoStack_New_IsEmpty()
        {
            var stack = new UndoStack<int>(10);
            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void UndoStack_PushAndPop_ReturnsInOrder()
        {
            var stack = new UndoStack<int>(2);
            stack.Push(1);
            stack.Push(2);
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual(2, stack.Pop());
            Assert.AreEqual(1, stack.Pop());
            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void UndoStack_Pop_Empty_ReturnsDefault()
        {
            var stack = new UndoStack<int>(2);
            Assert.AreEqual(0, stack.Pop());
        }

        [Test]
        public void UndoStack_GrowsAutomatically()
        {
            var stack = new UndoStack<int>(2);
            stack.Push(1);
            stack.Push(2);
            stack.Push(3); // Should trigger resize
            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(2, stack.Pop());
            Assert.AreEqual(1, stack.Pop());
        }

        [Test]
        public void UndoStack_Clear_Empties()
        {
            var stack = new UndoStack<int>(2);
            stack.Push(1);
            stack.Push(2);
            stack.Clear();
            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(0, stack.Pop());
        }

        // ── MoveRecord Tests ─────────────────────────────────────────────────

        [Test]
        public void MoveRecord_New_HasDefaults()
        {
            var record = new MoveRecord();
            Assert.AreEqual(-1, record.FromPoleId);
            Assert.AreEqual(-1, record.ToPoleId);
            Assert.IsFalse(record.WasMysteryRevealedOnFrom);
            Assert.IsFalse(record.WasIceBrokenOnTarget);
            Assert.IsFalse(record.WasPainted);
            Assert.IsFalse(record.WasRainbowTargetConverted);
        }

        [Test]
        public void MoveRecord_ParameterizedCtor_SetsFields()
        {
            var ring = new RingData(RingColor.Red, RingType.Standard);
            var record = new MoveRecord(fromPoleId: 0, toPoleId: 1, ring: ring,
                wasMysteryRevealedOnFrom: true, wasPainted: true, paintedRingIndex: 0,
                paintedRingOriginalColor: RingColor.Blue);

            Assert.AreEqual(0, record.FromPoleId);
            Assert.AreEqual(1, record.ToPoleId);
            Assert.AreEqual(RingColor.Red, record.Ring.Color);
            Assert.IsTrue(record.WasMysteryRevealedOnFrom);
            Assert.IsTrue(record.WasPainted);
            Assert.AreEqual(1, record.PaintedRingIndex);
            Assert.AreEqual(RingColor.Blue, record.PaintedRingOriginalColor);
        }

        [Test]
        public void MoveRecord_Clear_ReturnsToDefaults()
        {
            var record = new MoveRecord(0, 1, new RingData(RingColor.Red, RingType.Standard));
            record.SubMoves.Add(new MoveRecord());
            record.BombCountersBeforeTick.Add((0, 0, 3));

            record.Clear();

            Assert.AreEqual(-1, record.FromPoleId);
            Assert.AreEqual(0, record.SubMoves.Count);
            Assert.AreEqual(0, record.BombCountersBeforeTick.Count);
        }

        // ── MoveRecordPool Tests ─────────────────────────────────────────────

        [Test]
        public void MoveRecordPool_Rent_ReturnsNonNullRecord()
        {
            var record = MoveRecordPool.Rent();
            Assert.IsNotNull(record);
        }

        [Test]
        public void MoveRecordPool_ReturnAndRent_ReusesInstance()
        {
            var record1 = MoveRecordPool.Rent();
            MoveRecordPool.Return(record1);
            var record2 = MoveRecordPool.Rent();
            Assert.AreSame(record1, record2); // Same instance reused
        }

        [Test]
        public void MoveRecordPool_ReturnNull_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => MoveRecordPool.Return(null));
        }
    }
}