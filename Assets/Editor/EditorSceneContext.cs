using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Nexus.Core;
using RingFlow.Gameplay.UI;

namespace RingFlow.Editor
{
    /// <summary>
    /// Tiny scene-resolution cache. We deliberately avoid FindAnyObjectByType in
    /// tight OnGUI loops because each call walks the whole hierarchy. Both
    /// caches invalidate when the scene is opened/closed/recreated.
    /// </summary>
    internal static class EditorSceneContext
    {
        private static Root s_cachedRoot;
        private static UIRoot s_cachedUIRoot;
        private static EventSystem s_cachedEventSystem;
        private static double s_lastRootLookup;
        private static double s_lastUIRootLookup;
        private static double s_lastEventSystemLookup;

        public static void InvalidateAll()
        {
            s_cachedRoot = null;
            s_cachedUIRoot = null;
            s_cachedEventSystem = null;
            s_lastRootLookup = 0;
            s_lastUIRootLookup = 0;
            s_lastEventSystemLookup = 0;
        }

        public static Root GetRoot()
        {
            var now = EditorApplicationTime();
            if (s_cachedRoot == null || (now - s_lastRootLookup) > EditorPaths.RootCacheSeconds)
            {
                s_cachedRoot = Object.FindAnyObjectByType<Root>(FindObjectsInactive.Include);
                s_lastRootLookup = now;
            }
            return s_cachedRoot;
        }

        public static UIRoot GetUIRoot()
        {
            var now = EditorApplicationTime();
            if (s_cachedUIRoot == null || (now - s_lastUIRootLookup) > EditorPaths.UIRootCacheSeconds)
            {
                s_cachedUIRoot = Object.FindAnyObjectByType<UIRoot>(FindObjectsInactive.Include);
                s_lastUIRootLookup = now;
            }
            return s_cachedUIRoot;
        }

        public static EventSystem GetEventSystem()
        {
            var now = EditorApplicationTime();
            if (s_cachedEventSystem == null || (now - s_lastEventSystemLookup) > EditorPaths.UIRootCacheSeconds)
            {
                s_cachedEventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
                s_lastEventSystemLookup = now;
            }
            return s_cachedEventSystem;
        }

        private static double EditorApplicationTime() => EditorApplication.timeSinceStartup;
    }
}
