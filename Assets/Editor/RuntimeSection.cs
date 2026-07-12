using System.IO;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using RingFlow.Gameplay.Diagnostics;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;

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
                EditorUtility.DisplayDialog(result.Success ? "Setup" : "Setup",
                    result.Success
                        ? "Nexus Bootstrapper successfully added to the active scene! Press Play to run."
                        : result.Message,
                    "OK");
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
            var economy = context.TryResolve<IEconomyService>();

            if (fsm == null)
                EditorGUILayout.HelpBox("IGameStateMachine is not resolved in the current context.", MessageType.Error);
            else
                DrawFsmRow(fsm);

            if (model == null)
                EditorGUILayout.HelpBox("GameplayModel is not resolved in the current context.", MessageType.Warning);
            else
                DrawModelRow(model);

            if (progress == null)
                EditorGUILayout.HelpBox("PlayerProgressModel is not resolved in the current context.", MessageType.Warning);
            if (economy == null)
                EditorGUILayout.HelpBox("IEconomyService is not resolved in the current context.", MessageType.Warning);

            if (progress != null && economy != null)
                DrawEconomyRow(progress, economy);

            EditorGUILayout.Space();
            if (GUILayout.Button("Run Diagnostic Health Check", GUILayout.Height(ButtonHeight)))
                RunHealthCheck(context);

            EditorGUILayout.Space(10f);
            DrawResetButton();
        }

        private static void RunHealthCheck(IContext context)
        {
            var diag = context.TryResolve<RingFlow.Gameplay.Diagnostics.IGameDiagnostics>();
            if (diag == null)
            {
                Debug.LogError("[Diagnostics] IGameDiagnostics is not registered!");
                return;
            }

            diag.Log("HealthCheck", "=== HEALTH CHECK STARTED ===");

            var fsm = context.TryResolve<IGameStateMachine>();
            diag.Log("HealthCheck", $"FSM status: {(fsm != null ? (fsm.CurrentState != null ? fsm.CurrentState.GetType().Name : "No State Active") : "NULL")}",
                fsm == null ? DiagnosticSeverity.Critical : DiagnosticSeverity.Info);

            var serviceChecks = new (string Name, System.Func<object> Resolve)[]
            {
                ("IEconomyService",       () => context.TryResolve<IEconomyService>()),
                ("IProgressionService",   () => context.TryResolve<IProgressionService>()),
                ("IAudioService",         () => context.TryResolve<IAudioService>()),
                ("ILocalizationService",  () => context.TryResolve<ILocalizationService>()),
                ("IAdService",            () => context.TryResolve<IAdService>()),
                ("ILoggerService",        () => context.TryResolve<ILoggerService>()),
            };

            foreach (var (name, resolve) in serviceChecks)
            {
                var svc = resolve();
                diag.Log("HealthCheck", $"Service '{name}': {(svc != null ? "OK" : "MISSING")}",
                    svc == null ? DiagnosticSeverity.Error : DiagnosticSeverity.Info);
            }

            var tracker = context.TryResolve<IViewMediatorTracker>();
            diag.Log("HealthCheck", $"Service 'IViewMediatorTracker': {(tracker != null ? "OK" : "MISSING")}",
                tracker == null ? DiagnosticSeverity.Error : DiagnosticSeverity.Info);

            var uiRoot = Object.FindAnyObjectByType<UIRoot>();
            diag.Log("HealthCheck", $"UIRoot: {(uiRoot != null ? "FOUND" : "MISSING")}",
                uiRoot == null ? DiagnosticSeverity.Critical : DiagnosticSeverity.Info);

            var views = Object.FindObjectsByType<View>(FindObjectsInactive.Exclude);
            diag.Log("HealthCheck", $"Active Views count: {views.Length}");
            foreach (var v in views)
                diag.Log("HealthCheck", $"  - View '{v.GetType().Name}' on GameObject '{v.gameObject.name}' (active: {v.gameObject.activeInHierarchy})");

            diag.Log("HealthCheck", "=== HEALTH CHECK COMPLETED ===");
        }

        private static void DrawFsmRow(IGameStateMachine fsm)
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

        private static void DrawEconomyRow(PlayerProgressModel progress, IEconomyService economy)
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
                var db = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
                int totalLevels = db != null ? db.LevelsPerWorld * db.TotalWorlds : 40;
                progress.MaxUnlockedLevel.Value = totalLevels;
                int worldCount = progress.UnlockedWorlds?.Count ?? 0;
                for (int i = 0; i < worldCount; i++)
                    progress.UnlockedWorlds[i] = true;
            }
        }

        private static void DrawResetButton()
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("RESET ALL PLAYER DATA (Progress & Settings)", GUILayout.Height(ButtonHeight + 10)))
            {
                if (EditorUtility.DisplayDialog("Reset Player Data?",
                    "This will delete all saved level progress, coins, diamonds, and game settings on disk. This action cannot be undone.",
                    "Reset", "Cancel"))
                {
                    ResetAllPlayerData();

                    if (Application.isPlaying)
                    {
                        var context = NexusRuntime.CurrentContext;
                        if (context != null)
                        {
                            context.TryResolve<PlayerProgressModel>()?.Reset();
                            context.TryResolve<SettingsModel>()?.Reset();
                        }
                    }

                    EditorUtility.DisplayDialog("Reset Complete", "All player progress and settings have been reset.", "OK");
                }
            }
            GUI.backgroundColor = originalColor;
        }

        private static void ResetAllPlayerData()
        {
            string secureFolder = Path.Combine(Application.persistentDataPath, EditorPaths.SecureDataFolderName);
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

            PlayerPrefs.DeleteKey(EditorPaths.PlayerPrefsStorageSeed);
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[Editor] Cleared PlayerPrefs.");
        }
    }
}
