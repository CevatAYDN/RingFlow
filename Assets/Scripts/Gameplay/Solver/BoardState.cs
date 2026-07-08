using System;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Tahta durumunu temsil eden, 0-GC uyumlu, kopyalaması son derece hızlı,
    /// bit-maskeleme destekli değer tipi (struct).
    /// Her direğin renk ve tip verileri sıkıştırılmış uint alanlarında tutulur.
    /// </summary>
    public struct BoardState : IEquatable<BoardState>
    {
        // Renkler, Halka Sayısı ve Kilit/Buz bayrakları için 12 uint (Direkler)
        public uint Pole0;
        public uint Pole1;
        public uint Pole2;
        public uint Pole3;
        public uint Pole4;
        public uint Pole5;
        public uint Pole6;
        public uint Pole7;
        public uint Pole8;
        public uint Pole9;
        public uint Pole10;
        public uint Pole11;

        // Halka tipleri (Mystery, Paint, Chain vb.) için 12 uint
        public uint Types0;
        public uint Types1;
        public uint Types2;
        public uint Types3;
        public uint Types4;
        public uint Types5;
        public uint Types6;
        public uint Types7;
        public uint Types8;
        public uint Types9;
        public uint Types10;
        public uint Types11;

        public int PoleCount;
        public int MaxCapacity;

        public uint GetPoleRaw(int index)
        {
            return index switch
            {
                0 => Pole0,
                1 => Pole1,
                2 => Pole2,
                3 => Pole3,
                4 => Pole4,
                5 => Pole5,
                6 => Pole6,
                7 => Pole7,
                8 => Pole8,
                9 => Pole9,
                10 => Pole10,
                11 => Pole11,
                _ => 0
            };
        }

        public void SetPoleRaw(int index, uint value)
        {
            switch (index)
            {
                case 0: Pole0 = value; break;
                case 1: Pole1 = value; break;
                case 2: Pole2 = value; break;
                case 3: Pole3 = value; break;
                case 4: Pole4 = value; break;
                case 5: Pole5 = value; break;
                case 6: Pole6 = value; break;
                case 7: Pole7 = value; break;
                case 8: Pole8 = value; break;
                case 9: Pole9 = value; break;
                case 10: Pole10 = value; break;
                case 11: Pole11 = value; break;
            }
        }

        public uint GetTypesRaw(int index)
        {
            return index switch
            {
                0 => Types0,
                1 => Types1,
                2 => Types2,
                3 => Types3,
                4 => Types4,
                5 => Types5,
                6 => Types6,
                7 => Types7,
                8 => Types8,
                9 => Types9,
                10 => Types10,
                11 => Types11,
                _ => 0
            };
        }

        public void SetTypesRaw(int index, uint value)
        {
            switch (index)
            {
                case 0: Types0 = value; break;
                case 1: Types1 = value; break;
                case 2: Types2 = value; break;
                case 3: Types3 = value; break;
                case 4: Types4 = value; break;
                case 5: Types5 = value; break;
                case 6: Types6 = value; break;
                case 7: Types7 = value; break;
                case 8: Types8 = value; break;
                case 9: Types9 = value; break;
                case 10: Types10 = value; break;
                case 11: Types11 = value; break;
            }
        }

        public int GetRingCount(int poleIndex)
        {
            uint val = GetPoleRaw(poleIndex);
            return (int)((val >> 24) & 0xF);
        }

        public void SetRingCount(int poleIndex, int count)
        {
            uint val = GetPoleRaw(poleIndex);
            val &= ~(0xFu << 24);
            val |= ((uint)count & 0xF) << 24;
            SetPoleRaw(poleIndex, val);
        }

        public RingColor GetRingColor(int poleIndex, int ringIndex)
        {
            uint val = GetPoleRaw(poleIndex);
            int shift = ringIndex * 4;
            return (RingColor)((val >> shift) & 0xF);
        }

        public void SetRingColor(int poleIndex, int ringIndex, RingColor color)
        {
            uint val = GetPoleRaw(poleIndex);
            int shift = ringIndex * 4;
            val &= ~(0xFu << shift);
            val |= ((uint)color & 0xF) << shift;
            SetPoleRaw(poleIndex, val);
        }

        public RingType GetRingType(int poleIndex, int ringIndex)
        {
            uint val = GetTypesRaw(poleIndex);
            int shift = ringIndex * 4;
            return (RingType)((val >> shift) & 0xF);
        }

        public void SetRingType(int poleIndex, int ringIndex, RingType type)
        {
            uint val = GetTypesRaw(poleIndex);
            int shift = ringIndex * 4;
            val &= ~(0xFu << shift);
            val |= ((uint)type & 0xF) << shift;
            SetTypesRaw(poleIndex, val);
        }

        public bool IsPoleLocked(int poleIndex)
        {
            uint val = GetPoleRaw(poleIndex);
            return ((val >> 28) & 0x1) == 1;
        }

        public void SetPoleLocked(int poleIndex, bool locked)
        {
            uint val = GetPoleRaw(poleIndex);
            val &= ~(0x1u << 28);
            if (locked) val |= 1u << 28;
            SetPoleRaw(poleIndex, val);
        }

        public bool IsTopRingFrozen(int poleIndex)
        {
            uint val = GetPoleRaw(poleIndex);
            return ((val >> 29) & 0x1) == 1;
        }

        public void SetTopRingFrozen(int poleIndex, bool frozen)
        {
            uint val = GetPoleRaw(poleIndex);
            val &= ~(0x1u << 29);
            if (frozen) val |= 1u << 29;
            SetPoleRaw(poleIndex, val);
        }

        public bool IsEmpty(int poleIndex) => GetRingCount(poleIndex) == 0;

        public RingColor GetTopRingColor(int poleIndex)
        {
            int count = GetRingCount(poleIndex);
            if (count == 0) return RingColor.None;
            return GetRingColor(poleIndex, count - 1);
        }

        public RingType GetTopRingType(int poleIndex)
        {
            int count = GetRingCount(poleIndex);
            if (count == 0) return RingType.Standard;
            return GetRingType(poleIndex, count - 1);
        }

        public RingData GetTopRing(int poleIndex)
        {
            int count = GetRingCount(poleIndex);
            if (count == 0) return new RingData(RingColor.None);
            return new RingData(GetRingColor(poleIndex, count - 1), GetRingType(poleIndex, count - 1));
        }

        public bool CanPopRing(int poleIndex)
        {
            if (IsEmpty(poleIndex)) return false;
            if (IsPoleLocked(poleIndex)) return false;
            if (IsTopRingFrozen(poleIndex)) return false;
            if (GetTopRingType(poleIndex) == RingType.Stone) return false;
            return true;
        }

        public bool CanAddRing(int poleIndex, RingColor color, RingType type, int maxCapacity)
        {
            if (IsPoleLocked(poleIndex))
            {
                return type == RingType.Locked;
            }
            if (GetRingCount(poleIndex) >= maxCapacity) return false;
            if (IsEmpty(poleIndex)) return true;

            var top = GetTopRing(poleIndex);
            if (top.Type == RingType.Stone) return false;

            // Gökkuşağı (Rainbow) joker kuralları
            if (type == RingType.Rainbow || top.Type == RingType.Rainbow) return true;

            // Boya (Paint) kuralı: Her rengi kabul eder
            if (top.Type == RingType.Paint) return true;

            return top.Color == color;
        }

        public void AddRing(int poleIndex, RingData ring)
        {
            if (IsPoleLocked(poleIndex) && ring.Type == RingType.Locked)
            {
                SetPoleLocked(poleIndex, false);
            }

            int count = GetRingCount(poleIndex);

            // Paint Kontrolü 1 — Gelen halka Paint ise altındaki halkayı boyar ve kendisi Standard olur
            if (ring.Type == RingType.Paint)
            {
                if (count > 0)
                {
                    SetRingColor(poleIndex, count - 1, ring.Color);
                }
                ring.Type = RingType.Standard;
            }
            // Paint Kontrolü 2 — Paint halka üzerine gelen halkayı boyar ve Paint standartlaşır
            else if (count > 0 && GetTopRingType(poleIndex) == RingType.Paint)
            {
                ring.Color = GetTopRingColor(poleIndex);
                SetRingType(poleIndex, count - 1, RingType.Standard);
            }

            // Rainbow Kontrolü 1 — Gelen halka Rainbow ise yerleştiği halkanın rengini alır ve standartlaşır
            if (ring.Type == RingType.Rainbow)
            {
                if (count > 0)
                {
                    ring.Color = GetTopRingColor(poleIndex);
                    ring.Type = RingType.Standard;
                }
            }
            // Rainbow Kontrolü 2 — Rainbow halka üzerine gelen halkanın rengini kopyalar ve standartlaşır
            else if (count > 0 && GetTopRingType(poleIndex) == RingType.Rainbow)
            {
                SetRingColor(poleIndex, count - 1, ring.Color);
                SetRingType(poleIndex, count - 1, RingType.Standard);
            }

            SetRingColor(poleIndex, count, ring.Color);
            SetRingType(poleIndex, count, ring.Type);
            SetRingCount(poleIndex, count + 1);

            // Buz kırma — gerçek oyun TryBreakIceOnTarget mantığı: yeni eklenen halkanın
            // hemen altındaki halka (^2) Frozen ise ve renk eşleşiyorsa Standard yap
            int newCount = GetRingCount(poleIndex);
            if (newCount >= 2)
            {
                int belowIndex = newCount - 2;
                if (GetRingType(poleIndex, belowIndex) == RingType.Frozen
                    && GetRingColor(poleIndex, belowIndex) == ring.Color)
                {
                    SetRingType(poleIndex, belowIndex, RingType.Standard);
                    SetTopRingFrozen(poleIndex, false);
                }
            }

            // Mıknatıs (Magnet) kuralı: Aynı renkteki diğer halkaları çek
            if (ring.Type == RingType.Magnet)
            {
                int capacityLimit = MaxCapacity > 0 ? MaxCapacity : 4;
                for (int p = 0; p < PoleCount; p++)
                {
                    if (p == poleIndex) continue;
                    if (GetRingCount(poleIndex) >= capacityLimit) break;

                    if (CanPopRing(p) && GetTopRingColor(p) == ring.Color)
                    {
                        var pulled = PopRing(p);
                        AddRingSimple(poleIndex, pulled);
                    }
                }
            }
        }

        public void AddRingSimple(int poleIndex, RingData ring)
        {
            int count = GetRingCount(poleIndex);

            // Paint Kontrolü 1 — Gelen halka Paint ise altındaki halkayı boyar ve kendisi Standard olur
            if (ring.Type == RingType.Paint)
            {
                if (count > 0)
                {
                    SetRingColor(poleIndex, count - 1, ring.Color);
                }
                ring.Type = RingType.Standard;
            }
            // Paint Kontrolü 2 — Paint halka üzerine gelen halkayı boyar ve Paint standartlaşır
            else if (count > 0 && GetTopRingType(poleIndex) == RingType.Paint)
            {
                ring.Color = GetTopRingColor(poleIndex);
                SetRingType(poleIndex, count - 1, RingType.Standard);
            }

            // Rainbow Kontrolü 1 — Gelen halka Rainbow ise yerleştiği halkanın rengini alır ve standartlaşır
            if (ring.Type == RingType.Rainbow)
            {
                if (count > 0)
                {
                    ring.Color = GetTopRingColor(poleIndex);
                    ring.Type = RingType.Standard;
                }
            }
            // Rainbow Kontrolü 2 — Rainbow halka üzerine gelen halkanın rengini kopyalar ve standartlaşır
            else if (count > 0 && GetTopRingType(poleIndex) == RingType.Rainbow)
            {
                SetRingColor(poleIndex, count - 1, ring.Color);
                SetRingType(poleIndex, count - 1, RingType.Standard);
            }

            SetRingColor(poleIndex, count, ring.Color);
            SetRingType(poleIndex, count, ring.Type);
            SetRingCount(poleIndex, count + 1);
        }

        public RingData PopRing(int poleIndex)
        {
            int count = GetRingCount(poleIndex);
            if (count == 0) return new RingData(RingColor.None);
            int lastIndex = count - 1;
            
            var ring = new RingData(GetRingColor(poleIndex, lastIndex), GetRingType(poleIndex, lastIndex));
            
            SetRingColor(poleIndex, lastIndex, RingColor.None);
            SetRingType(poleIndex, lastIndex, RingType.Standard);
            SetRingCount(poleIndex, lastIndex);

            if (count == 1)
            {
                SetTopRingFrozen(poleIndex, false);
            }

            // Mystery Kontrolü: Yeni en üstte kalan halka Mystery ise açığa çıkar (Standartlaşır)
            int newCount = count - 1;
            if (newCount > 0 && GetRingType(poleIndex, newCount - 1) == RingType.Mystery)
            {
                SetRingType(poleIndex, newCount - 1, RingType.Standard);
            }

            return ring;
        }

        public bool Equals(BoardState other)
        {
            return Pole0 == other.Pole0 &&
                   Pole1 == other.Pole1 &&
                   Pole2 == other.Pole2 &&
                   Pole3 == other.Pole3 &&
                   Pole4 == other.Pole4 &&
                   Pole5 == other.Pole5 &&
                   Pole6 == other.Pole6 &&
                   Pole7 == other.Pole7 &&
                   Pole8 == other.Pole8 &&
                   Pole9 == other.Pole9 &&
                   Pole10 == other.Pole10 &&
                   Pole11 == other.Pole11 &&
                   Types0 == other.Types0 &&
                   Types1 == other.Types1 &&
                   Types2 == other.Types2 &&
                   Types3 == other.Types3 &&
                   Types4 == other.Types4 &&
                   Types5 == other.Types5 &&
                   Types6 == other.Types6 &&
                   Types7 == other.Types7 &&
                   Types8 == other.Types8 &&
                   Types9 == other.Types9 &&
                   Types10 == other.Types10 &&
                   Types11 == other.Types11 &&
                   PoleCount == other.PoleCount &&
                   MaxCapacity == other.MaxCapacity;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (int)Pole0;
                hash = hash * 23 + (int)Pole1;
                hash = hash * 23 + (int)Pole2;
                hash = hash * 23 + (int)Pole3;
                hash = hash * 23 + (int)Pole4;
                hash = hash * 23 + (int)Pole5;
                hash = hash * 23 + (int)Pole6;
                hash = hash * 23 + (int)Pole7;
                hash = hash * 23 + (int)Pole8;
                hash = hash * 23 + (int)Pole9;
                hash = hash * 23 + (int)Pole10;
                hash = hash * 23 + (int)Pole11;
                hash = hash * 23 + (int)Types0;
                hash = hash * 23 + (int)Types1;
                hash = hash * 23 + (int)Types2;
                hash = hash * 23 + (int)Types3;
                hash = hash * 23 + (int)Types4;
                hash = hash * 23 + (int)Types5;
                hash = hash * 23 + (int)Types6;
                hash = hash * 23 + (int)Types7;
                hash = hash * 23 + (int)Types8;
                hash = hash * 23 + (int)Types9;
                hash = hash * 23 + (int)Types10;
                hash = hash * 23 + (int)Types11;
                hash = hash * 23 + PoleCount;
                hash = hash * 23 + MaxCapacity;
                return hash;
            }
        }
    }
}
