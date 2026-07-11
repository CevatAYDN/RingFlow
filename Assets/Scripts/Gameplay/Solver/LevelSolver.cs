using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

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
        private class SolverContext
        {
            public readonly Dictionary<BoardState, int> TranspositionTable = new(80000);
            public int StatesSearched = 0;
            public int MaxStatesLimit = 100000;
        }

        private struct MoveWithHeuristic : IComparable<MoveWithHeuristic>
        {
            public Move Move;
            public int Heuristic;

            public int CompareTo(MoveWithHeuristic other)
            {
                return Heuristic.CompareTo(other.Heuristic);
            }
        }

        public static SolverResult Solve(BoardState initialState, int maxCapacity, int maxStatesLimit = 100000)
        {
            initialState.MaxCapacity = maxCapacity;
            int threshold = CalculateHeuristic(initialState, maxCapacity);
            
            int maxMovesLimit = 200;
            
            var path = new List<Move>(maxMovesLimit);
            var context = new SolverContext { MaxStatesLimit = maxStatesLimit };

            while (threshold <= maxMovesLimit)
            {
                context.TranspositionTable.Clear();
                context.StatesSearched = 0;
                
                int nextThreshold = Search(initialState, 0, threshold, maxCapacity, path, context);
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
                
                if (nextThreshold == int.MaxValue || context.StatesSearched >= context.MaxStatesLimit)
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

        private static int Search(BoardState state, int g, int threshold, int maxCapacity, List<Move> path, SolverContext context)
        {
            context.StatesSearched++;
            if (context.StatesSearched >= context.MaxStatesLimit)
            {
                return int.MaxValue; // Prune due to search limit
            }

            int h = CalculateHeuristic(state, maxCapacity);
            int f = g + h;
            if (f > threshold) return f;
            if (IsSolved(state, maxCapacity)) return -1; // Çözüme ulaşıldı

            // Transposition Table check
            if (context.TranspositionTable.TryGetValue(state, out int prevG) && prevG <= g)
            {
                return int.MaxValue; // Already visited at a shorter or equal path
            }
            context.TranspositionTable[state] = g;

            int min = int.MaxValue;
            Span<Move> moves = stackalloc Move[132]; // 12 direk * 11 hedef = maks 132 hamle kombinasyonu
            int movesCount = GetValidMoves(state, maxCapacity, moves);

            // Heuristic Move Ordering: Sort valid moves based on next state heuristic
            Span<MoveWithHeuristic> sortedMoves = stackalloc MoveWithHeuristic[movesCount];
            int sortIdx = 0;
            for (int i = 0; i < movesCount; i++)
            {
                var move = moves[i];
                var nextState = state;
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);
                // Prune moves that cause bomb explosion
                if (TickBombsAndCheckExplosion(ref nextState)) continue;
                int nextH = CalculateHeuristic(nextState, maxCapacity);
                sortedMoves[sortIdx++] = new MoveWithHeuristic { Move = move, Heuristic = nextH };
            }
            int validSortedCount = sortIdx;

            // Sort valid moves based on heuristic (ascending) using insertion sort
            for (int i = 1; i < validSortedCount; i++)
            {
                var key = sortedMoves[i];
                int j = i - 1;
                while (j >= 0 && sortedMoves[j].Heuristic > key.Heuristic)
                {
                    sortedMoves[j + 1] = sortedMoves[j];
                    j--;
                }
                sortedMoves[j + 1] = key;
            }

            for (int i = 0; i < validSortedCount; i++)
            {
                var move = sortedMoves[i].Move;
                var nextState = state;
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);
                // Tick all bombs — if any explodes, prune this branch
                if (TickBombsAndCheckExplosion(ref nextState)) continue;

                path.Add(move);

                int result = Search(nextState, g + 1, threshold, maxCapacity, path, context);
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

        public static bool IsSolved(PoleState pole, int maxCapacity)
        {
            if (pole == null || pole.IsEmpty) return false;
            if (pole.Rings.Count != pole.RingCapacity) return false;

            var firstRing = pole.Rings[0];
            for (int i = 1; i < pole.Rings.Count; i++)
            {
                if (pole.Rings[i].Color != firstRing.Color) return false;
            }
            return true;
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

                    // Chain: check if target has room for main ring + linked partners
                    if (topRing.Type == RingType.Chain && topRing.AdditionalData > 0)
                    {
                        int linked = CountChainLinkedPartners(state, topRing.AdditionalData, i);
                        int requiredSlots = 1 + linked;
                        if (!CanAddRingWithExtraCapacity(state, j, topRing.Color, topRing.Type, maxCapacity, requiredSlots))
                            continue;
                    }
                    else if (!state.CanAddRing(j, topRing.Color, topRing.Type, maxCapacity))
                    {
                        continue;
                    }

                    destination[count++] = new Move(i, j);
                }
            }
            return count;
        }

        private static int CountChainLinkedPartners(BoardState state, int groupId, int excludePole)
        {
            int partners = 0;
            for (int p = 0; p < state.PoleCount; p++)
            {
                if (p == excludePole) continue;
                if (state.IsEmpty(p)) continue;
                var top = state.GetTopRing(p);
                if (top.Type == RingType.Chain && top.AdditionalData == groupId)
                    partners++;
            }
            return partners;
        }

        private static bool CanAddRingWithExtraCapacity(BoardState state, int poleIndex,
            RingColor color, RingType type, int maxCapacity, int requiredSlots)
        {
            if (state.IsPoleLocked(poleIndex)) return type == RingType.Locked;
            int freeSlots = maxCapacity - state.GetRingCount(poleIndex);
            if (freeSlots < requiredSlots) return false;
            if (state.IsEmpty(poleIndex)) return true;
            var top = state.GetTopRing(poleIndex);
            if (top.Type == RingType.Stone) return top.Color == color;
            if (type == RingType.Rainbow || type == RingType.Paint
                || top.Type == RingType.Rainbow || top.Type == RingType.Paint) return true;
            return top.Color == color;
        }

        private static bool TickBombsAndCheckExplosion(ref BoardState state)
        {
            bool exploded = false;
            for (int p = 0; p < state.PoleCount; p++)
            {
                int count = state.GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    if (state.GetRingType(p, r) != RingType.Bomb) continue;
                    int counter = state.GetRingAdditional(p, r) - 1;
                    state.SetRingAdditional(p, r, counter < 0 ? 0 : counter);
                    if (counter <= 0) exploded = true;
                }
            }
            return exploded;
        }

        /// <summary>
        /// Background-thread wrapper around <see cref="Solve"/>. Forwards the same
        /// IDA* search onto the thread-pool so the Unity main thread never blocks.
        /// Cancellation propagates through the supplied token as
        /// <see cref="OperationCanceledException"/>; callers should let Nexus'
        /// async-command recovery handle retries/aborts.
        /// </summary>
        public static ValueTask<SolverResult> SolveAsync(
            BoardState initialState,
            int maxCapacity,
            int maxStatesLimit = 100000,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<SolverResult>(Task.Run(
                () => Solve(initialState, maxCapacity, maxStatesLimit),
                cancellationToken));
        }
    }
}
