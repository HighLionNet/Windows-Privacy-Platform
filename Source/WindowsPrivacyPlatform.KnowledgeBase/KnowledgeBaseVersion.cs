namespace WindowsPrivacyPlatform.KnowledgeBase;

using WindowsPrivacyPlatform.Models;

public class KnowledgeBaseVersion
{
    public string SchemaVersion { get; set; } = ManagedObjectCatalog.CatalogVersion;
    public string Version { get; set; } = "catalog-current";
    public DateTime CreatedTimestamp { get; set; }
    public int ObjectCount { get; set; }
}
