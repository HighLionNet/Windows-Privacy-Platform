namespace WindowsPrivacyPlatform.Models;

public sealed record SettingOptionCopy(string Action, string Effect);

/// <summary>Concise action/effect copy. Raw registry values stay in tooltips and audit records.</summary>
public static class SettingOptionLanguage
{
    public static SettingOptionCopy For(ManagedObject item, ValueMeaning value)
    {
        var raw = value.RawValue ?? string.Empty;
        var id = item.ObjectId.ToLowerInvariant();

        if (id.StartsWith("privacy.consentstore."))
            return raw.ToLowerInvariant() switch
            {
                "allow" => new("Allow", "Apps may use this permission"),
                "deny" => new("Deny", "Blocks apps from this permission"),
                "prompt" => new("Ask every time", "Ask when apps request access"),
                _ => FromMeaning(value)
            };

        if (id.StartsWith("policy.appprivacy."))
            return raw switch
            {
                "0" => new("User controlled", "User privacy choice applies"),
                "1" => new("Force allow", "All apps receive access"),
                "2" => new("Force deny", "All apps are blocked"),
                _ => FromMeaning(value)
            };

        if (id.Contains("asr."))
            return raw switch
            {
                "0" => new("Off", "No rule enforcement"),
                "1" => new("Block", "Block matching activity"),
                "2" => new("Audit", "Log without blocking"),
                "6" => new("Warn", "Allow temporary user override"),
                _ => FromMeaning(value)
            };

        if (id.Contains("firewall.profile") && id.EndsWith(".enabled"))
            return raw == "1" ? new("Enable", "Firewall protects this profile") : new("Disable", "Firewall stops protecting this profile");
        if (id.Contains("firewall.profile") && id.EndsWith(".inbound"))
            return raw == "0" ? new("Block", "Block unmatched inbound traffic") : new("Allow", "Allow unmatched inbound traffic");
        if (id.Contains("firewall.profile") && id.EndsWith(".outbound"))
            return raw == "0" ? new("Allow", "Allow unmatched outbound traffic") : new("Block", "Block unmatched outbound traffic");
        if (id.Contains("firewall.profile") && id.EndsWith(".notifications"))
            return raw == "0" ? new("Notify", "Show blocked-app notifications") : new("Silence", "Hide blocked-app notifications");

        if (id == "policy.remote.rdp")
            return raw == "0" ? new("Allow", "Accept Remote Desktop connections") : new("Block", "Reject Remote Desktop connections");
        if (id == "policy.copilot.turnoff")
            return raw == "1" ? new("Hide legacy integration", "Legacy Copilot entry is hidden") : new("Show legacy integration", "Legacy Copilot entry is available");
        if (id == "policy.network.llmnr")
            return raw == "0" ? new("Disable", "Block multicast name resolution") : new("Enable", "Allow multicast name resolution");

        if (id.Contains("disable") || item.ObjectName.StartsWith("Disable", StringComparison.OrdinalIgnoreCase) ||
            item.ObjectName.StartsWith("Turn Off", StringComparison.OrdinalIgnoreCase))
            return raw switch
            {
                "0" => new("Keep enabled", "Feature remains available"),
                "1" => new("Disable", "Feature is blocked"),
                _ => FromMeaning(value)
            };

        if (raw is "0" or "1")
        {
            var allow = id.Contains("allow") || item.ObjectName.StartsWith("Allow", StringComparison.OrdinalIgnoreCase);
            if (allow)
                return raw == "1" ? new("Allow", "Feature is available") : new("Block", "Feature is blocked");
            return raw == "1" ? new("Enable", "Policy is enabled") : new("Disable", "Policy is disabled");
        }

        return FromMeaning(value);
    }

    public static SettingOptionCopy Clear() => new("Use Windows default", "Removes the local policy value");

    private static SettingOptionCopy FromMeaning(ValueMeaning value)
    {
        var action = string.IsNullOrWhiteSpace(value.DisplayLabel) ? value.Canonical : value.DisplayLabel;
        var effect = Clean(value.Description);
        if (string.IsNullOrWhiteSpace(effect) || effect.StartsWith("Policy value", StringComparison.OrdinalIgnoreCase) ||
            effect.Equals(action, StringComparison.OrdinalIgnoreCase))
            effect = $"Sets {action.ToLowerInvariant()} behavior";
        return new(action.Trim(), effect);
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = value.Trim().TrimEnd('.');
        return text.Length <= 58 ? text : text[..55].TrimEnd() + "…";
    }
}
