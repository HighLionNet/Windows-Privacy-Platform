namespace WindowsPrivacyPlatform.Models;

public class AuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = "Prototype";
    public string Action { get; set; } = string.Empty;
    public string ObjectId { get; set; } = "System";
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}
