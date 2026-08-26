namespace WindowsPrivacyPlatform.Models;

/// <summary>Small, product-facing groups for the editable privacy and security surface.</summary>
public static class HubTaxonomy
{
    public static void Apply(ManagedObject item)
    {
        if (item.Bucket != CatalogBucket.Settings)
            return;

        item.SubCategory = item.ProductDomain switch
        {
            ProductDomain.ConsentStore => PermissionGroup(item.ObjectName),
            ProductDomain.AppPrivacy => PermissionGroup(item.ObjectName),
            ProductDomain.Telemetry => "Diagnostic data & feedback",
            ProductDomain.Defender => DefenderGroup(item),
            ProductDomain.Firewall => FirewallGroup(item.ObjectId),
            ProductDomain.Edge => EdgeGroup(item.ObjectName),
            ProductDomain.Search => "Search privacy",
            ProductDomain.ActivityHistory => "Activity sharing",
            ProductDomain.CloudContent => "Suggestions & cloud content",
            ProductDomain.Advertising => "Advertising & personalization",
            ProductDomain.Location => "Location access",
            ProductDomain.Biometrics => "Windows Hello biometrics",
            ProductDomain.Device => "Device location",
            ProductDomain.Speech => "Online speech",
            ProductDomain.Network => "Discovery & transport",
            ProductDomain.RemoteAccess => "Remote connections",
            ProductDomain.LocalSecurity => "Authentication & sharing",
            ProductDomain.Copilot => "Copilot",
            ProductDomain.Recall => "Recall & Click to Do",
            ProductDomain.Widgets => "Widgets",
            ProductDomain.OneDrive => "OneDrive sync",
            ProductDomain.Other when item.ObjectId.Contains("clipboard", StringComparison.OrdinalIgnoreCase) => "Clipboard",
            _ => item.SubCategory ?? "Settings"
        };
    }

    private static string PermissionGroup(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("camera") || n.Contains("microphone") || n.Contains("location") || n.Contains("sensor"))
            return "Sensors";
        if (n.Contains("file") || n.Contains("folder") || n.Contains("document") || n.Contains("picture") || n.Contains("video") || n.Contains("music"))
            return "Files & media";
        if (n.Contains("account") || n.Contains("contact") || n.Contains("calendar") || n.Contains("email") || n.Contains("message"))
            return "Personal data";
        if (n.Contains("background") || n.Contains("notification") || n.Contains("activity") || n.Contains("diagnostic"))
            return "App activity";
        return "Device access";
    }

    private static string DefenderGroup(ManagedObject item)
    {
        var id = item.ObjectId.ToLowerInvariant();
        if (id.Contains("asr.") || id.Contains("networkprotection")) return "Attack surface reduction";
        if (id.Contains("controlledfolder")) return "Ransomware protection";
        if (id.Contains("cloud") || id.Contains("sample") || id.Contains("spynet")) return "Cloud protection";
        if (id.Contains("smartscreen")) return "SmartScreen";
        return "Antivirus";
    }

    private static string FirewallGroup(string objectId)
    {
        if (objectId.Contains(".domain.", StringComparison.OrdinalIgnoreCase)) return "Domain profile";
        if (objectId.Contains(".private.", StringComparison.OrdinalIgnoreCase)) return "Private profile";
        if (objectId.Contains(".public.", StringComparison.OrdinalIgnoreCase)) return "Public profile";
        return "Profiles";
    }

    private static string EdgeGroup(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("password") || n.Contains("autofill")) return "Passwords & autofill";
        if (n.Contains("search") || n.Contains("site info")) return "Search & suggestions";
        return "Tracking & reporting";
    }
}
