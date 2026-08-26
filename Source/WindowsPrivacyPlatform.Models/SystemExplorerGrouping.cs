namespace WindowsPrivacyPlatform.Models;

public static class SystemExplorerGrouping
{
    public static string TypeLabel(FeatureCategory type) => type switch
    {
        FeatureCategory.WindowsService => "Services",
        FeatureCategory.ScheduledTask => "Scheduled tasks",
        FeatureCategory.AppxPackage => "Installed apps",
        FeatureCategory.ProvisionedPackage => "Provisioned apps",
        FeatureCategory.OptionalFeature => "Optional features",
        FeatureCategory.WindowsCapability => "Capabilities",
        FeatureCategory.FirewallRule => "Firewall rules",
        _ => "Components"
    };

    public static string GroupFor(ManagedObject item) => item.FeatureCategory switch
    {
        FeatureCategory.WindowsService => ServiceGroup(item.ObjectName),
        FeatureCategory.ScheduledTask => TaskGroup(TechnicalLocationFormatter.DirectPath(item.TechnicalLocation)),
        FeatureCategory.AppxPackage or FeatureCategory.ProvisionedPackage => PackageGroup(item.ObjectName),
        FeatureCategory.OptionalFeature or FeatureCategory.WindowsCapability => ComponentGroup(item.ObjectName),
        FeatureCategory.FirewallRule => FirewallGroup(item.CurrentState),
        _ => "Other components"
    };

    private static string ServiceGroup(string name)
    {
        var n = name.ToLowerInvariant();
        if (Has(n, "defend", "security", "firewall", "sense", "crypt", "cert", "bio")) return "Security & identity";
        if (Has(n, "update", "bits", "installer", "trustedinstaller", "dosvc", "uso")) return "Updates & servicing";
        if (Has(n, "network", "dns", "dhcp", "wlan", "wwan", "tcp", "nla", "lanman", "iphlp")) return "Networking";
        if (Has(n, "audio", "camera", "display", "bluetooth", "bth", "device", "print", "spool", "sensor")) return "Devices & media";
        if (Has(n, "diag", "telemetry", "wer", "event", "perf", "wmi")) return "Diagnostics & events";
        if (Has(n, "xbox", "gaming", "search", "shell", "user", "clip", "push", "app")) return "Apps & user experience";
        if (Has(n, "hyper", "vm", "container", "virtual")) return "Virtualization";
        return "Core system";
    }

    private static string TaskGroup(string path)
    {
        var parts = path.Trim('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0].Equals("Microsoft", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("Windows", StringComparison.OrdinalIgnoreCase))
            return parts[2];
        return parts.Length > 1 ? parts[^2] : "Other tasks";
    }

    private static string PackageGroup(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.StartsWith("microsoft.windows") || n.StartsWith("windows.")) return "Windows components";
        if (n.Contains("edge")) return "Microsoft Edge";
        if (n.Contains("xbox") || n.Contains("gaming")) return "Gaming";
        if (n.StartsWith("microsoft.")) return "Microsoft apps";
        return "Other publishers";
    }

    private static string ComponentGroup(string name)
    {
        var n = name.ToLowerInvariant();
        if (Has(n, "language", "speech", "texttospeech", "ocr", "handwriting")) return "Language & accessibility";
        if (Has(n, "media", "print", "xps", "internetexplorer")) return "Media & legacy";
        if (Has(n, "hyper", "virtual", "container", "sandbox", "wsl")) return "Virtualization & development";
        return "Windows components";
    }

    private static string FirewallGroup(string? state)
    {
        var s = state ?? string.Empty;
        if (s.Contains("Inbound", StringComparison.OrdinalIgnoreCase)) return "Inbound rules";
        if (s.Contains("Outbound", StringComparison.OrdinalIgnoreCase)) return "Outbound rules";
        return "Other rules";
    }

    private static bool Has(string value, params string[] terms) => terms.Any(value.Contains);
}
