namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Read-only capture of local Windows inventory at a point in time.
/// Organized by domain sections for scalable navigation.
/// Compatibility properties preserve existing collector and binder call sites.
/// </summary>
public class InventorySnapshot
{
    // --- Domain sections (preferred access path) ---

    public IdentityInventory Identity { get; set; } = new();
    public ApplicationInventory Applications { get; set; } = new();
    public PrivacyInventory Privacy { get; set; } = new();
    public PolicyInventory Policies { get; set; } = new();
    public SystemInventory System { get; set; } = new();
    public SecurityInventory Security { get; set; } = new();
    public NetworkingInventory Networking { get; set; } = new();
    public DeviceInventory Devices { get; set; } = new();

    public DateTime CaptureTimestamp { get; set; }

    // --- Compatibility surface (collectors / binder / CLI) ---
    // These map onto domain sections so existing code continues without rewrite.

    public string WindowsVersion
    {
        get => Identity.WindowsVersion;
        set => Identity.WindowsVersion = value;
    }

    public string Edition
    {
        get => Identity.Edition;
        set => Identity.Edition = value;
    }

    public int BuildNumber
    {
        get => Identity.BuildNumber;
        set => Identity.BuildNumber = value;
    }

    public List<string> InstalledCapabilities
    {
        get => Applications.InstalledCapabilities;
        set => Applications.InstalledCapabilities = value ?? new();
    }

    public List<string> InstalledPackages
    {
        get => Applications.InstalledPackages;
        set => Applications.InstalledPackages = value ?? new();
    }

    public List<ServiceInfo> Services
    {
        get => System.Services;
        set => System.Services = value ?? new();
    }

    public List<TaskInfo> ScheduledTasks
    {
        get => System.ScheduledTasks;
        set => System.ScheduledTasks = value ?? new();
    }

    public List<PrivacySettingInfo> PrivacySettings
    {
        get => Privacy.Settings;
        set => Privacy.Settings = value ?? new();
    }

    /// <summary>
    /// Read-only probes of known privacy/security policy and preference registry values.
    /// Includes "Not configured" when the value is absent (useful for GPO surface mapping).
    /// </summary>
    public List<PolicySettingInfo> PolicySettings
    {
        get => Policies.Settings;
        set => Policies.Settings = value ?? new();
    }
}

public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string StartMode { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class TaskInfo
{
    public string Path { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public class PrivacySettingInfo
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// One known policy/preference registry value discovered (or explicitly absent).
/// Name aligns with ManagedObjectCatalog ObjectId where possible.
/// </summary>
public class PolicySettingInfo
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Hive { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ValueName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
