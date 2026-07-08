using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
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
    }
}
