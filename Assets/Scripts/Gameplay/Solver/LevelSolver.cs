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
        public RingData Ring;
        public Move(int from, int to, RingData ring)
        {
            From = from;
            To = to;
            Ring = ring;
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
            public int MaxStatesLimit;
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

        public static SolverResult Solve(BoardState initialState, int maxCapacity, int maxStatesLimit = 100000, int maxMovesLimit = 200, int[] portalTargets = null, CancellationToken cancellationToken = default, BombTickMode bombTickMode = BombTickMode.AllBombsPerMove)
        {
            initialState.MaxCapacity = maxCapacity;
            int threshold = CalculateHeuristic(initialState, maxCapacity);
            
            var path = new List<Move>(maxMovesLimit);
            var context = new SolverContext { MaxStatesLimit = maxStatesLimit };

            while (threshold <= maxMovesLimit)
            {
                // Allow cooperative cancellation at each threshold iteration.
                // This makes SolveAsync truly cancellable once Search begins.
                if (cancellationToken.IsCancellationRequested)
                    break;

                context.TranspositionTable.Clear();
                context.StatesSearched = 0;
                
                int nextThreshold = Search(initialState, 0, threshold, maxCapacity, path, context, portalTargets, bombTickMode);
                if (nextThreshold == -1) // Çözüm bulundu
                {
                    var movesList = new List<MoveRecord>(path.Count);
                    for (int i = 0; i < path.Count; i++)
                    {
                        var m = path[i];
                        movesList.Add(new MoveRecord(m.From, m.To, m.Ring));
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

        private static int Search(BoardState state, int g, int threshold, int maxCapacity, List<Move> path, SolverContext context, int[] portalTargets, BombTickMode bombTickMode)
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
            int movesCount = GetValidMoves(state, maxCapacity, moves, portalTargets);

            // Heuristic Move Ordering: Sort valid moves based on next state heuristic
            Span<MoveWithHeuristic> sortedMoves = stackalloc MoveWithHeuristic[movesCount];
            int sortIdx = 0;
            for (int i = 0; i < movesCount; i++)
            {
                var move = moves[i];
                var nextState = state;
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);
                // Use heuristic for move ordering (no state mutation during ordering)
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
                int landingIndex = nextState.GetRingCount(move.To);
                var ring = nextState.PopRing(move.From);
                nextState.AddRing(move.To, ring);

                // Portal teleport: forward the moved ring from its landing index.
                // Chain/magnet may append above it, so never blindly pop target top.
                if (portalTargets != null && move.To >= 0 && move.To < portalTargets.Length && portalTargets[move.To] >= 0)
                {
                    int partner = portalTargets[move.To];
                    if (partner >= 0 && partner < nextState.PoleCount &&
                        landingIndex < nextState.GetRingCount(move.To) &&
                        nextState.GetRingCount(partner) < nextState.MaxCapacity)
                    {
                        var portalRing = nextState.RemoveRingAtRaw(move.To, landingIndex);
                        if (nextState.CanAddRing(partner, portalRing.Color, portalRing.Type, maxCapacity, portalRing.AdditionalData))
                        {
                            nextState.AddRing(partner, portalRing);
                        }
                        else
                        {
                            nextState.AddRingSimple(move.To, portalRing, true);
                        }
                    }
                }

                // Tick bombs per config mode — if any explodes, prune this branch
                int portalTarget = (portalTargets != null && move.To >= 0 && move.To < portalTargets.Length) ? portalTargets[move.To] : -1;
                if (TickBombsAndCheckExplosion(ref nextState, bombTickMode, move.From, move.To, portalTarget, move.Ring.Type)) continue;

                path.Add(move);

                int result = Search(nextState, g + 1, threshold, maxCapacity, path, context, portalTargets, bombTickMode);
                if (result == -1) return -1; // Bulunduysa yukarı doğru propagate et
                if (result < min) min = result;

                path.RemoveAt(path.Count - 1);
            }

            return min;
        }

        public static bool IsSolved(BoardState state, int maxCapacity)
        {
            bool hasCompletedPole = false;
            for (int i = 0; i < state.PoleCount; i++)
            {
                int count = state.GetRingCount(i);
                if (count == 0) continue;
                if (count < maxCapacity) return false;
                hasCompletedPole = true;

                RingColor firstColor = state.GetRingColor(i, 0);
                for (int r = 1; r < count; r++)
                {
                    if (state.GetRingColor(i, r) != firstColor) return false;
                }
            }
            return hasCompletedPole;
        }

        public static bool IsSolved(PoleState pole, int maxCapacity)
        {
            if (pole == null || pole.IsEmpty) return false;
            if (pole.Rings.Count < maxCapacity) return false;

            var firstRing = pole.Rings[0];
            for (int i = 1; i < pole.Rings.Count; i++)
            {
                if (pole.Rings[i].Color != firstRing.Color) return false;
            }
            return true;
        }

        /// <summary>
        /// Admissible (optimistic) heuristic for the IDA* search. Per pole it counts the
        /// number of misplaced rings plus a penalty for non-full (incomplete) poles.
        /// Every misplaced ring needs at least one move to fix and an incomplete pole needs
        /// more rings, so this never overestimates the remaining cost — the search therefore
        /// returns an optimal solution. Special-ring side effects (paint/portal/ice) are
        /// realised by the search expansion, not the heuristic.
        /// </summary>
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

        public static int GetValidMoves(BoardState state, int maxCapacity, Span<Move> destination, int[] portalTargets = null)
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

                    if (!state.CanAddRing(j, topRing.Color, topRing.Type, maxCapacity, topRing.AdditionalData))
                        continue;

                    int effectiveTarget = j;
                    if (portalTargets != null && j >= 0 && j < portalTargets.Length && portalTargets[j] >= 0)
                    {
                        var simulated = state;
                        int landingIndex = simulated.GetRingCount(j);
                        var moved = simulated.PopRing(i);
                        simulated.AddRing(j, moved);
                        int partner = portalTargets[j];
                        if (partner < 0 || partner >= simulated.PoleCount || landingIndex >= simulated.GetRingCount(j))
                            continue;
                        var portalRing = simulated.RemoveRingAtRaw(j, landingIndex);
                        if (!simulated.CanAddRing(partner, portalRing.Color, portalRing.Type, maxCapacity, portalRing.AdditionalData))
                            continue;
                        effectiveTarget = partner;
                    }

                    // Chain: tek eş çekme kuralı — hedef direğin 2 boşluğu (taşınan + 1 eş) ya da
                    // eş yoksa 1 boşluğu yeterli. Uygulama yolu (BoardState.AddRing) ile aynı mantık.
                    if (topRing.Type == RingType.Chain && topRing.AdditionalData > 0)
                    {
                        bool hasPullablePartner = false;
                        for (int p = 0; p < state.PoleCount; p++)
                        {
                            if (p == i) continue;
                            var partnerTop = state.GetTopRing(p);
                            if (partnerTop.Type == RingType.Chain && partnerTop.AdditionalData == topRing.AdditionalData)
                            {
                                hasPullablePartner = true;
                                break;
                            }
                        }
                        int requiredSlots = hasPullablePartner ? 2 : 1;
                        if (!CanAddRingWithExtraCapacity(state, effectiveTarget, topRing.Color, topRing.Type, maxCapacity, requiredSlots))
                            continue;
                    }

                    destination[count++] = new Move(i, j, topRing);
                }
            }
            return count;
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

        private static bool TickBombsAndCheckExplosion(ref BoardState state, BombTickMode tickMode, int fromPoleId, int toPoleId, int portalTargetPoleId, RingType movedRingType)
        {
            bool exploded = false;
            for (int p = 0; p < state.PoleCount; p++)
            {
                int count = state.GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    if (state.GetRingType(p, r) != RingType.Bomb) continue;
                    if (!ShouldTickBombForSolver(tickMode, p, r, fromPoleId, toPoleId, portalTargetPoleId, movedRingType)) continue;
                    int counter = state.GetRingAdditional(p, r) - 1;
                    state.SetRingAdditional(p, r, counter < 0 ? 0 : counter);
                    if (counter <= 0) exploded = true;
                }
            }
            return exploded;
        }

        private static bool ShouldTickBombForSolver(BombTickMode tickMode, int poleId, int ringIndex, int fromPoleId, int toPoleId, int portalTargetPoleId, RingType movedRingType)
        {
            switch (tickMode)
            {
                case BombTickMode.SourceAndTargetPolesOnly:
                    return poleId == fromPoleId ||
                           poleId == toPoleId ||
                           poleId == portalTargetPoleId;
                case BombTickMode.MovedBombOnly:
                    // Mirror of MoveRingCommand.ShouldTickBomb:
                    // Only the bomb that was moved ticks. The moved ring is a bomb iff movedRingType==Bomb,
                    // and it lands on toPoleId. Ignore ringIndex — the ring at the landing position is
                    // what matters, not the stack depth.
                    if (movedRingType != RingType.Bomb) return false;
                    return poleId == toPoleId;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Background-thread wrapper around <see cref="Solve"/>. Forwards the same
        /// IDA* search onto the thread-pool so the Unity main thread never blocks.
        /// <para>
        /// Cancellation is propagated cooperatively: the token is checked at each
        /// threshold iteration inside <see cref="Solve"/>, so cancellation may take
        /// up to one full IDA* threshold pass to take effect. For interactive hints
        /// this is acceptable (typical solve &lt; 100ms). For very large puzzles the
        /// solver will respect the token before starting the next threshold.
        /// </para>
        /// Callers should let Nexus' async-command recovery handle retries/aborts.
        /// </summary>
        public static ValueTask<SolverResult> SolveAsync(
            BoardState initialState,
            int maxCapacity,
            int maxStatesLimit = 100000,
            int maxMovesLimit = 200,
            int[] portalTargets = null,
            CancellationToken cancellationToken = default,
            BombTickMode bombTickMode = BombTickMode.AllBombsPerMove)
        {
            // Capture locals for closure (avoids boxing of struct arguments)
            var capturedState   = initialState;
            int capturedCap     = maxCapacity;
            int capturedStates  = maxStatesLimit;
            int capturedMoves   = maxMovesLimit;
            int[] capturedPorts = portalTargets;
            CancellationToken ct = cancellationToken;
            BombTickMode capturedTickMode = bombTickMode;

            return new ValueTask<SolverResult>(Task.Run(
                () => Solve(capturedState, capturedCap, capturedStates, capturedMoves, capturedPorts, ct, capturedTickMode),
                cancellationToken));
        }
    }
}
