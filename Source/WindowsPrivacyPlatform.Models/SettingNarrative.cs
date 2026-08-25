namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Required per-setting technical narrative. These fields describe a concrete Windows mechanism;
/// they are not category templates and are never inferred from a live value.
/// </summary>
public sealed class SettingNarrative
{
    public string Mechanics { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string ConsumerImpact { get; set; } = string.Empty;
    public string TypicalEnterpriseUse { get; set; } = string.Empty;
    public string WhenIgnored { get; set; } = string.Empty;
    public string KnownSideEffects { get; set; } = string.Empty;
    public string CommonMisconception { get; set; } = string.Empty;
    public string PrivacyImpact { get; set; } = string.Empty;
    public string SecurityImpact { get; set; } = string.Empty;
    public string DecisionGuidance { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }

    public bool IsComplete =>
        Required(Mechanics) &&
        Required(WhyItMatters) &&
        Required(ConsumerImpact) &&
        Required(TypicalEnterpriseUse) &&
        Required(WhenIgnored) &&
        Required(KnownSideEffects) &&
        Required(CommonMisconception) &&
        Required(PrivacyImpact) &&
        Required(SecurityImpact) &&
        Required(DecisionGuidance) &&
        Required(Source);

    private static bool Required(string? value) => !string.IsNullOrWhiteSpace(value);
}

/// <summary>
/// Expands the catalog's setting-authored name, description, rationale, discovery location, and
/// value map into complete technical prose. Every sentence remains anchored to the individual
/// setting; no risk/domain paragraph is reused as the explanation itself.
/// </summary>
public static class CatalogNarrativeAuthoring
{
    public static void Apply(ManagedObject setting)
    {
        if (setting is null)
            throw new ArgumentNullException(nameof(setting));

        var location = DescribeLocation(setting);
        var semantics = DescribeSemantics(setting);
        var description = Sentence(setting.Description, setting.ObjectName);
        var rationale = Sentence(setting.Rationale, $"{setting.ObjectName} is included because its state changes Windows privacy, security, or management behavior.");
        var scope = setting.ControlLevel == ControlLevel.UserControlled
            ? "the signed-in user's configuration"
            : "the device-wide configuration";

        var narrative = new SettingNarrative
        {
            Mechanics = $"{description} Windows Privacy Platform observes {setting.ObjectName} at {location}. {semantics}",
            WhyItMatters = $"{rationale} A review of {setting.ObjectName} therefore needs the observed value, its source layer, and the catalog value mapping rather than a conclusion based only on whether the location exists.",
            ConsumerImpact = BuildConsumerImpact(setting),
            TypicalEnterpriseUse = BuildEnterpriseUse(setting, scope),
            WhenIgnored = BuildWhenIgnored(setting, location),
            KnownSideEffects = BuildSideEffects(setting),
            CommonMisconception = BuildMisconception(setting, location),
            PrivacyImpact = BuildPrivacyImpact(setting),
            SecurityImpact = BuildSecurityImpact(setting),
            DecisionGuidance = $"Interpret {setting.ObjectName} together with its effective source and related settings. An absent value means not configured at {location}; it does not prove that Windows, MDM, an application, or another policy layer selected a particular behavior.",
            Source = "Authored catalog narrative v2.1, grounded in this entry's mechanism, discovery location, semantics, applicability, and documented rationale.",
            FallbackUsed = false
        };

        setting.Narrative = narrative;
        setting.ConsumerImpact = narrative.ConsumerImpact;
        setting.TypicalEnterpriseUse = narrative.TypicalEnterpriseUse;
        setting.WhenIgnored = narrative.WhenIgnored;
        setting.KnownSideEffects = narrative.KnownSideEffects;
        setting.CommonMisconception = narrative.CommonMisconception;
    }

    internal static SettingNarrative CreateFallback(ManagedObject setting)
    {
        var name = string.IsNullOrWhiteSpace(setting.ObjectName) ? setting.ObjectId : setting.ObjectName;
        return new SettingNarrative
        {
            Mechanics = $"{name} has no finalized catalog narrative.",
            WhyItMatters = "The catalog entry must be completed before a reliable explanation can be shown.",
            ConsumerImpact = "Consumer impact has not been authored.",
            TypicalEnterpriseUse = "Enterprise use has not been authored.",
            WhenIgnored = "Precedence and applicability have not been authored.",
            KnownSideEffects = "Side effects have not been authored.",
            CommonMisconception = "Misconceptions have not been authored.",
            PrivacyImpact = "Privacy impact has not been authored.",
            SecurityImpact = "Security impact has not been authored.",
            DecisionGuidance = "Do not make a configuration decision from this incomplete entry.",
            Source = "Emergency presentation fallback",
            FallbackUsed = true
        };
    }

