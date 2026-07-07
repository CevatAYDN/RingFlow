using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class GameplayModel : IReactiveModel, IResettableModel
    {
        public List<PoleState> Poles { get; } = new(10);
        public ObservableProperty<int> SelectedPoleId { get; } = new(-1);
        public ObservableProperty<int> MovesCount { get; } = new(0);
        public ObservableProperty<int> TargetMovesCount { get; } = new(0);
        public ObservableProperty<bool> IsGameWon { get; } = new(false);
        
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
            MoveHistory.Clear();
        }
    }

    public struct MoveRecord
    {
        public int FromPoleId;
        public int ToPoleId;
        public RingData Ring;
        public bool WasMysteryRevealedOnFrom;
        public bool WasIceBrokenOnTarget;
        public bool WasTargetPoleUnlocked;
        public bool WasPainted;
        public RingColor OriginalColor;
        public List<MoveRecord> SubMoves;

        public MoveRecord(int fromPoleId, int toPoleId, RingData ring, 
            bool wasMysteryRevealedOnFrom = false, 
            bool wasIceBrokenOnTarget = false, 
            bool wasTargetPoleUnlocked = false,
            bool wasPainted = false,
            RingColor originalColor = RingColor.None)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
            Ring = ring;
            WasMysteryRevealedOnFrom = wasMysteryRevealedOnFrom;
            WasIceBrokenOnTarget = wasIceBrokenOnTarget;
            WasTargetPoleUnlocked = wasTargetPoleUnlocked;
            WasPainted = wasPainted;
            OriginalColor = originalColor;
            SubMoves = null;
        }
    }

    /// <summary>
    /// A pre-allocated, zero-allocation array-backed stack.
    /// </summary>
    public class UndoStack<T>
    {
        private readonly T[] _items;
        private int _top = -1;

        public UndoStack(int capacity)
        {
            _items = new T[capacity];
        }

        public int Count => _top + 1;

        public void Push(T item)
        {
            if (_top < _items.Length - 1)
            {
                _top++;
                _items[_top] = item;
            }
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
