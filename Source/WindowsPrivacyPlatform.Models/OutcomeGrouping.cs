namespace WindowsPrivacyPlatform.Models;

public sealed record OutcomeSettingFamily(string Family, IReadOnlyList<string> ObjectIds);

/// <summary>Presentation grouping for user ConsentStore choices and their AppPrivacy policy layer.</summary>
public static class OutcomeGrouping
{
    public static IReadOnlyList<OutcomeSettingFamily> Build(IEnumerable<ManagedObject> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var present = items.Where(item => item is not null)
            .GroupBy(item => item.ObjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<OutcomeSettingFamily>();

        foreach (var pair in OutcomeConflictEngine.ConsentFamilies)
        {
            var ids = new[] { pair.UserId, pair.PolicyId }.Where(present.ContainsKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (ids.Count == 0) continue;
            result.Add(new OutcomeSettingFamily(pair.Family, ids));
            foreach (var id in ids) assigned.Add(id);
        }

        result.AddRange(present.Values.Where(item => !assigned.Contains(item.ObjectId))
            .OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new OutcomeSettingFamily(item.ObjectName, new[] { item.ObjectId })));
        return result;
    }
}

public static class FeaturedSettings
{
    private static readonly HashSet<string> DecisionIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "policy.telemetry.allowtelemetry", "privacy.advertisingid.enabled",
        "policy.edge.trackingprevention", "policy.edge.metricsreporting", "policy.edge.diagnosticdata",
        "policy.edge.personalizationreporting", "policy.network.dohmode",
        "policy.network.llmnr", "policy.network.netbios",
        "policy.recall.disableaidataanalysis", "policy.copilot.turnoff",
        "policy.widgets.allow", "policy.onedrive.disablefilesync"
    };

    public static bool IsFeatured(ManagedObject item) =>
        item is not null && (DecisionIds.Contains(item.ObjectId) ||
        item.ObjectId.StartsWith("policy.appprivacy.", StringComparison.OrdinalIgnoreCase) &&
        item.ValueSemantics.Any(value => value.RawValue == "2"));
}
