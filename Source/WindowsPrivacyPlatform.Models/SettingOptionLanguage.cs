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

        if (id == "privacy.advertisingid.enabled")
            return raw == "0"
                ? new("Don't give apps an ID", "Apps do not get an Advertising ID")
                : new("Give apps an ID", "Apps may get an Advertising ID");
        if (id == "policy.advertising.disabledbygpo")
            return raw == "1"
                ? new("Force advertising ID off", "Windows blocks the Advertising ID for everyone")
                : new("Do not force it off", "This GPO is not forcing the Advertising ID off");

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
            return raw == "1" ? new("Protect this profile", "Firewall protects this profile") : new("Stop profile protection", "Firewall stops protecting this profile");
        if (id.Contains("firewall.profile") && id.EndsWith(".inbound"))
            return raw == "0" ? new("Block", "Block unmatched inbound traffic") : new("Allow", "Allow unmatched inbound traffic");
        if (id.Contains("firewall.profile") && id.EndsWith(".outbound"))
            return raw == "0" ? new("Allow", "Allow unmatched outbound traffic") : new("Block", "Block unmatched outbound traffic");
        if (id.Contains("firewall.profile") && id.EndsWith(".notifications"))
            return raw == "0" ? new("Notify", "Show blocked-app notifications") : new("Silence", "Hide blocked-app notifications");

        if (id == "policy.remote.rdp")
            return raw == "0" ? new("Allow connections", "Accept Remote Desktop connections") : new("Block connections", "Reject Remote Desktop connections");
        if (id == "policy.copilot.turnoff")
            return raw == "1" ? new("Hide legacy integration", "Legacy Copilot entry is hidden") : new("Show legacy integration", "Legacy Copilot entry is available");
        if (id == "policy.copilot.app.browsing")
            return raw == "0" ? new("Block browsing", "Copilot cannot browse the web") : new("Allow browsing", "Copilot can browse the web");
        if (id == "policy.copilot.app.componentupdates")
            return raw == "0" ? new("Block component updates", "Noncritical component updates are blocked") : new("Allow component updates", "Component updates are allowed");
        if (id == "policy.copilot.app.coworkactions")
            return raw == "0" ? new("Block tool actions", "Cowork cannot take actions for the user") : new("Allow tool actions", "Cowork can take actions for the user");
        if (id == "policy.network.llmnr")
            return raw == "0" ? new("Turn off", "Block multicast name resolution") : new("Leave on", "Allow multicast name resolution");

        if (id.Contains("disable") || id.Contains("prevent") || id.Contains("turnoff") ||
            id.Contains("settingsagent") || id.Contains("paint.") ||
            item.ObjectName.StartsWith("Disable", StringComparison.OrdinalIgnoreCase) ||
            item.ObjectName.StartsWith("Prevent", StringComparison.OrdinalIgnoreCase) ||
            item.ObjectName.StartsWith("Turn Off", StringComparison.OrdinalIgnoreCase))
            return raw switch
            {
                "0" => new("Do not block the feature", "The feature remains available"),
                "1" => new("Block the feature", "The feature is unavailable"),
                _ => FromMeaning(value)
            };

        if (raw is "0" or "1")
        {
            var allow = id.Contains("allow") || item.ObjectName.StartsWith("Allow", StringComparison.OrdinalIgnoreCase);
            if (allow)
                return raw == "1" ? new("Allow the feature", "The feature is available") : new("Block the feature", "The feature is unavailable");
            return raw == "1"
                ? new("Windows uses this behavior", Clean(item.ObjectName + " applies"))
                : new("Windows doesn't use it", Clean(item.ObjectName + " does not apply"));
        }

        return FromMeaning(value);
    }

    public static SettingOptionCopy Clear() => new("Not configured", "Windows default applies");

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
