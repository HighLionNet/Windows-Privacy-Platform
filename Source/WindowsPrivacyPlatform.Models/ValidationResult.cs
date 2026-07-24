namespace WindowsPrivacyPlatform.Models;

public class ValidationResult
{
    public string ObjectId { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> FailedRules { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
