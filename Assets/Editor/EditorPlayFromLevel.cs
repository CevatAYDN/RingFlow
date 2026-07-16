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
        private static bool s_isStartingPlayMode = false;

        public static void Play(int level)
        {
            if (level < 1) return;

            // PLAYMODE-BLOCK FIX: Unity's IMGUI fires OnGUI multiple times per frame
            // (Layout → Repaint → input). When the editor enters PlayMode, a layout-phase
            // event can re-trigger this pathway through cached button state and cause
            // sequential level jumps (40 → 41 → 42 ...). Only accept genuine user
            // clicks (MouseDown/MouseUp/Used) as a "Play this level" trigger. Layout
            // and Repaint events are ignored so they cannot chain transitions.
            if (Event.current != null)
            {
                EventType t = Event.current.type;
                if (t != EventType.MouseDown && t != EventType.MouseUp && t != EventType.Used)
                    return;
            }

            if (EditorApplication.isPlaying)
            {
                TransitionToLevel(level);
                return;
            }

            if (s_isStartingPlayMode)
            {
                s_pendingLevel = level;
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

            s_isStartingPlayMode = true;
            s_pendingLevel = level;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                s_isStartingPlayMode = false;
                EditorApplication.update -= Poll;
                EditorApplication.update += Poll;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode)
            {
                s_isStartingPlayMode = false;
                Reset();
            }
        }

        private static void Poll()
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null) return;

            var fsm = context.TryResolve<IGameStateMachine>();
            if (fsm == null) return;

            // FIX-E2: The original guard only blocked BootState/SplashState/LoadingState.
            // After those passed, the FSM typically lands in MainMenuState — at that point
            // the old code immediately called TransitionToLevel, but sometimes the
            // PlayingState transition was firing while MainMenu UI wasn't fully ready,
            // causing a black screen. Adding MainMenuState to the "wait" list and only
            // transitioning once the FSM is stable in a non-transitional state fixes
            // the "Oyna" not working issue.
            //
            // Accepted states to WAIT in (not yet ready to jump):
            //   BootState, SplashState, LoadingState — engine startup phases
            // Accepted state to JUMP from:
            //   MainMenuState (fully loaded, player at menu = safe to transition)
            //   Any other state = also safe (already in-game, resume-style jump)
            var state = fsm.CurrentState;
            if (state == null) return; // still transitioning

            if (state is BootState || state is SplashState || state is LoadingState)
                return; // still booting — keep polling

            // PLAYMODE-BLOCK FIX: After PlayMode is entered, multiple Poll() ticks
            // can fire before the FSM fully settles in MainMenuState. Each tick would
            // otherwise call TransitionToLevel(s_pendingLevel) repeatedly. The
            // s_pendingLevel consumer pattern below ensures the transition fires
            // only once: once consumed (set to -1 here), subsequent ticks short-circuit.
            int pending = s_pendingLevel;
            if (pending < 1)
            {
                Reset();
                return;
            }

            // FIX-E2: Transition with PlayingStateArgs instead of bare int.
            // PlayingState.OnEnterAsync handles both `int` and `PlayingStateArgs`,
            // but using PlayingStateArgs makes the intent explicit and avoids any
            // future boxing/unboxing ambiguity in the FSM dispatch chain.
            TransitionToLevel(pending);
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

            NexusLog.Info("EditorPlayFromLevel", nameof(TransitionToLevel), level.ToString(),
                $"[Editor] Jumping to level {level} via PlayingState FSM transition (PlayingStateArgs). StackTrace:\n{System.Environment.StackTrace}");

            // FIX-E2/E3: Use PlayingStateArgs instead of bare int to be explicit about
            // level index vs resume semantics. PlayingState.OnEnterAsync checks for both,
            // but PlayingStateArgs is the canonical contract and avoids boxing edge cases.
            fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(level));
        }

        private static void Reset()
        {
            s_pendingLevel = -1;
            EditorApplication.update -= Poll;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
    }
}
