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
}

public class SecurityInventory
{
    // Defender live state, Credential Guard, etc. — partially populated in v0.8 via service probes.
    public string DefenderServiceState { get; set; } = "Unknown";
}

public class NetworkingInventory
{
    public List<FirewallProfileInfo> FirewallProfiles { get; set; } = new();
    public List<FirewallRuleInfo> FirewallRules { get; set; } = new();
    public string FirewallServiceState { get; set; } = "Unknown";
    public string FirewallCollectionNotes { get; set; } = string.Empty;
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
            LastScanUtc = snapshot.CaptureTimestamp == default ? DateTime.UtcNow : snapshot.CaptureTimestamp,
            CatalogVersion = ManagedObjectCatalog.CatalogVersion,
            KnowledgeBaseVersion = ManagedObjectCatalog.CatalogVersion,
            IdentityCollectionNotes = id.IdentityCollectionNotes,
            IdentityConfidence = id.IdentityConfidence
        };
    }
}
