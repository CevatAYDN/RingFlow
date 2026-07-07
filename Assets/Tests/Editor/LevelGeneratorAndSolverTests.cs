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
    }
}
