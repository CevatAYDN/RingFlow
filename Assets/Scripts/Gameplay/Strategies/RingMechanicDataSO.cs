using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay.Strategies
{
    [System.Serializable]
    public struct MechanicEntry
    {
        public WorldMechanicType Type;
        public Sprite Icon;
        public bool IsMovementRestricting;
        public List<RingType> AffectedRingTypes;
    }

    [CreateAssetMenu(fileName = "RingMechanicData", menuName = "RingFlow/Ring Mechanic Data", order = 63)]
    public class RingMechanicDataSO : ScriptableObject
    {
        public List<MechanicEntry> Mechanics = new()
        {
            new() { Type = WorldMechanicType.None,        IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } },
            new() { Type = WorldMechanicType.Mystery,     IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Mystery } },
            new() { Type = WorldMechanicType.Frozen,      IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Frozen } },
            new() { Type = WorldMechanicType.LockedPole,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } },
            new() { Type = WorldMechanicType.Stone,       IsMovementRestricting = true,  AffectedRingTypes = new() { RingType.Stone } },
            new() { Type = WorldMechanicType.Glass,       IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Glass } },
            new() { Type = WorldMechanicType.Rainbow,     IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Rainbow } },
            new() { Type = WorldMechanicType.Bomb,        IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Bomb } },
            new() { Type = WorldMechanicType.Chain,       IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Chain } },
            new() { Type = WorldMechanicType.Magnet,      IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Magnet } },
            new() { Type = WorldMechanicType.Paint,       IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Paint } },
            new() { Type = WorldMechanicType.Ghost,       IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Ghost } },
            new() { Type = WorldMechanicType.Portal,      IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } }
        };

        public string GetDisplayNameKey(WorldMechanicType type, GameConfigDatabaseSO dbConfig)
        {
            if (dbConfig != null && dbConfig.MechanicUnlocks != null)
            {
                for (int i = 0; i < dbConfig.MechanicUnlocks.Count; i++)
                {
                    if (dbConfig.MechanicUnlocks[i].MechanicType == type)
                        return dbConfig.MechanicUnlocks[i].DisplayNameKey;
                }
            }
            return $"mechanic.{type.ToString().ToLower()}";
        }

        public int GetFirstAppearanceWorldIndex(WorldMechanicType type, GameConfigDatabaseSO dbConfig)
        {
            if (dbConfig != null && dbConfig.MechanicUnlocks != null)
            {
                for (int i = 0; i < dbConfig.MechanicUnlocks.Count; i++)
                {
                    if (dbConfig.MechanicUnlocks[i].MechanicType == type)
                        return dbConfig.MechanicUnlocks[i].FirstAppearanceWorldIndex;
                }
            }
            return 0;
        }
    }
}
