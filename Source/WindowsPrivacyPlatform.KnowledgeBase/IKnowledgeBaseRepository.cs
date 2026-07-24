namespace WindowsPrivacyPlatform.KnowledgeBase;

using WindowsPrivacyPlatform.Models;

public interface IKnowledgeBaseRepository
{
    KnowledgeBaseVersion GetVersion();

    IReadOnlyList<KnowledgeBaseEntry> GetAll();

    KnowledgeBaseEntry? GetByObjectId(string objectId);

    IReadOnlyList<KnowledgeBaseEntry> GetByFeatureCategory(FeatureCategory category);

    void Add(KnowledgeBaseEntry entry);

    bool Contains(string objectId);

    int Count { get; }
}
