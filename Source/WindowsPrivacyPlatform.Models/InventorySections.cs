namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Domain-organized inventory sections.
/// InventorySnapshot composes these so future UI can navigate Identity / Privacy / Policies / System
/// without a flat dump of unrelated collections.
/// v0.8: Identity expanded for Machine Overview; Networking holds Firewall profile observations.
/// </summary>

public class IdentityInventory
{
    public string WindowsVersion { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public int BuildNumber { get; set; }

    // --- v0.8 Machine Overview fields (best-effort, read-only) ---

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
    public List<string> InstalledCapabilities { get; set; } = new();
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
    public string FirewallServiceState { get; set; } = "Unknown";
    public string FirewallCollectionNotes { get; set; } = string.Empty;
}

public class DeviceInventory
{
    // Device-centric discoveries — not yet populated.
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
    public string SourcePath { get; set; } = string.Empty;
    public string CollectionNotes { get; set; } = string.Empty;
}

/// <summary>
/// Pure-data machine context for the Home / Machine Overview landing surface.
/// Separate from configuration exploration. Never a score.
/// </summary>
public class MachineOverview
{
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
    public string CatalogVersion { get; set; } = "1.0";
    public string KnowledgeBaseVersion { get; set; } = "1.0";

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
            CatalogVersion = "1.0",
            KnowledgeBaseVersion = "1.0",
            IdentityCollectionNotes = id.IdentityCollectionNotes,
            IdentityConfidence = id.IdentityConfidence
        };
    }
}
