using System;
using System.Collections.Generic;
using System.Diagnostics;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.Scripting;

namespace RingFlow.Gameplay.Diagnostics
{
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

            // Forward to Nexus Logger
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

            // Unity Debug log for critical issues
            if (severity >= DiagnosticSeverity.Error)
            {
                UnityEngine.Debug.LogError($"[GameDiagnostics] [{category}] {message}");
            }
            else if (severity == DiagnosticSeverity.Warning)
            {
                UnityEngine.Debug.LogWarning($"[GameDiagnostics] [{category}] {message}");
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
