using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    public class PoleState
    {
        public int Id { get; set; }
        public List<RingData> Rings { get; } = new(4);
        public int MaxCapacity { get; set; } = 4;
        public bool IsLocked { get; set; }

        public bool IsFull => Rings.Count >= MaxCapacity;
        public bool IsEmpty => Rings.Count == 0;

        public RingData TopRing => IsEmpty ? new RingData(RingColor.None) : Rings[^1];

        public bool CanAddRing(RingData ring)
        {
            if (IsLocked)
            {
                // Kilitli direğe sadece kilit açıcı Altın Anahtar Halka (Locked) yerleştirilebilir
                return ring.Type == RingType.Locked;
            }
            if (IsFull) return false;
            if (IsEmpty) return true;

            // Taş (Stone) halkanın üzerine başka halka yerleştirilemez
            if (TopRing.Type == RingType.Stone) return false;

            // Gökkuşağı (Rainbow) veya Boya (Paint) halkası her rengin üzerine yerleşebilir
            if (ring.Type == RingType.Rainbow || ring.Type == RingType.Paint) return true;

            // Gökkuşağı veya Boya (Paint) halkaların üzerine her renk yerleşebilir
            if (TopRing.Type == RingType.Rainbow || TopRing.Type == RingType.Paint) return true;

            // Renk eşleşme kuralı
            return TopRing.Color == ring.Color;
        }

        public bool CanPopRing()
        {
            if (IsEmpty) return false;
            if (IsLocked) return false;

            // Donmuş (Frozen) ve Taş (Stone) halkalar yerinden hareket ettirilemez
            if (TopRing.Type == RingType.Frozen) return false;
            if (TopRing.Type == RingType.Stone) return false;

            return true;
        }

        public void AddRing(RingData ring)
        {
            if (Rings.Count < MaxCapacity)
            {
                Rings.Add(ring);
            }
        }

        public RingData PopRing()
        {
            if (IsEmpty) return new RingData(RingColor.None);
            var ring = Rings[^1];
            Rings.RemoveAt(Rings.Count - 1);
            return ring;
        }
    }
}
