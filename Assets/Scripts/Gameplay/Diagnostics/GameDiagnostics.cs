using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Profiling;

namespace RingFlow.Gameplay.Diagnostics
{
    /// <summary>
    /// GDD §75 performance snapshot captured once per budget measurement.
    /// All values are read-only after capture.
    /// </summary>
    public struct PerformanceBudgetSnapshot
    {
        /// <summary>CPU frame time in milliseconds (FrameTimingManager).</summary>
        public float FrameTimeMs;
        /// <summary>Total allocated Unity heap in megabytes (Profiler API).</summary>
        public float AllocatedRamMb;
        /// <summary>Reserved Unity heap in megabytes.</summary>
        public float ReservedRamMb;
        /// <summary>Unity frame count when this snapshot was taken.</summary>
        public int FrameCount;
        /// <summary>Whether any GDD §75 target threshold was exceeded.</summary>
        public bool AnyTargetExceeded;
        /// <summary>Whether any GDD §75 critical threshold was exceeded.</summary>
        public bool AnyCriticalExceeded;

        // GDD §75 thresholds (targets / criticals)
        public const float FrameTimeTarget   = 14.0f;   // ms
        public const float FrameTimeCritical = 16.6f;   // ms  (60 FPS floor)
        public const float RamTargetMb       = 150f;
        public const float RamCriticalMb     = 220f;

        public override string ToString()
            => $"[Frame {FrameCount}] CPU={FrameTimeMs:F2}ms  RAM={AllocatedRamMb:F1}MB";
    }

    [Preserve]
    public interface IGameDiagnostics : INexusService
    {
        bool IsEnabled { get; set; }
        IReadOnlyList<DiagnosticEntry> Entries { get; }
        event Action<DiagnosticEntry> OnEntryAdded;
        void Log(string category, string message, DiagnosticSeverity severity = DiagnosticSeverity.Info);
        void LogWarning(string category, string message);
        void LogError(string category, string message);
        void Checkpoint(string name);  // Timing checkpoint
        TimeSpan GetElapsedSinceCheckpoint(string name);
        void Clear();

        /// <summary>
        /// Captures a GDD §75 performance budget snapshot using FrameTimingManager and Profiler API.
        /// Call this after a significant game event (level load, win screen, etc.) to record
        /// whether the frame stayed within target/critical thresholds.
        /// Safe to call every frame — internally throttled to once per second.
        /// </summary>
        PerformanceBudgetSnapshot CapturePerformanceSnapshot();

        /// <summary>Last snapshot taken, or default if none yet.</summary>
        PerformanceBudgetSnapshot LastSnapshot { get; }
    }

    public enum DiagnosticSeverity { Trace, Info, Warning, Error, Critical }

