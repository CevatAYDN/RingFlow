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

            while (attempts < 50) // GDD: En fazla 50 tohum deneme limiti (softlock koruması)
            {
                var rand = new Random(currentSeed);
                var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };

                // 1. Bitmiş hali oluştur (Her direğe tek renk dolacak şekilde)
                var colors = (RingColor[])Enum.GetValues(typeof(RingColor));
                
                // Kullanılabilir renkleri belirle (None rengini atla, index 1'den başla)
                var selectedColors = new List<RingColor>();
                for (int i = 1; i <= colorCount && i < colors.Length; i++)
                {
                    selectedColors.Add(colors[i]);
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

                int validScrambleMoves = 0;
                int scrambleTarget = 150 + rand.Next(80);
                const int maxScrambleAttempts = 1500;
                int lastFrom = -1;
                int[] validSources = new int[poleCount];
                int[] validTargets = new int[poleCount];

                for (int attempt = 0; attempt < maxScrambleAttempts && validScrambleMoves < scrambleTarget; attempt++)
                {
                    int sourceCount = 0;
                    for (int p = 0; p < poleCount; p++)
                    {
                        if (board.IsEmpty(p)) continue;
                        if (p == lastFrom) continue;
                        validSources[sourceCount++] = p;
                    }
                    if (sourceCount == 0)
                    {
                        for (int p = 0; p < poleCount; p++)
                        {
                            if (!board.IsEmpty(p)) validSources[sourceCount++] = p;
                        }
                        if (sourceCount == 0) break;
                    }

                    int from = validSources[rand.Next(sourceCount)];
                    var fromRing = board.GetTopRing(from);

                    int validTargetCount = 0;
                    for (int p = 0; p < poleCount; p++)
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

                // GDD §4 & §5 Kuralları uyarınca özel halka mekaniklerini enjekte et
                InjectSpecialMechanics(ref board, levelIndex, rand);

                var scrambledState = board;

                // 3. Çözülebilirliği ve optimal hamle sayısını test et
                var solveResult = LevelSolver.Solve(scrambledState, maxCapacity, maxStatesLimit: 5000);

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

            return null;
        }

        private static void InjectSpecialMechanics(ref BoardState board, int levelIndex, Random rand)
        {
            int worldIndex = WorldConfigSO.WorldFromAbsoluteLevel(levelIndex);
            var mechanic = GameConfigDatabaseSO.Instance.GetMechanicForWorld(worldIndex);
            
            if (mechanic == WorldMechanicType.None) return;

            int poleCount = board.PoleCount;

            // GDD §4: Map each mechanic specifically to teach in 1 world
            if (mechanic == WorldMechanicType.Mystery)
            {
                InjectSingleType(ref board, RingType.Mystery, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Frozen)
            {
                InjectFrozen(ref board, 2, rand);
            }
            else if (mechanic == WorldMechanicType.LockedPole)
            {
                InjectLockedPole(ref board, rand);
            }
            else if (mechanic == WorldMechanicType.Stone)
            {
                InjectSingleType(ref board, RingType.Stone, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Glass)
            {
                InjectSingleType(ref board, RingType.Glass, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Rainbow)
            {
                InjectSingleType(ref board, RingType.Rainbow, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Bomb)
            {
                InjectSingleType(ref board, RingType.Bomb, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Chain)
            {
                InjectSingleType(ref board, RingType.Chain, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Magnet)
            {
                InjectSingleType(ref board, RingType.Magnet, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Paint)
            {
                InjectSingleType(ref board, RingType.Paint, 2, rand);
            }
            else if (mechanic == WorldMechanicType.Ghost)
            {
                InjectSingleType(ref board, RingType.Ghost, 2, rand);
            }
            // Advanced mechanics pool (RandomPool1/2/3)
            else
            {
                var availableTypes = new[] {
                    RingType.Rainbow,
                    RingType.Bomb,
                    RingType.Chain,
                    RingType.Magnet,
                    RingType.Paint,
                    RingType.Ghost,
                    RingType.Stone,
                    RingType.Glass
                };

                int numMechanicTypes = 1;
                if (mechanic == WorldMechanicType.RandomPool3) numMechanicTypes = 3;
                else if (mechanic == WorldMechanicType.RandomPool2) numMechanicTypes = 2;

                var chosenTypes = new List<RingType>();
                for (int i = 0; i < numMechanicTypes; i++)
                {
                    chosenTypes.Add(availableTypes[rand.Next(availableTypes.Length)]);
                }

                foreach (var chosenType in chosenTypes)
                {
                    InjectSingleType(ref board, chosenType, 2, rand);
                }
            }

            EnforceMaxMechanicsLimit(ref board);
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

            if (uniqueTypes.Count > 4)
            {
                var allowed = new List<RingType>(uniqueTypes);
                while (allowed.Count > 4)
                {
                    allowed.RemoveAt(allowed.Count - 1);
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
        }
    }
}
