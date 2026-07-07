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
                        board.AddRing(i, color);
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
                    var color = board.PopRing(from);
                    board.AddRing(to, color);
                }

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
                        var poleData = new PoleData(maxCapacity);
                        int count = scrambledState.GetRingCount(p);
                        for (int r = 0; r < count; r++)
                        {
                            var color = scrambledState.GetRingColor(p, r);
                            poleData.Rings.Add(new RingData(color));
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
    }
}
