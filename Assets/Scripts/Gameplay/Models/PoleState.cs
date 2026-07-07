using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    public class PoleState
    {
        public int Id { get; set; }
        public List<RingColor> Rings { get; } = new(4);
        public int MaxCapacity { get; set; } = 4;
        
        public bool IsFull => Rings.Count >= MaxCapacity;
        public bool IsEmpty => Rings.Count == 0;
        
        public RingColor TopRing => IsEmpty ? RingColor.None : Rings[^1];
        
        public bool CanAddRing(RingColor color)
        {
            if (IsFull) return false;
            if (IsEmpty) return true;
            return TopRing == color;
        }
        
        public void AddRing(RingColor color)
        {
            if (Rings.Count < MaxCapacity)
            {
                Rings.Add(color);
            }
        }
        
        public RingColor PopRing()
        {
            if (IsEmpty) return RingColor.None;
            var ring = Rings[^1];
            Rings.RemoveAt(Rings.Count - 1);
            return ring;
        }
    }
}
