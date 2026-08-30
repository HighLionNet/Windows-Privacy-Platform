namespace WindowsPrivacyPlatform.Models;

/// <summary>Product-wide evidence terms. UI surfaces must not invent synonyms for these states.</summary>
public enum EvidenceState
{
    Configured,
    NotConfigured,
    NotObserved,
    Unsupported,
    AccessDenied,
    Unknown,
    Stale,
    Error
}

public static class EvidenceStateSemantics
{
    public static string Label(EvidenceState state) => state switch
    {
        EvidenceState.Configured => "Configured",
        EvidenceState.NotConfigured => "Not configured",
        EvidenceState.NotObserved => "Not observed",
        EvidenceState.Unsupported => "Unsupported",
        EvidenceState.AccessDenied => "Access denied",
        EvidenceState.Unknown => "Unknown",
        EvidenceState.Stale => "Stale",
        EvidenceState.Error => "Error",
        _ => "Unknown"
    };

    public static EvidenceState Classify(ManagedObject item, DateTime? nowUtc = null)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Applicability is ApplicabilityState.NotAvailableOnBuild or
            ApplicabilityState.NotAvailableOnEdition or
            ApplicabilityState.NotAvailableOnWindowsVersion or
            ApplicabilityState.NotPresentOnDevice)
            return EvidenceState.Unsupported;

        var value = item.CurrentState?.Trim() ?? string.Empty;
        if (value.Contains("access denied", StringComparison.OrdinalIgnoreCase))
            return EvidenceState.AccessDenied;
        if (value.StartsWith("error", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("read failure", StringComparison.OrdinalIgnoreCase))
            return EvidenceState.Error;
        if (value.StartsWith("Not configured", StringComparison.OrdinalIgnoreCase))
            return EvidenceState.NotConfigured;
        if (value.StartsWith("Not observed", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(value))
            return EvidenceState.NotObserved;
        if (value.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("Unknown (", StringComparison.OrdinalIgnoreCase))
            return EvidenceState.Unknown;

        var now = nowUtc ?? DateTime.UtcNow;
        if (item.LastVerified is DateTime verified && now - verified > TimeSpan.FromMinutes(30))
            return EvidenceState.Stale;

        return EvidenceState.Configured;
    }

    public static string Detail(EvidenceState state) => state switch
    {
        EvidenceState.Configured => "A value was read from the documented source.",
        EvidenceState.NotConfigured => "The source was read successfully and no value was present.",
        EvidenceState.NotObserved => "This scan did not obtain enough evidence to report a value.",
        EvidenceState.Unsupported => "This control is unavailable on the detected edition, build, or device.",
        EvidenceState.AccessDenied => "Windows denied access to the evidence source.",
        EvidenceState.Unknown => "Evidence was returned, but it cannot be interpreted safely.",
        EvidenceState.Stale => "The value came from an older completed scan and should be refreshed.",
        EvidenceState.Error => "The evidence source failed; no state is inferred.",
        _ => "No reliable state is available."
    };
}

public sealed record CategoryStateSummary(
    int Visible,
    int Configured,
    int NotConfigured,
    int Unknown,
    int Unsupported,
    int AccessDenied,
    int Stale,
    int Error)
{
    public static CategoryStateSummary From(IEnumerable<ManagedObject> items)
    {
        var states = items.Select(item => EvidenceStateSemantics.Classify(item)).ToList();
        return new CategoryStateSummary(
            states.Count,
            states.Count(s => s == EvidenceState.Configured),
            states.Count(s => s == EvidenceState.NotConfigured),
            states.Count(s => s is EvidenceState.Unknown or EvidenceState.NotObserved),
            states.Count(s => s == EvidenceState.Unsupported),
            states.Count(s => s == EvidenceState.AccessDenied),
            states.Count(s => s == EvidenceState.Stale),
            states.Count(s => s == EvidenceState.Error));
    }
}

public sealed record CategoryCopy(string Description, string WhyItMatters);

/// <summary>Authored category-level copy kept outside rendering code.</summary>
public static class CategoryContent
{
    public static CategoryCopy For(ProductDomain domain, string category) => domain switch
    {
        ProductDomain.ConsentStore => new(
            $"Review which apps may use {Friendly(category)} for the signed-in Windows account.",
            "These user choices affect access to sensors and personal data, but a machine AppPrivacy policy can override them."),
        ProductDomain.AppPrivacy => new(
            $"Review machine policy for app access to {Friendly(category)}.",
            "Machine policy can force access on or off for every user, so it is shown separately from each user's privacy choice."),
        ProductDomain.Telemetry => new(
            "Review Windows diagnostic-data collection, feedback prompts, and related data use.",
            "Lower data collection can reduce optional sharing, while some support and managed analytics features may receive less information."),
        ProductDomain.Defender => new(
            $"Review Microsoft Defender controls for {Friendly(category)}.",
            "These settings can change malware prevention and cloud analysis; privacy tradeoffs must not be confused with reducing protection."),
        ProductDomain.Firewall => new(
            $"Review the bounded Windows Firewall controls for the {Friendly(category)}.",
            "Profile defaults determine how unmatched traffic is handled. Individual firewall rules remain read-only in Explore."),
        ProductDomain.Location => new(
            "Review machine-wide location controls and provider availability.",
            "Location can reveal physical movement and is used independently by apps, Search, and Find My Device."),
        ProductDomain.Copilot => new(
            "Review policy-backed Windows Copilot controls that this build can verify.",
            "The legacy Windows integration and the newer Copilot app are separate surfaces; this category never claims that one policy controls both."),
        ProductDomain.Recall => new(
            "Review Windows AI policies that govern Recall snapshots and screen analysis.",
            "Captured screen content can include sensitive information, and availability depends on Windows build and compatible hardware."),
        ProductDomain.RemoteAccess => new(
            "Review policy-backed Remote Desktop and Remote Assistance entry points.",
            "Remote access exposes interactive support or logon surfaces and should match the device's support model and network reachability."),
        ProductDomain.Edge => new(
            $"Review Microsoft Edge policy for {Friendly(category)}.",
            "Browser policies can change tracking, suggestions, reporting, credentials, and compatibility for every Edge profile on the device."),
        _ => new(
            $"Review Windows controls for {Friendly(category)}.",
            "Open Details for the full behavior, side effects, source, and raw values.")
    };

    private static string Friendly(string value) =>
        string.IsNullOrWhiteSpace(value) ? "this category" : value.Trim().ToLowerInvariant();
}

/// <summary>Destination used by search, Overview, and category navigation. It intentionally has no detail-route flag.</summary>
public sealed record SettingsListTarget(
    ProductDomain Domain,
    string Category,
    string Filter,
    string HighlightObjectId)
{
    public static SettingsListTarget For(ManagedObject item, string? filter = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Bucket != CatalogBucket.Settings)
            throw new InvalidOperationException("Only curated Settings entries have a settings-list destination.");

        return new SettingsListTarget(
            item.ProductDomain,
            string.IsNullOrWhiteSpace(item.SubCategory) ? item.ProductDomain.ToString() : item.SubCategory!,
            filter?.Trim() ?? string.Empty,
            item.ObjectId);
    }
}
