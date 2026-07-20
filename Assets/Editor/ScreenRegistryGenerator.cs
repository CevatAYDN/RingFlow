using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using System.Collections.Generic;

namespace RingFlow.Editor
{
    public static class ScreenRegistryGenerator
    {
        [MenuItem("Nexus/Generate Screen Registry")]
        public static void GenerateRegistry()
        {
            string path = "Assets/Resources/Configs/ScreenRegistry.asset";
            
            var registry = ScriptableObject.CreateInstance<ScreenRegistrySO>();
            registry.Mappings = new List<ScreenRegistrySO.ScreenMapping>
            {
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.Splash, PrefabPath = "UI/Splash", ViewTypeName = "RingFlow.Gameplay.UI.SplashView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.MainMenu, PrefabPath = "UI/MainMenu", ViewTypeName = "RingFlow.Gameplay.UI.MainMenuView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.WorldMap, PrefabPath = "UI/WorldMap", ViewTypeName = "RingFlow.Gameplay.UI.WorldMapView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.LevelSelect, PrefabPath = "UI/LevelSelect", ViewTypeName = "RingFlow.Gameplay.UI.LevelSelectView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.Gameplay, PrefabPath = "UI/Gameplay", ViewTypeName = "RingFlow.Gameplay.UI.HUDView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.Pause, PrefabPath = "UI/Pause", ViewTypeName = "RingFlow.Gameplay.UI.PauseView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.Win, PrefabPath = "UI/Win", ViewTypeName = "RingFlow.Gameplay.UI.WinView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.Settings, PrefabPath = "UI/Settings", ViewTypeName = "RingFlow.Gameplay.UI.SettingsView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.DailyReward, PrefabPath = "UI/DailyReward", ViewTypeName = "RingFlow.Gameplay.UI.DailyRewardPopupView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.Onboarding, PrefabPath = "UI/Onboarding", ViewTypeName = "RingFlow.Gameplay.UI.OnboardingView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.GameOver, PrefabPath = "UI/GameOver", ViewTypeName = "RingFlow.Gameplay.UI.GameOverView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.ChestPopup, PrefabPath = "UI/ChestPopup", ViewTypeName = "RingFlow.Gameplay.UI.ChestPopupView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.ParentalGate, PrefabPath = "UI/ParentalGate", ViewTypeName = "RingFlow.Gameplay.UI.ParentalGatePopupView, Assembly-CSharp" },
                new ScreenRegistrySO.ScreenMapping { Screen = ScreenType.MechanicGuide, PrefabPath = "UI/MechanicGuide", ViewTypeName = "RingFlow.Gameplay.UI.MechanicGuideView, Assembly-CSharp" }
            };

            AssetDatabase.CreateAsset(registry, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ScreenRegistryGenerator] Generated ScreenRegistry.asset with {registry.Mappings.Count} mappings.");
        }
    }
}
