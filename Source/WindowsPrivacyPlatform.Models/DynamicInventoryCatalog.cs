using System.Security.Cryptography;
using System.Text;

namespace WindowsPrivacyPlatform.Models;

/// <summary>Converts live bulk discovery into read-only catalog-shaped inventory entries.</summary>
public static class DynamicInventoryCatalog
{
    public static IReadOnlyList<ManagedObject> Create(
        InventorySnapshot snapshot,
        IEnumerable<ManagedObject> curatedCatalog)
    {
        var curated = curatedCatalog.ToList();
        var entries = new List<ManagedObject>();

        var curatedServices = CuratedIdentifiers(curated, WritableTargetKind.Service);
        foreach (var service in snapshot.Services.Where(s => !curatedServices.Contains(s.Name)))
        {
            entries.Add(Inventory(
                "inventory.service." + Token(service.Name),
                service.Name,
                "Reports an installed Windows service and its observed startup and runtime state.",
                "Service inventory helps identify background components and dependencies without treating them as privacy switches.",
                FeatureCategory.WindowsService,
                ProductDomain.Other,
                "Windows services",
                "Service: " + service.Name,
                $"Startup: {service.StartMode}; state: {service.State}"));
        }

        var curatedTasks = CuratedIdentifiers(curated, WritableTargetKind.ScheduledTask);
        foreach (var task in snapshot.ScheduledTasks.Where(t => !curatedTasks.Contains(t.Path)))
        {
            entries.Add(Inventory(
                "inventory.task." + Token(task.Path),
                LeafName(task.Path),
                "Reports an installed scheduled task and whether Windows considers it ready, running, or disabled.",
                "Task inventory helps explain recurring background activity without exposing task action editing.",
                FeatureCategory.ScheduledTask,
                ProductDomain.Other,
                "Scheduled tasks",
                "Scheduled task: " + task.Path,
                task.State));
        }

        var curatedPackages = CuratedIdentifiers(curated, WritableTargetKind.AppxPackage);
        foreach (var package in snapshot.InstalledPackages.Where(p => !curatedPackages.Contains(p)))
        {
            entries.Add(Inventory(
                "inventory.appx." + Token(package),
                package,
                "Reports an application package installed for the signed-in user.",
                "Package inventory can explain application capabilities and background components.",
                FeatureCategory.AppxPackage,
                ProductDomain.Other,
                "Installed app packages",
                "App package: " + package,
                "Installed"));
        }

        foreach (var package in snapshot.ProvisionedPackages)
        {
            entries.Add(Inventory(
                "inventory.provisioned." + Token(package),
                package,
                "Reports an application package provisioned for new user profiles.",
                "Provisioned packages can appear for users created after the image was prepared.",
                FeatureCategory.ProvisionedPackage,
                ProductDomain.Other,
                "Provisioned app packages",
                "Provisioned package: " + package,
                "Provisioned"));
        }

        var curatedFeatures = CuratedIdentifiers(curated, WritableTargetKind.OptionalFeature);
        foreach (var feature in snapshot.OptionalFeatures.Where(f => !curatedFeatures.Contains(f.Name)))
        {
            entries.Add(Inventory(
                "inventory.feature." + Token(feature.Name),
                feature.Name,
                "Reports an optional Windows component and its servicing state.",
                "Optional-feature inventory helps explain components that may not be present on another device.",
                FeatureCategory.OptionalFeature,
                ProductDomain.Other,
                "Optional features",
                "Optional feature: " + feature.Name,
                feature.State));
        }

        foreach (var capability in snapshot.InstalledCapabilities)
        {
            entries.Add(Inventory(
                "inventory.capability." + Token(capability),
                capability,
                "Reports a Windows capability present in the online image.",
                "Capabilities are separately serviced components and vary by edition, language, and device role.",
                FeatureCategory.WindowsCapability,
                ProductDomain.Other,
                "Windows capabilities",
                "Capability: " + capability,
                "Installed"));
        }

        foreach (var rule in snapshot.FirewallRules)
        {
            var entry = Inventory(
                "inventory.firewallrule." + Token(rule.Name),
                DisplayNameOrFallback(rule.DisplayName, rule.Name),
                "Reports an active Windows Defender Firewall rule definition.",
                "Rule inventory explains network filtering without authorizing port, program, protocol, or scope changes.",
                FeatureCategory.FirewallRule,
                ProductDomain.Firewall,
                "Firewall rules",
                "Firewall rule: " + rule.Name,
                $"{rule.Enabled}; {rule.Direction}; {rule.Action}; profile {rule.Profile}");
            entry.NativeTool = new NativeToolLink
            {
                Label = "Open Firewall with Advanced Security",
                Executable = "wf.msc"
            };
            entries.Add(entry);
        }

        return entries
            .GroupBy(e => e.ObjectId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList()
            .AsReadOnly();
    }

    private static HashSet<string> CuratedIdentifiers(IEnumerable<ManagedObject> catalog, WritableTargetKind kind) =>
        catalog
            .Where(m => m.WritableTarget?.Kind == kind)
            .Select(m => m.WritableTarget!.Identifier)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static ManagedObject Inventory(
        string id,
        string name,
        string description,
        string rationale,
        FeatureCategory category,
        ProductDomain domain,
        string subCategory,
        string technicalLocation,
        string currentState)
    {
        var entry = new ManagedObject
        {
            ObjectId = id,
            ObjectName = string.IsNullOrWhiteSpace(name) ? "Unnamed inventory item" : name,
            ObjectType = "InventoryItem",
            CanonicalPath = id,
            TechnicalLocation = technicalLocation,
            Description = description,
            Rationale = rationale,
            FeatureCategory = category,
            ProductDomain = domain,
            SubCategory = subCategory,
            RiskLevel = RiskLevel.Low,
            ImpactLevel = ImpactLevel.System,
            InterfaceName = InterfaceFor(category),
            ConfigurationType = ConfigurationFor(category),
            DiscoveryMethod = technicalLocation,
            Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None,
            PriorityLevel = PriorityLevel.Optional,
            ControlLevel = ControlLevel.Advisory,
            ComponentOwner = ComponentOwner.Other,
            SchemaVersion = ManagedObjectCatalog.CatalogVersion,
            CreatedBy = nameof(DynamicInventoryCatalog),
            CreatedTimestamp = DateTime.UnixEpoch,
            LifecycleState = LifecycleState.Active,
            ConfidenceScore = 75,
            ConfidenceSource = "Live local inventory",
            ExclusionReason = ExclusionReason.ReadOnlyByDesign,
            Bucket = CatalogBucket.SystemInventory,
            IsDynamicInventory = true,
            Applicability = ApplicabilityState.Applicable,
            ApplicabilityReason = "Observed on this device.",
            CurrentState = currentState,
            LastVerified = DateTime.UtcNow,
            Observation = new SettingObservation
            {
                CurrentValue = currentState,
                ObservedAt = DateTime.UtcNow,
                SourceSummary = technicalLocation
            }
        };
        CatalogNarrativeAuthoring.Apply(entry);
        return entry;
    }

    private static InterfaceName InterfaceFor(FeatureCategory category) => category switch
    {
        FeatureCategory.WindowsService => InterfaceName.ServiceControlManager,
        FeatureCategory.ScheduledTask => InterfaceName.TaskScheduler,
        FeatureCategory.AppxPackage or FeatureCategory.ProvisionedPackage => InterfaceName.AppX,
        FeatureCategory.OptionalFeature => InterfaceName.DISM,
        FeatureCategory.WindowsCapability => InterfaceName.WindowsCapability,
        FeatureCategory.FirewallRule => InterfaceName.Firewall,
        _ => InterfaceName.Registry
    };

    private static ConfigurationType ConfigurationFor(FeatureCategory category) => category switch
    {
        FeatureCategory.WindowsService => ConfigurationType.ServiceState,
        FeatureCategory.ScheduledTask => ConfigurationType.TaskState,
        FeatureCategory.AppxPackage or FeatureCategory.ProvisionedPackage => ConfigurationType.PackageState,
        FeatureCategory.OptionalFeature => ConfigurationType.FeatureState,
        FeatureCategory.WindowsCapability => ConfigurationType.CapabilityState,
        FeatureCategory.FirewallRule => ConfigurationType.FirewallRuleState,
        _ => ConfigurationType.PolicyState
    };

    private static string Token(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string LeafName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Unnamed scheduled task";
        var index = path.LastIndexOf('\\');
        return index >= 0 && index < path.Length - 1 ? path[(index + 1)..] : path;
    }

    private static string DisplayNameOrFallback(string? displayName, string fallback)
    {
        if (string.IsNullOrWhiteSpace(displayName) ||
            displayName.StartsWith("@{", StringComparison.Ordinal) ||
            displayName.Contains("ms-resource:", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(fallback) ? "Unnamed firewall rule" : fallback;
        return displayName;
    }
}
