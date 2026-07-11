using System;
using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Tohum (seed) değerinden çözülebilir seviyeler üreten ve çözücüyü kullanarak
    /// seviyenin kilitlenmediğini (softlock) doğrulayan seviye üreticisi (Level Generator).
    /// </summary>
    public static class LevelGenerator
    {
        private static readonly int[] _sourcePoles = new int[12];
        private static readonly int[] _targetPoles = new int[12];

        public static LevelData GenerateLevel(int levelIndex, int seed, int poleCount, int colorCount, int maxCapacity)
        {
            int currentSeed = seed;
            int attempts = 0;

            while (attempts < 50) // GDD: Up to 50 seed retry limit for softlock protection
            {
                var rand = new Random(currentSeed);
                var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };

                // 1. Bitmiş hali oluştur (Her direğe tek renk dolacak şekilde)
                var colors = (RingColor[])Enum.GetValues(typeof(RingColor));
                
                // Kullanılabilir renkleri belirle (None rengini atla, index 1'den başla)
                var selectedColors = new List<RingColor>();
                for (int i = 1; i < colors.Length && selectedColors.Count < colorCount; i++)
                {
                    selectedColors.Add(colors[i]);
                }

                // Renk sayısı yeterli değilse fallback
                if (selectedColors.Count < colorCount)
                {
                    colorCount = selectedColors.Count;
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

                int minEmptyPoles = DifficultyCurve.MinEmptyPolesForLevel(levelIndex);
                if (minEmptyPoles < 1) minEmptyPoles = 1;
                if (minEmptyPoles > poleCount - colorCount)
                {
                    minEmptyPoles = poleCount - colorCount;
                }

                int untouchedPoles = System.Math.Max(0, minEmptyPoles - 1);
                int scramblePoleCount = poleCount - untouchedPoles;
                if (scramblePoleCount < colorCount + minEmptyPoles) scramblePoleCount = colorCount + minEmptyPoles;
                if (scramblePoleCount > poleCount) scramblePoleCount = poleCount;

                int validScrambleMoves = 0;
                int scrambleTarget = 150 + rand.Next(80);
                const int maxScrambleAttempts = 1500;
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

                // GDD §4 & §5 Kuralları uyarınca özel halka mekaniklerini enjekte et
                InjectSpecialMechanics(ref scrambledState, levelIndex, rand);



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

                // 3. Çözülebilirliği ve optimal hamle sayısını test et
                // Solver limit scales down with complexity to keep editor iteration fast:
                // fewer colors → bigger budget (fast solve), 10 colors → tighter budget
                int solverLimit = colorCount <= 3 ? 20000 : colorCount <= 5 ? 15000 : colorCount <= 7 ? 12000 : colorCount <= 9 ? 8000 : 6000;
                int solverCapacity = maxCapacity;
                var solveResult = LevelSolver.Solve(scrambledState, solverCapacity, maxStatesLimit: solverLimit);

                if (solveResult.IsSolvable && solveResult.MoveCount > 0)
                {
                    if (solveResult.MoveCount < 2)
                    {
                        currentSeed++;
                        attempts++;
                        continue;
                    }

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
                            IsLocked = scrambledState.IsPoleLocked(p)
                        };

                        int count = scrambledState.GetRingCount(p);
                        for (int r = 0; r < count; r++)
                        {
                            var color = scrambledState.GetRingColor(p, r);
                            var type = scrambledState.GetRingType(p, r);
                            int additionalData = 0;
                            if (type == RingType.Bomb)
                            {
                                additionalData = 5; // GDD: Bomb (sayaç 5→0)
                            }
                            else if (type == RingType.Chain)
                            {
                                additionalData = (int)color;
                            }
                            poleData.Rings.Add(new RingData(color, type, additionalData));
                        }
                        levelData.Poles.Add(poleData);
                    }

                    if (attempts > 0)
                    {
                        NexusLog.Info("LevelGenerator", nameof(GenerateLevel), levelIndex.ToString(),
                            $"Solvable level found on seed retry {attempts} (target moves={solveResult.MoveCount}).");
                    }

                    return levelData;
                }

                // Çözülemiyorsa (veya kilitlendiyse) tohumu değiştirip tekrar dene
                currentSeed++;
                attempts++;
            }

            NexusLog.Warn("LevelGenerator", nameof(GenerateLevel), levelIndex.ToString(),
                $"Exhausted 50 seeds without solver-detected solvable level. Increase MaxStatesLimit or check seed distribution.");

            // Son çare: tutorial override yapmadan, minimum parametrelerle deneme
            if (levelIndex <= 3)
            {
                return GenerateFallbackLevel(levelIndex, poleCount, colorCount, maxCapacity);
            }

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

        private static LevelData GenerateFallbackLevel(int levelIndex, int poleCount, int colorCount, int maxCapacity)
        {
            var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };
            var colors = (RingColor[])Enum.GetValues(typeof(RingColor));
            var selectedColors = new List<RingColor>();
            for (int i = 1; i <= colorCount && i < colors.Length; i++)
            {
                selectedColors.Add(colors[i]);
            }
            if (selectedColors.Count < 1)
            {
                selectedColors.Add(RingColor.Red);
                colorCount = 1;
            }

            for (int i = 0; i < colorCount; i++)
            {
                for (int r = 0; r < maxCapacity; r++)
                {
                    board.AddRingSimple(i, new RingData(selectedColors[i]));
                }
            }

            // Basit karıştırma: ilk direkten son direğe tek hamle
            if (board.GetRingCount(0) > 0)
            {
                var ring = board.PopRing(0);
                board.AddRing(poleCount - 1, ring);
            }

            var solveResult = LevelSolver.Solve(board, maxCapacity, maxStatesLimit: 5000);
            if (solveResult.IsSolvable && solveResult.MoveCount > 0)
            {
                var levelData = new LevelData
                {
                    LevelIndex = levelIndex,
                    Seed = 0,
                    TargetMoves = solveResult.MoveCount
                };

                for (int p = 0; p < poleCount; p++)
                {
                    var poleData = new PoleData(maxCapacity)
                    {
                        IsLocked = board.IsPoleLocked(p)
                    };
                    int count = board.GetRingCount(p);
                    for (int r = 0; r < count; r++)
                    {
                        var color = board.GetRingColor(p, r);
                        var type = board.GetRingType(p, r);
                        int additionalData = 0;
                        if (type == RingType.Bomb) additionalData = 5;
                        else if (type == RingType.Chain) additionalData = (int)color;
                        poleData.Rings.Add(new RingData(color, type, additionalData));
                    }
                    levelData.Poles.Add(poleData);
                }
                return levelData;
            }

            return null;
        }

        private static void InjectSpecialMechanics(ref BoardState board, int levelIndex, Random rand)
        {
            var db = GameConfigDatabaseSO.Instance;
            int worldIndex = WorldConfigSO.WorldFromAbsoluteLevel(levelIndex);
            var mechanic = db.GetMechanicForWorld(worldIndex);
            int intensity = db.GetMechanicIntensityForLevel(levelIndex);
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
                InjectSingleType(ref board, RingType.Mystery, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Frozen)
                InjectFrozen(ref board, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.LockedPole)
                for (int i = 0; i < mechanicCount; i++) InjectLockedPole(ref board, rand);
            else if (mechanic == WorldMechanicType.Stone)
                InjectSingleType(ref board, RingType.Stone, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Glass)
                InjectSingleType(ref board, RingType.Glass, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Rainbow)
                InjectSingleType(ref board, RingType.Rainbow, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Bomb)
                InjectSingleType(ref board, RingType.Bomb, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Chain)
                InjectSingleType(ref board, RingType.Chain, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Magnet)
                InjectSingleType(ref board, RingType.Magnet, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Paint)
                InjectSingleType(ref board, RingType.Paint, mechanicCount, rand);
            else if (mechanic == WorldMechanicType.Ghost)
                InjectSingleType(ref board, RingType.Ghost, mechanicCount, rand);
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
                    InjectSingleType(ref board, chosenTypes[i], mechanicCount, rand);
            }

            EnforceMaxMechanicsLimit(ref board);
            EnforceMechanicCompatibility(ref board);
        }

        private static void InjectSingleType(ref BoardState board, RingType type, int maxCount, Random rand)
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
                    if (type == RingType.Stone)
                    {
                        r = 0;
                    }
                    if (board.GetRingType(p, r) == RingType.Standard)
                    {
                        board.SetRingType(p, r, type);
                        if (type == RingType.Bomb)
                        {
                            board.SetRingAdditional(p, r, 5);
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
                int minRings = 999;
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
                        if (board.GetRingCount(target) < 4)
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

        private static void EnforceMaxMechanicsLimit(ref BoardState board)
        {
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

            if (uniqueTypes.Count <= 4)
            {
                return;
            }

            var priority = new[]
            {
                RingType.Locked,
                RingType.Mystery,
                RingType.Frozen,
                RingType.Stone,
                RingType.Glass,
                RingType.Rainbow,
                RingType.Bomb,
                RingType.Chain,
                RingType.Magnet,
                RingType.Paint,
                RingType.Ghost
            };

            var allowed = new List<RingType>(4);
            for (int i = 0; i < priority.Length && allowed.Count < 4; i++)
            {
                if (uniqueTypes.Contains(priority[i]))
                {
                    allowed.Add(priority[i]);
                }
            }

            if (allowed.Count == 0)
            {
                allowed.Add(RingType.Mystery);
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
                int minRings = 999;
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

                bool success = true;
                var ringsBackup = new List<RingData>();
                int rcToMove = board.GetRingCount(bestPoleToEmpty);
                for (int r = rcToMove - 1; r >= 0; r--)
                {
                    ringsBackup.Add(new RingData(
                        board.GetRingColor(bestPoleToEmpty, r),
                        board.GetRingType(bestPoleToEmpty, r),
                        board.GetRingAdditional(bestPoleToEmpty, r)));
                }

                board.SetRingCount(bestPoleToEmpty, 0);

                foreach (var ring in ringsBackup)
                {
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
                    for (int r = ringsBackup.Count - 1; r >= 0; r--)
                    {
                        board.AddRingSimple(bestPoleToEmpty, ringsBackup[r]);
                    }
                    break;
                }
            }
        }
    }
}
