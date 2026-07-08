using System;
using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    public struct Move
    {
        public int From;
        public int To;
        public Move(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    public struct SolverResult
    {
        public bool IsSolvable;
        public int MoveCount;
        public List<MoveRecord> Moves;
    }

    /// <summary>
    /// Bulmaca çözücü sınıfı. GDD kurallarına uygun olarak seviyelerin çözülebilirliğini
    /// ve optimal hamle sayısını sıfır bellek tüketimi (0-GC) hedefli IDA* algoritması ile bulur.
    /// </summary>
    public static class LevelSolver
    {
        private static readonly HashSet<BoardState> _visited = new(10000);

        public static SolverResult Solve(BoardState initialState, int maxCapacity)
        {
            int threshold = CalculateHeuristic(initialState, maxCapacity);
            
            // Çözülemeyen seviyelerde sonsuz döngüyü engellemek için maksimum hamle derinliği sınırı (GDD limitlerine uygun)
            int maxMovesLimit = 50;
            
            var path = new List<Move>(maxMovesLimit);
            _visited.Clear();
            _visited.Add(initialState);

            while (threshold <= maxMovesLimit)
            {
                int nextThreshold = Search(initialState, 0, threshold, maxCapacity, path);
                if (nextThreshold == -1) // Çözüm bulundu
                {
                    var movesList = new List<MoveRecord>(path.Count);
                    for (int i = 0; i < path.Count; i++)
                    {
                        var m = path[i];
                        movesList.Add(new MoveRecord(m.From, m.To, new RingData(RingColor.None)));
                    }
                    return new SolverResult
                    {
                        IsSolvable = true,
                        MoveCount = path.Count,
                        Moves = movesList
                    };
                }
                if (nextThreshold == int.MaxValue)
                {
                    // Çözülemez durum
                    break;
                }
                threshold = nextThreshold;
            }

            return new SolverResult
            {
                IsSolvable = false,
                MoveCount = 0,
                Moves = null
            };
        }

        private static int Search(BoardState state, int g, int threshold, int maxCapacity, List<Move> path)
        {
            int h = CalculateHeuristic(state, maxCapacity);
            int f = g + h;
            if (f > threshold) return f;
            if (IsSolved(state, maxCapacity)) return -1; // Çözüme ulaşıldı

            int min = int.MaxValue;
            Span<Move> moves = stackalloc Move[132]; // 12 direk * 11 hedef = maks 132 hamle kombinasyonu
            int movesCount = GetValidMoves(state, maxCapacity, moves);

            for (int i = 0; i < movesCount; i++)
            {
                var move = moves[i];
                var nextState = state;
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);

                if (_visited.Contains(nextState)) continue;

                _visited.Add(nextState);
                path.Add(move);

                int result = Search(nextState, g + 1, threshold, maxCapacity, path);
                if (result == -1) return -1; // Bulunduysa yukarı doğru propagate et
                if (result < min) min = result;

                path.RemoveAt(path.Count - 1);
                _visited.Remove(nextState);
            }

            return min;
        }

        public static bool IsSolved(BoardState state, int maxCapacity)
        {
            int nonEmptyCount = 0;
            for (int i = 0; i < state.PoleCount; i++)
            {
                int count = state.GetRingCount(i);
                if (count == 0) continue;
                nonEmptyCount++;

                if (count != maxCapacity) return false;

                RingColor firstColor = state.GetRingColor(i, 0);
                for (int r = 1; r < count; r++)
                {
                    if (state.GetRingColor(i, r) != firstColor) return false;
                }
            }
            return nonEmptyCount > 0;
        }

        public static int CalculateHeuristic(BoardState state, int maxCapacity)
        {
            // Heuristic = Σ(yanlış pozisyondaki ring) + (incomplete pole × 2)
            int h = 0;
            for (int i = 0; i < state.PoleCount; i++)
            {
                int count = state.GetRingCount(i);
                if (count == 0) continue;

                bool incomplete = false;
                if (count < maxCapacity)
                {
                    incomplete = true;
                }

                RingColor bottomColor = state.GetRingColor(i, 0);
                for (int r = 1; r < count; r++)
                {
                    if (state.GetRingColor(i, r) != bottomColor)
                    {
                        h++; // Yanlış pozisyondaki halka
                        incomplete = true;
                    }
                }

                if (incomplete)
                {
                    h += 2; // Tamamlanmamış direk cezası
                }
            }
            return h;
        }

        public static int GetValidMoves(BoardState state, int maxCapacity, Span<Move> destination)
        {
            int count = 0;
            int numPoles = state.PoleCount;
            for (int i = 0; i < numPoles; i++)
            {
                if (!state.CanPopRing(i)) continue;

                var topRing = state.GetTopRing(i);

                for (int j = 0; j < numPoles; j++)
                {
                    if (i == j) continue;

                    if (state.CanAddRing(j, topRing.Color, topRing.Type, maxCapacity))
                    {
                        destination[count++] = new Move(i, j);
                    }
                }
            }
            return count;
        }
    }
}
