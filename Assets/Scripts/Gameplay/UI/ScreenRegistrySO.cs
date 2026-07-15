using System;
using System.Collections.Generic;
using UnityEngine;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Authoritative registry for mapping UI ScreenTypes to their prefab paths
    /// and view class type names. Replaces hardcoded switch-cases in UI Studio
    /// and dynamic instantiators with a serialized configuration asset.
    /// </summary>
    [CreateAssetMenu(fileName = "ScreenRegistry", menuName = "RingFlow/UI/Screen Registry")]
    public sealed class ScreenRegistrySO : ScriptableObject
    {
        [Serializable]
        public struct ScreenMapping
        {
            public ScreenType Screen;
            public string PrefabPath;      // e.g., "UI/Splash" (Resources relative) or Addressables key
            public string ViewTypeName;    // e.g., "RingFlow.Gameplay.UI.SplashView, Assembly-CSharp"
        }

        [Tooltip("Configure screen prefabs and view class mappings here.")]
        public List<ScreenMapping> Mappings = new();

        /// <summary>
        /// Retrieves the mapping entry for a specific screen type.
        /// </summary>
        public bool TryGetMapping(ScreenType screen, out ScreenMapping mapping)
        {
            for (int i = 0; i < Mappings.Count; i++)
            {
                if (Mappings[i].Screen == screen)
                {
                    mapping = Mappings[i];
                    return true;
                }
            }
            mapping = default;
            return false;
        }
    }
}
