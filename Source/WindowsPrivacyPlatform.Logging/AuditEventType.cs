// Source/WindowsPrivacyPlatform.Logging/AuditEventType.cs
namespace WindowsPrivacyPlatform.Logging
{
    public enum AuditEventType
    {
        Debug,
        Information,
        Warning,
        Error,
        /// <summary>Authentication / elevation attempts and outcomes.</summary>
        Auth,
        /// <summary>Configuration change attempts and verified outcomes.</summary>
        Change
    }
}
