namespace WindowsPrivacyPlatform.KnowledgeBase;

using WindowsPrivacyPlatform.Models;

public class InMemoryKnowledgeBaseRepository : IKnowledgeBaseRepository
{
    private readonly Dictionary<string, KnowledgeBaseEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly KnowledgeBaseVersion _version;

    public InMemoryKnowledgeBaseRepository()
    {
        _version = new KnowledgeBaseVersion
        {
            SchemaVersion = "0.2",
            Version = "0.1-prototype",
            CreatedTimestamp = DateTime.UtcNow,
            ObjectCount = 0
        };
    }

    public KnowledgeBaseVersion GetVersion()
    {
        _version.ObjectCount = _entries.Count;
        return _version;
    }

    public IReadOnlyList<KnowledgeBaseEntry> GetAll()
    {
        return _entries.Values.ToList().AsReadOnly();
    }

    public KnowledgeBaseEntry? GetByObjectId(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return null;

        _entries.TryGetValue(objectId, out var entry);
        return entry;
    }

    public IReadOnlyList<KnowledgeBaseEntry> GetByFeatureCategory(FeatureCategory category)
    {
        return _entries.Values
            .Where(e => e.Object.FeatureCategory == category)
            .ToList()
            .AsReadOnly();
    }

    public void Add(KnowledgeBaseEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        if (string.IsNullOrWhiteSpace(entry.ObjectId))
            throw new ArgumentException("ObjectId is required.", nameof(entry));

        if (entry.Object == null)
            throw new ArgumentException("ManagedObject is required.", nameof(entry));

        // Basic validation hook only – no rules implemented yet
        entry.Metadata.IsValidated = false;
        entry.Metadata.ValidationNotes.Clear();
        entry.Metadata.LoadedTimestamp = DateTime.UtcNow;

        _entries[entry.ObjectId] = entry;
        _version.ObjectCount = _entries.Count;
    }

    public bool Contains(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return false;

        return _entries.ContainsKey(objectId);
    }

    public int Count => _entries.Count;
}
