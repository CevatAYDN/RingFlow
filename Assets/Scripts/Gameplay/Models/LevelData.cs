using System;
using System.Collections.Generic;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Ring type enum. Values are serialized — never reorder or remove existing values.
    /// </summary>
    public enum RingType
    {
        Standard = 0,
        /// <summary>Key ring (alias for Locked). Use <see cref="Locked"/> for new code.</summary>
        Key,
        Mystery,
        Frozen,
        /// <summary>
        /// Key ring that unlocks a locked pole when placed on it.
        /// Named "Locked" for historical serialization compatibility — represents
        /// the KEY that unlocks, not the pole state.
        /// </summary>
        Locked,
        Stone,
        Glass,
        Rainbow,
        Bomb,
        Chain,
        Magnet,
        Paint,
        Ghost
    }

    [Serializable]
    public struct RingData
    {
        public RingColor Color;
        public RingType Type;
        public int AdditionalData; // bomb sayacı, kilit ID'si vb. ek veriler için

        public RingData(RingColor color, RingType type = RingType.Standard, int additionalData = 0)
        {
            Color = color;
            Type = type;
            AdditionalData = additionalData;
        }
    }

    [Serializable]
    public class PoleData
    {
        public int MaxCapacity = 4;
        public List<RingData> Rings = new(4);
        public bool IsLocked;

        public PoleData() { }

        public PoleData(int maxCapacity)
        {
            MaxCapacity = maxCapacity;
        }
    }

    [Serializable]
    public class LevelData
    {
        public int LevelIndex;
        public int Seed;
        public int TargetMoves;
        public List<PoleData> Poles = new();
    }
}

