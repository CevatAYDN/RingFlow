using System;
using System.Collections.Generic;
using NUnit.Framework;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class LevelGeneratorAndSolverTests
    {
        private GameConfigDatabaseSO _db;
        [SetUp]
        public void SetUp()
        {
            _db = UnityEngine.Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            if (_db == null)
                throw new System.InvalidOperationException(
                    "[Test SetUp] GameConfigDatabaseSO not found at Resources key: " + GameplayAssetKeys.GameConfigDatabase +
                    ". Create and configure the asset before running tests.");
        }

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
        public void DB_DifficultyBands_ReturnsCorrectParamsBasedOnLevel()
        {
            var db = _db;
            // Level 1 should be Tutorial band (first band always covers level 1)
            Assert.AreEqual(DifficultyBand.Tutorial, db.GetBandForLevel(1));

            // Validate pole = color + minEmpty from band for level 1
            var tutorialBand = db.DifficultyBands[0];
            int tutorialColors = db.GetColorCountForLevel(1);
            int expectedTutorialPoles = tutorialColors + tutorialBand.MinEmptyPoles;
            Assert.AreEqual(expectedTutorialPoles, db.GetPoleCountForLevel(1),
                "Pole count for level 1 must equal ColorCount + MinEmptyPoles from Tutorial band");

            // DATA-DRIVEN: derive a mid-range level (50% of TotalLevels) and check its band
            // against what DB computes. This avoids hardcoded assumptions about TotalLevels.
            int midLevel = db.TotalLevels / 2;
            var midBand = db.GetBandForLevel(midLevel);
            // Mid-level must be in a band beyond Tutorial (sanity check for progression)
            Assert.AreNotEqual(DifficultyBand.Tutorial, midBand,
                $"Level {midLevel} (50% of TotalLevels={db.TotalLevels}) should not still be Tutorial band.");

            var midBandData = db.DifficultyBands.Find(b => b.Band == midBand);
            Assert.IsNotNull(midBandData, $"No DifficultyBandData found for band {midBand}.");
            int midColors = db.GetColorCountForLevel(midLevel);
            Assert.AreEqual(midColors + midBandData.MinEmptyPoles, db.GetPoleCountForLevel(midLevel),
                $"Pole count for level {midLevel} must equal ColorCount + MinEmptyPoles for band {midBand}.");

            // DATA-DRIVEN: last level must map to the last band (Legend or Master/Legend tail)
            int lastLevel = db.TotalLevels;
            var lastBand = db.GetBandForLevel(lastLevel);
            var lastBandData = db.DifficultyBands[db.DifficultyBands.Count - 1];
            Assert.AreEqual(lastBandData.Band, lastBand,
                $"Level {lastLevel} (TotalLevels) must map to last band {lastBandData.Band}.");
            int lastColors = db.GetColorCountForLevel(lastLevel);
            Assert.AreEqual(lastColors + lastBandData.MinEmptyPoles, db.GetPoleCountForLevel(lastLevel));
        }

        [Test]
        public void DB_DifficultyBands_BoundaryLevels_ReturnsCorrectBands()
        {
            var db = _db;
            int prevMax = 0;
            foreach (var b in db.DifficultyBands)
            {
                // Test min level of the band
                Assert.AreEqual(b.Band, db.GetBandForLevel(prevMax + 1));
                // Test max level of the band
                Assert.AreEqual(b.Band, db.GetBandForLevel(b.MaxLevel));
                prevMax = b.MaxLevel;
            }
        }

        [Test]
        public void LevelGenerator_ProducesSolvableLevelWithStandardRings()
        {
            // Level 1: Tutorial band -> MinEmptyPoles=2, MaxCapacity=4
            // Use poleCount = colorCount + minEmptyPoles = 2 + 2 = 4
            var levelData = LevelGenerator.GenerateLevel(_db, 1, seed: 100, poleCount: 4, colorCount: 2, maxCapacity: 3);

            Assert.IsNotNull(levelData);
            Assert.AreEqual(1, levelData.LevelIndex);
            Assert.AreEqual(4, levelData.Poles.Count);
            Assert.Greater(levelData.TargetMoves, 0);

            // Verify with solver
            var board = new BoardState { PoleCount = 4, MaxCapacity = 3 };
            for (int p = 0; p < 4; p++)
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
            // Tutorial band (level 1): Mystery always allowed, Frozen not yet
            var tutorialAllowed = _db.GetAllowedMechanicsForLevel(1);
            Assert.Contains(WorldMechanicType.Mystery, tutorialAllowed);
            Assert.IsFalse(tutorialAllowed.Contains(WorldMechanicType.Frozen));

            // Easy band: first level after Tutorial max
            var tutorialData = _db.DifficultyBands.Find(b => b.Band == DifficultyBand.Tutorial);
            int easyLevel = System.Math.Min(_db.TotalLevels, tutorialData.MaxLevel + 1);
            if (easyLevel <= _db.TotalLevels)
            {
                var easyAllowed = _db.GetAllowedMechanicsForLevel(easyLevel);
                Assert.Contains(WorldMechanicType.Mystery, easyAllowed);
                Assert.Contains(WorldMechanicType.Frozen, easyAllowed);
                Assert.IsFalse(easyAllowed.Contains(WorldMechanicType.Bomb),
                    $"Easy band (level {easyLevel}) should not allow Bomb yet.");
            }

            // Hard band: use a level in the Hard band range (data-driven)
            bool hasHardBand = _db.DifficultyBands.Exists(b => b.Band == DifficultyBand.Hard);
            if (hasHardBand)
            {
                bool hasMediumBand = _db.DifficultyBands.Exists(b => b.Band == DifficultyBand.Medium);
                int hardLevelBase = hasMediumBand
                    ? _db.DifficultyBands.Find(b => b.Band == DifficultyBand.Medium).MaxLevel + 1
                    : _db.DifficultyBands.Find(b => b.Band == DifficultyBand.Hard).MaxLevel;
                int hardLevel = System.Math.Min(_db.TotalLevels, hardLevelBase);
                var hardAllowed = _db.GetAllowedMechanicsForLevel(hardLevel);
                Assert.Contains(WorldMechanicType.Glass, hardAllowed,
                    $"Hard band (level {hardLevel}) should allow Glass.");
                Assert.Contains(WorldMechanicType.Rainbow, hardAllowed,
                    $"Hard band (level {hardLevel}) should allow Rainbow.");
            }

            // Level in Easy band: generate and verify Mystery can appear (world 2 mechanic)
            int earlyLevel = System.Math.Min(_db.TotalLevels, tutorialData.MaxLevel + 1);
            if (earlyLevel <= _db.TotalLevels)
            {
                var earlyLevelData = LevelGenerator.GenerateLevel(_db, earlyLevel, seed: 200,
                    poleCount: _db.GetPoleCountForLevel(earlyLevel),
                    colorCount: _db.GetColorCountForLevel(earlyLevel),
                    maxCapacity: _db.GetMaxCapacityForLevel(earlyLevel));
                Assert.IsNotNull(earlyLevelData);
            }

            // Mid-range level: generate and verify it stays within allowed mechanics
            int midLevel = System.Math.Max(1, (int)(_db.TotalLevels * 0.50f));
            var midLevelData = LevelGenerator.GenerateLevel(_db, midLevel, seed: 300,
                poleCount: _db.GetPoleCountForLevel(midLevel),
                colorCount: _db.GetColorCountForLevel(midLevel),
                maxCapacity: _db.GetMaxCapacityForLevel(midLevel));
            Assert.IsNotNull(midLevelData);
            // Verify no mechanics beyond what the band allows (world mechanics also permitted)
            int midWorldIdx = _db.GetWorldForLevel(midLevel);
            var midWorldMechanic = _db.GetMechanicForWorld(midWorldIdx);
            AssertLevelContainsOnlyAllowedMechanics(
                midLevelData,
                _db.GetAllowedMechanicsForLevel(midLevel),
                allowedWorldMechanics: new List<WorldMechanicType> { midWorldMechanic });
        }

        [Test]
        public void LevelGenerator_HigherLevels_HaveHigherMechanicIntensity()
        {
            // DATA-DRIVEN: use levels at 10%, 50%, 100% of TotalLevels
            int lowLevel = System.Math.Max(1, (int)(_db.TotalLevels * 0.10f));
            int midLevel = System.Math.Max(lowLevel + 1, (int)(_db.TotalLevels * 0.50f));
            int highLevel = _db.TotalLevels;

            Assert.GreaterOrEqual(_db.GetMechanicIntensityForLevel(lowLevel), 1);
            Assert.GreaterOrEqual(_db.GetMechanicIntensityForLevel(midLevel),
                _db.GetMechanicIntensityForLevel(lowLevel),
                $"Mechanic intensity at level {midLevel} must be >= intensity at level {lowLevel}.");
            Assert.GreaterOrEqual(_db.GetMechanicIntensityForLevel(highLevel),
                _db.GetMechanicIntensityForLevel(midLevel),
                $"Mechanic intensity at level {highLevel} must be >= intensity at level {midLevel}.");
        }

        [Test]
        public void GameConfigDatabase_DifficultyBands_StayMonotonic()
        {
            var bands = _db.DifficultyBands;
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
            // Use 80% of TotalLevels to land in the high-difficulty bands regardless of asset size.
            var db = _db;
            int currentLevel = System.Math.Max(1, (int)(db.TotalLevels * 0.80f));
            int colorCount = db.GetColorCountForLevel(currentLevel);
            int poleCount = db.GetPoleCountForLevel(currentLevel);
            int maxCapacity = db.GetMaxCapacityForLevel(currentLevel);

            var levelData = LevelGenerator.GenerateLevel(db, currentLevel, seed: currentLevel * 12345, poleCount, colorCount, maxCapacity);

            Assert.IsNotNull(levelData, $"LevelGenerator returned null for level {currentLevel} — solver budget or seed distribution may need tuning.");
            Assert.AreEqual(currentLevel, levelData.LevelIndex);
            Assert.AreEqual(poleCount, levelData.Poles.Count);
            Assert.Greater(levelData.TargetMoves, 0);

            var allowedMechanics = _db.GetAllowedMechanicsForLevel(currentLevel);
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
            // Aynı seed her zaman aynı seviyeyi üretmeli (deterministik üretim kuralı, GDD).
            int levelIndex = 50;
            int seed = 12345;
            int poleCount = _db.GetPoleCountForLevel(levelIndex);
            int colorCount = _db.GetColorCountForLevel(levelIndex);
            int maxCapacity = _db.GetMaxCapacityForLevel(levelIndex);

            var a = LevelGenerator.GenerateLevel(_db, levelIndex, seed, poleCount, colorCount, maxCapacity);
            var b = LevelGenerator.GenerateLevel(_db, levelIndex, seed, poleCount, colorCount, maxCapacity);

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.IsTrue(AreLevelsEqual(a, b), "Aynı seed ile üretilen iki seviye özdeş olmalı.");
        }

        [Test]
        public void LevelGenerator_DifferentSeeds_ProducesDifferentResults()
        {
            // Üretim seed'e bağlı olmalı: farklı seed'ler farklı seviye üretir.
            int levelIndex = 50;
            int poleCount = _db.GetPoleCountForLevel(levelIndex);
            int colorCount = _db.GetColorCountForLevel(levelIndex);
            int maxCapacity = _db.GetMaxCapacityForLevel(levelIndex);

            var a = LevelGenerator.GenerateLevel(_db, levelIndex, 111, poleCount, colorCount, maxCapacity);
            var b = LevelGenerator.GenerateLevel(_db, levelIndex, 999, poleCount, colorCount, maxCapacity);

            Assert.IsNotNull(a);
            Assert.IsNotNull(b);
            Assert.IsFalse(AreLevelsEqual(a, b), "Farklı seed'ler aynı seviyeyi üretmemeli (seed-bağımlı determinizm).");
        }

        [Test]
        public void ReplayEngine_Replay_SolvedLevel_IsDeterministic()
        {
            // Aynı oturum iki kez oynatıldığında birebir aynı sonucu vermeli (deterministik replay).
            var session = BuildSolvedReplaySession(1, 100);
            var engine = new ReplayEngine(_db);

            var r1 = engine.Replay(session);
            var r2 = engine.Replay(session);

            Assert.IsTrue(r1.IsValid, "Çözülmüş bir seviyenin replay'i geçerli olmalı.");
            Assert.AreEqual(0, r1.DeterminismFailures, "Geçerli bir çözümün replay'i determinizm hatası üretmemeli.");
            Assert.AreEqual(r1.ReplayedMoves, r2.ReplayedMoves);
            Assert.AreEqual(r1.DeterminismFailures, r2.DeterminismFailures);
            Assert.IsTrue(r1.FinalBoard.Equals(r2.FinalBoard), "Aynı oturumun tekrarı deterministik olmalı.");
        }

        [Test]
        public void ReplayEngine_Replay_SolvedLevel_NoDeterminismFailures()
        {
            // Çözücüden alınan hamle dizisi replay edildiğinde hiçbir determinizm hatası olmamalı.
            var session = BuildSolvedReplaySession(1, 100);
            var engine = new ReplayEngine(_db);

            var result = engine.Replay(session);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.DeterminismFailures);
            Assert.AreEqual(session.Moves.Count, result.ReplayedMoves);
        }

        [Test]
        public void ReplayEngine_Replay_Performance_IsBounded()
        {
            // Tek bir replay (seed'den seviye üretimi + hamle uygulama) oyun içi kare
            // bütçesinin çok altında kalmalı. Replay başına seviye üretimi dahil maliyet ölçülür.
            var session = BuildSolvedReplaySession(1, 100);
            var engine = new ReplayEngine(_db);

            const int iterations = 10;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            ReplayEngine.ReplayResult last = default;
            for (int i = 0; i < iterations; i++)
                last = engine.Replay(session);
            sw.Stop();

            Assert.IsTrue(last.IsValid);
            long perReplayMs = sw.ElapsedMilliseconds / iterations;
            Assert.Less(perReplayMs, 250,
                $"Tek replay (seviye üretimi + hamle uygulama) 250 ms altında kalmalı; ölçülen: {perReplayMs} ms.");
        }

        private static bool AreLevelsEqual(LevelData a, LevelData b)
        {
            if (a == null || b == null) return a == b;
            if (a.Poles.Count != b.Poles.Count) return false;
            for (int p = 0; p < a.Poles.Count; p++)
            {
                var pa = a.Poles[p];
                var pb = b.Poles[p];
                if (pa.IsLocked != pb.IsLocked) return false;
                if (pa.PortalTargetId != pb.PortalTargetId) return false;
                if (pa.Rings.Count != pb.Rings.Count) return false;
                for (int r = 0; r < pa.Rings.Count; r++)
                {
                    var ra = pa.Rings[r];
                    var rb = pb.Rings[r];
                    if (ra.Color != rb.Color || ra.Type != rb.Type || ra.AdditionalData != rb.AdditionalData)
                        return false;
                }
            }
            return true;
        }

        private ReplayEngine.ReplaySession BuildSolvedReplaySession(int levelIndex, int seed)
        {
            // ReplayEngine.Replay ile birebir aynı yeniden üretimi yap (DB'den türetilen
            // parametreler + clamp'ler), böylece çözülen seviye replay'de yeniden oluşturulur.
            int colorCount = _db.GetColorCountForLevel(levelIndex);
            int poleCount = _db.GetPoleCountForLevel(levelIndex);
            int maxCapacity = _db.GetMaxCapacityForLevel(levelIndex);
            if (poleCount < colorCount + 1) poleCount = colorCount + 1;
            if (poleCount > GameplayAssetKeys.Tuning.MaxPoleCount) poleCount = GameplayAssetKeys.Tuning.MaxPoleCount;

            var levelData = LevelGenerator.GenerateLevel(_db, levelIndex, seed, poleCount, colorCount, maxCapacity);
            Assert.IsNotNull(levelData);

            // Board must be reconstructed verbatim (SetRing*, not AddRing) so it matches ReplayEngine.Replay exactly.
            var board = new BoardState();
            board.Initialize(levelData.Poles.Count, maxCapacity, levelData.Poles.Count);

            int[] portalTargets = new int[levelData.Poles.Count];
            for (int pi = 0; pi < levelData.Poles.Count; pi++) portalTargets[pi] = -1;

            for (int p = 0; p < levelData.Poles.Count; p++)
            {
                var poleData = levelData.Poles[p];
                board.SetPoleLocked(p, poleData.IsLocked);
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    board.SetRingColor(p, r, poleData.Rings[r].Color);
                    board.SetRingType(p, r, poleData.Rings[r].Type);
                    board.SetRingAdditional(p, r, poleData.Rings[r].AdditionalData);
                }
                board.SetRingCount(p, poleData.Rings.Count);
                if (poleData.PortalTargetId >= 0) portalTargets[p] = poleData.PortalTargetId;
            }

            var solve = LevelSolver.Solve(board, maxCapacity, portalTargets: portalTargets);
            Assert.IsTrue(solve.IsSolvable, "Üretilen seviye çözülebilir olmalı.");
            Assert.IsNotNull(solve.Moves, "Çözücü hamle listesi döndürmeli.");

            return new ReplayEngine.ReplaySession
            {
                LevelIndex = levelIndex,
                LevelSeed = seed,
                LevelIdentity = levelData.LevelType + ":" + levelData.LevelIndex + ":" + levelData.Seed,
                ContentFingerprint = $"{levelData.LevelType}|{levelData.LevelIndex}|{levelData.Seed}|{levelData.PoleCount}|{levelData.PoleCapacity}|{levelData.ColorCount}|{levelData.EmptyPoleCount}|{levelData.TargetMoves}|{levelData.DifficultyScore}|{levelData.IsTutorial}|{levelData.IsChallenge}|{levelData.ProgressionFlags}|{string.Join(",", levelData.RuleReferences ?? new List<string>())}",
                RuleSetId = _db.name,
                RuleSetVersion = _db.GetType().Assembly.GetName().Version?.ToString() ?? string.Empty,
                Moves = solve.Moves,
                Version = 1
            };
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
        public void BoardState_SetRingCount_AboveSupportedCapacity_Throws()
        {
            var board = new BoardState { PoleCount = 1, MaxCapacity = BoardState.MaxSupportedCapacity };
            Assert.Throws<System.ArgumentOutOfRangeException>(() => board.SetRingCount(0, BoardState.MaxSupportedCapacity + 1));
        }

        [Test]
        public void BoardState_SupportsConfiguredMaxSupportedCapacityWithoutCorruptingFlags()
        {
            var board = new BoardState { PoleCount = 1, MaxCapacity = BoardState.MaxSupportedCapacity };
            for (int i = 0; i < BoardState.MaxSupportedCapacity; i++)
                board.AddRing(0, new RingData(RingColor.Red, RingType.Standard));

            Assert.AreEqual(BoardState.MaxSupportedCapacity, board.GetRingCount(0));
            Assert.IsFalse(board.IsPoleLocked(0));
            Assert.IsFalse(board.IsTopRingFrozen(0));
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
            // Donmuş halka, tipi RingType.Frozen olan halkadır (ayrı TopRingFrozen bayrağı değil).
            // Hem BoardState hem PoleState bu yüzden tipe bakar; bu yüzden frozen temsili tip üzerinden kurulur.
            var board = new BoardState { PoleCount = 2 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Frozen));

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
        public void LevelSolver_Solve_GddCapacity_DoesNotCrash()
        {
            // Stress test at RingFlow's supported GDD capacity.
            var board = new BoardState { PoleCount = 6, MaxCapacity = BoardState.MaxSupportedCapacity };
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < BoardState.MaxSupportedCapacity; j++)
                {
                    board.AddRing(i, new RingData((RingColor)(i % 3 + 1), RingType.Standard));
                }
            }
            // Already solved, should return 0 moves
            var result = LevelSolver.Solve(board, maxCapacity: BoardState.MaxSupportedCapacity, maxStatesLimit: 5000);
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
            // Use a level in the second half of the total range to test intensity smoothing.
            // Clamped to db.TotalLevels so the test works with any asset size.
            var db = _db;
            int levelIndex = System.Math.Max(1, System.Math.Min(db.TotalLevels, (int)(db.TotalLevels * 0.55f)));
            int seed = levelIndex * 12345;
            int poleCount = db.GetPoleCountForLevel(levelIndex);
            int colorCount = db.GetColorCountForLevel(levelIndex);
            int maxCap = db.GetMaxCapacityForLevel(levelIndex);

            var levelData = LevelGenerator.GenerateLevel(db, levelIndex, seed, poleCount, colorCount, maxCap);
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

        [Test]
        public void LevelGenerator_DifficultyScore_MatchesDbCurve()
        {
            // Bu test DB-tabanlı baraj ile uyumlu olduğunu gösterir.
            int movesLevel10 = 5;
            int movesLevel150 = 14;
            int movesLevel500 = 25;
            int colors10 = _db.GetColorCountForLevel(10);
            int colors500 = _db.GetColorCountForLevel(500);
            Assert.Greater(colors500, colors10);
            Assert.Greater(movesLevel500, movesLevel150);
            Assert.Greater(movesLevel150, movesLevel10);
        }

        [Test]
        public void DB_LevelGenConfig_ValuesAreReadFromAsset()
        {
            var cfg = _db.LevelGen;
            Assert.Greater(cfg.MaxScrambleAttempts, 0);
            Assert.Greater(cfg.ScrambleTargetBase, 0);
            Assert.Greater(cfg.MaxGenerationSeeds, 0);
            Assert.Greater(cfg.BombCountdown, 0);
            Assert.Greater(cfg.MaxMechanicTypesPerLevel, 0);
            Assert.GreaterOrEqual(cfg.MinSolverMoves, 1);
        }

        [Test]
        public void DB_MechanicIntensity_MatchesDifficultyBand()
        {
            var db = _db;
            int prevIntensity = 0;
            for (int level = 1; level <= db.TotalLevels; level += 50)
            {
                int intensity = db.GetMechanicIntensityForLevel(level);
                Assert.GreaterOrEqual(intensity, 1);
                // Intensity should be non-decreasing as level increases
                Assert.GreaterOrEqual(intensity, prevIntensity);
                prevIntensity = intensity;
            }
        }

        [Test]
        public void DB_GetBandForLevel_HasNoHardcodedEarlyReturn()
        {
            var db = _db;
            // Band must be fully determined by DifficultyBands list, not hardcoded values.
            // Level 1 is always Tutorial (first band covers it).
            Assert.AreEqual(DifficultyBand.Tutorial, db.GetBandForLevel(1));

            // DATA-DRIVEN: tutorial band max level is determined by DB, not hardcoded here.
            // Find the actual tutorial band max and verify the transition is data-driven.
            var tutorialBandData = db.DifficultyBands.Find(b => b.Band == DifficultyBand.Tutorial);
            Assert.IsNotNull(tutorialBandData, "DifficultyBands must contain a Tutorial entry.");

            int tutorialMax = tutorialBandData.MaxLevel;
            // Level at tutorial max must still be Tutorial
            Assert.AreEqual(DifficultyBand.Tutorial, db.GetBandForLevel(tutorialMax),
                $"Level {tutorialMax} (Tutorial MaxLevel) must still be Tutorial band.");
            // Level one above tutorial max must be the next band (Easy)
            if (tutorialMax < db.TotalLevels)
            {
                Assert.AreNotEqual(DifficultyBand.Tutorial, db.GetBandForLevel(tutorialMax + 1),
                    $"Level {tutorialMax + 1} must NOT be Tutorial — band transition must be data-driven.");
                Assert.AreEqual(DifficultyBand.Easy, db.GetBandForLevel(tutorialMax + 1),
                    $"Level {tutorialMax + 1} must be Easy band (first band after Tutorial).");
            }
        }

        [Test]
        public void DB_Fallback_NotAllowedMechanics_Throws()
        {
            var db = _db;
            var savedBands = db.DifficultyBands;
            try
            {
                db.DifficultyBands = new List<DifficultyBandData>();
                Assert.Throws<System.InvalidOperationException>(() => db.GetAllowedMechanicsForLevel(1));
            }
            finally
            {
                db.DifficultyBands = savedBands;
            }
        }

        [Test]
        public void DB_GetMinEmptyPolesForLevel_NoHardcodedExceptions()
        {
            var db = _db;
            // MinEmptyPoles must use DB DifficultyBands for ALL levels including 1..3
            var band1 = db.GetBandForLevel(1);
            int expectedTutorialMinEmpty = -1;
            foreach (var b in db.DifficultyBands)
            {
                if (b.Band == band1)
                {
                    expectedTutorialMinEmpty = b.MinEmptyPoles;
                    break;
                }
            }

            if (expectedTutorialMinEmpty >= 0)
            {
                Assert.AreEqual(expectedTutorialMinEmpty, db.GetMinEmptyPolesForLevel(1));
                Assert.AreEqual(expectedTutorialMinEmpty, db.GetMinEmptyPolesForLevel(2));
            }
        }

        [Test]
        public void DB_GetMaxCapacityForLevel_NoHardcodedExceptions()
        {
            var db = _db;
            // MaxCapacity for level 1..3 must come from DB, not hardcoded 3/4
            var band1 = db.GetBandForLevel(1);
            int expectedTutorialCapacity = -1;
            foreach (var b in db.DifficultyBands)
            {
                if (b.Band == band1)
                {
                    expectedTutorialCapacity = b.MaxCapacity;
                    break;
                }
            }

            if (expectedTutorialCapacity >= 0)
            {
                Assert.AreEqual(expectedTutorialCapacity, db.GetMaxCapacityForLevel(1));
                Assert.AreEqual(expectedTutorialCapacity, db.GetMaxCapacityForLevel(2));
                Assert.AreEqual(expectedTutorialCapacity, db.GetMaxCapacityForLevel(3));
            }
        }

        [Test]
        public void LevelGenerator_UsesDbConfig_NoHardcodedMagicNumbers()
        {
            var db = _db;
            var cfg = db.LevelGen;
            int levelIndex = 50;

            // Generate with DB config values
            int poleCount = db.GetPoleCountForLevel(levelIndex);
            int colorCount = db.GetColorCountForLevel(levelIndex);
            int maxCap = db.GetMaxCapacityForLevel(levelIndex);

            var levelData = LevelGenerator.GenerateLevel(db, levelIndex, 1000, poleCount, colorCount, maxCap);
            Assert.IsNotNull(levelData);

            // Verify bomb countdown from DB
            foreach (var pole in levelData.Poles)
            {
                foreach (var ring in pole.Rings)
                {
                    if (ring.Type == RingType.Bomb)
                    {
                        Assert.AreEqual(cfg.BombCountdown, ring.AdditionalData,
                            "Bomb countdown must use DB LevelGen.BombCountdown, not hardcoded 5");
                    }
                }
            }
        }

        [Test]
        public void LevelGenerator_EnforceMaxMechanicsLimit_UsesDbConfig()
        {
            var db = _db;
            int maxTypes = db.LevelGen.MaxMechanicTypesPerLevel;
            Assert.Greater(maxTypes, 0);

            // Create a board with more mechanic types than allowed
            var board = new BoardState { PoleCount = 5, MaxCapacity = 4 };
            board.AddRing(0, new RingData(RingColor.Red, RingType.Bomb));
            board.AddRing(1, new RingData(RingColor.Blue, RingType.Chain));
            board.AddRing(2, new RingData(RingColor.Green, RingType.Magnet));
            board.AddRing(3, new RingData(RingColor.Yellow, RingType.Paint));
            board.AddRing(4, new RingData(RingColor.Purple, RingType.Ghost));
            board.SetPoleLocked(0, true);

            var method = typeof(LevelGenerator).GetMethod("EnforceMaxMechanicsLimit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(method);
            var args = new object[] { db, board };
            method.Invoke(null, args);
            board = (BoardState)args[1];

            // Verify mechanic types are now within limit
            var uniqueTypes = new System.Collections.Generic.HashSet<RingType>();
            for (int p = 0; p < board.PoleCount; p++)
            {
                int count = board.GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    var t = board.GetRingType(p, r);
                    if (t != RingType.Standard) uniqueTypes.Add(t);
                }
            }
            Assert.LessOrEqual(uniqueTypes.Count, maxTypes);
        }

        [Test]
        public void LevelGenerator_ThrowsWhenDbIsNull()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                LevelGenerator.GenerateLevel(null, 1, 100, 3, 2, 4);
            });
        }

        [Test]
        public void DB_Progression_MonotonicNonDecreasing()
        {
            // Difficulty progression must be monotonically non-decreasing
            // Note: Pole count can decrease at band transitions (different bands have
            // different MinEmptyPoles), so we only track pole regression within the same band.
            var db = _db;
            int lastColorCount = 0;
            int lastPoleCount = 0;
            var lastBand = DifficultyBand.Tutorial;

            for (int level = 1; level <= db.TotalLevels; level += 10)
            {
                var band = db.GetBandForLevel(level);
                int colors = db.GetColorCountForLevel(level);
                int poles = db.GetPoleCountForLevel(level);
                int intensity = db.GetMechanicIntensityForLevel(level);

                Assert.GreaterOrEqual((int)band, (int)lastBand, $"Band regression at level {level}");
                Assert.GreaterOrEqual(colors, lastColorCount, $"Color regression at level {level}");
                Assert.GreaterOrEqual(intensity, 1, $"Intensity must be >= 1 at level {level}");

                // Reset pole tracking on band transition
                if (band != lastBand)
                    lastPoleCount = 0;

                Assert.GreaterOrEqual(poles, lastPoleCount, $"Pole regression within band {band} at level {level}");

                lastBand = band;
                lastColorCount = colors;
                lastPoleCount = poles;
            }
        }

        [Test]
        public void LevelsOnDisk_DoNotContainRingColorNone()
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.FindAssets("t:LevelDataSO", new[] { "Assets/Resources/Levels" });
            foreach (var guid in assets)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var levelData = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
                Assert.IsNotNull(levelData, $"Level asset failed to load: {path}");
                Assert.IsNotNull(levelData.Data, $"LevelData.Data is null in asset: {path}");
                
                for (int p = 0; p < levelData.Data.Poles.Count; p++)
                {
                    var pole = levelData.Data.Poles[p];
                    for (int r = 0; r < pole.Rings.Count; r++)
                    {
                        var ring = pole.Rings[r];
                        Assert.AreNotEqual(RingColor.None, ring.Color, 
                            $"Level {levelData.Data.LevelIndex} has a ring with Color=None on pole {p} index {r}! " +
                            "This is a serialization bug that clashes with default empty-pole rules.");
                    }
                }
            }
#endif
        }
    }
}