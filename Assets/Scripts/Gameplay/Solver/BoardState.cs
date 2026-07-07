using System;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Tahta durumunu temsil eden, 0-GC uyumlu, kopyalaması son derece hızlı,
    /// bit-maskeleme destekli değer tipi (struct).
    /// Her direğin durumu tek bir uint (32-bit) ile sıkıştırılmıştır.
    /// </summary>
    public struct BoardState : IEquatable<BoardState>
    {
        // 10 direğe kadar veri tutar (GDD limiti)
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

        public int PoleCount;

        // Her direk için bit-maskeleme düzeni:
        // - 0-23. bitler: 6 halkaya kadar renk bilgisi (her halka için 4 bit, maks 16 renk)
        // - 24-27. bitler: Direkteki halka sayısı (0-6)
        // - 28-31. bitler: Ekstra özellikler (örneğin kilitli direk durumları)

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
            val &= ~(0xFu << 24); // Count bitlerini sıfırla
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
            val &= ~(0xFu << shift); // Renk bitlerini sıfırla
            val |= ((uint)color & 0xF) << shift;
            SetPoleRaw(poleIndex, val);
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

        public bool CanPopRing(int poleIndex)
        {
            if (IsEmpty(poleIndex)) return false;
            if (IsPoleLocked(poleIndex)) return false;
            if (IsTopRingFrozen(poleIndex)) return false;
            if (GetTopRingColor(poleIndex) == RingColor.Stone) return false;
            return true;
        }

        public bool CanAddRing(int poleIndex, RingColor color, int maxCapacity)
        {
            if (IsPoleLocked(poleIndex))
            {
                return color == RingColor.Key;
            }
            if (GetRingCount(poleIndex) >= maxCapacity) return false;
            if (IsEmpty(poleIndex)) return true;

            RingColor topColor = GetTopRingColor(poleIndex);
            if (topColor == RingColor.Stone) return false;

            return topColor == color;
        }

        public void AddRing(int poleIndex, RingColor color)
        {
            // Eğer direk kilitliyse ve Key yerleşiyorsa kilidi aç
            if (IsPoleLocked(poleIndex) && color == RingColor.Key)
            {
                SetPoleLocked(poleIndex, false);
            }

            // Eğer üstteki halka donmuşsa ve üzerine aynı renk geliyorsa buzu erit
            if (IsTopRingFrozen(poleIndex) && GetTopRingColor(poleIndex) == color)
            {
                SetTopRingFrozen(poleIndex, false);
            }

            int count = GetRingCount(poleIndex);
            SetRingColor(poleIndex, count, color);
            SetRingCount(poleIndex, count + 1);
        }

        public RingColor PopRing(int poleIndex)
        {
            int count = GetRingCount(poleIndex);
            if (count == 0) return RingColor.None;
            int lastIndex = count - 1;
            RingColor color = GetRingColor(poleIndex, lastIndex);
            SetRingColor(poleIndex, lastIndex, RingColor.None);
            SetRingCount(poleIndex, lastIndex);

            if (count == 1)
            {
                SetTopRingFrozen(poleIndex, false);
            }
            return color;
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
                   PoleCount == other.PoleCount;
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
                hash = hash * 23 + PoleCount;
                return hash;
            }
        }
    }
}
