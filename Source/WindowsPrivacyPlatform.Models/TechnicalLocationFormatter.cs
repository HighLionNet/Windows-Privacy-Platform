namespace WindowsPrivacyPlatform.Models;

public static class TechnicalLocationFormatter
{
    public static string DirectPath(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return "Not available";

        var value = location.Trim();
        foreach (var prefix in new[] { "Registry: ", "Service: ", "Scheduled task: ", "App package: ", "Optional feature: " })
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return value[prefix.Length..];
        return value;
    }

    public static string FromDefinition(ManagedObject mo)
    {
        var discovery = mo.DiscoveryMethod?.Trim() ?? string.Empty;
        if (discovery.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase))
            return "Service: " + discovery["ServiceController:".Length..];
        if (discovery.StartsWith("ScheduledTask:", StringComparison.OrdinalIgnoreCase))
            return "Scheduled task: " + discovery["ScheduledTask:".Length..];
        if (discovery.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) ||
            discovery.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) ||
            discovery.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase))
            return "Registry: " + discovery;

        if (mo.WritableTarget is { Kind: not WritableTargetKind.Registry } target)
        {
            var prefix = target.Kind switch
            {
                WritableTargetKind.Service => "Service: ",
                WritableTargetKind.ScheduledTask => "Scheduled task: ",
                WritableTargetKind.AppxPackage => "App package: ",
                WritableTargetKind.OptionalFeature => "Optional feature: ",
                _ => string.Empty
            };
            return prefix + target.Identifier;
        }

        return string.IsNullOrWhiteSpace(discovery) ? mo.InterfaceName.ToString() : discovery;
    }
}
