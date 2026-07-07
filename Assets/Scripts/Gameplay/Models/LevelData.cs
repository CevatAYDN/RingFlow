using System;
using System.Collections.Generic;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public enum RingType
    {
        Standard = 0,
        Mystery,
        Frozen,
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

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "RingFlow/Level Data")]
    public class LevelDataSO : ScriptableObject
    {
        public LevelData Data;
    }
}
