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
    }
}
