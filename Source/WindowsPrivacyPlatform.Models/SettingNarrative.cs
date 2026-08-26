using System.Text.RegularExpressions;

namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Plain-language decision support kept separate from technical identifiers and locations.
/// </summary>
public sealed class SettingNarrative
{
    public string Summary { get; set; } = string.Empty;
    public string Mechanics { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string ConsumerImpact { get; set; } = string.Empty;
    public string WhenIgnored { get; set; } = string.Empty;
    public string DecisionGuidance { get; set; } = string.Empty;
    public string SideEffects { get; set; } = string.Empty;
    public string CommonMisconception { get; set; } = string.Empty;

    public IEnumerable<string> ProseFields()
    {
        yield return Summary;
        yield return Mechanics;
        yield return WhyItMatters;
        yield return ConsumerImpact;
        yield return WhenIgnored;
        yield return DecisionGuidance;
        yield return SideEffects;
        yield return CommonMisconception;
    }

    public bool IsComplete(ManagedObject owner, out string error)
    {
        if (string.IsNullOrWhiteSpace(Summary) ||
            string.IsNullOrWhiteSpace(Mechanics) ||
            string.IsNullOrWhiteSpace(WhyItMatters) ||
            string.IsNullOrWhiteSpace(ConsumerImpact) ||
            string.IsNullOrWhiteSpace(DecisionGuidance) ||
            string.IsNullOrWhiteSpace(SideEffects))
        {
            error = "Narrative requires summary, mechanics, impact, guidance, and side-effect text.";
            return false;
        }

        foreach (var prose in ProseFields().Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (ContainsTechnicalLeak(prose, owner.ObjectId, out var token))
            {
                error = $"Narrative contains technical identifier or path token '{token}'.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static bool ContainsTechnicalLeak(string prose, string? objectId, out string token)
    {
        var forbidden = new[]
        {
            "HKLM", "HKCU", "HKEY_LOCAL_MACHINE", "HKEY_CURRENT_USER",
            "ServiceController:", "ScheduledTask:", "Windows Privacy Platform observes"
        };

        foreach (var candidate in forbidden)
        {
            if (prose.Contains(candidate, StringComparison.OrdinalIgnoreCase))
            {
                token = candidate;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(objectId) &&
            prose.Contains(objectId, StringComparison.OrdinalIgnoreCase))
        {
            token = objectId;
            return true;
        }

        // A backslash-delimited fragment is a location, not explanatory prose.
        if (Regex.IsMatch(prose, @"(?:^|\s)[A-Za-z0-9_. -]+\\[A-Za-z0-9_.{}$ -]+"))
        {
            token = "backslash-delimited path";
            return true;
        }

        token = string.Empty;
        return false;
    }
}

/// <summary>Applies complete, non-technical narrative copy to every catalog entry.</summary>
public static class CatalogNarrativeAuthoring
{
    public static void Apply(ManagedObject setting)
    {
        var summary = Sentence(setting.Description, $"This setting controls {FriendlySubject(setting)}.");
        var why = Sentence(setting.Rationale, WhyFallback(setting));
        var impact = Sentence(setting.ConsumerImpact, ImpactFor(setting));
        var sideEffects = Sentence(setting.KnownSideEffects, SideEffectsFor(setting));

        setting.Narrative = new SettingNarrative
        {
            Summary = summary,
            Mechanics = MechanicsFor(setting),
            WhyItMatters = why,
            ConsumerImpact = impact,
            WhenIgnored = Sentence(setting.WhenIgnored, IgnoredFor(setting)),
            DecisionGuidance = GuidanceFor(setting),
            SideEffects = sideEffects,
            CommonMisconception = Sentence(setting.CommonMisconception, MisconceptionFor(setting))
        };
    }

    private static string FriendlySubject(ManagedObject mo)
    {
        if (!mo.IsDynamicInventory)
            return string.IsNullOrWhiteSpace(mo.ObjectName) ? "this Windows behavior" : mo.ObjectName.ToLowerInvariant();

        // Live names are untrusted technical data. They belong in ObjectName/TechnicalLocation, never prose.
        return mo.FeatureCategory switch
        {
            FeatureCategory.WindowsService => "this discovered service",
            FeatureCategory.ScheduledTask => "this discovered scheduled task",
            FeatureCategory.AppxPackage or FeatureCategory.ProvisionedPackage => "this discovered application package",
            FeatureCategory.OptionalFeature => "this discovered optional feature",
            FeatureCategory.WindowsCapability => "this discovered Windows capability",
            FeatureCategory.FirewallRule => "this discovered firewall rule",
            _ => "this discovered inventory item"
        };
    }

    private static string Sentence(string? value, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback.Trim() : value.Trim();
        if (text.Length == 0)
            return string.Empty;
        return char.ToUpperInvariant(text[0]) + text[1..].TrimEnd() + (text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?') ? string.Empty : ".");
    }

    private static string MechanicsFor(ManagedObject mo) => mo.FeatureCategory switch
    {
        FeatureCategory.PrivacyPermission =>
            $"Windows uses this permission when an app asks to use {FriendlySubject(mo)} for the signed-in account.",
        FeatureCategory.WindowsService =>
            $"The Service Control Manager reports whether {FriendlySubject(mo)} is installed, how it starts, and whether it is running.",
        FeatureCategory.ScheduledTask =>
            $"Task Scheduler reports whether {FriendlySubject(mo)} is enabled and its current scheduling state.",
        FeatureCategory.AppxPackage =>
            $"Windows reports whether {FriendlySubject(mo)} is installed for the current user.",
        FeatureCategory.OptionalFeature =>
            $"Windows servicing reports whether {FriendlySubject(mo)} is enabled.",
        FeatureCategory.FirewallProfile =>
            $"Windows Defender Firewall applies this value to the {mo.SubCategory?.ToLowerInvariant() ?? "selected"} network profile.",
        FeatureCategory.FirewallRule =>
            $"Windows Defender Firewall evaluates {FriendlySubject(mo)} when matching network traffic reaches the device.",
        FeatureCategory.DefenderSetting =>
            $"Microsoft Defender reads this policy when it evaluates {FriendlySubject(mo)}.",
        _ when mo.FeatureCategory == FeatureCategory.EdgePolicy =>
            $"Microsoft Edge applies this managed preference when it handles {FriendlySubject(mo)}.",
        _ =>
            $"Windows evaluates this value when it applies {FriendlySubject(mo)} for the device or signed-in user."
    };

    private static string WhyFallback(ManagedObject mo) => mo.ProductDomain switch
    {
        ProductDomain.Defender => $"{FriendlySubject(mo)} can materially change malware prevention or investigation coverage.",
        ProductDomain.Firewall => $"{FriendlySubject(mo)} affects the device's exposure to inbound or outbound network traffic.",
        ProductDomain.Telemetry => $"{FriendlySubject(mo)} affects diagnostic collection, reporting, or related personalization.",
        ProductDomain.WindowsUpdate => $"{FriendlySubject(mo)} can change patch delivery, supportability, or update timing.",
        ProductDomain.AppPrivacy or ProductDomain.ConsentStore => $"{FriendlySubject(mo)} determines whether apps can reach a device capability or personal data.",
        ProductDomain.Location => $"{FriendlySubject(mo)} affects access to physical-location information.",
        ProductDomain.Edge => $"{FriendlySubject(mo)} changes browser privacy, identity, or data-handling behavior.",
        _ => $"{FriendlySubject(mo)} is relevant when reviewing the device's effective privacy and security posture."
    };

    private static string ImpactFor(ManagedObject mo) => mo.FeatureCategory switch
    {
        FeatureCategory.WindowsService => $"Changing {FriendlySubject(mo)} can stop or restore the Windows feature that depends on that service.",
        FeatureCategory.ScheduledTask => $"Disabling {FriendlySubject(mo)} prevents its scheduled background work until the task is enabled again.",
        FeatureCategory.AppxPackage => $"Removing {FriendlySubject(mo)} removes it for the current user while leaving the rest of Windows in place.",
        FeatureCategory.OptionalFeature => $"Changing {FriendlySubject(mo)} adds or removes the component and may require a restart before every app sees the result.",
        FeatureCategory.FirewallProfile => $"The new value takes effect for connections classified under the {mo.SubCategory?.ToLowerInvariant() ?? "selected"} profile.",
        _ => $"Changing {FriendlySubject(mo)} can alter the related feature for the current user, the device, or both, depending on the policy scope."
    };

    private static string GuidanceFor(ManagedObject mo)
    {
        if (mo.ExclusionReason == ExclusionReason.HighRiskIrreversible)
            return $"Review the current state and use the linked Windows management tool if {FriendlySubject(mo)} must change; the operation is intentionally outside this product's safety boundary.";
        if (!mo.IsWritable)
            return $"Use {FriendlySubject(mo)} as diagnostic evidence. This entry is intentionally view-only in this release.";
        if (mo.RiskLevel == RiskLevel.High)
            return $"Confirm the operational dependency and a recovery path before changing {FriendlySubject(mo)}. Apply one value at a time and verify the result.";
        return $"Choose the value that matches the device's management intent, then confirm the independent read-back before treating {FriendlySubject(mo)} as changed.";
    }

    private static string SideEffectsFor(ManagedObject mo) => mo.ProductDomain switch
    {
        ProductDomain.Defender => $"A restrictive change to {FriendlySubject(mo)} can reduce protection or increase false positives; test business applications after a change.",
        ProductDomain.WindowsUpdate => $"A restrictive change to {FriendlySubject(mo)} can delay security fixes when no alternate servicing channel is working.",
        ProductDomain.Firewall => $"An incorrect value for {FriendlySubject(mo)} can interrupt required connectivity or expose a local service.",
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy => $"Blocking {FriendlySubject(mo)} can break applications that legitimately need the capability.",
        _ => $"A change to {FriendlySubject(mo)} may affect dependent applications or be replaced by a higher management policy."
    };

    private static string IgnoredFor(ManagedObject mo) => mo.ControlLevel switch
    {
        ControlLevel.UserControlled => $"A device policy can override the signed-in user's choice for {FriendlySubject(mo)}.",
        ControlLevel.AdministratorControlled => $"Domain, mobile-device-management, or security-baseline policy can replace a local value for {FriendlySubject(mo)}.",
        _ => $"Windows may ignore {FriendlySubject(mo)} when the component is absent or a stronger policy controls the same behavior."
    };

    private static string MisconceptionFor(ManagedObject mo) => mo.ProductDomain switch
    {
        ProductDomain.Telemetry => $"Changing {FriendlySubject(mo)} does not stop unrelated Windows Update, Store, licensing, or account traffic.",
        ProductDomain.Defender => $"Reducing {FriendlySubject(mo)} is not a privacy improvement when it also removes malware protection.",
        ProductDomain.Firewall => $"A firewall profile setting does not replace application-layer authentication or endpoint protection.",
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy => $"A single {FriendlySubject(mo)} value may not be effective when a stronger machine policy is present.",
        _ => $"The observed value for {FriendlySubject(mo)} is one part of the effective configuration and should not be read as a complete device assessment."
    };
}
