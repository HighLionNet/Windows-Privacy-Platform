namespace WindowsPrivacyPlatform.Models;

public class ComplianceReport
{
    public string BaselineName { get; set; } = string.Empty;
    public List<ComplianceItem> Results { get; set; } = new();
    public DateTime GenerationTimestamp { get; set; }
}

public class ComplianceItem
{
    public string ObjectId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