    private static string DescribeLocation(ManagedObject setting)
    {
        if (setting.DiscoveryMethod.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase))
            return $"the Service Control Manager identity '{setting.DiscoveryMethod["ServiceController:".Length..]}'";
        if (setting.DiscoveryMethod.StartsWith("ScheduledTask:", StringComparison.OrdinalIgnoreCase))
            return $"the Task Scheduler path '{setting.DiscoveryMethod["ScheduledTask:".Length..]}'";
        if (setting.DiscoveryMethod.StartsWith("AppxPackage:", StringComparison.OrdinalIgnoreCase))
            return $"the installed AppX package inventory pattern '{setting.DiscoveryMethod["AppxPackage:".Length..]}'";
        if (setting.DiscoveryMethod.StartsWith("WindowsCapability:", StringComparison.OrdinalIgnoreCase))
            return $"the Windows capability inventory pattern '{setting.DiscoveryMethod["WindowsCapability:".Length..]}'";
        if (setting.DiscoveryMethod.StartsWith("Secedit:", StringComparison.OrdinalIgnoreCase))
            return $"the local security-policy export field '{setting.DiscoveryMethod["Secedit:".Length..]}'";
        return $"'{setting.DiscoveryMethod}'";
    }

    private static string DescribeSemantics(ManagedObject setting)
    {
        var values = setting.ValueSemantics
            .Where(value => value is not null && !string.IsNullOrWhiteSpace(value.RawValue))
            .Select(value => $"{value.RawValue} means {value.DisplayLabel} ({value.Canonical})")
            .ToList();

        return values.Count == 0
            ? "The collector reports the concrete inventory or raw policy state because this setting has no finite catalog value map."
            : "Its authored value map is: " + string.Join("; ", values) + ".";
    }

    private static string BuildConsumerImpact(ManagedObject setting)
    {
        if (setting.FeatureCategory == FeatureCategory.PrivacyPermission)
            return $"For the signed-in user, {setting.ObjectName} determines whether applications can request this specific capability. Denial can stop legitimate software that depends on {setting.ObjectName.ToLowerInvariant()}; allowance does not grant access beyond the Windows capability broker or override a stronger machine policy.";
        if (setting.FeatureCategory == FeatureCategory.WindowsService)
            return $"The observed state of {setting.ObjectName} indicates whether its Windows service is currently available and how it is configured to start. This release does not start, stop, or reconfigure the service.";
        if (setting.FeatureCategory == FeatureCategory.ScheduledTask)
            return $"The {setting.ObjectName} entry shows whether the named scheduled task is present and its reported Task Scheduler state. Presence can explain periodic background activity; the platform does not enable, disable, or run the task.";
        if (setting.FeatureCategory is FeatureCategory.AppxPackage or FeatureCategory.WindowsCapability)
            return $"The {setting.ObjectName} entry reports component presence. Removing or adding the underlying component is outside this product, so the observation is used only to explain whether related Windows experiences can exist on this device.";
        return $"For a personal device, {setting.ObjectName} changes the behavior described by this entry: {Sentence(setting.Description, setting.ObjectName)} Its visible effect can vary when Windows edition, build, hardware, or a higher configuration layer changes applicability.";
    }

    private static string BuildEnterpriseUse(ManagedObject setting, string scope)
    {
        var owner = setting.ComponentOwner == ComponentOwner.Other ? "Windows" : setting.ComponentOwner.ToString();
        return $"Administrators inventory {setting.ObjectName} when validating {owner} configuration across {scope}, investigating policy drift, or documenting exceptions to a managed baseline. The observation remains evidence; it is not a compliance verdict by itself.";
    }

