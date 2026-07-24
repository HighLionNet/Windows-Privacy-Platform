// Source/WindowsPrivacyPlatform.Logging/AuditLogger.cs
using System;

namespace WindowsPrivacyPlatform.Logging
{
    /// <summary>
    /// Thread-safe, lightweight console logger.
    /// Zero dependency on Models.
    /// Designed so additional sinks can be added later.
    /// </summary>
    public sealed class AuditLogger : IAuditLogger
    {
        private readonly object _syncRoot = new object();

        // Future: private readonly List<IAuditSink> _sinks = new();

        public void Debug(string component, string message)
            => Log(AuditEventType.Debug, component, message);

        public void Info(string component, string message)
            => Log(AuditEventType.Information, component, message);

        public void Warning(string component, string message)
            => Log(AuditEventType.Warning, component, message);

        public void Error(string component, string message)
            => Log(AuditEventType.Error, component, message);

        public void Log(AuditEventType eventType, string component, string message)
        {
            if (component is null) throw new ArgumentNullException(nameof(component));
            if (message is null) throw new ArgumentNullException(nameof(message));

            lock (_syncRoot)
            {
                var timestamp = DateTime.UtcNow;
                var severity = eventType.ToString().ToUpperInvariant();
                Console.WriteLine($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{severity}] [{component}] {message}");

                // Future sinks invoked here.
            }
        }
    }
}
