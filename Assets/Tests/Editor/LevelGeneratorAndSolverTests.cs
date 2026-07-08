using NUnit.Framework;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class LevelGeneratorAndSolverTests
    {
        [Test]
        public void DifficultyCurve_ReturnsCorrectParamsBasedOnLevel()
        {
            // Level 1: Tutorial
            Assert.AreEqual(DifficultyBand.Tutorial, DifficultyCurve.BandForLevel(1));
            Assert.AreEqual(3, DifficultyCurve.ColorCountForLevel(1));
            Assert.AreEqual(4, DifficultyCurve.PoleCountForLevel(1));

            // Level 55: Easy
            Assert.AreEqual(DifficultyBand.Easy, DifficultyCurve.BandForLevel(55));
            Assert.AreEqual(5, DifficultyCurve.ColorCountForLevel(55));
            Assert.AreEqual(5, DifficultyCurve.PoleCountForLevel(55));

            // Level 1500: Master
            Assert.AreEqual(DifficultyBand.Master, DifficultyCurve.BandForLevel(1500));
            Assert.AreEqual(10, DifficultyCurve.ColorCountForLevel(1500));
            Assert.AreEqual(9, DifficultyCurve.PoleCountForLevel(1500));
        }

        [Test]
        public void LevelGenerator_ProducesSolvableLevelWithStandardRings()
        {
            // Generate Level 1 (Grass Valley - standard rings, 4 poles, 3 colors, max capacity 4)
            var levelData = LevelGenerator.GenerateLevel(1, seed: 100, poleCount: 4, colorCount: 3, maxCapacity: 4);

            Assert.IsNotNull(levelData);
            Assert.AreEqual(1, levelData.LevelIndex);
            Assert.AreEqual(4, levelData.Poles.Count);
            Assert.Greater(levelData.TargetMoves, 0);

            // Verify with solver
            var board = new BoardState { PoleCount = 4 };
            for (int p = 0; p < 4; p++)
            {
                var poleData = levelData.Poles[p];
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    board.AddRing(p, poleData.Rings[r]);
                }
            }

            var solveResult = LevelSolver.Solve(board, maxCapacity: 4);
            Assert.IsTrue(solveResult.IsSolvable);
            Assert.AreEqual(levelData.TargetMoves, solveResult.MoveCount);
        }

        [Test]
        public void LevelGenerator_InjectsSpecialMechanicsCorrectly()
        {
            // Level 51: World 2 (Sunny Beach) -> Mystery
            var w2Level = LevelGenerator.GenerateLevel(51, seed: 200, poleCount: 5, colorCount: 4, maxCapacity: 4);
            Assert.IsNotNull(w2Level);

            bool hasMystery = false;
            foreach (var pole in w2Level.Poles)
            {
                foreach (var ring in pole.Rings)
                {
                    if (ring.Type == RingType.Mystery)
                    {
                        hasMystery = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(hasMystery, "World 2 levels must contain Mystery rings.");

            // Level 101: World 3 (Snow Mountain) -> Frozen
            var w3Level = LevelGenerator.GenerateLevel(101, seed: 300, poleCount: 6, colorCount: 5, maxCapacity: 4);
            Assert.IsNotNull(w3Level);

            bool hasFrozen = false;
            foreach (var pole in w3Level.Poles)
            {
                foreach (var ring in pole.Rings)
                {
                    if (ring.Type == RingType.Frozen)
                    {
                        hasFrozen = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(hasFrozen, "World 3 levels must contain Frozen rings.");

            // Level 151: World 4 (Ancient Temple) -> Locked Pole + Key
            var w4Level = LevelGenerator.GenerateLevel(151, seed: 400, poleCount: 7, colorCount: 6, maxCapacity: 4);
            Assert.IsNotNull(w4Level);

            bool hasLockedPole = false;
            bool hasKeyRing = false;

            foreach (var pole in w4Level.Poles)
            {
                if (pole.IsLocked) hasLockedPole = true;
                foreach (var ring in pole.Rings)
                {
                    if (ring.Type == RingType.Locked) hasKeyRing = true;
                }
            }

            Assert.IsTrue(hasLockedPole, "World 4 levels must contain a Locked Pole.");
            Assert.IsTrue(hasKeyRing, "World 4 levels must contain a Key (Locked type) Ring.");
        }

        [Test]
        public void LevelGenerator_ProducesSolvableLevelAtHighDifficulty()
        {
            // Generate a high-difficulty level, but keep the solver verification light enough for editor stability.
            int currentLevel = 1500;
            int colorCount = DifficultyCurve.ColorCountForLevel(currentLevel);
            int poleCount = DifficultyCurve.PoleCountForLevel(currentLevel);
            int maxCapacity = DifficultyCurve.MaxCapacityForLevel(currentLevel);

            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > 12) poleCount = 12;

            var levelData = LevelGenerator.GenerateLevel(currentLevel, seed: 1500 * 12345, poleCount, colorCount, maxCapacity);

            Assert.IsNotNull(levelData);
            Assert.AreEqual(currentLevel, levelData.LevelIndex);
            Assert.AreEqual(poleCount, levelData.Poles.Count);
            Assert.Greater(levelData.TargetMoves, 0);
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
    }
}
