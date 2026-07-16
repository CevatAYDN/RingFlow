using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Editor-only helper that launches PlayMode and jumps straight into a chosen
    /// level. It transitions the FSM to <see cref="PlayingState"/> with the level
    /// index as arguments, so the player's saved progression is never modified.
    /// </summary>
    internal static class EditorPlayFromLevel
    {
        private static int s_pendingLevel = -1;

        public static void Play(int level)
        {
            if (level < 1) return;

            if (EditorApplication.isPlaying)
            {
                TransitionToLevel(level);
                return;
            }

            // Ensure the active scene has a Nexus Root so the context can boot.
            if (EditorSceneContext.GetRoot() == null)
            {
                var result = EditorBootstrapper.Bootstrap();
                if (!result.Success)
                {
                    EditorUtility.DisplayDialog("Oyun Başlatılamadı", result.Message, "Tamam");
                    return;
                }

                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }

            s_pendingLevel = level;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.update += Poll;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                Reset();
            }
        }

        private static void Poll()
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null) return;

            var fsm = context.TryResolve<IGameStateMachine>();
            if (fsm == null) return;

            // Wait until boot / splash / loading finishes, then jump into the level.
            var state = fsm.CurrentState;
            if (state is BootState || state is SplashState || state is LoadingState)
                return;

            TransitionToLevel(s_pendingLevel);
            Reset();
        }

        private static void TransitionToLevel(int level)
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null)
            {
                NexusLog.Warn("EditorPlayFromLevel", nameof(TransitionToLevel), level.ToString(),
                    "[Editor] NexusRuntime.CurrentContext is null — cannot transition to PlayingState. Is the scene bootstrapped?");
                return;
            }

            var fsm = context.TryResolve<IGameStateMachine>();
            if (fsm == null)
            {
                NexusLog.Error("EditorPlayFromLevel", nameof(TransitionToLevel), level.ToString(),
                    "[Editor] IGameStateMachine not resolved — FSM unavailable. Check GameplayLifecycle registration.");
                return;
            }

            // EDIT-2: Log the editor-triggered level jump so it appears in the runtime
            // log stream alongside gameplay events. Useful for diagnosing level-select issues.
            NexusLog.Info("EditorPlayFromLevel", nameof(TransitionToLevel), level.ToString(),
                $"[Editor] Jumping to level {level} via PlayingState FSM transition.");

            fsm.ChangeStateAsync<PlayingState>(level);
        }

        private static void Reset()
        {
            s_pendingLevel = -1;
            EditorApplication.update -= Poll;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
