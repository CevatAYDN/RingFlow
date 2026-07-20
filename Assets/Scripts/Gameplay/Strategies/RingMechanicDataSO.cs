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
            new() { Type = WorldMechanicType.Portal,      IsMovementRestricting = false, AffectedRingTypes = new() { RingType.Standard } }
        };

        // ── Dictionary cache (lazy, built from MechanicUnlocks on first access) ──
        private Dictionary<WorldMechanicType, MechanicUnlockEntry> _mechanicLookup;

        private void OnEnable()
        {
            _mechanicLookup = null; // Invalidates cache on domain reload / asset re-import
        }

        private Dictionary<WorldMechanicType, MechanicUnlockEntry> BuildLookup(GameConfigDatabaseSO dbConfig)
        {
            var lookup = new Dictionary<WorldMechanicType, MechanicUnlockEntry>();
            if (dbConfig != null && dbConfig.MechanicUnlocks != null)
            {
                for (int i = 0; i < dbConfig.MechanicUnlocks.Count; i++)
                {
                    var entry = dbConfig.MechanicUnlocks[i];
                    if (!lookup.ContainsKey(entry.MechanicType))
                        lookup.Add(entry.MechanicType, entry);
                }
            }
            return lookup;
        }

        public string GetDisplayNameKey(WorldMechanicType type, GameConfigDatabaseSO dbConfig)
        {
            if (dbConfig != null && dbConfig.MechanicUnlocks != null)
            {
                if (_mechanicLookup == null)
                    _mechanicLookup = BuildLookup(dbConfig);

                if (_mechanicLookup.TryGetValue(type, out var entry))
                    return entry.DisplayNameKey;
            }
            return $"mechanic.{type.ToString().ToLower()}";
        }

        public int GetFirstAppearanceWorldIndex(WorldMechanicType type, GameConfigDatabaseSO dbConfig)
        {
            if (dbConfig != null && dbConfig.MechanicUnlocks != null)
            {
                if (_mechanicLookup == null)
                    _mechanicLookup = BuildLookup(dbConfig);

                if (_mechanicLookup.TryGetValue(type, out var entry))
                    return entry.FirstAppearanceWorldIndex;
            }
            return 0;
        }

        /// <summary>
        /// Zorla yeniden derleme: dbConfig değiştiğinde veya test ortamında cache'i sıfırlar.
        /// </summary>
        public void InvalidateLookup()
        {
            _mechanicLookup = null;
        }
    }
}
