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

        // Ek veriler (Bomb sayacı, Chain grup ID vb.) için 12 uint
        public uint AddData0;
        public uint AddData1;
        public uint AddData2;
        public uint AddData3;
        public uint AddData4;
        public uint AddData5;
        public uint AddData6;
        public uint AddData7;
        public uint AddData8;
        public uint AddData9;
        public uint AddData10;
        public uint AddData11;

        public int PoleCount;
        public int MaxCapacity;

        public void Initialize(int poleCount, int maxCapacity, int poleCapacity)
        {
            PoleCount = poleCount;
            MaxCapacity = maxCapacity;
        }

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

        public int GetRingAdditional(int poleIndex, int ringIndex)
        {
            uint val = GetAddDataRaw(poleIndex);
            int shift = ringIndex * 4;
            return (int)((val >> shift) & 0xF);
        }

        public void SetRingAdditional(int poleIndex, int ringIndex, int value)
        {
            uint val = GetAddDataRaw(poleIndex);
            int shift = ringIndex * 4;
            val &= ~(0xFu << shift);
            val |= ((uint)(value & 0xF)) << shift;
            SetAddDataRaw(poleIndex, val);
        }

        private uint GetAddDataRaw(int index)
        {
            return index switch
            {
                0 => AddData0,
                1 => AddData1,
                2 => AddData2,
                3 => AddData3,
                4 => AddData4,
                5 => AddData5,
                6 => AddData6,
                7 => AddData7,
                8 => AddData8,
                9 => AddData9,
                10 => AddData10,
                11 => AddData11,
                _ => 0
            };
        }

        private void SetAddDataRaw(int index, uint value)
        {
            switch (index)
            {
                case 0: AddData0 = value; break;
                case 1: AddData1 = value; break;
                case 2: AddData2 = value; break;
                case 3: AddData3 = value; break;
                case 4: AddData4 = value; break;
                case 5: AddData5 = value; break;
                case 6: AddData6 = value; break;
                case 7: AddData7 = value; break;
                case 8: AddData8 = value; break;
                case 9: AddData9 = value; break;
                case 10: AddData10 = value; break;
                case 11: AddData11 = value; break;
            }
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
            return new RingData(
                GetRingColor(poleIndex, count - 1),
                GetRingType(poleIndex, count - 1),
                GetRingAdditional(poleIndex, count - 1));
        }

        public bool CanPopRing(int poleIndex)
        {
            if (IsEmpty(poleIndex)) return false;
            if (IsPoleLocked(poleIndex)) return false;
            if (IsTopRingFrozen(poleIndex)) return false;
            if (GetTopRingType(poleIndex) == RingType.Stone) return false;
            return true;
        }

        public bool CanAddRing(int poleIndex, RingColor color, RingType type, int maxCapacity, int additionalData = 0)
        {
            if (IsPoleLocked(poleIndex))
            {
                return type == RingType.Locked;
            }
            if (GetRingCount(poleIndex) >= maxCapacity) return false;

            // Chain kapasite kuralı: Çözücüde de 2 boşluk gerektiği kontrol edilmeli
            if (type == RingType.Chain && additionalData > 0)
            {
                int partners = 0;
                for (int p = 0; p < PoleCount; p++)
                {
                    if (p == poleIndex) continue;
                    if (IsEmpty(p)) continue;
                    var partnerTop = GetTopRing(p);
                    if (partnerTop.Type == RingType.Chain && partnerTop.AdditionalData == additionalData)
                        partners++;
                }
                if (GetRingCount(poleIndex) + 1 + partners > maxCapacity)
                {
                    return false;
                }
            }

            if (IsEmpty(poleIndex)) return true;

            var top = GetTopRing(poleIndex);
            if (top.Type == RingType.Stone) return top.Color == color;

            // Gökkuşağı (Rainbow) veya Boya (Paint) joker kuralları
            if (type == RingType.Rainbow || type == RingType.Paint || top.Type == RingType.Rainbow || top.Type == RingType.Paint) return true;

            return top.Color == color;
        }

        public void AddRing(int poleIndex, RingData ring)
        {
            int count = GetRingCount(poleIndex);
            if (MaxCapacity > 0 && count >= MaxCapacity) return;

            if (IsPoleLocked(poleIndex) && ring.Type == RingType.Locked)
            {
                SetPoleLocked(poleIndex, false);
                ring.Type = RingType.Standard;
            }

            ResolvePaintAndRainbowSpecial(ref ring, poleIndex, count);

            SetRingColor(poleIndex, count, ring.Color);
            SetRingType(poleIndex, count, ring.Type);
            SetRingAdditional(poleIndex, count, ring.AdditionalData);
            SetRingCount(poleIndex, count + 1);

            // Update TopRingFrozen based on the added ring
            if (ring.Type == RingType.Frozen)
            {
                SetTopRingFrozen(poleIndex, true);
            }
            else
            {
                SetTopRingFrozen(poleIndex, false);
            }

            // Buz kırma — gerçek oyun TryBreakIceOnTarget mantığı:
            // yeni eklenen halkanın altındaki tüm contiguous Frozen halkaları
            // renk eşleşiyorsa kır (while döngüsü)
            int newCount = GetRingCount(poleIndex);
            int belowIndex = newCount - 2;
            bool anyIceBroken = false;
            while (belowIndex >= 0
                && GetRingType(poleIndex, belowIndex) == RingType.Frozen
                && GetRingColor(poleIndex, belowIndex) == ring.Color)
            {
                SetRingType(poleIndex, belowIndex, RingType.Standard);
                anyIceBroken = true;
                belowIndex--;
            }
            if (anyIceBroken)
            {
                SetTopRingFrozen(poleIndex, false);
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
                        AddRingSimple(poleIndex, pulled, true);
                    }
                }
            }

            // Chain Kontrolü — Zincir halkası hareket ettiğinde eşini de yanına çeker
            if (ring.Type == RingType.Chain)
            {
                int capacityLimit = MaxCapacity > 0 ? MaxCapacity : 4;
                if (GetRingCount(poleIndex) < capacityLimit)
                {
                    for (int p = 0; p < PoleCount; p++)
                    {
                        if (p == poleIndex) continue;
                        var topR = GetTopRing(p);
                        if (topR.Type == RingType.Chain && topR.AdditionalData == ring.AdditionalData)
                        {
                            var partner = PopRing(p);
                            AddRingSimple(poleIndex, partner, true);
                            break;
                        }
                    }
                }
            }
        }

        public void AddRingSimple(int poleIndex, RingData ring, bool isSubMove = false)
        {
            int count = GetRingCount(poleIndex);

            if (!isSubMove)
            {
                ResolvePaintAndRainbowSpecial(ref ring, poleIndex, count);
            }

            SetRingColor(poleIndex, count, ring.Color);
            SetRingType(poleIndex, count, ring.Type);
            SetRingAdditional(poleIndex, count, ring.AdditionalData);
            SetRingCount(poleIndex, count + 1);
        }

        public bool HasExplodedBomb()
        {
            for (int p = 0; p < PoleCount; p++)
            {
                int count = GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    if (GetRingType(p, r) == RingType.Bomb && GetRingAdditional(p, r) <= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public RingData PopRing(int poleIndex)
        {
            int count = GetRingCount(poleIndex);
            if (count == 0) return new RingData(RingColor.None);
            int lastIndex = count - 1;
            
            var ring = new RingData(
                GetRingColor(poleIndex, lastIndex),
                GetRingType(poleIndex, lastIndex),
                GetRingAdditional(poleIndex, lastIndex));
            
            SetRingColor(poleIndex, lastIndex, RingColor.None);
            SetRingType(poleIndex, lastIndex, RingType.Standard);
            SetRingAdditional(poleIndex, lastIndex, 0);
            SetRingCount(poleIndex, lastIndex);

            // Frozen flag maintenance: update based on new top ring
            int newCount = count - 1;
            if (newCount > 0 && GetRingType(poleIndex, newCount - 1) == RingType.Frozen)
            {
                SetTopRingFrozen(poleIndex, true);
            }
            else
            {
                SetTopRingFrozen(poleIndex, false);
            }

            // Ghost Kontrolü: Pop edilen halka Ghost ise standartlaşır (oyun içi seçilince standartlaşmasıyla uyumlu)
            if (ring.Type == RingType.Ghost)
            {
                ring.Type = RingType.Standard;
            }

            // Mystery Kontrolü: Yeni en üstte kalan halka Mystery ise açığa çıkar (Standartlaşır)
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
                   AddData0 == other.AddData0 &&
                   AddData1 == other.AddData1 &&
                   AddData2 == other.AddData2 &&
                   AddData3 == other.AddData3 &&
                   AddData4 == other.AddData4 &&
                   AddData5 == other.AddData5 &&
                   AddData6 == other.AddData6 &&
                   AddData7 == other.AddData7 &&
                   AddData8 == other.AddData8 &&
                   AddData9 == other.AddData9 &&
                   AddData10 == other.AddData10 &&
                   AddData11 == other.AddData11 &&
                   PoleCount == other.PoleCount &&
                   MaxCapacity == other.MaxCapacity;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardState other && Equals(other);
        }

        private void ResolvePaintAndRainbowSpecial(ref RingData ring, int poleIndex, int count)
        {
            if (ring.Type == RingType.Paint)
            {
                if (count > 0) SetRingColor(poleIndex, count - 1, ring.Color);
                ring.Type = RingType.Standard;
            }
            else if (count > 0 && GetTopRingType(poleIndex) == RingType.Paint)
            {
                ring.Color = GetTopRingColor(poleIndex);
                SetRingType(poleIndex, count - 1, RingType.Standard);
            }

            if (ring.Type == RingType.Rainbow)
            {
                if (count > 0) { ring.Color = GetTopRingColor(poleIndex); ring.Type = RingType.Standard; }
            }
            else if (count > 0 && GetTopRingType(poleIndex) == RingType.Rainbow)
            {
                SetRingColor(poleIndex, count - 1, ring.Color);
                SetRingType(poleIndex, count - 1, RingType.Standard);
            }
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
                hash = hash * 23 + (int)AddData0;
                hash = hash * 23 + (int)AddData1;
                hash = hash * 23 + (int)AddData2;
                hash = hash * 23 + (int)AddData3;
                hash = hash * 23 + (int)AddData4;
                hash = hash * 23 + (int)AddData5;
                hash = hash * 23 + (int)AddData6;
                hash = hash * 23 + (int)AddData7;
                hash = hash * 23 + (int)AddData8;
                hash = hash * 23 + (int)AddData9;
                hash = hash * 23 + (int)AddData10;
                hash = hash * 23 + (int)AddData11;
                hash = hash * 23 + PoleCount;
                hash = hash * 23 + MaxCapacity;
                return hash;
            }
        }
    }
}
