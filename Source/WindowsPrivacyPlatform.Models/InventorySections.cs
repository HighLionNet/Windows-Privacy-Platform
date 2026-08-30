namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Domain-organized read-only observations used by the machine overview, settings evidence,
/// and System Explorer surfaces.
/// </summary>

public class IdentityInventory
{
    public string ComputerName { get; set; } = string.Empty;
    public string SignedInUser { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Unknown";
    public string WindowsVersion { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public int BuildNumber { get; set; }

    public string Architecture { get; set; } = string.Empty;
    public string DeviceManufacturer { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public long TotalPhysicalMemoryMiB { get; set; }
    public string SecureBootState { get; set; } = "Unknown";
    public string TpmPresent { get; set; } = "Unknown";
    public string TpmVersion { get; set; } = "Unknown";
    public string BitLockerProtectionStatus { get; set; } = "Unknown";
    public string DomainMembership { get; set; } = "Unknown";
    public string AzureAdJoined { get; set; } = "Unknown";
    public string PowerShellVersion { get; set; } = "Unknown";
    public string DotNetRuntimeVersion { get; set; } = string.Empty;
    public string IdentityCollectionNotes { get; set; } = string.Empty;
    public EffectiveConfidence IdentityConfidence { get; set; } = EffectiveConfidence.Unknown;
}

public class ApplicationInventory
{
    public List<string> InstalledPackages { get; set; } = new();
    public List<string> ProvisionedPackages { get; set; } = new();
    public List<string> InstalledCapabilities { get; set; } = new();
    public List<OptionalFeatureInfo> OptionalFeatures { get; set; } = new();
    public BrowserPresenceSnapshot Browsers { get; set; } = new();
}

public class PrivacyInventory
{
    public List<PrivacySettingInfo> Settings { get; set; } = new();
}

public class PolicyInventory
{
    public List<PolicySettingInfo> Settings { get; set; } = new();
}

public class SystemInventory
{
    public List<ServiceInfo> Services { get; set; } = new();
    public List<TaskInfo> ScheduledTasks { get; set; } = new();
    public EvidenceState ScheduledTaskEvidence { get; set; } = EvidenceState.NotObserved;
    public string ScheduledTaskCollectionNotes { get; set; } = string.Empty;
}

public class SecurityInventory
{
    // Read-only local evidence. ProtectionProducts comes from Windows Security Center;
    // no vendor API is queried and no protection state is changed.
    public string DefenderServiceState { get; set; } = "Unknown";
    public ProtectionProductObservationStatus ProtectionProductStatus { get; set; } =
        ProtectionProductObservationStatus.NotObserved;
    public List<ProtectionProductInfo> ProtectionProducts { get; set; } = new();
    public string ProtectionProductCollectionNotes { get; set; } = string.Empty;
}

public enum ProtectionProductObservationStatus
{
    NotObserved,
    Observed,
    AccessDenied,
    Error
}

/// <summary>One read-only product registration reported by Windows Security Center.</summary>
public class ProtectionProductInfo
{
    public string DisplayName { get; set; } = string.Empty;
    public int? ProductState { get; set; }
    public bool? IsActive { get; set; }
    public bool IsMicrosoftDefender { get; set; }
}

public static class ProtectionProductPresentation
{
    public static string Summary(SecurityInventory security)
    {
        ArgumentNullException.ThrowIfNull(security);

        if (security.ProtectionProductStatus == ProtectionProductObservationStatus.AccessDenied)
            return "access denied";
        if (security.ProtectionProductStatus == ProtectionProductObservationStatus.Error)
            return "observation error";
        if (security.ProtectionProductStatus != ProtectionProductObservationStatus.Observed ||
            security.ProtectionProducts.Count == 0)
            return "not observed";

        var parts = new List<string>();
        var defender = security.ProtectionProducts.FirstOrDefault(product => product.IsMicrosoftDefender);
        if (defender is { IsActive: true })
            parts.Add("Defender active");
        else if (defender is not null)
            parts.Add("Microsoft Defender reported");

        var vendors = security.ProtectionProducts
            .Where(product => !product.IsMicrosoftDefender)
            .Select(product => product.DisplayName.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (vendors.Count > 0)
            parts.Add(string.Join(", ", vendors) + " reported");

        return parts.Count == 0 ? "protection product reported" : string.Join(" · ", parts);
    }
}

public class NetworkingInventory
{
    public List<FirewallProfileInfo> FirewallProfiles { get; set; } = new();
    public List<FirewallRuleInfo> FirewallRules { get; set; } = new();
    public string FirewallServiceState { get; set; } = "Unknown";
    public string FirewallCollectionNotes { get; set; } = string.Empty;
    public DnsResolutionSnapshot Dns { get; set; } = new();
}

public sealed class DnsResolutionSnapshot
{
    public DateTime CapturedAtUtc { get; set; }
    public List<DnsInterfaceInfo> Interfaces { get; set; } = new();
    public List<NrptRuleInfo> NrptRules { get; set; } = new();
    public DnsLayerObservation Nrpt { get; set; } = new();
    public DnsLayerObservation WindowsDoh { get; set; } = new();
    public DnsLayerObservation PreferredPath { get; set; } = new();
    public DnsLayerObservation VpnDnsPath { get; set; } = new();
    public List<DnsProbeInfo> ResolverProbes { get; set; } = new();
    public List<ExternalDnsInfo> ExternalApps { get; set; } = new();
}

public sealed class DnsInterfaceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int InterfaceIndex { get; set; }
    public int? InterfaceMetric { get; set; }
    public List<string> IPv4Addresses { get; set; } = new();
    public List<string> IPv6Addresses { get; set; } = new();
    public List<string> DnsServers { get; set; } = new();
    public string AddressSource { get; set; } = "Unknown";
    public EvidenceState Evidence { get; set; } = EvidenceState.Unknown;
    public bool IsVpnOrTunnel { get; set; }
}

public sealed class NrptRuleInfo
{
    public string Namespace { get; set; } = string.Empty;
    public string NameServers { get; set; } = string.Empty;
    public string DnsSec { get; set; } = "Unknown";
    public string DirectAccess { get; set; } = "Unknown";
    public string Source { get; set; } = string.Empty;
}

public sealed class DnsLayerObservation
{
    public EvidenceState Evidence { get; set; } = EvidenceState.Unknown;
    public string Summary { get; set; } = "Unknown";
    public string Source { get; set; } = string.Empty;
}

public sealed class DnsProbeInfo
{
    public string Resolver { get; set; } = string.Empty;
    public string QueryName { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public EvidenceState Evidence { get; set; } = EvidenceState.Unknown;
    public string Source { get; set; } = string.Empty;
}

public sealed class ExternalDnsInfo
{
    public string Application { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public EvidenceState Evidence { get; set; } = EvidenceState.Unknown;
    public string Source { get; set; } = "ExternalApp";
}

public sealed class BrowserPresenceSnapshot
{
    public BrowserProductInfo Edge { get; set; } = new() { Name = "Microsoft Edge" };
    public BrowserProductInfo WebView2 { get; set; } = new() { Name = "Microsoft Edge WebView2 Runtime" };
    public DnsLayerObservation DefaultBrowser { get; set; } = new();
}

public sealed class BrowserProductInfo
{
    public string Name { get; set; } = string.Empty;
    public EvidenceState Evidence { get; set; } = EvidenceState.Unknown;
    public string Version { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

public class DeviceInventory
{
}

/// <summary>
/// Read-only observation of one Windows Firewall profile (Domain / Private / Public).
/// </summary>
public class FirewallProfileInfo
{
    public string ProfileName { get; set; } = string.Empty;
    public string Enabled { get; set; } = "Unknown";
    public string DefaultInboundAction { get; set; } = "Unknown";
    public string DefaultOutboundAction { get; set; } = "Unknown";
    public string LoggingEnabled { get; set; } = "Unknown";
    public string InboundNotifications { get; set; } = "Unknown";
    public string SourcePath { get; set; } = string.Empty;
    public string CollectionNotes { get; set; } = string.Empty;
}

public class OptionalFeatureInfo
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = "Unknown";
}

public class FirewallRuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Enabled { get; set; } = "Unknown";
    public string Direction { get; set; } = "Unknown";
    public string Action { get; set; } = "Unknown";
    public string Profile { get; set; } = "Unknown";
}

/// <summary>
/// Pure-data machine context for the Home / Machine Overview landing surface.
/// Separate from configuration exploration. Never a score.
/// </summary>
public class MachineOverview
{
    public string ComputerName { get; set; } = string.Empty;
    public string SignedInUser { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Unknown";
    public string WindowsEdition { get; set; } = string.Empty;
    public string WindowsVersion { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public string Architecture { get; set; } = string.Empty;

    public string DeviceManufacturer { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public long TotalPhysicalMemoryMiB { get; set; }

    public string SecureBootState { get; set; } = "Unknown";
    public string TpmPresent { get; set; } = "Unknown";
    public string TpmVersion { get; set; } = "Unknown";
    public string BitLockerProtectionStatus { get; set; } = "Unknown";

    public string DomainMembership { get; set; } = "Unknown";
    public string AzureAdJoined { get; set; } = "Unknown";
    public string PowerShellVersion { get; set; } = "Unknown";
    public string DotNetRuntimeVersion { get; set; } = string.Empty;

    public string FirewallServiceState { get; set; } = "Unknown";
    public string FirewallProfilesSummary { get; set; } = "Unknown";
    public string DefenderServiceState { get; set; } = "Unknown";
    public string ProtectionProductSummary { get; set; } = "not observed";
    public ProtectionProductObservationStatus ProtectionProductStatus { get; set; } =
        ProtectionProductObservationStatus.NotObserved;

    public DateTime LastScanUtc { get; set; }
    public string CatalogVersion { get; set; } = ManagedObjectCatalog.CatalogVersion;
    public string KnowledgeBaseVersion { get; set; } = ManagedObjectCatalog.CatalogVersion;

    public string IdentityCollectionNotes { get; set; } = string.Empty;
    public EffectiveConfidence IdentityConfidence { get; set; } = EffectiveConfidence.Unknown;

    public static MachineOverview FromSnapshot(InventorySnapshot snapshot, int catalogCount)
    {
        if (snapshot is null)
            return new MachineOverview { LastScanUtc = DateTime.UtcNow };

        var id = snapshot.Identity;
        var fwSummary = "Unknown";
        if (snapshot.Networking.FirewallProfiles.Count > 0)
        {
            var parts = snapshot.Networking.FirewallProfiles
                .Select(p => $"{p.ProfileName}:{p.Enabled}")
                .ToList();
            fwSummary = string.Join(", ", parts);
        }

        return new MachineOverview
        {
            ComputerName = id.ComputerName,
            SignedInUser = id.SignedInUser,
            AccountType = id.AccountType,
            WindowsEdition = id.Edition,
            WindowsVersion = id.WindowsVersion,
            BuildNumber = id.BuildNumber,
            Architecture = id.Architecture,
            DeviceManufacturer = id.DeviceManufacturer,
            DeviceModel = id.DeviceModel,
            Processor = id.Processor,
            TotalPhysicalMemoryMiB = id.TotalPhysicalMemoryMiB,
            SecureBootState = id.SecureBootState,
            TpmPresent = id.TpmPresent,
            TpmVersion = id.TpmVersion,
            BitLockerProtectionStatus = id.BitLockerProtectionStatus,
            DomainMembership = id.DomainMembership,
            AzureAdJoined = id.AzureAdJoined,
            PowerShellVersion = id.PowerShellVersion,
            DotNetRuntimeVersion = id.DotNetRuntimeVersion,
            FirewallServiceState = snapshot.Networking.FirewallServiceState,
            FirewallProfilesSummary = fwSummary,
            DefenderServiceState = snapshot.Security.DefenderServiceState,
            ProtectionProductSummary = ProtectionProductPresentation.Summary(snapshot.Security),
            ProtectionProductStatus = snapshot.Security.ProtectionProductStatus,
            LastScanUtc = snapshot.CaptureTimestamp == default ? DateTime.UtcNow : snapshot.CaptureTimestamp,
            CatalogVersion = ManagedObjectCatalog.CatalogVersion,
            KnowledgeBaseVersion = ManagedObjectCatalog.CatalogVersion,
            IdentityCollectionNotes = id.IdentityCollectionNotes,
            IdentityConfidence = id.IdentityConfidence
        };
    }
}
