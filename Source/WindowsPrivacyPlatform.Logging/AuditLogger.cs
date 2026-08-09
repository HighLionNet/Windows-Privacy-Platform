// Source/WindowsPrivacyPlatform.Logging/AuditLogger.cs
using System;
using System.IO;

namespace WindowsPrivacyPlatform.Logging
{
    /// <summary>
    /// Thread-safe logger with console + dedicated file sinks.
    /// Auth events → auth.log; Change events → changes.log; others → console (+ optional general.log).
    /// Log root: %LocalAppData%\WindowsPrivacyPlatform\Logs
    /// </summary>
    public sealed class AuditLogger : IAuditLogger
    {
        private readonly object _syncRoot = new object();
        private readonly string _logRoot;
        private readonly string _authLogPath;
        private readonly string _changeLogPath;

        public AuditLogger()
        {
            _logRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WindowsPrivacyPlatform",
                "Logs");

            try
            {
                Directory.CreateDirectory(_logRoot);
            }
            catch
            {
                // Fall back to temp if LocalAppData is unavailable.
                _logRoot = Path.Combine(Path.GetTempPath(), "WindowsPrivacyPlatform", "Logs");
                try { Directory.CreateDirectory(_logRoot); } catch { /* last resort: console only */ }
            }

            _authLogPath = Path.Combine(_logRoot, "auth.log");
            _changeLogPath = Path.Combine(_logRoot, "changes.log");
        }

        public void Debug(string component, string message)
            => Log(AuditEventType.Debug, component, message);

        public void Info(string component, string message)
            => Log(AuditEventType.Information, component, message);

        public void Warning(string component, string message)
            => Log(AuditEventType.Warning, component, message);

        public void Error(string component, string message)
            => Log(AuditEventType.Error, component, message);

        public void Auth(string component, string message)
            => Log(AuditEventType.Auth, component, message);

        public void Change(string component, string message)
            => Log(AuditEventType.Change, component, message);

        public void Log(AuditEventType eventType, string component, string message)
        {
            if (component is null) throw new ArgumentNullException(nameof(component));
            if (message is null) throw new ArgumentNullException(nameof(message));

            lock (_syncRoot)
            {
                var timestamp = DateTime.UtcNow;
                var severity = eventType.ToString().ToUpperInvariant();
                var line = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{severity}] [{component}] {message}";

                Console.WriteLine(line);

                try
                {
                    if (eventType == AuditEventType.Auth)
                        File.AppendAllText(_authLogPath, line + Environment.NewLine);
                    else if (eventType == AuditEventType.Change)
                        File.AppendAllText(_changeLogPath, line + Environment.NewLine);
                }
                catch
                {
                    // File write failure must never break the application.
                }
            }
        }
    }
}
