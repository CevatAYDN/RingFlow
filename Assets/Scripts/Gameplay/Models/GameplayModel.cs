using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class GameplayModel : IReactiveModel, IResettableModel
    {
        public List<PoleState> Poles { get; } = new(12);
        public ObservableProperty<int> SelectedPoleId { get; } = new(-1);
        public ObservableProperty<int> MovesCount { get; } = new(0);
        public ObservableProperty<int> TargetMovesCount { get; } = new(0);
        public ObservableProperty<bool> IsGameWon { get; } = new(false);
        public ObservableProperty<WinReward> LastReward { get; } = new(default);

        public UndoStack<MoveRecord> MoveHistory { get; } = new(1000);

        public ValueTask OnBind(CancellationToken ct)
        {
            return default;
        }

        public void Reset()
        {
            Poles.Clear();
            SelectedPoleId.Value = -1;
            MovesCount.Value = 0;
            TargetMovesCount.Value = 0;
            IsGameWon.Value = false;
            LastReward.Value = default;
            MoveHistory.Clear();
        }
    }

    public struct WinReward
    {
        public int Moves;
        public int TargetMoves;
        public int Coins;
        public int Xp;
        public int Stars;

        public static WinReward From(int moves, int targetMoves, int coins, int xp, int stars)
            => new WinReward { Moves = moves, TargetMoves = targetMoves, Coins = coins, Xp = xp, Stars = stars };
    }

    public class MoveRecord
    {
        public int FromPoleId;
        public int ToPoleId;
        public RingData Ring;
        public bool WasMysteryRevealedOnFrom;
        public readonly List<int> IceBrokenRingIndices = new(4); // Indices of rings whose ice was broken (from bottom upward).
        public bool WasIceBrokenOnTarget => IceBrokenRingIndices.Count > 0;
        public bool WasTargetPoleUnlocked;
        public bool WasPainted;
        public int PaintedRingIndex;
        public RingColor PaintedRingOriginalColor;
        public RingColor OriginalColor;
        public bool WasRainbowTargetConverted;
        public int RainbowTargetRingIndex;
        public RingColor RainbowTargetOriginalColor;
        public readonly List<MoveRecord> SubMoves = new(4);

        /// <summary>Snapshot of every bomb's counter BEFORE this move's TickAllBombs() ran.
        /// Undo restores these counters instead of blindly incrementing every current bomb.</summary>
        public readonly List<(int PoleId, int RingIndex, int Counter)> BombCountersBeforeTick = new(4);

        /// <summary>Full ring data of bombs that exploded (counter reached 0) during this move.
        /// Undo inserts these rings back at their original positions.</summary>
        public readonly List<(int PoleId, int RingIndex, RingData Ring)> BombExplodedRings = new(4);

        public MoveRecord()
        {
            Clear();
        }

        public MoveRecord(int fromPoleId, int toPoleId, RingData ring,
            bool wasMysteryRevealedOnFrom = false,
            bool wasIceBrokenOnTarget = false,
            bool wasTargetPoleUnlocked = false,
            bool wasPainted = false,
            int paintedRingIndex = -1,
            RingColor paintedRingOriginalColor = RingColor.None,
            RingColor originalColor = RingColor.None,
            List<(int PoleId, int RingIndex, int Counter)> bombCountersBeforeTick = null,
            bool wasRainbowTargetConverted = false,
            int rainbowTargetRingIndex = -1,
            RingColor rainbowTargetOriginalColor = RingColor.None,
            List<(int PoleId, int RingIndex, RingData Ring)> bombExplodedRings = null)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
            Ring = ring;
            WasMysteryRevealedOnFrom = wasMysteryRevealedOnFrom;
            WasTargetPoleUnlocked = wasTargetPoleUnlocked;
            WasPainted = wasPainted;
            PaintedRingIndex = paintedRingIndex;
            PaintedRingOriginalColor = paintedRingOriginalColor;
            OriginalColor = originalColor;
            WasRainbowTargetConverted = wasRainbowTargetConverted;
            RainbowTargetRingIndex = rainbowTargetRingIndex;
            RainbowTargetOriginalColor = rainbowTargetOriginalColor;

            if (bombCountersBeforeTick != null)
            {
                BombCountersBeforeTick.AddRange(bombCountersBeforeTick);
            }
            if (bombExplodedRings != null)
            {
                BombExplodedRings.AddRange(bombExplodedRings);
            }
        }

        public void Clear()
        {
            FromPoleId = -1;
            ToPoleId = -1;
            Ring = default;
            WasMysteryRevealedOnFrom = false;
            IceBrokenRingIndices.Clear();
            WasTargetPoleUnlocked = false;
            WasPainted = false;
            PaintedRingIndex = -1;
            PaintedRingOriginalColor = RingColor.None;
            OriginalColor = RingColor.None;
            WasRainbowTargetConverted = false;
            RainbowTargetRingIndex = -1;
            RainbowTargetOriginalColor = RingColor.None;

            for (int i = 0; i < SubMoves.Count; i++)
            {
                MoveRecordPool.Return(SubMoves[i]);
            }
            SubMoves.Clear();
            BombCountersBeforeTick.Clear();
            BombExplodedRings.Clear();
        }
    }

    /// <summary>
    /// Thread-safe pooling mechanism for MoveRecord instances to prevent GC allocations.
    /// </summary>
    public static class MoveRecordPool
    {
        private static readonly Stack<MoveRecord> _pool = new(128);

        public static MoveRecord Rent()
        {
            lock (_pool)
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
            }
            return new MoveRecord();
        }

        public static void Return(MoveRecord record)
        {
            if (record == null) return;
            record.Clear();
            lock (_pool)
            {
                _pool.Push(record);
            }
        }
    }

    /// <summary>
    /// A pre-allocated, zero-allocation array-backed stack that grows automatically when full.
    /// </summary>
    public class UndoStack<T>
    {
        private T[] _items;
        private int _top = -1;

        public UndoStack(int capacity)
        {
            _items = new T[capacity];
        }

        public int Count => _top + 1;

        public void Push(T item)
        {
            if (_top >= _items.Length - 1)
            {
                int newCapacity = _items.Length * 2;
                var newItems = new T[newCapacity];
                System.Array.Copy(_items, newItems, _items.Length);
                _items = newItems;
            }
            _top++;
            _items[_top] = item;
        }

        public T Pop()
        {
            if (_top >= 0)
            {
                var item = _items[_top];
                _items[_top] = default;
                _top--;
                return item;
            }
            return default;
        }

        public void Clear()
        {
            System.Array.Clear(_items, 0, _items.Length);
            _top = -1;
        }
    }
}
