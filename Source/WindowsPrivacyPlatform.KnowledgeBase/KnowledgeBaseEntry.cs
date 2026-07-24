namespace WindowsPrivacyPlatform.KnowledgeBase;

using WindowsPrivacyPlatform.Models;

public class KnowledgeBaseEntry
{
    public string ObjectId { get; set; } = string.Empty;
    public ManagedObject Object { get; set; } = new();
    public KnowledgeBaseMetadata Metadata { get; set; } = new();
}
