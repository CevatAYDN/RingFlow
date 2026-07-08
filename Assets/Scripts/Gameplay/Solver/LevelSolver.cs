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
        private static readonly Dictionary<BoardState, int> _transpositionTable = new(80000);
        private static int _statesSearched = 0;
        private const int MaxStatesLimit = 100000;

        private struct MoveWithHeuristic : IComparable<MoveWithHeuristic>
        {
            public Move Move;
            public int Heuristic;

            public int CompareTo(MoveWithHeuristic other)
            {
                return Heuristic.CompareTo(other.Heuristic);
            }
        }

        public static SolverResult Solve(BoardState initialState, int maxCapacity)
        {
            initialState.MaxCapacity = maxCapacity;
            int threshold = CalculateHeuristic(initialState, maxCapacity);
            
            int maxMovesLimit = 200;
            
            var path = new List<Move>(maxMovesLimit);

            while (threshold <= maxMovesLimit)
            {
                _transpositionTable.Clear();
                _statesSearched = 0;
                
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
                
                if (nextThreshold == int.MaxValue || _statesSearched >= MaxStatesLimit)
                {
                    // Çözülemez durum veya limit aşıldı
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
            _statesSearched++;
            if (_statesSearched >= MaxStatesLimit)
            {
                return int.MaxValue; // Prune due to search limit
            }

            int h = CalculateHeuristic(state, maxCapacity);
            int f = g + h;
            if (f > threshold) return f;
            if (IsSolved(state, maxCapacity)) return -1; // Çözüme ulaşıldı

            // Transposition Table check
            if (_transpositionTable.TryGetValue(state, out int prevG) && prevG <= g)
            {
                return int.MaxValue; // Already visited at a shorter or equal path
            }
            _transpositionTable[state] = g;

            int min = int.MaxValue;
            Span<Move> moves = stackalloc Move[132]; // 12 direk * 11 hedef = maks 132 hamle kombinasyonu
            int movesCount = GetValidMoves(state, maxCapacity, moves);

            // Heuristic Move Ordering: Sort valid moves based on next state heuristic
            Span<MoveWithHeuristic> sortedMoves = stackalloc MoveWithHeuristic[movesCount];
            for (int i = 0; i < movesCount; i++)
            {
                var move = moves[i];
                var nextState = state;
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);
                int nextH = CalculateHeuristic(nextState, maxCapacity);
                sortedMoves[i] = new MoveWithHeuristic { Move = move, Heuristic = nextH };
            }

            // Selection sort on stackalloc span
            for (int i = 0; i < movesCount - 1; i++)
            {
                int bestIdx = i;
                for (int j = i + 1; j < movesCount; j++)
                {
                    if (sortedMoves[j].Heuristic < sortedMoves[bestIdx].Heuristic)
                    {
                        bestIdx = j;
                    }
                }
                if (bestIdx != i)
                {
                    var temp = sortedMoves[i];
                    sortedMoves[i] = sortedMoves[bestIdx];
                    sortedMoves[bestIdx] = temp;
                }
            }

            for (int i = 0; i < movesCount; i++)
            {
                var move = sortedMoves[i].Move;
                var nextState = state;
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);

                path.Add(move);

                int result = Search(nextState, g + 1, threshold, maxCapacity, path);
                if (result == -1) return -1; // Bulunduysa yukarı doğru propagate et
                if (result < min) min = result;

                path.RemoveAt(path.Count - 1);
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
