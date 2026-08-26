namespace WindowsPrivacyPlatform.Models;

public static class CatalogPolicy
{
    public const int CategoryDrillDownThreshold = 5;

    public static bool RequiresDrillDown(int entryCount) => entryCount >= CategoryDrillDownThreshold;

    public static CatalogBucket ResolveBucket(ManagedObject mo)
    {
        if (mo.IsDynamicInventory)
            return CatalogBucket.SystemInventory;

        var inventoryKind = mo.FeatureCategory is
            FeatureCategory.WindowsService or
            FeatureCategory.ScheduledTask or
            FeatureCategory.AppxPackage or
            FeatureCategory.ProvisionedPackage or
            FeatureCategory.OptionalFeature or
            FeatureCategory.WindowsCapability or
            FeatureCategory.FirewallRule;

        if (inventoryKind)
            return CatalogBucket.SystemInventory;

        // The public Settings surface is a curated policy editor, not a catalog browser.
        // Definitions without a verified write contract remain available to validation and
        // relationships, but are deliberately absent from navigation.
        if (!mo.IsWritable || mo.ProductDomain is ProductDomain.WindowsUpdate or ProductDomain.Storage)
            return CatalogBucket.InternalReference;

        return CatalogBucket.Settings;
    }

    public static string ExclusionReasonText(ExclusionReason reason) => reason switch
    {
        ExclusionReason.UnsupportedValueKind => "The underlying value type is not supported by the verified write pipeline.",
        ExclusionReason.RequiresMultiKeyCoordination => "A safe change requires coordinating multiple system values as one transaction.",
        ExclusionReason.HighRiskIrreversible => "Changing this control can cause lockout or a broad security-policy change, so it is managed only in the native Windows tool.",
        ExclusionReason.ReadOnlyByDesign => "This item is diagnostic inventory and is intentionally view-only.",
        ExclusionReason.NotYetCatalogued => "No approved, version-aware write contract is available for this item.",
        _ => string.Empty
    };

    public static string ApplicabilityBadgeText(ApplicabilityState state) => state switch
    {
        ApplicabilityState.Unknown => "AVAILABILITY UNKNOWN",
        ApplicabilityState.NotPresentOnDevice => "NOT PRESENT",
        _ => "NOT AVAILABLE HERE"
    };
}

public static class ApplicabilityEvaluator
{
    public static (ApplicabilityState State, string Reason) Evaluate(
        ManagedObject mo,
        string? windowsVersion,
        string? edition,
        int build)
    {
        if (mo.MinimumBuild > 0 && build > 0 && build < mo.MinimumBuild)
            return (ApplicabilityState.NotAvailableOnBuild, $"Requires Windows build {mo.MinimumBuild} or later; this device is build {build}.");

        if (mo.MaximumBuild is int maximum && build > maximum)
            return (ApplicabilityState.NotAvailableOnBuild, $"Supported through Windows build {maximum}; this device is build {build}.");

        if (mo.SupportedWindowsVersions is { Count: > 0 } && !string.IsNullOrWhiteSpace(windowsVersion))
        {
            var matched = mo.SupportedWindowsVersions.Any(v =>
                windowsVersion.Contains(v, StringComparison.OrdinalIgnoreCase) ||
                v.Contains(windowsVersion, StringComparison.OrdinalIgnoreCase));
            if (!matched)
                return (ApplicabilityState.NotAvailableOnWindowsVersion, $"Catalog support is {string.Join(" or ", mo.SupportedWindowsVersions)}; this device reports {windowsVersion}.");
        }

        if (mo.SupportedEditions is { Count: > 0 } && !string.IsNullOrWhiteSpace(edition))
        {
            var normalized = NormalizeEdition(edition);
            var matched = mo.SupportedEditions.Any(e => NormalizeEdition(e) == normalized);
            if (!matched)
                return (ApplicabilityState.NotAvailableOnEdition, $"Available on {string.Join(", ", mo.SupportedEditions)}; this device reports {edition}.");
        }

        if (string.IsNullOrWhiteSpace(windowsVersion) && string.IsNullOrWhiteSpace(edition) && build <= 0)
            return (ApplicabilityState.Unknown, "Device version and edition were not available for this scan.");

        return (ApplicabilityState.Applicable, "Available on this Windows edition and build.");
    }

    public static bool IsValueApplicable(ValueMeaning value, string? windowsVersion, string? edition)
    {
        if (value.SupportedVersions.Count > 0 && !string.IsNullOrWhiteSpace(windowsVersion) &&
            !value.SupportedVersions.Any(v => windowsVersion.Contains(v, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (value.SupportedEditions.Count > 0 && !string.IsNullOrWhiteSpace(edition) &&
            !value.SupportedEditions.Any(e => NormalizeEdition(e) == NormalizeEdition(edition)))
            return false;

        return true;
    }

    private static string NormalizeEdition(string value)
    {
        var v = value.Trim().ToLowerInvariant();
        if (v.Contains("workstation")) return "pro for workstations";
        if (v.Contains("enterprise")) return "enterprise";
        if (v.Contains("education")) return "education";
        if (v.Contains("iot")) return "iot";
        if (v.Contains("home")) return "home";
        if (v.Contains("pro")) return "pro";
        return v.Replace("windows 11", string.Empty).Replace("windows 10", string.Empty).Trim();
    }
}
