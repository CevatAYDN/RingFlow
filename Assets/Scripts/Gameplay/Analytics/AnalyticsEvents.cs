using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §13 — required analytics events. Centralized so the event-name
    /// strings don't drift between callers. All methods are no-ops when no
    /// IAnalyticsService is registered (e.g. in tests).
    /// </summary>
    internal static class AnalyticsEvents
    {
        private static IAnalyticsService _service;

        public static void SetService(IAnalyticsService service) => _service = service;

        public const string EventLevelStart    = "level_start";
        public const string EventLevelComplete = "level_complete";
        public const string EventHintUse       = "hint_use";
        public const string EventUndoUse       = "undo_use";
        public const string EventRestartUse    = "restart_use";
        public const string EventRewardedAd    = "rewarded_ad";
        public const string EventInterstitialAd = "interstitial_ad";
        public const string EventSessionStart  = "session_start";
        public const string EventSessionEnd    = "session_end";

        public static void Track(string eventName, (string key, string value)[] props = null)
        {
            var analytics = _service ?? NexusRuntime.CurrentContext?.TryResolve<IAnalyticsService>();
            if (analytics == null) return;

            if (props == null || props.Length == 0)
            {
                analytics.LogEvent(eventName);
            }
            else
            {
                var dict = new System.Collections.Generic.Dictionary<string, object>(props.Length);
                for (int i = 0; i < props.Length; i++) dict[props[i].key] = props[i].value;
                analytics.LogEvent(eventName, dict);
            }
        }

        public static void LevelStart(int levelIndex, int worldIndex)
        {
            Track(EventLevelStart, new[] { ("level", levelIndex.ToString()), ("world", worldIndex.ToString()) });
        }

        public static void LevelComplete(int levelIndex, int moves, int stars)
        {
            Track(EventLevelComplete, new[]
            {
                ("level", levelIndex.ToString()),
                ("moves", moves.ToString()),
                ("stars", stars.ToString())
            });
        }

        public static void HintUse(int levelIndex)
        {
            Track(EventHintUse, new[] { ("level", levelIndex.ToString()) });
        }

        public static void UndoUse(int levelIndex, bool wasFree)
        {
            Track(EventUndoUse, new[]
            {
                ("level", levelIndex.ToString()),
                ("free", wasFree ? "1" : "0")
            });
        }

        public static void RestartUse(int levelIndex)
        {
            Track(EventRestartUse, new[] { ("level", levelIndex.ToString()) });
        }

        public static void RewardedAd(string placement, bool completed)
        {
            Track(EventRewardedAd, new[]
            {
                ("placement", placement),
                ("completed", completed ? "1" : "0")
            });
        }

        public static void InterstitialAd(string placement)
        {
            Track(EventInterstitialAd, new[]
            {
                ("placement", placement)
            });
        }
    }
}
