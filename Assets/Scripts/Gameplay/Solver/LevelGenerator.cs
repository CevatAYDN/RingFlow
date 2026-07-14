using System;
using System.Collections.Generic;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Tohum (seed) değerinden çözülebilir seviyeler üreten ve çözücüyü kullanarak
    /// seviyenin kilitlenmediğini (softlock) doğrulayan seviye üreticisi (Level Generator).
    /// </summary>
    public static class LevelGenerator
    {
        // Static readonly arrays are safe — used as temporary per-method scratch inside GenerateLevel,
        // which is always called from a single thread (editor or game startup).
        private static readonly int[] _sourcePoles = new int[GameplayAssetKeys.Tuning.MaxPoleCount];
        private static readonly int[] _targetPoles = new int[GameplayAssetKeys.Tuning.MaxPoleCount];
        // Pre-allocated ring backup list for EnforceEmptyPolesFloor (reused per GenerateLevel call)
        private static readonly List<RingData> _ringsBackup = new(GameplayAssetKeys.Tuning.MaxCapacity);
        // Static comparer to avoid lambda closure allocation in candidates.Sort()
        private static readonly Comparison<(LevelData level, float diff)> _candidateComparer =
            static (a, b) => a.diff.CompareTo(b.diff);

        /// <summary>
        /// Seviye endeksinden deterministik ve iyi ayrışmış (well-separated) bir tohum üretir.
        /// Komşu seviyeler için <c>100 + level</c> gibi bitişik tohumlar, üreticinin ardışık
        /// tohum penceresi (candidate window) taraması ile çakışıp aynı tahtayı/renk paletini
        /// tekrar ürettiği için renk çeşitliliğini bozuyordu (revizyon notları §2/§3/§4).
        /// Asal bir çarpanla her seviyeye çakışmayan bir tohum aralığı verilir; formül
        /// deterministiktir, bu yüzden aynı seviye her zaman aynı tohumu (dolayısıyla aynı
        /// çözülebilir seviyeyi) verir.
        /// </summary>
        public static int GetDeterministicSeed(int levelIndex)
        {
            return 1000 + (levelIndex * 7919);
        }

        public static LevelData GenerateLevel(GameConfigDatabaseSO db, int levelIndex, int seed, int poleCount, int colorCount, int maxCapacity)
        {
            if (db == null)
            {
                throw new System.ArgumentNullException(nameof(db), "GameConfigDatabaseSO is null — cannot generate level.");
            }

            if (maxCapacity > BoardState.MaxSupportedCapacity)
            {
                throw new System.InvalidOperationException(
                    $"MaxCapacity={maxCapacity} exceeds BoardState.MaxSupportedCapacity={BoardState.MaxSupportedCapacity}. " +
                    "Update GameConfigDatabaseSO DifficultyBands or expand BoardState bit packing before generating levels.");
            }

            var cfg = db.LevelGen;
            int bombCountdown = cfg.BombCountdown; // Local, thread-safe (was static mutable s_bombCountdown)
            int currentSeed = seed;
            int attempts = 0;
            var candidates = new List<(LevelData level, float diff)>();
            float targetScore = cfg.TargetScoreBase + (levelIndex / cfg.TargetScoreLevelDenominator) * cfg.TargetScoreMultiplier;

            while (attempts < cfg.MaxGenerationSeeds && candidates.Count < cfg.MaxCandidates)
            {
                var rand = new Random(currentSeed);
                var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };

                // 1. Bitmiş hali oluştur (Her direğe tek renk dolacak şekilde)
                var colors = (RingColor[])Enum.GetValues(typeof(RingColor));

                // Kullanılabilir tüm renkleri havuza al (None rengini atla, index 1'den başla)
                int availableColorCount = colors.Length - 1;

                // DB-driven: Renk sayısı enum kapasitesini aşıyorsa hard hatayı fırlat
                if (availableColorCount < colorCount)
                {
                    throw new System.InvalidOperationException(
                        $"DB {colorCount} renk istiyor ama RingColor enum'ında yalnızca {availableColorCount} " +
                        "renk kullanılabilir. RingColor enum'ını genişletin veya DB ColorCurve'u düşürün.");
                }

                // Renk çeşitliliği: Her seviyede sabit ilk N renk yerine, seed'e bağlı
                // deterministik Fisher-Yates karıştırma ile farklı renk kombinasyonları seçilir.
                // Böylece ardışık seviyeler aynı renk paletini tekrar etmez (revizyon notları §2/§3/§4).
                var selectedColors = new List<RingColor>(availableColorCount);
                for (int i = 1; i < colors.Length; i++)
                {
                    selectedColors.Add(colors[i]);
                }
                for (int i = selectedColors.Count - 1; i > 0; i--)
                {
                    int j = rand.Next(i + 1);
                    (selectedColors[i], selectedColors[j]) = (selectedColors[j], selectedColors[i]);
                }

                // Direkleri doldur
                for (int i = 0; i < colorCount; i++)
                {
                    var color = selectedColors[i];
                    for (int r = 0; r < maxCapacity; r++)
                    {
                        board.AddRingSimple(i, new RingData(color));
                    }
                }

                int minEmptyPoles = db.GetMinEmptyPolesForLevel(levelIndex);

                if (minEmptyPoles > poleCount - colorCount)
                {
                    throw new System.InvalidOperationException(
                        $"MinEmptyPoles {minEmptyPoles} > poleCount ({poleCount}) - colorCount ({colorCount}) for level {levelIndex}. " +
                        "Update DB DifficultyBands or ColorCurve.");
                }

                int untouchedPoles = System.Math.Max(0, minEmptyPoles - 1);
                int scramblePoleCount = poleCount - untouchedPoles;

                int validScrambleMoves = 0;
                int scrambleTarget = cfg.ScrambleTargetBase + rand.Next(cfg.ScrambleTargetRandomRange);
                int maxScrambleAttempts = cfg.MaxScrambleAttempts;
                int lastFrom = -1;
                int[] validSources = new int[scramblePoleCount];
                int[] validTargets = new int[scramblePoleCount];

                for (int attempt = 0; attempt < maxScrambleAttempts && validScrambleMoves < scrambleTarget; attempt++)
                {
                    int sourceCount = 0;
                    for (int p = 0; p < scramblePoleCount; p++)
                    {
                        if (board.IsEmpty(p)) continue;
                        if (p == lastFrom) continue;
                        validSources[sourceCount++] = p;
                    }
                    if (sourceCount == 0)
                    {
                        for (int p = 0; p < scramblePoleCount; p++)
                        {
                            if (!board.IsEmpty(p)) validSources[sourceCount++] = p;
                        }
                        if (sourceCount == 0) break;
                    }

                    int from = validSources[rand.Next(sourceCount)];
                    var fromRing = board.GetTopRing(from);

                    int validTargetCount = 0;
                    for (int p = 0; p < scramblePoleCount; p++)
                    {
                        if (p == from) continue;
                        if (board.GetRingCount(p) >= maxCapacity) continue;
                        validTargets[validTargetCount++] = p;
                    }

                    if (validTargetCount == 0) continue;
                    int to = validTargets[rand.Next(validTargetCount)];
                    var ring = board.PopRing(from);
                    board.AddRing(to, ring);
                    validScrambleMoves++;
                    lastFrom = to;
                }

                // GDD §5 Enforce MinEmptyPoles by compacting least occupied poles if needed
                EnforceEmptyPolesFloor(ref board, minEmptyPoles, maxCapacity);

                var scrambledState = board;

                // Portal pole pair array — all -1 by default
                int[] portalTargets = new int[poleCount];
                for (int pi = 0; pi < poleCount; pi++) portalTargets[pi] = -1;

                // GDD §4 & §5 Kuralları uyarınca özel halka mekaniklerini enjekte et
                InjectSpecialMechanics(db, ref scrambledState, levelIndex, rand, bombCountdown);

                // GDD §41: Portal pole çiftlerini enjekte et
                InjectPortalPoles(db, ref scrambledState, portalTargets, levelIndex, rand, minEmptyPoles);

                // Enforce that we successfully reached the minEmptyPoles count
                int finalEmptyCount = 0;
                for (int p = 0; p < scrambledState.PoleCount; p++)
                    if (scrambledState.IsEmpty(p)) finalEmptyCount++;

                if (finalEmptyCount < minEmptyPoles)
                {
                    currentSeed++;
                    attempts++;
                    continue;
                }

                // Solver budget from LevelGenConfig.SolverLimitBuckets — data-driven.
                int solverLimit = cfg.MaxSolverStatesLimit;
                if (cfg.SolverLimitBuckets != null)
                {
                    for (int b = 0; b < cfg.SolverLimitBuckets.Count; b++)
                    {
                        if (colorCount <= cfg.SolverLimitBuckets[b].MaxColorCount)
                        {
                            solverLimit = cfg.SolverLimitBuckets[b].StateLimit;
                            break;
                        }
                    }
                }
                int solverCapacity = maxCapacity;
                var solveResult = LevelSolver.Solve(scrambledState, solverCapacity,
                    maxStatesLimit: solverLimit, maxMovesLimit: cfg.DefaultMaxMovesLimit,
                    portalTargets: portalTargets);

                if (solveResult.IsSolvable && solveResult.MoveCount >= 2)
                {
                    var levelData = new LevelData
                    {
                        LevelIndex = levelIndex,
                        Seed = currentSeed,
                        TargetMoves = solveResult.MoveCount
                    };

                    for (int p = 0; p < poleCount; p++)
                    {
                        var poleData = new PoleData(maxCapacity)
                        {
                            IsLocked = scrambledState.IsPoleLocked(p),
                            PortalTargetId = portalTargets[p]
                        };

                        int count = scrambledState.GetRingCount(p);
                        for (int r = 0; r < count; r++)
                        {
                            var color = scrambledState.GetRingColor(p, r);
                            var type = scrambledState.GetRingType(p, r);
                            int additionalData = 0;
                            if (type == RingType.Bomb)
                            {
                                additionalData = cfg.BombCountdown;
                            }
                            else if (type == RingType.Chain)
                            {
                                additionalData = (int)color;
                            }
                            poleData.Rings.Add(new RingData(color, type, additionalData));
                        }
                        levelData.Poles.Add(poleData);
                    }

                    // GDD Zorluk Puanı hesaplama ve karşılaştırma
                    int specialCount = 0;
                    for (int p = 0; p < scrambledState.PoleCount; p++)
                    {
                        if (scrambledState.IsPoleLocked(p)) specialCount++;
                        int rCount = scrambledState.GetRingCount(p);
                        for (int r = 0; r < rCount; r++)
                        {
                            if (scrambledState.GetRingType(p, r) != RingType.Standard)
                                specialCount++;
                        }
                    }
                    float difficultyScore = (scrambledState.PoleCount * 2.5f) + (colorCount * 3.0f) + (solveResult.MoveCount * 0.8f) + (finalEmptyCount * -4.0f) + (specialCount * 5.0f);
                    float scoreDiff = Math.Abs(difficultyScore - targetScore);
                    candidates.Add((levelData, scoreDiff));
                }

                currentSeed++;
                attempts++;
            }

            if (candidates.Count > 0)
            {
                candidates.Sort(_candidateComparer); // static comparer — no lambda allocation
                var bestCandidate = candidates[0].level;
                
                NexusLog.Info("LevelGenerator", nameof(GenerateLevel), levelIndex.ToString(),
                    $"Solvable level optimized via GDD curve. Seed={bestCandidate.Seed}, Score diff={candidates[0].diff:F2}, Moves={bestCandidate.TargetMoves}. Candidates: {candidates.Count}");
                
                return bestCandidate;
            }

            NexusLog.Warn("LevelGenerator", nameof(GenerateLevel), levelIndex.ToString(),
                $"Exhausted {cfg.MaxGenerationSeeds} seeds without solver-detected solvable level. " +
                $"Increase MaxGenerationSeeds in DB LevelGen config or check seed distribution.");

            return null;
        }

        private static int CountEmptyPoles(BoardState board)
        {
            int empty = 0;
            for (int p = 0; p < board.PoleCount; p++)
            {
                if (board.IsEmpty(p)) empty++;
            }
            return empty;
        }

        private static int GetSmoothedIntensity(GameConfigDatabaseSO db, int levelIndex, int rawIntensity)
        {
            int transitionCount = db.LevelGen.TransitionLevelCount;
            if (transitionCount <= 0) return rawIntensity;

            var bands = db.DifficultyBands;
            if (bands == null || bands.Count <= 1) return rawIntensity;

            int levelCursor = 1;
            for (int i = 0; i < bands.Count; i++)
            {
                int bandEnd = bands[i].MaxLevel;
                if (levelIndex >= levelCursor && levelIndex <= bandEnd)
                {
                    if (i == 0) return rawIntensity;

                    int bandStart = levelCursor;
                    int offset = levelIndex - bandStart;

                    if (offset < transitionCount)
                    {
                        var prevBand = bands[i - 1];
                        int prevIntensity = Math.Max(1, prevBand.MechanicIntensity);

                        float t = (float)offset / transitionCount;
                        float smoothed = prevIntensity + t * (rawIntensity - prevIntensity);
                        return Math.Max(1, (int)Math.Floor(smoothed));
                    }

                    break;
                }
                levelCursor = bandEnd + 1;
            }

            return rawIntensity;
        }

        private static void InjectSpecialMechanics(GameConfigDatabaseSO db, ref BoardState board, int levelIndex, Random rand, int bombCountdown)
        {
            if (db == null) throw new System.ArgumentNullException(nameof(db));
            int worldIndex = db.GetWorldForLevel(levelIndex);
            var mechanic = db.GetMechanicForWorld(worldIndex);
            int rawIntensity = db.GetMechanicIntensityForLevel(levelIndex);
            int intensity = GetSmoothedIntensity(db, levelIndex, rawIntensity);
            var allowedMechanics = db.GetAllowedMechanicsForLevel(levelIndex);
            var theme = db.GetLevelThemeForLevel(levelIndex);

            if (theme.ForcedMechanics != null && theme.ForcedMechanics.Count > 0)
            {
                if (levelIndex % db.LevelsPerThemeStep == 0)
                    intensity += 1;
            }

            if (mechanic == WorldMechanicType.None && (theme.ForcedMechanics == null || theme.ForcedMechanics.Count == 0)) return;

            int minEmptyPoles = db.GetMinEmptyPolesForLevel(levelIndex);
            int occupiedPoles = 0;
            for (int p = 0; p < board.PoleCount; p++)
                if (!board.IsEmpty(p)) occupiedPoles++;
            int emptyPoles = board.PoleCount - occupiedPoles;
            if (emptyPoles < minEmptyPoles)
            {
                NexusLog.Warn("LevelGenerator", nameof(InjectSpecialMechanics), levelIndex.ToString(),
                    $"Empty-pole floor violated for level {levelIndex}: empty={emptyPoles}, required>={minEmptyPoles}.");
            }

            int mechanicCount = intensity;

            if (mechanic == WorldMechanicType.Mystery)
                InjectSingleType(ref board, RingType.Mystery, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Frozen)
                InjectFrozen(ref board, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.LockedPole)
                for (int i = 0; i < mechanicCount; i++) InjectLockedPole(ref board, rand);
            else if (mechanic == WorldMechanicType.Stone)
                InjectSingleType(ref board, RingType.Stone, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Glass)
                InjectSingleType(ref board, RingType.Glass, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Rainbow)
                InjectSingleType(ref board, RingType.Rainbow, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Bomb)
                InjectSingleType(ref board, RingType.Bomb, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Chain)
                InjectSingleType(ref board, RingType.Chain, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Magnet)
                InjectSingleType(ref board, RingType.Magnet, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Paint)
                InjectSingleType(ref board, RingType.Paint, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.Ghost)
                InjectSingleType(ref board, RingType.Ghost, mechanicCount, rand, bombCountdown);
            else if (mechanic == WorldMechanicType.RandomPool1 ||
                     mechanic == WorldMechanicType.RandomPool2 ||
                     mechanic == WorldMechanicType.RandomPool3)
            {
                var availableTypes = new List<RingType>();
                if (allowedMechanics.Contains(WorldMechanicType.Stone)) availableTypes.Add(RingType.Stone);
                if (allowedMechanics.Contains(WorldMechanicType.Glass)) availableTypes.Add(RingType.Glass);
                if (allowedMechanics.Contains(WorldMechanicType.Rainbow)) availableTypes.Add(RingType.Rainbow);
                if (allowedMechanics.Contains(WorldMechanicType.Bomb)) availableTypes.Add(RingType.Bomb);
                if (allowedMechanics.Contains(WorldMechanicType.Chain)) availableTypes.Add(RingType.Chain);
                if (allowedMechanics.Contains(WorldMechanicType.Magnet)) availableTypes.Add(RingType.Magnet);
                if (allowedMechanics.Contains(WorldMechanicType.Paint)) availableTypes.Add(RingType.Paint);
                if (allowedMechanics.Contains(WorldMechanicType.Ghost)) availableTypes.Add(RingType.Ghost);

                if (availableTypes.Count == 0) return;

                int numMechanicTypes = 1;
                if (mechanic == WorldMechanicType.RandomPool3) numMechanicTypes = 3;
                else if (mechanic == WorldMechanicType.RandomPool2) numMechanicTypes = 2;
                numMechanicTypes = Math.Min(numMechanicTypes, Math.Min(mechanicCount, availableTypes.Count));

                var chosenTypes = new List<RingType>();
                for (int i = 0; i < numMechanicTypes; i++)
                {
                    int idx = rand.Next(availableTypes.Count);
                    chosenTypes.Add(availableTypes[idx]);
                    availableTypes.RemoveAt(idx);
                }

                for (int i = 0; i < chosenTypes.Count; i++)
                    InjectSingleType(ref board, chosenTypes[i], mechanicCount, rand, bombCountdown);
            }

            EnforceMaxMechanicsLimit(db, ref board);
            EnforceMechanicCompatibility(ref board);
        }

        private static void InjectSingleType(ref BoardState board, RingType type, int maxCount, Random rand, int bombCountdown)
        {
            // Validate bomb countdown fits in 4-bit BoardState storage
            if (type == RingType.Bomb && bombCountdown > 15)
            {
                throw new System.InvalidOperationException(
                    $"BombCountdown={bombCountdown} exceeds 4-bit BoardState limit (max 15). " +
                    "Update DB LevelGen.BombCountdown to a valid value (0-15).");
            }

            int poleCount = board.PoleCount;
            int count = 0;
            for (int attempt = 0; attempt < 20 && count < maxCount; attempt++)
            {
                int p = rand.Next(poleCount);
                int ringCount = board.GetRingCount(p);
                if (ringCount > 0)
                {
                    int r = rand.Next(ringCount);
                    if (type == RingType.Stone)
                    {
                        r = 0;
                    }
                    if (board.GetRingType(p, r) == RingType.Standard)
                    {
                        board.SetRingType(p, r, type);
                        if (type == RingType.Bomb)
                        {
                            board.SetRingAdditional(p, r, bombCountdown);
                        }
                        else if (type == RingType.Chain)
                        {
                            board.SetRingAdditional(p, r, (int)board.GetRingColor(p, r));
                        }
                        count++;
                    }
                }
            }
        }

        private static void InjectFrozen(ref BoardState board, int maxCount, Random rand)
        {
            int poleCount = board.PoleCount;
            int count = 0;
            for (int attempt = 0; attempt < 20 && count < maxCount; attempt++)
            {
                int p = rand.Next(poleCount);
                int ringCount = board.GetRingCount(p);
                if (ringCount > 0)
                {
                    int r = rand.Next(ringCount);
                    if (board.GetRingType(p, r) == RingType.Standard)
                    {
                        board.SetRingType(p, r, RingType.Frozen);
                        if (r == ringCount - 1)
                        {
                            board.SetTopRingFrozen(p, true);
                        }
                        count++;
                    }
                }
            }
        }

        private static void InjectLockedPole(ref BoardState board, Random rand)
        {
            int poleCount = board.PoleCount;
            int lockedPole = -1;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int p = rand.Next(poleCount);
                if (board.IsEmpty(p))
                {
                    board.SetPoleLocked(p, true);
                    lockedPole = p;
                    break;
                }
            }

            if (lockedPole == -1)
            {
                int bestPole = 0;
                int minRings = GameplayAssetKeys.Tuning.SentinelMinRings;
                for (int p = 0; p < poleCount; p++)
                {
                    int rc = board.GetRingCount(p);
                    if (rc < minRings)
                    {
                        minRings = rc;
                        bestPole = p;
                    }
                }

                while (board.GetRingCount(bestPole) > 0)
                {
                    var ringToMove = board.GetTopRing(bestPole);
                    board.PopRing(bestPole);
                    for (int target = 0; target < poleCount; target++)
                    {
                        if (target == bestPole) continue;
                        if (board.GetRingCount(target) < board.MaxCapacity)
                        {
                            board.AddRing(target, ringToMove);
                            break;
                        }
                    }
                }
                board.SetPoleLocked(bestPole, true);
                lockedPole = bestPole;
            }

            if (lockedPole != -1)
            {
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int p = rand.Next(poleCount);
                    if (p == lockedPole) continue;
                    int ringCount = board.GetRingCount(p);
                    if (ringCount > 0)
                    {
                        bool hasFreeEmptyPole = false;
                        for (int x = 0; x < poleCount; x++)
                        {
                            if (x != lockedPole && board.IsEmpty(x))
                            {
                                hasFreeEmptyPole = true;
                                break;
                            }
                        }

                        int r = hasFreeEmptyPole ? rand.Next(ringCount) : (ringCount - 1);
                        if (board.GetRingType(p, r) == RingType.Standard)
                        {
                            board.SetRingType(p, r, RingType.Locked);
                            break;
                        }
                    }
                }
            }
        }

        private static void EnforceMaxMechanicsLimit(GameConfigDatabaseSO db, ref BoardState board)
        {
            int maxTypes = db.LevelGen.MaxMechanicTypesPerLevel;

            var uniqueTypes = new HashSet<RingType>();
            for (int p = 0; p < board.PoleCount; p++)
            {
                if (board.IsPoleLocked(p))
                {
                    uniqueTypes.Add(RingType.Locked);
                }

                int count = board.GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    var t = board.GetRingType(p, r);
                    if (t != RingType.Standard)
                    {
                        uniqueTypes.Add(t);
                    }
                }
            }

            if (uniqueTypes.Count <= maxTypes)
            {
                return;
            }

            var priorityOrder = db.LevelGen.MechanicPriorityOrder;
            if (priorityOrder == null || priorityOrder.Count == 0)
            {
                throw new System.InvalidOperationException(
                    "MechanicPriorityOrder DB'de tanımlı değil. Lütfen LevelGen.MechanicPriorityOrder listesini doldurun.");
            }

            var allowed = new List<RingType>(maxTypes);
            for (int i = 0; i < priorityOrder.Count && allowed.Count < maxTypes; i++)
            {
                var ringType = (RingType)priorityOrder[i];
                if (uniqueTypes.Contains(ringType))
                {
                    allowed.Add(ringType);
                }
            }

            if (allowed.Count == 0)
            {
                throw new System.InvalidOperationException(
                    "Hiçbir mekanik türü priority order'da bulunamadı. " +
                    "MechanicPriorityOrder, board'daki mekaniklerle eşleşecek şekilde güncellenmelidir.");
            }

            for (int p = 0; p < board.PoleCount; p++)
            {
                if (board.IsPoleLocked(p) && !allowed.Contains(RingType.Locked))
                {
                    board.SetPoleLocked(p, false);
                }

                int count = board.GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    var t = board.GetRingType(p, r);
                    if (t != RingType.Standard && !allowed.Contains(t))
                    {
                        board.SetRingType(p, r, RingType.Standard);
                    }
                }
            }
        }

        private static void EnforceMechanicCompatibility(ref BoardState board)
        {
            for (int p = 0; p < board.PoleCount; p++)
            {
                if (board.IsPoleLocked(p))
                {
                    // Locked poles must not contain Stone or Bomb rings
                    int count = board.GetRingCount(p);
                    for (int r = 0; r < count; r++)
                    {
                        var t = board.GetRingType(p, r);
                        if (t == RingType.Stone || t == RingType.Bomb)
                        {
                            board.SetRingType(p, r, RingType.Standard);
                        }
                    }
                }
            }
        }

        private static void InjectPortalPoles(GameConfigDatabaseSO db, ref BoardState board,
            int[] portalTargets, int levelIndex, Random rand, int minEmptyPoles)
        {
            int worldIndex = db.GetWorldForLevel(levelIndex);
            var mechanic = db.GetMechanicForWorld(worldIndex);
            if (mechanic != WorldMechanicType.Portal) return;

            int intensity = db.GetMechanicIntensityForLevel(levelIndex);
            int poleCount = board.PoleCount;

            // Her portal çifti için 2 pole gerekir. intensity kadar çift üret.
            int pairCount = Math.Min(intensity, poleCount / 2);
            if (pairCount < 1) return;

            // Kullanılabilir pole'lar: portal olmayan, tercihen boş
            var available = new List<int>();
            for (int p = 0; p < poleCount; p++)
            {
                if (portalTargets[p] >= 0) continue;
                available.Add(p);
            }

            if (available.Count < pairCount * 2) return;

            for (int i = 0; i < pairCount; i++)
            {
                int idxA = rand.Next(available.Count);
                int poleA = available[idxA];
                available.RemoveAt(idxA);

                int idxB = rand.Next(available.Count);
                int poleB = available[idxB];
                available.RemoveAt(idxB);

                portalTargets[poleA] = poleB;
                portalTargets[poleB] = poleA;
            }
        }

        private static void EnforceEmptyPolesFloor(ref BoardState board, int minEmptyPoles, int maxCapacity)
        {
            int currentEmptyCount = 0;
            for (int p = 0; p < board.PoleCount; p++)
                if (board.IsEmpty(p)) currentEmptyCount++;

            int attemptsToEmpty = 0;
            while (currentEmptyCount < minEmptyPoles && attemptsToEmpty < 10)
            {
                attemptsToEmpty++;
                int bestPoleToEmpty = -1;
                int minRings = GameplayAssetKeys.Tuning.SentinelMinRings;
                for (int p = 0; p < board.PoleCount; p++)
                {
                    if (board.IsEmpty(p) || board.IsPoleLocked(p)) continue;
                    int rc = board.GetRingCount(p);
                    if (rc < minRings)
                    {
                        minRings = rc;
                        bestPoleToEmpty = p;
                    }
                }

                if (bestPoleToEmpty == -1) break;

                // Reuse pre-allocated backup list — avoids new List<> on every iteration (was alloc_in_loop)
                _ringsBackup.Clear();
                bool success = true;
                int rcToMove = board.GetRingCount(bestPoleToEmpty);
                for (int r = rcToMove - 1; r >= 0; r--)
                {
                    _ringsBackup.Add(new RingData(
                        board.GetRingColor(bestPoleToEmpty, r),
                        board.GetRingType(bestPoleToEmpty, r),
                        board.GetRingAdditional(bestPoleToEmpty, r)));
                }

                board.SetRingCount(bestPoleToEmpty, 0);

                for (int ri = 0; ri < _ringsBackup.Count; ri++)
                {
                    var ring = _ringsBackup[ri];
                    bool placed = false;
                    for (int p = 0; p < board.PoleCount; p++)
                    {
                        if (p == bestPoleToEmpty || board.IsPoleLocked(p) || board.IsEmpty(p)) continue;
                        if (board.GetRingCount(p) < maxCapacity)
                        {
                            board.AddRingSimple(p, ring);
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    currentEmptyCount++;
                }
                else
                {
                    board.SetRingCount(bestPoleToEmpty, 0);
                    for (int r = _ringsBackup.Count - 1; r >= 0; r--)
                    {
                        board.AddRingSimple(bestPoleToEmpty, _ringsBackup[r]);
                    }
                    break;
                }
            }
        }
    }
}
