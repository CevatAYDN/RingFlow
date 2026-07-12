using System.Collections.Generic;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public static class AnalyticsServiceExtensions
    {
        public static void LogEvent(this IAnalyticsService service, string eventName, (string key, string value)[] props)
        {
            if (service == null) return;
            if (props == null || props.Length == 0)
            {
                service.LogEvent(eventName);
                return;
            }

            var dict = new Dictionary<string, object>(props.Length);
            for (int i = 0; i < props.Length; i++)
            {
                dict[props[i].key] = props[i].value;
            }
            service.LogEvent(eventName, dict);
        }

        public static void LevelStart(this IAnalyticsService service, int levelIndex, int worldIndex)
        {
            service.LogEvent("level_start", new[] { ("level", levelIndex.ToString()), ("world", worldIndex.ToString()) });
        }

        public static void LevelComplete(this IAnalyticsService service, int levelIndex, int moves, int stars)
        {
            service.LogEvent("level_complete", new[]
            {
                ("level", levelIndex.ToString()),
                ("moves", moves.ToString()),
                ("stars", stars.ToString())
            });
        }

        public static void HintUse(this IAnalyticsService service, int levelIndex)
        {
            service.LogEvent("hint_use", new[] { ("level", levelIndex.ToString()) });
        }

        public static void UndoUse(this IAnalyticsService service, int levelIndex, bool wasFree)
        {
            service.LogEvent("undo_use", new[]
            {
                ("level", levelIndex.ToString()),
                ("free", wasFree ? "1" : "0")
            });
        }

        public static void RestartUse(this IAnalyticsService service, int levelIndex)
        {
            service.LogEvent("restart_use", new[] { ("level", levelIndex.ToString()) });
        }

        public static void RewardedAd(this IAnalyticsService service, string placement, bool completed)
        {
            service.LogEvent("rewarded_ad", new[]
            {
                ("placement", placement),
                ("completed", completed ? "1" : "0")
            });
        }

        public static void InterstitialAd(this IAnalyticsService service, string placement)
        {
            service.LogEvent("interstitial_ad", new[]
            {
                ("placement", placement)
            });
        }
    }
}
