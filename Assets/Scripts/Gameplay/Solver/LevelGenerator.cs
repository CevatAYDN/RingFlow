using System;
using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Tohum (seed) değerinden çözülebilir seviyeler üreten ve çözücüyü kullanarak
    /// seviyenin kilitlenmediğini (softlock) doğrulayan seviye üreticisi (Level Generator).
    /// </summary>
    public static class LevelGenerator
    {
        private static readonly int[] _sourcePoles = new int[10];
        private static readonly int[] _targetPoles = new int[10];

        public static LevelData GenerateLevel(int levelIndex, int seed, int poleCount, int colorCount, int maxCapacity)
        {
            int currentSeed = seed;
            int attempts = 0;

            while (attempts < 50) // GDD: En fazla 50 tohum deneme limiti (softlock koruması)
            {
                var rand = new Random(currentSeed);
                var board = new BoardState { PoleCount = poleCount };

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
                        board.AddRing(i, new RingData(color));
                    }
                }

                // 2. Karıştır (Scramble - Tersten geçerli rastgele hamleler yap)
                int scrambleMoves = 30 + rand.Next(20);
                for (int s = 0; s < scrambleMoves; s++)
                {
                    int sourceCount = 0;
                    int targetCount = 0;

                    for (int p = 0; p < poleCount; p++)
                    {
                        if (!board.IsEmpty(p))
                        {
                            _sourcePoles[sourceCount++] = p;
                        }
                        if (board.GetRingCount(p) < maxCapacity)
                        {
                            _targetPoles[targetCount++] = p;
                        }
                    }

                    if (sourceCount == 0 || targetCount == 0) break;

                    int from = _sourcePoles[rand.Next(sourceCount)];
                    int to = _targetPoles[rand.Next(targetCount)];

                    if (from == to) continue;

                    // Halka rengini tersten taşı (renk uyumu aramaksızın)
                    var ring = board.PopRing(from);
                    board.AddRing(to, ring);
                }

                // GDD §4 & §5 Kuralları uyarınca özel halka mekaniklerini enjekte et
                InjectSpecialMechanics(ref board, levelIndex, rand);

                var scrambledState = board;

                // 3. Çözülebilirliği ve optimal hamle sayısını test et
                var solveResult = LevelSolver.Solve(scrambledState, maxCapacity);

                if (solveResult.IsSolvable && solveResult.MoveCount > 0)
                {
                    // Çözülebilir bir seviye oluşturuldu, veri modeline çevir
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
                            poleData.Rings.Add(new RingData(color, type));
                        }
                        levelData.Poles.Add(poleData);
                    }

                    return levelData;
                }

                // Çözülemiyorsa (veya kilitlendiyse) tohumu değiştirip tekrar dene
                currentSeed++;
                attempts++;
            }

            return null; // 50 denemede de başarılı olamazsa (teorik olarak imkansız)
        }

        private static void InjectSpecialMechanics(ref BoardState board, int levelIndex, Random rand)
        {
            int worldIndex = WorldConfigSO.WorldFromAbsoluteLevel(levelIndex);
            
            // World 1: Özel mekanik yok
            if (worldIndex == 0) return;

            int poleCount = board.PoleCount;

            // World 2: Mystery (Gizemli)
            if (worldIndex == 1)
            {
                // En üstte olmayan 1 ya da 2 halkayı Mystery yap
                int count = 0;
                for (int attempt = 0; attempt < 10 && count < 2; attempt++)
                {
                    int p = rand.Next(poleCount);
                    int ringCount = board.GetRingCount(p);
                    if (ringCount >= 2)
                    {
                        int r = rand.Next(ringCount - 1); // En üst halka hariç
                        if (board.GetRingType(p, r) == RingType.Standard)
                        {
                            board.SetRingType(p, r, RingType.Mystery);
                            count++;
                        }
                    }
                }
            }
            // World 3: Frozen (Buzlu)
            else if (worldIndex == 2)
            {
                // 1 ya da 2 halkayı Frozen yap
                int count = 0;
                for (int attempt = 0; attempt < 10 && count < 2; attempt++)
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
            // World 4: Locked Pole (Kilitli Direk + Anahtar Halka)
            else if (worldIndex == 3)
            {
                // Boş direklerden birini kilitle
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

                // Eğer tamamen boş direk bulunamadıysa, en az halkası olan direği bulup boşaltalım ve kilitleyelim
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

                    // Bu direği boşaltalım (halkalarını diğer boş yerleri olan direklere dağıtalım)
                    while (board.GetRingCount(bestPole) > 0)
                    {
                        var ringToMove = board.GetTopRing(bestPole);
                        board.PopRing(bestPole);
                        
                        // Başka bir direğe ekleyelim
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

                // Diğer direklerden birindeki bir halkayı Anahtar (Locked) yap
                if (lockedPole != -1)
                {
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        int p = rand.Next(poleCount);
                        if (p == lockedPole) continue;
                        int ringCount = board.GetRingCount(p);
                        if (ringCount > 0)
                        {
                            int r = rand.Next(ringCount);
                            if (board.GetRingType(p, r) == RingType.Standard)
                            {
                                board.SetRingType(p, r, RingType.Locked);
                                break;
                            }
                        }
                    }
                }
            }
            // World 5+: İleri mekanikler (Rainbow, Bomb, Chain, Magnet, Paint, Ghost, Stone, Glass)
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

                var chosenType = availableTypes[rand.Next(availableTypes.Length)];

                int count = 0;
                for (int attempt = 0; attempt < 20 && count < 2; attempt++)
                {
                    int p = rand.Next(poleCount);
                    int ringCount = board.GetRingCount(p);
                    if (ringCount > 0)
                    {
                        int r = rand.Next(ringCount);
                        if (board.GetRingType(p, r) == RingType.Standard)
                        {
                            board.SetRingType(p, r, chosenType);
                            count++;
                        }
                    }
                }
            }
        }
    }
}
