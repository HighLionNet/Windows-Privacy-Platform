namespace WindowsPrivacyPlatform.KnowledgeBase;

public class KnowledgeBaseMetadata
{
    public string Source { get; set; } = string.Empty;
    public int SourceReliabilityScore { get; set; }
    public DateTime LoadedTimestamp { get; set; }
    public string LoadedBy { get; set; } = "Application";
    public bool IsValidated { get; set; }
    public List<string> ValidationNotes { get; set; } = new();
}