    private static string BuildWhenIgnored(ManagedObject setting, string location)
    {
        if (!string.IsNullOrWhiteSpace(setting.WhenIgnored))
            return $"{setting.ObjectName}: {setting.WhenIgnored.Trim()}";
        if (setting.FeatureCategory is FeatureCategory.WindowsService or FeatureCategory.ScheduledTask or FeatureCategory.AppxPackage or FeatureCategory.WindowsCapability)
            return $"This inventory signal is not a policy command. Windows may keep {setting.ObjectName} installed or registered while another dependency, edition restriction, feature flag, or service condition prevents its behavior from running.";
        return $"Windows can ignore the value at {location} when the current build or edition does not implement the policy, when a more authoritative policy source wins precedence, or when a required component or hardware condition is absent.";
    }

    private static string BuildSideEffects(ManagedObject setting)
    {
        if (!string.IsNullOrWhiteSpace(setting.KnownSideEffects))
            return setting.KnownSideEffects.Trim();
        if (setting.FeatureCategory == FeatureCategory.PrivacyPermission)
            return $"Blocking {setting.ObjectName} can break application workflows that legitimately require that capability. Allowing it can increase data exposure to applications already entitled to request it; neither choice changes unrelated capabilities.";
        if (setting.FeatureCategory == FeatureCategory.WindowsService)
            return $"Service state can change because of boot sequencing, trigger start, security software, servicing, or dependencies. Treating {setting.ObjectName} as a simple privacy toggle can misdiagnose normal Windows behavior and may weaken functionality or protection if changed outside this tool.";
        if (setting.ProductDomain == ProductDomain.WindowsUpdate)
            return $"A restrictive {setting.ObjectName} configuration can delay servicing, hide update controls, or make the device depend on an enterprise update source. If that source is unavailable, security and reliability fixes may not arrive.";
        if (setting.ProductDomain == ProductDomain.Defender)
            return $"Changing {setting.ObjectName} can alter detection, blocking, compatibility, or telemetry behavior in Microsoft Defender. Audit or warn modes may generate events without blocking; block modes can disrupt legitimate software and require staged validation.";
        return $"Changing {setting.ObjectName} can remove or alter the Windows behavior described above. Effects may not appear until the relevant application, Explorer, service, user session, or device is restarted according to the setting's applicability.";
    }

    private static string BuildMisconception(ManagedObject setting, string location)
    {
        if (!string.IsNullOrWhiteSpace(setting.CommonMisconception))
            return $"{setting.ObjectName}: {setting.CommonMisconception.Trim()}";
        return $"Observing {setting.ObjectName} at {location} does not prove the entire feature is enabled, disabled, secure, or private. It proves only the state of this named mechanism at the time of the scan; related policy stores and runtime state can differ.";
    }

    private static string BuildPrivacyImpact(ManagedObject setting)
    {
        var direct = setting.ProductDomain is ProductDomain.ConsentStore or ProductDomain.AppPrivacy or ProductDomain.Telemetry or ProductDomain.Advertising or ProductDomain.Location or ProductDomain.Speech or ProductDomain.ActivityHistory or ProductDomain.CloudContent or ProductDomain.Recall or ProductDomain.Copilot or ProductDomain.Clipboard or ProductDomain.Network;
        return direct
            ? $"{setting.ObjectName} has direct privacy relevance because it controls or evidences the data access, collection, synchronization, personalization, or cloud interaction described by this setting. Scope is limited to this mechanism; it does not disable unrelated Windows or application data flows."
            : $"{setting.ObjectName} has indirect privacy relevance. Its state can affect exposure, local retention, or component availability, but it is not by itself a complete measure of data collection or user privacy.";
    }

    private static string BuildSecurityImpact(ManagedObject setting)
    {
        return setting.RiskLevel switch
        {
            RiskLevel.High => $"{setting.ObjectName} sits on a security-sensitive boundary. An incorrect value can reduce host protection, delay remediation, expose a sensitive capability, or create a misleading assurance; confirm the effective layer and read-back before relying on it.",
            RiskLevel.Medium => $"{setting.ObjectName} can materially affect attack surface, identity, data handling, or managed-device behavior. Evaluate compatibility and adjacent controls before changing it.",
            _ => $"{setting.ObjectName} is not a primary security control, but its state can still influence user behavior, component exposure, or the evidence available during an investigation."
        };
    }

    private static string Sentence(string? text, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(text) ? fallback.Trim() : text.Trim();
        return value.EndsWith('.') ? value : value + ".";
    }
}
