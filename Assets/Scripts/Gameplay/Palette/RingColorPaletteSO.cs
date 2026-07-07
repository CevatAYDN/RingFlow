using System;
using System.Collections.Generic;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §11 — Color palette for ring colors.
    /// Supports 3 color-blind modes (Protanopia / Deuteranopia / Tritanopia) per accessibility requirement.
    /// Implementation is a single ScriptableObject so the whole UI can be re-skinned / re-themed without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "RingColorPalette", menuName = "RingFlow/Palette/Color")]
    public class RingColorPaletteSO : ScriptableObject
    {
        public enum ColorBlindMode
        {
            Off = 0,
            Protanopia = 1,
            Deuteranopia = 2,
            Tritanopia = 3
        }

        [Serializable]
        public struct ColorEntry
        {
            public RingColor Color;
            public Color Normal;
            public Color Protanopia;
            public Color Deuteranopia;
            public Color Tritanopia;

            public Color Get(ColorBlindMode mode) => mode switch
            {
                ColorBlindMode.Protanopia  => Protanopia,
                ColorBlindMode.Deuteranopia => Deuteranopia,
                ColorBlindMode.Tritanopia  => Tritanopia,
                _                          => Normal
            };
        }

        [SerializeField] private ColorEntry[] _entries;

        private Dictionary<RingColor, ColorEntry> _cache;

        private void BuildCacheIfNeeded()
        {
            if (_cache != null && _entries != null && _cache.Count == _entries.Length) return;
            _cache = new Dictionary<RingColor, ColorEntry>(_entries.Length);
            for (int i = 0; i < _entries.Length; i++) _cache[_entries[i].Color] = _entries[i];
        }

        public Color GetColor(RingColor color, ColorBlindMode mode)
        {
            BuildCacheIfNeeded();
            if (_cache != null && _cache.TryGetValue(color, out var entry))
                return entry.Get(mode);
            return Color.magenta;
        }

        public bool TryGetEntry(RingColor color, out ColorEntry entry)
        {
            BuildCacheIfNeeded();
            if (_cache != null && _cache.TryGetValue(color, out entry)) return true;
            entry = default;
            return false;
        }
    }
}
