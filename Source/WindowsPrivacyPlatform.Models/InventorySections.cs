namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Domain-organized inventory sections.
/// InventorySnapshot composes these so future UI can navigate Identity / Privacy / Policies / System
/// without a flat dump of unrelated collections.
/// </summary>

public class IdentityInventory
{
    public string WindowsVersion { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
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

// Future expansion placeholders (empty until collectors exist).
public class SecurityInventory
{
    // Defender live state, Credential Guard, etc. — not yet populated.
}

public class NetworkingInventory
{
    // Firewall rules, network settings — not yet populated.
}

public class DeviceInventory
{
    // Device-centric discoveries — not yet populated.
}
