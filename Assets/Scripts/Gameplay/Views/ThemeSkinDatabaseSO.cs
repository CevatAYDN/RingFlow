using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay.Views
{
    [System.Serializable]
    public struct ThemeSkinEntry
    {
        public int WorldIndex;
        public string ThemeNameKey;
        public Color BackgroundColor;
        public Color PoleColor;
        public Color FloorColor;
        public Material PoleMaterial;
        public Material FloorMaterial;
        public Sprite BgSprite;
    }

    [CreateAssetMenu(fileName = "ThemeSkinDatabase", menuName = "RingFlow/Theme Skin Database", order = 64)]
    public class ThemeSkinDatabaseSO : ScriptableObject
    {
        public List<ThemeSkinEntry> Entries = new();

        public ThemeSkinEntry GetForWorld(int worldIndex)
        {
            if (Entries == null || Entries.Count == 0)
                return new ThemeSkinEntry
                {
                    WorldIndex = worldIndex,
                    BackgroundColor = new Color(0.12f, 0.14f, 0.17f),
                    PoleColor = Color.white,
                    FloorColor = new Color(0.3f, 0.3f, 0.35f)
                };

            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].WorldIndex == worldIndex)
                    return Entries[i];
            }

            // Fallback to last entry
            return Entries[^1];
        }
    }
}
