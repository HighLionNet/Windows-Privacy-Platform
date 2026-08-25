using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding;

/// <summary>
/// Binds curated, observation-only service/task/package/capability catalog anchors to inventories
/// already collected by the scanner. It never changes any inventory item.
/// </summary>
public sealed class InventoryAnchorBinder : IStateBinder
{
    public string Name => nameof(InventoryAnchorBinder);

    public bool CanBind(ManagedObject managedObject) => managedObject.FeatureCategory is
        FeatureCategory.WindowsService or
        FeatureCategory.ScheduledTask or
        FeatureCategory.AppxPackage or
        FeatureCategory.WindowsCapability;

    public void Bind(InventorySnapshot snapshot, ManagedObject managedObject)
    {
        if (snapshot is null || managedObject is null)
            return;

        var (value, source, observed) = managedObject.FeatureCategory switch
        {
            FeatureCategory.WindowsService => BindService(snapshot, managedObject.DiscoveryMethod),
            FeatureCategory.ScheduledTask => BindTask(snapshot, managedObject.DiscoveryMethod),
            FeatureCategory.AppxPackage => BindPattern(snapshot.InstalledPackages, managedObject.DiscoveryMethod, "AppxPackage:"),
            FeatureCategory.WindowsCapability => BindPattern(snapshot.InstalledCapabilities, managedObject.DiscoveryMethod, "WindowsCapability:"),
            _ => ("Not observed in this scan", managedObject.DiscoveryMethod, false)
        };

        var layer = new ConfigurationObservation
        {
            ObjectId = managedObject.ObjectId,
            Layer = ConfigurationLayer.ApplicationPreference,
            RawValue = value,
            SourcePath = source,
            ObservedAt = DateTime.UtcNow,
            ConfidenceScore = observed ? 85 : 40,
            CollectorName = Name,
            EvidenceSource = source,
            CollectionNotes = observed ? "Curated inventory anchor matched collected data." : "The relevant inventory was empty or the anchor was not present.",
            EffectiveConfidence = observed ? EffectiveConfidence.High : EffectiveConfidence.Low
        };
        BinderHelpers.ApplyObservation(managedObject, value, layer);
    }

    private static (string, string, bool) BindService(InventorySnapshot snapshot, string discovery)
    {
        var name = TrimPrefix(discovery, "ServiceController:");
        if (snapshot.Services.Count == 0)
            return ("Not observed in this scan", discovery, false);

        var matches = snapshot.Services.Where(service =>
            service.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            service.Name.StartsWith(name + "_", StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
            return ("Not installed", discovery, true);

        return (string.Join("; ", matches.Select(service => $"{service.Name}: {service.State}, start={service.StartMode}")), discovery, true);
    }

    private static (string, string, bool) BindTask(InventorySnapshot snapshot, string discovery)
    {
        var path = NormalizeTaskPath(TrimPrefix(discovery, "ScheduledTask:"));
        if (snapshot.ScheduledTasks.Count == 0)
            return ("Not observed in this scan", discovery, false);

        var task = snapshot.ScheduledTasks.FirstOrDefault(item =>
            NormalizeTaskPath(item.Path).Equals(path, StringComparison.OrdinalIgnoreCase));
        return task is null
            ? ("Not installed", discovery, true)
            : ($"Present: {task.State}", task.Path, true);
    }

    private static (string, string, bool) BindPattern(IReadOnlyCollection<string> inventory, string discovery, string prefix)
    {
        var pattern = TrimPrefix(discovery, prefix).Replace("*", string.Empty, StringComparison.Ordinal);
        if (inventory.Count == 0)
            return ("Not observed in this scan", discovery, false);

        var matches = inventory.Where(item => item.Contains(pattern, StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
        return matches.Count == 0
            ? ("Not installed", discovery, true)
            : ("Installed: " + string.Join("; ", matches), discovery, true);
    }

    private static string TrimPrefix(string value, string prefix) =>
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? value[prefix.Length..] : value;

    private static string NormalizeTaskPath(string path) => "\\" + path.Trim().Trim('\\');
}
