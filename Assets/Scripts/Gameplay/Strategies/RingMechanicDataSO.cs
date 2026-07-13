using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay.Strategies
{
    [System.Serializable]
    public struct MechanicEntry
    {
        public WorldMechanicType Type;
        public string DisplayNameKey;
        public int FirstAppearanceWorldIndex;
        public Sprite Icon;
        public bool IsMovementRestricting;
        public List<RingType> AffectedRingTypes;
    }

    [CreateAssetMenu(fileName = "RingMechanicData", menuName = "RingFlow/Ring Mechanic Data", order = 63)]
    public class RingMechanicDataSO : ScriptableObject
    {
        public List<MechanicEntry> Mechanics = new()
        {
            new() { Type = WorldMechanicType.None,        DisplayNameKey = "mechanic.none",        FirstAppearanceWorldIndex = 0,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } },
            new() { Type = WorldMechanicType.Mystery,     DisplayNameKey = "mechanic.mystery",     FirstAppearanceWorldIndex = 1,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Mystery } },
            new() { Type = WorldMechanicType.Frozen,      DisplayNameKey = "mechanic.frozen",      FirstAppearanceWorldIndex = 2,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Frozen } },
            new() { Type = WorldMechanicType.LockedPole,  DisplayNameKey = "mechanic.locked_pole", FirstAppearanceWorldIndex = 3,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } },
            new() { Type = WorldMechanicType.Stone,       DisplayNameKey = "mechanic.stone",       FirstAppearanceWorldIndex = 4,  IsMovementRestricting = true,  AffectedRingTypes = new() { RingType.Stone } },
            new() { Type = WorldMechanicType.Glass,       DisplayNameKey = "mechanic.glass",       FirstAppearanceWorldIndex = 5,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Glass } },
            new() { Type = WorldMechanicType.Rainbow,     DisplayNameKey = "mechanic.rainbow",     FirstAppearanceWorldIndex = 6,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Rainbow } },
            new() { Type = WorldMechanicType.Bomb,        DisplayNameKey = "mechanic.bomb",        FirstAppearanceWorldIndex = 7,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Bomb } },
            new() { Type = WorldMechanicType.Chain,       DisplayNameKey = "mechanic.chain",       FirstAppearanceWorldIndex = 8,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Chain } },
            new() { Type = WorldMechanicType.Magnet,      DisplayNameKey = "mechanic.magnet",      FirstAppearanceWorldIndex = 9,  IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Magnet } },
            new() { Type = WorldMechanicType.Paint,       DisplayNameKey = "mechanic.paint",       FirstAppearanceWorldIndex = 10, IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Paint } },
            new() { Type = WorldMechanicType.Ghost,       DisplayNameKey = "mechanic.ghost",       FirstAppearanceWorldIndex = 11, IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Ghost } },
            new() { Type = WorldMechanicType.Portal,      DisplayNameKey = "mechanic.portal",      FirstAppearanceWorldIndex = 12, IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } }
        };
    }
}
