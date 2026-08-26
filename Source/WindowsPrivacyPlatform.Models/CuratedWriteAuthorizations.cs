namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Explicit registry authorizations that cannot be inferred from runtime discovery.
/// Native services, tasks, packages, features and capabilities are read-only.
/// </summary>
public static class CuratedWriteAuthorizations
{
    public static IReadOnlyDictionary<string, WritableTarget> Targets { get; } = BuildTargets();

    public static bool TryCreateTarget(string objectId, out WritableTarget? target)
    {
        if (Targets.TryGetValue(objectId, out var found))
        {
            target = Clone(found);
            return true;
        }

        target = null;
        return false;
    }

    private static IReadOnlyDictionary<string, WritableTarget> BuildTargets()
    {
        var targets = new Dictionary<string, WritableTarget>(StringComparer.OrdinalIgnoreCase);

        AddFirewallTargets(targets, "domain", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile");
        AddFirewallTargets(targets, "private", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile");
        AddFirewallTargets(targets, "public", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile");

        return targets;
    }

    private static void AddFirewallTargets(IDictionary<string, WritableTarget> targets, string profile, string subKey)
    {
        targets[$"firewall.profile.{profile}.enabled"] = Registry(subKey, "EnableFirewall", ["0", "1"]);
        targets[$"firewall.profile.{profile}.inbound"] = Registry(subKey, "DefaultInboundAction", ["0", "1"]);
        targets[$"firewall.profile.{profile}.outbound"] = Registry(subKey, "DefaultOutboundAction", ["0", "1"]);
        targets[$"firewall.profile.{profile}.notifications"] = Registry(subKey, "DisableNotifications", ["0", "1"]);
    }

    private static WritableTarget Registry(string subKey, string valueName, List<string> values) => new()
    {
        Kind = WritableTargetKind.Registry,
        Hive = "HKLM",
        View = RegistryViewKind.Registry64,
        SubKey = subKey,
        ValueName = valueName,
        ValueKind = RegistryValueKindExpected.DWord,
        SupportedRawValues = values,
        SupportsDeletion = true,
        RequiresElevation = true,
        Notes = "Explicit firewall-profile authorization; individual rules remain excluded."
    };

    private static WritableTarget Clone(WritableTarget source) => new()
    {
        Kind = source.Kind,
        Hive = source.Hive,
        View = source.View,
        SubKey = source.SubKey,
        ValueName = source.ValueName,
        ValueKind = source.ValueKind,
        SupportedRawValues = source.SupportedRawValues.ToList(),
        SupportsDeletion = source.SupportsDeletion,
        RequiresElevation = source.RequiresElevation,
        Notes = source.Notes,
        Identifier = source.Identifier,
        RecoveryHint = source.RecoveryHint
    };
}
