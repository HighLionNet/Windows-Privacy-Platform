// Source/WindowsPrivacyPlatform.Logging/IAuditLogger.cs
namespace WindowsPrivacyPlatform.Logging
{
    public interface IAuditLogger
    {
        void Debug(string component, string message);
        void Info(string component, string message);
        void Warning(string component, string message);
        void Error(string component, string message);
        void Log(AuditEventType eventType, string component, string message);

        /// <summary>Log an authentication / elevation event (written to auth.log).</summary>
        void Auth(string component, string message);

        /// <summary>Log a configuration change attempt (written to changes.log). Scaffold only until writes authorized.</summary>
        void Change(string component, string message);
    }
}
