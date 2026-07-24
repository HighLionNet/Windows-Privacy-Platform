namespace WindowsPrivacyPlatform.KnowledgeBase;

public class KnowledgeBaseVersion
{
    public string SchemaVersion { get; set; } = "0.2";
    public string KnowledgeBaseVersion { get; set; } = "0.1-prototype";
    public DateTime CreatedTimestamp { get; set; }
    public int ObjectCount { get; set; }
}
