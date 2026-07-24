namespace WindowsPrivacyPlatform.Models;

public class InventorySnapshot
{
    public string WindowsVersion { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public int BuildNumber { get; set; }
    public List<string> InstalledCapabilities { get; set; } = new();
    public List<string> InstalledPackages { get; set; } = new();
    public List<ServiceInfo> Services { get; set; } = new();
    public List<TaskInfo> ScheduledTasks { get; set; } = new();
    public List<PrivacySettingInfo> PrivacySettings { get; set; } = new();
    public DateTime CaptureTimestamp { get; set; }
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
