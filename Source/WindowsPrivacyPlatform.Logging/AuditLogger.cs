// Source/WindowsPrivacyPlatform.Logging/AuditLogger.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

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
        private readonly bool _fileLoggingEnabled;
        private readonly Dictionary<string, string> _previousHashes = new(StringComparer.OrdinalIgnoreCase);
        private const long MaxLogBytes = 2 * 1024 * 1024;

        public AuditLogger()
        {
            _logRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WindowsPrivacyPlatform",
                "Logs");

            try
            {
                Directory.CreateDirectory(_logRoot);
                _fileLoggingEnabled = true;
            }
            catch
            {
                // A shared temporary directory is not an acceptable audit fallback.
                // Continue with console diagnostics only.
                _fileLoggingEnabled = false;
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
                var line = $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{severity}] [{SanitizeField(component, 128)}] {SanitizeField(message, 8192)}";

                Console.WriteLine(line);

                try
                {
                    if (!_fileLoggingEnabled)
                        return;
                    if (eventType == AuditEventType.Auth)
                        AppendBounded(_authLogPath, line);
                    else if (eventType == AuditEventType.Change)
                        AppendBounded(_changeLogPath, line);
                }
                catch
                {
                    // File write failure must never break the application.
                }
            }
        }

        private void AppendBounded(string path, string line)
        {
            if (File.Exists(path) && new FileInfo(path).Length >= MaxLogBytes)
                File.Move(path, path + ".previous", overwrite: true);
            if (!_previousHashes.TryGetValue(path, out var previous))
                previous = ReadLastHash(path) ?? new string('0', 64);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(previous + "|" + line)));
            var chained = $"{line} [PREV:{previous}] [HASH:{hash}]";
            File.AppendAllText(path, chained + Environment.NewLine, new UTF8Encoding(false));
            _previousHashes[path] = hash;
        }

        private static string? ReadLastHash(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length > MaxLogBytes) return null;
                using var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: false);
                string? line;
                string? last = null;
                while ((line = reader.ReadLine()) is not null) last = line;
                if (last is null) return null;
                var marker = last.LastIndexOf("[HASH:", StringComparison.Ordinal);
                if (marker < 0 || marker + 71 > last.Length) return null;
                var candidate = last.Substring(marker + 6, 64);
                return candidate.All(Uri.IsHexDigit) ? candidate.ToUpperInvariant() : null;
            }
            catch
            {
                return null;
            }
        }

        public static bool VerifyHashChain(IEnumerable<string> lines)
        {
            var expectedPrevious = new string('0', 64);
            foreach (var fullLine in lines)
            {
                var previousMarker = fullLine.LastIndexOf(" [PREV:", StringComparison.Ordinal);
                var hashMarker = fullLine.LastIndexOf(" [HASH:", StringComparison.Ordinal);
                if (previousMarker < 0 || hashMarker < 0 || hashMarker <= previousMarker) return false;
                var previous = fullLine.Substring(previousMarker + 7, 64);
                var actual = fullLine.Substring(hashMarker + 7, 64);
                if (!previous.Equals(expectedPrevious, StringComparison.OrdinalIgnoreCase)) return false;
                var payload = fullLine[..previousMarker];
                var computed = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(previous + "|" + payload)));
                if (!computed.Equals(actual, StringComparison.OrdinalIgnoreCase)) return false;
                expectedPrevious = actual;
            }
            return true;
        }

        public static string SanitizeField(string value, int maxLength = 8192)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (maxLength is < 1 or > 65_536) throw new ArgumentOutOfRangeException(nameof(maxLength));
            var singleLine = new string(value
                .Where(c => !char.IsControl(c) || c == '\t')
                .Take(maxLength)
                .ToArray());
            try
            {
                return Regex.Replace(
                    singleLine,
                    @"(?i)\b(password|passwd|token|secret|authorization)\s*[:=]\s*[^\s;]+",
                    "$1=[redacted]",
                    RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(50));
            }
            catch (RegexMatchTimeoutException)
            {
                return "[redacted: audit field exceeded sanitizer time budget]";
            }
        }
    }
}
