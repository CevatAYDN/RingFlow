using System.IO;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Diagnostics;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using DiagnosticsSeverity = RingFlow.Gameplay.Diagnostics.DiagnosticSeverity;

namespace RingFlow.Editor
{
    public sealed class RuntimeSection : EditorSection
    {
        public override string DisplayName => "PlayMode Lifecycle & State Controller";
        public override string PrefKey => EditorPrefsKeys.FoldRuntime;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!Application.isPlaying)
                {
                    DrawNonPlayMode();
                    return;
                }
                DrawPlayMode();
            }
        }

        private void DrawNonPlayMode()
        {
            EditorGUILayout.HelpBox(
                "Enter PlayMode to control game states, unlock progress, and inject economy.",
                MessageType.Warning);
            EditorGUILayout.Space();
            if (GUILayout.Button("Setup Nexus Bootstrapper in Scene", GUILayout.Height(ButtonHeight)))
            {
                var result = EditorBootstrapper.Bootstrap();
                if (result.Success)
                {
                    EditorUtility.DisplayDialog("Setup",
                        "Nexus Bootstrapper successfully added to the active scene! Press Play to run.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Setup", result.Message, "OK");
                }
            }

            EditorGUILayout.Space(10f);
            DrawResetButton();
        }

        private void DrawPlayMode()
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null)
            {
                EditorGUILayout.HelpBox(
                    "Nexus runtime context is not available yet. Please make sure the context is initialized.",
                    MessageType.Warning);
                return;
            }

            var fsm = context.TryResolve<IGameStateMachine>();
            var model = context.TryResolve<GameplayModel>();
            var progress = context.TryResolve<PlayerProgressModel>();
            var economy = context.TryResolve<Nexus.Core.Services.IEconomyService>();

            if (fsm == null)
            {
                EditorGUILayout.HelpBox("IGameStateMachine is not resolved in the current context.", MessageType.Error);
            }
            else
            {
                DrawFsmRow(fsm);
            }

            if (model == null)
            {
                EditorGUILayout.HelpBox("GameplayModel is not resolved in the current context.", MessageType.Warning);
            }
            else
            {
                DrawModelRow(model);
            }

            if (progress == null)
            {
                EditorGUILayout.HelpBox("PlayerProgressModel is not resolved in the current context.", MessageType.Warning);
            }
            if (economy == null)
            {
                EditorGUILayout.HelpBox("IEconomyService is not resolved in the current context.", MessageType.Warning);
            }

            if (progress != null && economy != null)
            {
                DrawEconomyRow(progress, economy);
            }

            // Diagnostic Health Check
            EditorGUILayout.Space();
            if (GUILayout.Button("Run Diagnostic Health Check", GUILayout.Height(ButtonHeight)))
            {
                RunHealthCheck(context);
            }

            EditorGUILayout.Space(10f);
            DrawResetButton();
        }

        private void RunHealthCheck(IContext context)
        {
            var diag = context.TryResolve<RingFlow.Gameplay.Diagnostics.IGameDiagnostics>();
            if (diag == null)
            {
                Debug.LogError("[Diagnostics] IGameDiagnostics is not registered!");
                return;
            }

            diag.Log("HealthCheck", "=== HEALTH CHECK STARTED ===");

            // 1. Check FSM
            var fsm = context.TryResolve<IGameStateMachine>();
            diag.Log("HealthCheck", $"FSM status: {(fsm != null ? (fsm.CurrentState != null ? fsm.CurrentState.GetType().Name : "No State Active") : "NULL")}",
                fsm == null ? DiagnosticsSeverity.Critical : DiagnosticsSeverity.Info);

            // 2. Check Services
            var economy = context.TryResolve<IEconomyService>();
            diag.Log("HealthCheck", $"Service 'IEconomyService': {(economy != null ? "OK" : "MISSING")}",
                economy == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            var progression = context.TryResolve<IProgressionService>();
            diag.Log("HealthCheck", $"Service 'IProgressionService': {(progression != null ? "OK" : "MISSING")}",
                progression == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            var audio = context.TryResolve<IAudioService>();
            diag.Log("HealthCheck", $"Service 'IAudioService': {(audio != null ? "OK" : "MISSING")}",
                audio == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            var localization = context.TryResolve<ILocalizationService>();
            diag.Log("HealthCheck", $"Service 'ILocalizationService': {(localization != null ? "OK" : "MISSING")}",
                localization == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            var ads = context.TryResolve<IAdService>();
            diag.Log("HealthCheck", $"Service 'IAdService': {(ads != null ? "OK" : "MISSING")}",
                ads == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            var logger = context.TryResolve<ILoggerService>();
            diag.Log("HealthCheck", $"Service 'ILoggerService': {(logger != null ? "OK" : "MISSING")}",
                logger == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            var tracker = context.TryResolve<RingFlow.Gameplay.Diagnostics.IViewMediatorTracker>();
            diag.Log("HealthCheck", $"Service 'IViewMediatorTracker': {(tracker != null ? "OK" : "MISSING")}",
                tracker == null ? DiagnosticsSeverity.Error : DiagnosticsSeverity.Info);

            // 3. Find UIRoot
            var uiRoot = Object.FindAnyObjectByType<RingFlow.Gameplay.UI.UIRoot>();
            diag.Log("HealthCheck", $"UIRoot: {(uiRoot != null ? "FOUND" : "MISSING")}",
                uiRoot == null ? DiagnosticsSeverity.Critical : DiagnosticsSeverity.Info);

            // 4. Trace Active Views in Scene
            var views = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude);
            diag.Log("HealthCheck", $"Active Views count: {views.Length}");
            foreach (var v in views)
            {
                diag.Log("HealthCheck", $"  - View '{v.GetType().Name}' on GameObject '{v.gameObject.name}' (active: {v.gameObject.activeInHierarchy})");
            }

            diag.Log("HealthCheck", "=== HEALTH CHECK COMPLETED ===");
        }

        private void DrawFsmRow(IGameStateMachine fsm)
        {
            if (fsm == null) return;
            EditorGUILayout.LabelField($"Active State: {fsm.CurrentState?.GetType().Name ?? "None"}", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("MainMenu"))   fsm.ChangeStateAsync<MainMenuState>();
                if (GUILayout.Button("LevelSelect")) fsm.ChangeStateAsync<LevelSelectState>();
                if (GUILayout.Button("Playing"))    fsm.ChangeStateAsync<PlayingState>();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Paused")) fsm.ChangeStateAsync<PausedState>();
                if (GUILayout.Button("Win"))    fsm.ChangeStateAsync<WinState>();
            }
        }

        private static void DrawModelRow(GameplayModel model)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gameplay Model:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Moves: {model.MovesCount.Value}  IsWon: {model.IsGameWon.Value}");
        }

        private static void DrawEconomyRow(PlayerProgressModel progress, Nexus.Core.Services.IEconomyService economy)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Economy:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Coins: {progress.Coins.Value}  Diamonds: {progress.Diamonds.Value}");
            EditorGUILayout.LabelField($"Player Lvl: {progress.PlayerLevel.Value}  XP: {progress.Xp.Value}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+100 Coins")) economy.Earn("Coins", 100);
                if (GUILayout.Button("+10 Diamonds")) economy.Earn("Diamonds", 10);
            }
            if (GUILayout.Button("Unlock All Levels"))
            {
                progress.MaxUnlockedLevel.Value = WorldConfigSO.TotalLevels;
                for (int i = 0; i < progress.UnlockedWorlds.Count; i++)
                {
                    progress.UnlockedWorlds[i] = true;
                }
            }
        }

        private void DrawResetButton()
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("RESET ALL PLAYER DATA (Progress & Settings)", GUILayout.Height(ButtonHeight + 10)))
            {
                if (EditorUtility.DisplayDialog("Reset Player Data?",
                    "This will delete all saved level progress, coins, diamonds, and game settings on disk. This action cannot be undone.",
                    "Reset", "Cancel"))
                {
                    DeletePlayerDataDisk();

                    if (Application.isPlaying)
                    {
                        var context = NexusRuntime.CurrentContext;
                        if (context != null)
                        {
                            var progress = context.TryResolve<PlayerProgressModel>();
                            progress?.Reset();

                            var settings = context.TryResolve<SettingsModel>();
                            settings?.Reset();
                        }
                    }

                    EditorUtility.DisplayDialog("Reset Complete", "All player progress and settings have been reset.", "OK");
                }
            }
            GUI.backgroundColor = originalColor;
        }

        private void DeletePlayerDataDisk()
        {
            // 1. Delete SecureData folder
            string secureFolder = Path.Combine(Application.persistentDataPath, "SecureData");
            if (Directory.Exists(secureFolder))
            {
                try
                {
                    Directory.Delete(secureFolder, true);
                    Debug.Log($"[Editor] Deleted secure storage folder: {secureFolder}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Editor] Failed to delete secure folder: {ex.Message}");
                }
            }

            // 2. Clear obfuscated seed and PlayerPrefs
            PlayerPrefs.DeleteKey("NT_StorageSeed");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Editor] Cleared PlayerPrefs.");
        }
    }
}
