using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.FSM;
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
            if (context == null) return;

            var fsm = context.TryResolve<IGameStateMachine>();
            if (fsm == null) return;

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
