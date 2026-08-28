namespace WindowsPrivacyPlatform.Models;

public enum ServiceEvidenceState
{
    Normal,
    StoppedAutomatic,
    MissingExecutable,
    DependencyIssue,
    InvalidConfiguration,
    AccessDenied,
    Unknown
}

public sealed record ServiceFilter(
    string Search = "",
    string State = "All",
    string Startup = "All",
    string Publisher = "All",
    string Issue = "All");

/// <summary>Pure service evidence classification and bounded, literal filtering.</summary>
public static class ServiceInspection
{
    public static ServiceEvidenceState Classify(ServiceInfo service)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (service.AccessDenied)
            return ServiceEvidenceState.AccessDenied;
        if (service.MissingExecutable)
            return ServiceEvidenceState.MissingExecutable;
        if (!string.IsNullOrWhiteSpace(service.ConfigurationError))
            return ServiceEvidenceState.InvalidConfiguration;
        if (service.Dependencies.Any(d => d.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                                          d.Contains("failed", StringComparison.OrdinalIgnoreCase)))
            return ServiceEvidenceState.DependencyIssue;
        if (service.StartMode.Contains("Automatic", StringComparison.OrdinalIgnoreCase) &&
            service.State.Equals("Stopped", StringComparison.OrdinalIgnoreCase))
            return ServiceEvidenceState.StoppedAutomatic;
        if (string.IsNullOrWhiteSpace(service.State))
            return ServiceEvidenceState.Unknown;
        return ServiceEvidenceState.Normal;
    }

    public static string IssueLabel(ServiceEvidenceState state) => state switch
    {
        ServiceEvidenceState.Normal => "No issue observed",
        ServiceEvidenceState.StoppedAutomatic => "Automatic service is stopped",
        ServiceEvidenceState.MissingExecutable => "Executable missing or inaccessible",
        ServiceEvidenceState.DependencyIssue => "Dependency issue observed",
        ServiceEvidenceState.InvalidConfiguration => "Configuration issue observed",
        ServiceEvidenceState.AccessDenied => "Access denied",
        _ => "Unable to verify"
    };

    public static IReadOnlyList<ServiceInfo> Apply(IEnumerable<ServiceInfo> services, ServiceFilter filter)
    {
        ArgumentNullException.ThrowIfNull(services);
        filter ??= new ServiceFilter();
        var search = (filter.Search ?? string.Empty).Trim();
        if (search.Length > 200)
            search = search[..200];

        return services
            .Where(s => filter.State == "All" || s.State.Equals(filter.State, StringComparison.OrdinalIgnoreCase))
            .Where(s => filter.Startup == "All" || s.StartMode.Contains(filter.Startup, StringComparison.OrdinalIgnoreCase))
            .Where(s => filter.Publisher == "All" || PublisherMatches(s, filter.Publisher))
            .Where(s => filter.Issue == "All" || IssueLabel(Classify(s)).Equals(filter.Issue, StringComparison.OrdinalIgnoreCase))
            .Where(s => search.Length == 0 || SearchText(s).Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => string.IsNullOrWhiteSpace(s.DisplayName) ? s.Name : s.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(20_000)
            .ToList();
    }

    private static bool PublisherMatches(ServiceInfo service, string publisher) => publisher switch
    {
        "Microsoft" => service.IsMicrosoft == true,
        "Non-Microsoft" => service.IsMicrosoft == false,
        "Unknown publisher" => service.IsMicrosoft is null,
        _ => true
    };

    private static string SearchText(ServiceInfo s) => string.Join(' ', new[]
    {
        s.Name, s.DisplayName, s.Description, s.State, s.StartMode, s.Account,
        s.ExecutablePath, s.CommandLine, s.FileVersion, s.Publisher, s.SignatureStatus,
        string.Join(' ', s.Dependencies), string.Join(' ', s.Dependents), string.Join(' ', s.RecentEvents), string.Join(' ', s.Tags)
    });

    public static string Diagnosis(ServiceInfo service) => Classify(service) switch
    {
        ServiceEvidenceState.StoppedAutomatic => "Automatic startup is configured, but the service is stopped. Check recent Service Control Manager events and dependencies.",
        ServiceEvidenceState.MissingExecutable => "The configured executable is missing or inaccessible. Verify the installed product before changing startup state.",
        ServiceEvidenceState.DependencyIssue => "A required dependency is missing or stopped. Diagnose that dependency first.",
        ServiceEvidenceState.InvalidConfiguration => "Windows returned incomplete service configuration. Review the configuration evidence and recent events.",
        ServiceEvidenceState.AccessDenied => "Windows denied part of the diagnosis. Re-scan in Administrator mode for additional evidence.",
        ServiceEvidenceState.Unknown => "The service state could not be verified.",
        _ => "No evidence pattern requiring action was observed."
    };
}

public static class ServiceMutationPolicy
{
    private static readonly HashSet<string> DeniedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "RpcSs", "DcomLaunch", "Lsa", "SamSs", "EventLog", "PlugPlay", "Power",
        "WinDefend", "Sense", "wdfilter", "BFE", "mpssvc", "CryptSvc", "Schedule",
        "ProfSvc", "UserManager", "Dhcp", "Dnscache", "NlaSvc", "nsi", "Winmgmt",
        "CSM", "CoreMessaging"
    };

    public static bool IsDeniedName(string? name) => !string.IsNullOrWhiteSpace(name) && DeniedNames.Contains(name);

    public static bool CanMutate(ServiceInfo service, out string reason)
    {
        reason = string.Empty;
        if (service is null || string.IsNullOrWhiteSpace(service.Name) || service.Name.Length > 256 ||
            service.Name.Contains('\\') || service.Name.Contains('/'))
        { reason = "The service identity is invalid."; return false; }
        if (IsDeniedName(service.Name))
        { reason = "This service is on the permanent critical deny-list."; return false; }
        if (service.StartMode.Equals("Boot", StringComparison.OrdinalIgnoreCase) ||
            service.StartMode.Equals("System", StringComparison.OrdinalIgnoreCase))
        { reason = "Boot and system-start services are never mutated."; return false; }
        if (service.CommandLine.Contains("svchost.exe", StringComparison.OrdinalIgnoreCase) &&
            service.CommandLine.Contains(" -k ", StringComparison.OrdinalIgnoreCase))
        { reason = "Shared svchost groups are diagnose-only."; return false; }
        if (service.IsMicrosoft != false)
        { reason = "Microsoft and unknown-publisher services are diagnose-only."; return false; }
        if (service.AccessDenied || service.MissingExecutable || !string.IsNullOrWhiteSpace(service.ConfigurationError))
        { reason = "The service does not have enough verified configuration evidence for mutation."; return false; }
        return true;
    }
}
