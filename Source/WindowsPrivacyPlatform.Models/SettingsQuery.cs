namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Read-only query surface over a bound catalog.
/// No mutation. No system interaction.
/// </summary>
public sealed class SettingsQuery
{
    private readonly IReadOnlyList<ManagedObject> _catalog;
    private readonly Dictionary<string, ManagedObject> _byId;

    public SettingsQuery(IReadOnlyList<ManagedObject> catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _byId = _catalog
            .Where(m => m is not null && !string.IsNullOrWhiteSpace(m.ObjectId))
            .GroupBy(m => m.ObjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ManagedObject> All() => _catalog;

    public IEnumerable<ManagedObject> GetByDomain(ProductDomain domain) =>
        _catalog.Where(m => m.ProductDomain == domain);

    public IEnumerable<ManagedObject> ByDomain(ProductDomain domain) => GetByDomain(domain);

    public ManagedObject? GetById(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return null;
        return _byId.TryGetValue(objectId.Trim(), out var mo) ? mo : null;
    }

    public ManagedObject? ById(string objectId) => GetById(objectId);

    public IEnumerable<ManagedObject> GetRelatedSettings(string objectId)
    {
        var mo = GetById(objectId);
        if (mo is null)
            yield break;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (mo.RelatedFeature is not null)
        {
            foreach (var id in mo.RelatedFeature)
                if (!string.IsNullOrWhiteSpace(id))
                    ids.Add(id);
        }

        foreach (var rel in mo.StructuredRelationships ?? Enumerable.Empty<SettingRelationship>())
        {
            if (!string.IsNullOrWhiteSpace(rel.ToObjectId))
                ids.Add(rel.ToObjectId);
        }

        foreach (var other in _catalog)
        {
            if (other.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase))
                continue;

            if (other.RelatedFeature?.Any(id => id.Equals(objectId, StringComparison.OrdinalIgnoreCase)) == true)
                ids.Add(other.ObjectId);

            if (other.StructuredRelationships?.Any(r =>
                    r.ToObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase)) == true)
                ids.Add(other.ObjectId);
        }

        foreach (var id in ids)
        {
            if (_byId.TryGetValue(id, out var related))
                yield return related;
        }
    }

    public IEnumerable<ManagedObject> RelatedTo(string objectId) => GetRelatedSettings(objectId);

    public IEnumerable<SettingRelationship> GetRelationshipEdges(string objectId)
    {
        var mo = GetById(objectId);
        if (mo?.StructuredRelationships is null)
            yield break;

        foreach (var edge in mo.StructuredRelationships)
            yield return edge;

        foreach (var other in _catalog)
        {
            if (other.StructuredRelationships is null)
                continue;
            foreach (var edge in other.StructuredRelationships)
            {
                if (edge.ToObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase))
                    yield return edge;
            }
        }
    }

    public IEnumerable<ManagedObject> GetConflicts() =>
        _catalog.Where(m =>
            m.Observation?.Effective?.HasConflict == true ||
            m.Observation?.Resolution?.HasConflict == true);

    public IEnumerable<ManagedObject> Conflicts() => GetConflicts();

    public IReadOnlyList<ConflictGroup> GetConflictGroups() => OutcomeConflictEngine.Evaluate(_catalog);

    public ConflictGroup? GetConflictGroup(string objectId) => GetConflictGroups().FirstOrDefault(group =>
        group.ObjectIds.Contains(objectId, StringComparer.OrdinalIgnoreCase));

    public IEnumerable<ManagedObject> GetMachineControlledSettings() =>
        _catalog.Where(m =>
            m.Observation?.Effective?.EffectiveSource is ConfigurationLayer.MachinePolicy
                or ConfigurationLayer.MDMPolicy
                or ConfigurationLayer.SecurityBaseline ||
            m.Observation?.Resolution?.EffectiveSource is ConfigurationLayer.MachinePolicy
                or ConfigurationLayer.MDMPolicy
                or ConfigurationLayer.SecurityBaseline ||
            m.ControlLevel == ControlLevel.AdministratorControlled);

    public IEnumerable<ManagedObject> MachineEnforced() => GetMachineControlledSettings();

    public IEnumerable<ManagedObject> GetSettingsNeedingReview() =>
        _catalog.Where(m =>
            (m.RiskLevel == RiskLevel.High && IsConfigured(m.CurrentState)) ||
            m.Observation?.Effective?.HasConflict == true ||
            m.Observation?.Resolution?.HasConflict == true);

    public IEnumerable<ManagedObject> ByRisk(RiskLevel risk) =>
        _catalog.Where(m => m.RiskLevel == risk);

    public IEnumerable<ManagedObject> HighImpactConfigured() =>
        _catalog.Where(m => m.RiskLevel == RiskLevel.High && IsConfigured(m.CurrentState));

    public IEnumerable<ManagedObject> Unconfigured() =>
        _catalog.Where(m => !IsConfigured(m.CurrentState));

    public IEnumerable<ManagedObject> Search(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return _catalog;

        var t = term.Trim();
        if (t.Length > 200)
            t = t[..200];

        return _catalog.Where(m =>
            (m.ObjectName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.SearchAliases?.Any(alias => alias.Contains(t, StringComparison.OrdinalIgnoreCase)) ?? false) ||
            (m.ObjectId?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.Description?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.TechnicalLocation?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.CurrentState?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.Narrative?.Summary.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.SubCategory?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            m.ProductDomain.ToString().Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<ProductDomain> DomainsPresent() =>
        _catalog.Select(m => m.ProductDomain).Distinct().OrderBy(d => d);

    public SettingExplanation GetExplanation(string objectId)
    {
        var mo = GetById(objectId)
            ?? throw new KeyNotFoundException($"Unknown setting id: {objectId}");
        return SettingExplanationFactory.FromDefinition(mo);
    }

    public SettingExplanation? TryGetExplanation(string objectId)
    {
        var mo = GetById(objectId);
        return mo is null ? null : SettingExplanationFactory.FromDefinition(mo);
    }

    /// <summary>
    /// True only when a real configured value is present.
    /// Unknown, Not configured, Not observed, and Error are all "not configured" for query purposes.
    /// </summary>
    public static bool IsConfigured(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return false;

        var s = state.Trim();
        if (s.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.Equals("Not configured", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.Equals("Not observed", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("Not observed in this scan", StringComparison.OrdinalIgnoreCase))
            return false;
        if (s.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