    public struct DiagnosticEntry
    {
        public DateTime Timestamp;
        public string Category;
        public string Message;
        public DiagnosticSeverity Severity;
        public float GameTime;
        public int FrameCount;
        public string StackTrace;

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] [{Severity}] [{Category}] {Message}";
        }
    }

    [Preserve]
    public class GameDiagnostics : IGameDiagnostics, IDisposable
    {
        private readonly List<DiagnosticEntry> _entries = new();
        private readonly Dictionary<string, Stopwatch> _checkpoints = new();
        private readonly int _maxEntries = 1000;

        public bool IsEnabled { get; set; } = true;
        public IReadOnlyList<DiagnosticEntry> Entries => _entries;
        public event Action<DiagnosticEntry> OnEntryAdded;

        // FIX-PERF: GDD §75 performance budget tracking fields
        private PerformanceBudgetSnapshot _lastSnapshot;
        private float _lastSnapshotRealtime = -999f;
        private const float SnapshotThrottleSeconds = 1.0f;
        private static readonly UnityEngine.FrameTiming[] _frameTimings = new UnityEngine.FrameTiming[1];

        public PerformanceBudgetSnapshot LastSnapshot => _lastSnapshot;

        private ILoggerService _nexusLogger;

        [Inject]
        public void Initialize(ILoggerService logger)
        {
            _nexusLogger = logger;
            Log("Diagnostics", "GameDiagnostics injected");
        }

        public System.Threading.Tasks.ValueTask InitializeAsync(System.Threading.CancellationToken ct)
        {
            Log("Diagnostics", "GameDiagnostics initialized via INexusService");
            return default;
        }

        public void Log(string category, string message, DiagnosticSeverity severity = DiagnosticSeverity.Info)
        {
            if (!IsEnabled) return;

            var entry = new DiagnosticEntry
            {
                Timestamp = DateTime.Now,
                Category = category,
                Message = message,
                Severity = severity,
                GameTime = Time.time,
                FrameCount = Time.frameCount,
                StackTrace = severity >= DiagnosticSeverity.Warning ? 
                    new StackTrace(1, true).ToString() : null
            };

            lock (_entries)
            {
                _entries.Add(entry);
                if (_entries.Count > _maxEntries)
                    _entries.RemoveAt(0);
            }

            OnEntryAdded?.Invoke(entry);

            // Forward to Nexus Logger (single authoritative output path).
            // Debug.*Log calls are intentionally omitted: NexusLog + ILoggerService handle
            // console output and the in-game diagnostics viewer. Double-logging to Debug.*
            // would duplicate every entry in the Unity Console without adding value.
            switch (severity)
            {
                case DiagnosticSeverity.Error:
                case DiagnosticSeverity.Critical:
                    _nexusLogger?.LogError($"[{category}] {message}");
                    break;
                case DiagnosticSeverity.Warning:
                    _nexusLogger?.LogWarning($"[{category}] {message}");
                    break;
                default:
                    _nexusLogger?.Log($"[{category}] {message}");
                    break;
            }
        }

        public void LogWarning(string category, string message)
        {
            Log(category, message, DiagnosticSeverity.Warning);
        }

        public void LogError(string category, string message)
        {
            Log(category, message, DiagnosticSeverity.Error);
        }

        public void Checkpoint(string name)
        {
            if (!IsEnabled) return;
            _checkpoints[name] = Stopwatch.StartNew();
            Log("Checkpoint", $"Checkpoint '{name}' set");
        }

        public TimeSpan GetElapsedSinceCheckpoint(string name)
        {
            if (_checkpoints.TryGetValue(name, out var sw))
                return sw.Elapsed;
            return TimeSpan.Zero;
        }

        public void Clear()
        {
            lock (_entries) _entries.Clear();
            _checkpoints.Clear();
        }

        /// <inheritdoc/>
        public PerformanceBudgetSnapshot CapturePerformanceSnapshot()
        {
            // Throttle to once per second — Profiler.GetTotalAllocatedMemoryLong() and
            // FrameTimingManager.CaptureFrameTimings() are not free operations.
            float now = Time.realtimeSinceStartup;
            if (now - _lastSnapshotRealtime < SnapshotThrottleSeconds)
                return _lastSnapshot;
            _lastSnapshotRealtime = now;

            // Capture CPU frame time via FrameTimingManager (Unity 2021.2+, works on all platforms).
            UnityEngine.FrameTimingManager.CaptureFrameTimings();
            uint captured = UnityEngine.FrameTimingManager.GetLatestTimings(1, _frameTimings);
            float frameMs = captured > 0 ? (float)_frameTimings[0].cpuFrameTime : Time.deltaTime * 1000f;

            // RAM via Profiler (works in Development and Release builds on mobile).
            float allocMb  = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            float resvMb   = Profiler.GetTotalReservedMemoryLong()  / (1024f * 1024f);

            bool anyTarget   = frameMs > PerformanceBudgetSnapshot.FrameTimeTarget   || allocMb > PerformanceBudgetSnapshot.RamTargetMb;
            bool anyCritical = frameMs > PerformanceBudgetSnapshot.FrameTimeCritical || allocMb > PerformanceBudgetSnapshot.RamCriticalMb;

            _lastSnapshot = new PerformanceBudgetSnapshot
            {
                FrameTimeMs       = frameMs,
                AllocatedRamMb    = allocMb,
                ReservedRamMb     = resvMb,
                FrameCount        = Time.frameCount,
                AnyTargetExceeded   = anyTarget,
                AnyCriticalExceeded = anyCritical,
            };

            if (IsEnabled)
            {
                if (anyCritical)
                    Log("Perf", $"GDD §75 CRITICAL exceeded: {_lastSnapshot}", DiagnosticSeverity.Critical);
                else if (anyTarget)
                    Log("Perf", $"GDD §75 target exceeded: {_lastSnapshot}", DiagnosticSeverity.Warning);
#if DEVELOPMENT_BUILD
                else
                    Log("Perf", $"GDD §75 OK: {_lastSnapshot}", DiagnosticSeverity.Trace);
#endif
            }

            return _lastSnapshot;
        }

        public void OnDispose()
        {
            Dispose();
        }

        public void Dispose()
        {
            IsEnabled = false;
            _entries.Clear();
            _checkpoints.Clear();
        }
    }
}
