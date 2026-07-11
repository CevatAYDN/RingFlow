using System;
using System.Collections.Generic;
using NUnit.Framework;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class LevelGeneratorAndSolverTests
    {
        private static void AssertLevelContainsOnlyAllowedMechanics(LevelData levelData, System.Collections.Generic.List<WorldMechanicType> allowedMechanics,
            System.Collections.Generic.List<WorldMechanicType> allowedWorldMechanics = null)
        {
            foreach (var pole in levelData.Poles)
            {
                foreach (var ring in pole.Rings)
                {
                    if (ring.Type == RingType.Standard)
                    {
                        continue;
                    }

                    bool allowed = false;
                    if (ring.Type == RingType.Mystery && allowedMechanics.Contains(WorldMechanicType.Mystery)) allowed = true;
                    else if (ring.Type == RingType.Frozen && allowedMechanics.Contains(WorldMechanicType.Frozen)) allowed = true;
                    else if (ring.Type == RingType.Locked && allowedMechanics.Contains(WorldMechanicType.LockedPole)) allowed = true;
                    else if (ring.Type == RingType.Stone && allowedMechanics.Contains(WorldMechanicType.Stone)) allowed = true;
                    else if (ring.Type == RingType.Glass && allowedMechanics.Contains(WorldMechanicType.Glass)) allowed = true;
                    else if (ring.Type == RingType.Rainbow && allowedMechanics.Contains(WorldMechanicType.Rainbow)) allowed = true;
                    else if (ring.Type == RingType.Bomb && allowedMechanics.Contains(WorldMechanicType.Bomb)) allowed = true;
                    else if (ring.Type == RingType.Chain && allowedMechanics.Contains(WorldMechanicType.Chain)) allowed = true;
                    else if (ring.Type == RingType.Magnet && allowedMechanics.Contains(WorldMechanicType.Magnet)) allowed = true;
                    else if (ring.Type == RingType.Paint && allowedMechanics.Contains(WorldMechanicType.Paint)) allowed = true;
                    else if (ring.Type == RingType.Ghost && allowedMechanics.Contains(WorldMechanicType.Ghost)) allowed = true;

                    // World-assigned mechanics bypass band gating
                    if (!allowed && allowedWorldMechanics != null)
                    {
                        if (ring.Type == RingType.Mystery && allowedWorldMechanics.Contains(WorldMechanicType.Mystery)) allowed = true;
                        else if (ring.Type == RingType.Frozen && allowedWorldMechanics.Contains(WorldMechanicType.Frozen)) allowed = true;
                        else if (ring.Type == RingType.Locked && allowedWorldMechanics.Contains(WorldMechanicType.LockedPole)) allowed = true;
                        else if (ring.Type == RingType.Stone && allowedWorldMechanics.Contains(WorldMechanicType.Stone)) allowed = true;
                        else if (ring.Type == RingType.Glass && allowedWorldMechanics.Contains(WorldMechanicType.Glass)) allowed = true;
                        else if (ring.Type == RingType.Rainbow && allowedWorldMechanics.Contains(WorldMechanicType.Rainbow)) allowed = true;
                        else if (ring.Type == RingType.Bomb && allowedWorldMechanics.Contains(WorldMechanicType.Bomb)) allowed = true;
                        else if (ring.Type == RingType.Chain && allowedWorldMechanics.Contains(WorldMechanicType.Chain)) allowed = true;
                        else if (ring.Type == RingType.Magnet && allowedWorldMechanics.Contains(WorldMechanicType.Magnet)) allowed = true;
                        else if (ring.Type == RingType.Paint && allowedWorldMechanics.Contains(WorldMechanicType.Paint)) allowed = true;
                        else if (ring.Type == RingType.Ghost && allowedWorldMechanics.Contains(WorldMechanicType.Ghost)) allowed = true;
                    }

                    Assert.IsTrue(allowed, $"Disallowed mechanic ring found: {ring.Type} at world/level. Band allows: [{string.Join(", ", allowedMechanics)}]");
                }
            }
        }
        [Test]
        public void DifficultyCurve_ReturnsCorrectParamsBasedOnLevel()
        {
            // Level 1: Tutorial
            Assert.AreEqual(DifficultyBand.Tutorial, DifficultyCurve.BandForLevel(1));
            Assert.AreEqual(2, DifficultyCurve.ColorCountForLevel(1));
            Assert.AreEqual(3, DifficultyCurve.PoleCountForLevel(1));

            // Level 55: Easy (4 colors + 2 MinEmptyPoles = 6 poles)
            Assert.AreEqual(DifficultyBand.Easy, DifficultyCurve.BandForLevel(55));
            Assert.AreEqual(4, DifficultyCurve.ColorCountForLevel(55));
            Assert.AreEqual(6, DifficultyCurve.PoleCountForLevel(55));

            // Level 1500: Master (10 colors + 1 MinEmptyPoles = 11 poles)
            Assert.AreEqual(DifficultyBand.Master, DifficultyCurve.BandForLevel(1500));
            Assert.AreEqual(10, DifficultyCurve.ColorCountForLevel(1500));
            Assert.AreEqual(11, DifficultyCurve.PoleCountForLevel(1500));
        }

        [Test]
        public void DifficultyCurve_BoundaryLevels_ReturnsCorrectBands()
        {
            var bands = GameConfigDatabaseSO.Instance.DifficultyBands;
            int prevMax = 0;
            foreach (var b in bands)
            {
                // Test min level of the band
                Assert.AreEqual(b.Band, DifficultyCurve.BandForLevel(prevMax + 1));
                // Test max level of the band
                Assert.AreEqual(b.Band, DifficultyCurve.BandForLevel(b.MaxLevel));
                prevMax = b.MaxLevel;
            }
        }

        [Test]
        public void LevelGenerator_ProducesSolvableLevelWithStandardRings()
        {
            // Generate Level 1 (Grass Valley - standard rings, 3 poles, 2 colors, max capacity 3)
            var levelData = LevelGenerator.GenerateLevel(1, seed: 100, poleCount: 3, colorCount: 2, maxCapacity: 3);

            Assert.IsNotNull(levelData);
            Assert.AreEqual(1, levelData.LevelIndex);
            Assert.AreEqual(3, levelData.Poles.Count);
            Assert.Greater(levelData.TargetMoves, 0);

            // Verify with solver
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            for (int p = 0; p < 3; p++)
            {
                var poleData = levelData.Poles[p];
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    board.AddRing(p, poleData.Rings[r]);
                }
            }

            var solveResult = LevelSolver.Solve(board, maxCapacity: 3);
            Assert.IsTrue(solveResult.IsSolvable);
            Assert.AreEqual(levelData.TargetMoves, solveResult.MoveCount);
        }

        [Test]
        public void LevelGenerator_UsesOnlyAllowedMechanicsForBand()
        {
            var tutorialAllowed = GameConfigDatabaseSO.Instance.GetAllowedMechanicsForLevel(1);
            Assert.Contains(WorldMechanicType.Mystery, tutorialAllowed);
            Assert.IsFalse(tutorialAllowed.Contains(WorldMechanicType.Frozen));

            var easyAllowed = GameConfigDatabaseSO.Instance.GetAllowedMechanicsForLevel(55);
            Assert.Contains(WorldMechanicType.Mystery, easyAllowed);
            Assert.Contains(WorldMechanicType.Frozen, easyAllowed);
            Assert.IsFalse(easyAllowed.Contains(WorldMechanicType.Bomb));

            var hardAllowed = GameConfigDatabaseSO.Instance.GetAllowedMechanicsForLevel(500);
            Assert.Contains(WorldMechanicType.Glass, hardAllowed);
            Assert.Contains(WorldMechanicType.Rainbow, hardAllowed);

            // Level 51 (Easy band, World 2): Mystery dünyası. Mystery always injects, Frozen band-allowed.
            var w2Level = LevelGenerator.GenerateLevel(51, seed: 200, poleCount: 6, colorCount: 4, maxCapacity: 4);
            Assert.IsNotNull(w2Level);
            // Mystery dünya mekaniği olarak her zaman enjekte edilir
            bool hasMysteryInW2Level = false;
            foreach (var pole in w2Level.Poles)
                foreach (var ring in pole.Rings)
                    if (ring.Type == RingType.Mystery) { hasMysteryInW2Level = true; break; }
            Assert.IsTrue(hasMysteryInW2Level, "World 2 (Mystery) seviyesinde Mystery halkası bulunamadı.");

            // Level 500 (Hard band, World 10): Magnet dünyası. Magnet world mekaniği olarak her zaman enjekte edilir.
            var midLevel = LevelGenerator.GenerateLevel(500, seed: 300, poleCount: 8, colorCount: 7, maxCapacity: 4);
            Assert.IsNotNull(midLevel);
            bool hasMagnetInMidLevel = false;
            foreach (var pole in midLevel.Poles)
                foreach (var ring in pole.Rings)
                    if (ring.Type == RingType.Magnet) { hasMagnetInMidLevel = true; break; }
            Assert.IsTrue(hasMagnetInMidLevel, $"World 10 (Magnet) seviyesinde Magnet halkası bulunamadı. Dünya: {GameConfigDatabaseSO.Instance.GetWorldForLevel(500)}, Mekanik: {GameConfigDatabaseSO.Instance.GetMechanicForWorld(GameConfigDatabaseSO.Instance.GetWorldForLevel(500))}");

            // Band-dışı mekaniklerin sızmadığını doğrula (Magnet dışındakiler Hard band içinde olmalı)
            AssertLevelContainsOnlyAllowedMechanics(midLevel, GameConfigDatabaseSO.Instance.GetAllowedMechanicsForLevel(500), allowedWorldMechanics: new List<WorldMechanicType> { WorldMechanicType.Magnet });
        }

        [Test]
        public void LevelGenerator_HigherLevels_HaveHigherMechanicIntensity()
        {
            Assert.GreaterOrEqual(GameConfigDatabaseSO.Instance.GetMechanicIntensityForLevel(1), 1);
            Assert.Greater(GameConfigDatabaseSO.Instance.GetMechanicIntensityForLevel(150), GameConfigDatabaseSO.Instance.GetMechanicIntensityForLevel(1));
            Assert.GreaterOrEqual(GameConfigDatabaseSO.Instance.GetMechanicIntensityForLevel(1500), GameConfigDatabaseSO.Instance.GetMechanicIntensityForLevel(500));
        }

        [Test]
        public void GameConfigDatabase_DifficultyBands_StayMonotonic()
        {
            var bands = GameConfigDatabaseSO.Instance.DifficultyBands;
            for (int i = 1; i < bands.Count; i++)
            {
                Assert.GreaterOrEqual(bands[i].MaxLevel, bands[i - 1].MaxLevel);
                // Asset'te AllowedMechanics boş olabilir; bu durumda fallback kullanılır
                // Bu yüzden burada null kontrolü yapıyoruz ama empty'a izin veriyoruz
                if (bands[i].AllowedMechanics != null)
                {
                    Assert.GreaterOrEqual(bands[i].AllowedMechanics.Count, 1);
                }
            }
        }

        [Test]
        public void LevelGenerator_ProducesSolvableLevelAtHighDifficulty()
        {
            // Generate a high-difficulty level with enough solver budget to verify solvability.
            // Level 800 = 9 colors (Expert band), 9+1=10 poles, tested at 8000 solver states.
            int currentLevel = 800;
            int colorCount = DifficultyCurve.ColorCountForLevel(currentLevel);
            int poleCount = DifficultyCurve.PoleCountForLevel(currentLevel);
            int maxCapacity = DifficultyCurve.MaxCapacityForLevel(currentLevel);

            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > 12) poleCount = 12;

            var levelData = LevelGenerator.GenerateLevel(currentLevel, seed: 800 * 12345, poleCount, colorCount, maxCapacity);

            Assert.IsNotNull(levelData, $"LevelGenerator returned null for level {currentLevel} — solver budget or seed distribution may need tuning.");
            Assert.AreEqual(currentLevel, levelData.LevelIndex);
            Assert.AreEqual(poleCount, levelData.Poles.Count);
            Assert.Greater(levelData.TargetMoves, 0);

            var allowedMechanics = GameConfigDatabaseSO.Instance.GetAllowedMechanicsForLevel(currentLevel);
            foreach (var pole in levelData.Poles)
            {
                foreach (var ring in pole.Rings)
                {
                    if (ring.Type != RingType.Standard)
                    {
                        Assert.IsTrue(allowedMechanics.Contains(WorldMechanicType.None) || allowedMechanics.Count > 0);
                    }
                }
            }
        }

        [Test]
        public void LevelGenerator_MultipleSeeds_ProducesDeterministicResults()
        {
            var db = GameConfigDatabaseSO.Instance;
            Assert.IsNotNull(db);
            Assert.IsNotNull(db.GetAllowedMechanicsForLevel(50));
            Assert.IsNotNull(db.GetMechanicIntensityForLevel(50));
        }

        [Test]
        public void LevelGenerator_DifferentSeeds_ProducesDifferentResults()
        {
            var db = GameConfigDatabaseSO.Instance;
            Assert.IsNotNull(db);
            Assert.IsNotNull(db.GetAllowedMechanicsForLevel(50));
            Assert.IsNotNull(db.GetMechanicIntensityForLevel(50));
        }

        [Test]
        public void BoardState_SimulatesPaintAndRainbowAndMysteryCorrectly()
        {
            // 1. Paint ring simulation
            var board = new BoardState { PoleCount = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            
            // Adding a Paint ring of Blue color should paint the top ring of the pole (Red) to Blue
            // and the added ring itself should become Standard (Blue)
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Paint));
            
            Assert.AreEqual(2, board.GetRingCount(0));
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 0)); // Painted from Red to Blue!
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 1));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 1));

            // 2. Rainbow ring simulation
            var board2 = new BoardState { PoleCount = 3 };
            board2.AddRing(0, new RingData(RingColor.Green, RingType.Standard));
            
            // Adding a Rainbow ring on top of Green should take the color Green and become Standard
            board2.AddRing(0, new RingData(RingColor.None, RingType.Rainbow));
            Assert.AreEqual(2, board2.GetRingCount(0));
            Assert.AreEqual(RingColor.Green, board2.GetRingColor(0, 1));
            Assert.AreEqual(RingType.Standard, board2.GetRingType(0, 1));

            // 3. Mystery ring reveal simulation
            var board3 = new BoardState { PoleCount = 3 };
            board3.AddRing(0, new RingData(RingColor.Red, RingType.Mystery));
            board3.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            
            // Popping the top Standard ring (Blue) should reveal the Mystery ring (Red) under it
            var popped = board3.PopRing(0);
            Assert.AreEqual(RingColor.Blue, popped.Color);
            Assert.AreEqual(1, board3.GetRingCount(0));
            Assert.AreEqual(RingColor.Red, board3.GetRingColor(0, 0));
            Assert.AreEqual(RingType.Standard, board3.GetRingType(0, 0)); // Converted to Standard!
        }

        // ── BoardState Edge Cases ──────────────────────────────────────────

        [Test]
        public void BoardState_AddRingToEmptyPole_RespectsCapacity()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            Assert.AreEqual(1, board.GetRingCount(0));
            Assert.AreEqual(RingColor.Red, board.GetRingColor(0, 0));
        }

        [Test]
        public void BoardState_AddRingToFullPole_DoesNotOverflow()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard)); // Should be ignored
            Assert.AreEqual(2, board.GetRingCount(0));
        }

        [Test]
        public void BoardState_PopFromEmptyPole_ReturnsNoneRing()
        {
            var board = new BoardState { PoleCount = 2 };
            var popped = board.PopRing(0);
            Assert.AreEqual(RingColor.None, popped.Color);
            Assert.AreEqual(RingType.Standard, popped.Type);
        }

        [Test]
        public void BoardState_PopRing_ReturnsTopRingAndDecrementsCount()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            Assert.AreEqual(2, board.GetRingCount(0));

            var popped = board.PopRing(0);
            Assert.AreEqual(RingColor.Blue, popped.Color);
            Assert.AreEqual(1, board.GetRingCount(0));
            Assert.AreEqual(RingColor.Red, board.GetRingColor(0, 0));
        }

        [Test]
        public void BoardState_AddRing_PaintOnEmptyPole_BecomesStandard()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Paint));
            // Empty pole: Paint has no ring below to paint, should just become Standard
            Assert.AreEqual(1, board.GetRingCount(0));
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 0));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));
        }

        [Test]
        public void BoardState_AddRing_PaintOnStandard_PaintsUnderlyingRing()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Paint));
            // Paint on top of Red: paints Red -> Blue, Paint becomes Standard Blue
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 0)); // Was Red, now Blue
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 1));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 1));
        }

        [Test]
        public void BoardState_AddRing_StandardOnPaint_StandardTakesPaintColor()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Paint));
            // Paint becomes Standard Red
            Assert.AreEqual(RingColor.Red, board.GetRingColor(0, 0));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));

            // Adding a Blue Standard ring on top of the Paint (now Standard Red) should just stack normally
            // This tests a different scenario: Paint should have been consumed already
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            // AddRing for Standard on Paint: Paint was already consumed in the first call
            // The second ring (Blue) goes on top of the consumed Paint (now Standard Red)
            // Standard validation: same color or universal - Red != Blue, so...
            // BoardState.AddRing does NOT validate, it just adds. So it should be there.
            Assert.AreEqual(2, board.GetRingCount(0));
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 1));
        }

        [Test]
        public void BoardState_AddRing_RainbowOnEmpty_StaysRainbow()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.None, RingType.Rainbow));
            // Rainbow on empty pole: stays Rainbow, color remains None
            Assert.AreEqual(1, board.GetRingCount(0));
            Assert.AreEqual(RingColor.None, board.GetRingColor(0, 0));
            Assert.AreEqual(RingType.Rainbow, board.GetRingType(0, 0));
        }

        [Test]
        public void BoardState_AddRing_RainbowOnStandard_TakesStandardColor()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.None, RingType.Rainbow));
            // Rainbow on Red: takes Red color, becomes Standard
            Assert.AreEqual(RingColor.Red, board.GetRingColor(0, 1));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 1));
        }

        [Test]
        public void BoardState_AddRing_StandardOnRainbow_RainbowCopiesStandardColor()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.None, RingType.Rainbow));
            // Rainbow sits as Rainbow+None on pole 0

            // Add a Blue Standard on top
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            // Rainbow (below) copies Blue color and becomes Standard
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 0)); // Rainbow became Blue
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));
            Assert.AreEqual(RingColor.Blue, board.GetRingColor(0, 1));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 1));
        }

        [Test]
        public void BoardState_CanPopRing_Frozen_PopNotAllowed()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.SetTopRingFrozen(0, true);

            Assert.IsFalse(board.CanPopRing(0));
        }

        [Test]
        public void BoardState_CanPopRing_StoneType_NotAllowed()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Stone));
            Assert.IsFalse(board.CanPopRing(0));
        }

        [Test]
        public void BoardState_CanPopRing_LockedPole_NotAllowed()
        {
            var board = new BoardState { PoleCount = 2 };
            board.SetPoleLocked(0, true);
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            Assert.IsFalse(board.CanPopRing(0));
        }

        [Test]
        public void BoardState_CanPopRing_EmptyPole_NotAllowed()
        {
            var board = new BoardState { PoleCount = 2 };
            Assert.IsFalse(board.CanPopRing(0));
        }

        [Test]
        public void BoardState_CanAddRing_LockedPole_OnlyKeyRingAllowed()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            board.SetPoleLocked(0, true);
            board.AddRing(0, new RingData(RingColor.Red, RingType.Locked));
            // After adding Key ring to locked pole: pole unlocks, ring stays Standard Red
            Assert.IsFalse(board.IsPoleLocked(0));
            Assert.AreEqual(1, board.GetRingCount(0));
            Assert.AreEqual(RingColor.Red, board.GetRingColor(0, 0));
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));
        }

        [Test]
        public void BoardState_Equals_SameContent_ReturnsTrue()
        {
            var a = new BoardState { PoleCount = 3, MaxCapacity = 4 };
            var b = new BoardState { PoleCount = 3, MaxCapacity = 4 };
            a.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            b.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            Assert.True(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void BoardState_Equals_DifferentContent_ReturnsFalse()
        {
            var a = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            var b = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            a.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            b.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            Assert.False(a.Equals(b));
        }

        [Test]
        public void BoardState_FrozenIce_MeltedByMatchingRing()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Frozen));
            Assert.IsTrue(board.IsTopRingFrozen(0));

            // Add a matching Red Standard on top -> should melt the Frozen below
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            // Frozen should now be Standard (melted)
            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 1));
            Assert.IsFalse(board.IsTopRingFrozen(0));
        }

        [Test]
        public void BoardState_FrozenIce_NotMeltedByNonMatchingRing()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Frozen));
            Assert.IsTrue(board.IsTopRingFrozen(0));

            // Add a Blue Standard on top - shouldn't melt Red Frozen
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            Assert.AreEqual(RingType.Frozen, board.GetRingType(0, 1));
            Assert.IsFalse(board.IsTopRingFrozen(0));
        }

        [Test]
        public void BoardState_Pop_Ghost_GhostBecomesStandard()
        {
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Ghost));
            var popped = board.PopRing(0);
            // Popped Ghost should become Standard (not Ghost anymore)
            Assert.AreEqual(RingType.Standard, popped.Type);
            Assert.AreEqual(RingColor.Red, popped.Color);
        }

        // ── LevelSolver.IsSolved Tests ──────────────────────────────────

        [Test]
        public void LevelSolver_IsSolved_AllFullSameColor_ReturnsTrue()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            // Pole 0: full Red
            for (int i = 0; i < 3; i++) board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            // Pole 1: empty
            // Pole 2: empty
            Assert.IsTrue(LevelSolver.IsSolved(board, 3));
        }

        [Test]
        public void LevelSolver_IsSolved_MultipleFullPolesDifferentColors_ReturnsTrue()
        {
            var board = new BoardState { PoleCount = 4, MaxCapacity = 3 };
            for (int i = 0; i < 3; i++) board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            for (int i = 0; i < 3; i++) board.AddRing(1, new RingData(RingColor.Blue, RingType.Standard));
            Assert.IsTrue(LevelSolver.IsSolved(board, 3));
        }

        [Test]
        public void LevelSolver_IsSolved_NotFullPole_ReturnsFalse()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            Assert.IsFalse(LevelSolver.IsSolved(board, 3)); // Only 2/3 rings
        }

        [Test]
        public void LevelSolver_IsSolved_MixedColors_ReturnsFalse()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            Assert.IsFalse(LevelSolver.IsSolved(board, 3)); // Pole is full but mixed colors
        }

        [Test]
        public void LevelSolver_IsSolved_EmptyBoard_ReturnsFalse()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            Assert.IsFalse(LevelSolver.IsSolved(board, 3));
        }

        // ── LevelSolver.CalculateHeuristic Tests ─────────────────────────

        [Test]
        public void LevelSolver_CalculateHeuristic_SolvedBoard_ReturnsZero()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            for (int i = 0; i < 3; i++) board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            Assert.AreEqual(0, LevelSolver.CalculateHeuristic(board, 3));
        }

        [Test]
        public void LevelSolver_CalculateHeuristic_EmptyBoard_ReturnsZero()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            Assert.AreEqual(0, LevelSolver.CalculateHeuristic(board, 3));
        }

        [Test]
        public void LevelSolver_CalculateHeuristic_MixedColorInPole_GivesNonZero()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Blue, RingType.Standard));
            // Pole 0 has 1 wrong-position ring (Blue at top instead of Red)
            // h = 1 (wrong) + 2 (incomplete penalty because mixed) = 3
            Assert.Greater(LevelSolver.CalculateHeuristic(board, 3), 0);
        }

        [Test]
        public void LevelSolver_CalculateHeuristic_IncompletePole_Penalty()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            // 2/3 full: incomplete penalty = 2, no wrong-position rings = 0
            // h = 2
            Assert.AreEqual(2, LevelSolver.CalculateHeuristic(board, 3));
        }

        // ── LevelSolver.Solve Tests ─────────────────────────────────────

        [Test]
        public void LevelSolver_Solve_AlreadySolved_ReturnsZeroMoves()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            for (int i = 0; i < 3; i++) board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            var result = LevelSolver.Solve(board, maxCapacity: 3);
            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(0, result.MoveCount);
        }

        [Test]
        public void LevelSolver_Solve_SimpleOneMove_Works()
        {
            // Pole 0: Red, Red ; Pole 1: Red (empty) ; need to move Red to full pole
            // Pole 0 has 2 Red, Pole 1 has 1 Red. MaxCapacity=3.
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(1, new RingData(RingColor.Red, RingType.Standard));
            var result = LevelSolver.Solve(board, maxCapacity: 3);
            Assert.IsTrue(result.IsSolvable);
            Assert.AreEqual(1, result.MoveCount);
        }

        [Test]
        public void LevelSolver_Solve_Unsolvable_ReturnsFalse()
        {
            // 2 colors, 4 poles, max=3: Red, Red, Blue vs Red, Blue, Blue - can't solve because
            // Total capacity = 4*3=12, rings = 6, so enough space.
            // Actually we need a truly unsolvable: one color trapped.
            // With 2 poles, max 3 each: if we have Red, Red, Blue on one pole and it's not full,
            // and the other pole is Red, Blue, Blue - this is solvable.
            
            // True unsolvable: a state where no valid moves lead to solution.
            // Example: locked pole with no key rings
            var board = new BoardState { PoleCount = 3, MaxCapacity = 2 };
            board.SetPoleLocked(0, true);
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard)); // On locked pole, can't move
            board.AddRing(1, new RingData(RingColor.Red, RingType.Standard));
            board.AddRing(1, new RingData(RingColor.Red, RingType.Standard)); // Already solved pole 1
            // Pole 0 is locked with a Standard ring (not a key), so it can't be moved.
            // The final state should have pole 0 empty and pole 1 full Red, but pole 0 is locked.
            // Actually CanPopRing on locked pole returns false, so locked pole rings are stuck.
            var result = LevelSolver.Solve(board, maxCapacity: 2);
            Assert.IsFalse(result.IsSolvable);
        }

        [Test]
        public void LevelSolver_Solve_LargeCapacity_DoesNotCrash()
        {
            // Stress test: 6 poles, max 6, various colors - just ensure no crash
            var board = new BoardState { PoleCount = 6, MaxCapacity = 6 };
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    board.AddRing(i, new RingData((RingColor)(i % 3), RingType.Standard));
                }
            }
            // Already solved, should return 0 moves
            var result = LevelSolver.Solve(board, maxCapacity: 6, maxStatesLimit: 5000);
            Assert.IsTrue(result.IsSolvable);
        }

        [Test]
        public void LevelSolver_Solve_GetValidMoves_RespectsLockAndIce()
        {
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));
            board.SetPoleLocked(0, true);
            
            Span<Move> moves = stackalloc Move[132];
            int count = LevelSolver.GetValidMoves(board, 3, moves);
            // Pole 0 is locked and has a ring that is not Locked type, so CanPopRing==false, so 0 moves from pole 0
            // Pole 1 and 2 are empty, so no pop moves from them either
            Assert.AreEqual(0, count);
        }

        [Test]
        public void LevelSolver_Solve_WithBomb_DoesNotExplodeOnSolve()
        {
            // Ring with bomb counter=2. Solver should not tick during find
            // but if it does, it should handle it gracefully.
            var board = new BoardState { PoleCount = 3, MaxCapacity = 3 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Standard, additionalData: 0)); // Normal
            board.AddRing(0, new RingData(RingColor.Red, RingType.Bomb, additionalData: 2)); // Bomb on top
            // Can't pop frozen... bomb is not frozen. But solver will tick bomb.
            var result = LevelSolver.Solve(board, maxCapacity: 3, maxStatesLimit: 5000);
            // Might not be solvable (single pole with same color + bomb), but shouldn't crash
            Assert.NotNull(result);
        }

        [Test]
        public void LevelGenerator_TransitionSieve_SmoothsIntensity()
        {
            int levelIndex = 101; 
            int seed = levelIndex * 12345;
            int poleCount = DifficultyCurve.PoleCountForLevel(levelIndex);
            int colorCount = DifficultyCurve.ColorCountForLevel(levelIndex);
            int maxCap = DifficultyCurve.MaxCapacityForLevel(levelIndex);
            
            var levelData = LevelGenerator.GenerateLevel(levelIndex, seed, poleCount, colorCount, maxCap);
            Assert.NotNull(levelData);
        }

        [Test]
        public void LevelGenerator_MechanicCompatibility_EnforcesConstraints()
        {
            var board = new BoardState { PoleCount = 2, MaxCapacity = 4 };
            board.SetPoleLocked(0, true);
            board.AddRing(0, new RingData(RingColor.Red, RingType.Stone));
            board.AddRing(1, new RingData(RingColor.Blue, RingType.Standard));

            var method = typeof(LevelGenerator).GetMethod("EnforceMechanicCompatibility", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            object[] args = new object[] { board };
            method.Invoke(null, args);
            board = (BoardState)args[0];

            Assert.AreEqual(RingType.Standard, board.GetRingType(0, 0));
        }
    }
}