#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    /// <summary>
    /// Editor-time log counter. Tracks Unity console errors / warnings that
    /// arrive via <see cref="Application.logMessageReceived"/>, including compile
    /// and runtime logs. Stays alive across domain reloads via [InitializeOnLoad].
    /// </summary>
    [InitializeOnLoad]
    public static class LogMonitor
    {
        public static int ErrorCount { get; private set; }
        public static int WarningCount { get; private set; }

        static LogMonitor()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        public static void Reset()
        {
            ErrorCount = 0;
            WarningCount = 0;
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    ErrorCount++;
                    break;
                case LogType.Warning:
                    WarningCount++;
                    break;
            }
        }
    }
}
#endif
