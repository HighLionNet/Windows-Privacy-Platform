namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Read-only query surface over a bound catalog.
/// Future TUI/GUI consumes this instead of reaching into collectors or snapshot lists.
/// No mutation. No system interaction.
/// </summary>
public sealed class SettingsQuery
{
    private readonly IReadOnlyList<ManagedObject> _catalog;

    public SettingsQuery(IReadOnlyList<ManagedObject> catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public IReadOnlyList<ManagedObject> All() => _catalog;

    public IEnumerable<ManagedObject> ByDomain(ProductDomain domain) =>
        _catalog.Where(m => m.ProductDomain == domain);

    public IEnumerable<ManagedObject> ByRisk(RiskLevel risk) =>
        _catalog.Where(m => m.RiskLevel == risk);

    public IEnumerable<ManagedObject> HighImpactConfigured() =>
        _catalog.Where(m =>
            m.RiskLevel == RiskLevel.High &&
            !string.IsNullOrWhiteSpace(m.CurrentState) &&
            !m.CurrentState.Contains("Not configured", StringComparison.OrdinalIgnoreCase) &&
            !m.CurrentState.Contains("Not observed", StringComparison.OrdinalIgnoreCase));

    public IEnumerable<ManagedObject> Conflicts() =>
        _catalog.Where(m => m.Observation?.Effective?.HasConflict == true);

    public IEnumerable<ManagedObject> MachineEnforced() =>
        _catalog.Where(m =>
            m.Observation?.Effective?.EffectiveSource == ConfigurationLayer.MachinePolicy ||
            m.ControlLevel == ControlLevel.AdministratorControlled);

    public IEnumerable<ManagedObject> Unconfigured() =>
        _catalog.Where(m =>
            string.IsNullOrWhiteSpace(m.CurrentState) ||
            m.CurrentState.Contains("Not configured", StringComparison.OrdinalIgnoreCase) ||
            m.CurrentState.Contains("Not observed", StringComparison.OrdinalIgnoreCase));

    public IEnumerable<ManagedObject> Search(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return _catalog;

        var t = term.Trim();
        return _catalog.Where(m =>
            (m.ObjectName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.ObjectId?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.Description?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (m.SubCategory?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            m.ProductDomain.ToString().Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    public ManagedObject? ById(string objectId) =>
        _catalog.FirstOrDefault(m =>
            string.Equals(m.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<ManagedObject> RelatedTo(string objectId)
    {
        var mo = ById(objectId);
        if (mo is null)
            yield break;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (mo.RelatedFeature is not null)
        {
            foreach (var id in mo.RelatedFeature)
                ids.Add(id);
        }

        foreach (var rel in mo.StructuredRelationships)
            ids.Add(rel.ToObjectId);

        foreach (var other in _catalog)
        {
            if (ids.Contains(other.ObjectId))
                yield return other;
        }
    }

    public IEnumerable<ProductDomain> DomainsPresent() =>
        _catalog.Select(m => m.ProductDomain).Distinct().OrderBy(d => d);
}
