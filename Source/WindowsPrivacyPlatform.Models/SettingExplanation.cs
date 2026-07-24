namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Human decision-support content for a setting.
/// Trusted catalog-derived text only — no registry paths as the primary message.
/// Presentation layers (CLI/TUI/GUI) format this object; they do not invent meaning.
/// Treat as documentation, not metadata.
/// </summary>
public class SettingExplanation
{
    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DomainPath { get; set; } = string.Empty;
    public string RiskSummary { get; set; } = string.Empty;

    // Core documentation
    public string WhatIsIt { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string UserImpact { get; set; } = string.Empty;
    public string EnterpriseImpact { get; set; } = string.Empty;
    public string TypicalUseCases { get; set; } = string.Empty;
    public string DecisionGuidance { get; set; } = string.Empty;

    // Expanded professional fields (v0.7)
    public string PrivacyImpactText { get; set; } = string.Empty;
    public string SecurityImpactText { get; set; } = string.Empty;
    public string SideEffects { get; set; } = string.Empty;
    public string Exceptions { get; set; } = string.Empty;
    public string CommonMisconceptions { get; set; } = string.Empty;
    public string Unknowns { get; set; } = string.Empty;

    public List<string> RelatedApplications { get; set; } = new();
}

/// <summary>
/// Builds SettingExplanation from catalog definition fields.
/// Pure composition — no system access, no observation logic.
/// Writing standard: calm, neutral, factual, educational.
/// </summary>
public static class SettingExplanationFactory
{
    public static SettingExplanation FromDefinition(ManagedObject mo)
    {
        if (mo is null)
            throw new ArgumentNullException(nameof(mo));

        var domainPath = string.IsNullOrWhiteSpace(mo.SubCategory)
            ? mo.ProductDomain.ToString()
            : $"{mo.ProductDomain} > {mo.SubCategory}";

        var riskSummary = mo.RiskLevel switch
        {
            RiskLevel.High => "High-impact setting — review carefully before changing system policy.",
            RiskLevel.Medium => "Medium impact — understand trade-offs before adjusting.",
            _ => "Lower impact — still useful to understand in context."
        };

        var controlHint = mo.ControlLevel switch
        {
            ControlLevel.AdministratorControlled =>
                "Typically controlled by administrators or Group Policy on managed devices.",
            ControlLevel.UserControlled =>
                "Often adjustable by the signed-in user unless a higher policy layer overrides it.",
            ControlLevel.Locked =>
                "Intended to remain fixed; changes may be blocked by the platform.",
            _ => "Advisory control — guidance only."
        };

        var whatIsIt = string.IsNullOrWhiteSpace(mo.Description)
            ? mo.ObjectName
            : mo.Description;

        var why = string.IsNullOrWhiteSpace(mo.Rationale)
            ? riskSummary
            : mo.Rationale;

        return new SettingExplanation
        {
            ObjectId = mo.ObjectId,
            DisplayName = mo.ObjectName,
            DomainPath = domainPath,
            RiskSummary = riskSummary,
            WhatIsIt = whatIsIt,
            WhyItMatters = why,
            UserImpact = BuildUserImpact(mo),
            EnterpriseImpact = BuildEnterpriseImpact(mo, controlHint),
            TypicalUseCases = BuildUseCases(mo),
            DecisionGuidance = BuildGuidance(mo, controlHint),
            PrivacyImpactText = BuildPrivacyImpact(mo),
            SecurityImpactText = BuildSecurityImpact(mo),
            SideEffects = BuildSideEffects(mo),
            Exceptions = BuildExceptions(mo),
            CommonMisconceptions = BuildMisconceptions(mo),
            Unknowns = BuildUnknowns(mo),
            RelatedApplications = InferRelatedApplications(mo)
        };
    }

    private static string BuildUserImpact(ManagedObject mo)
    {
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission)
            return "Affects whether applications on this device can use this capability for the current user account. " +
                   "A Deny or Prompt value limits exposure; Allow grants capability to apps that request it (subject to higher policy layers).";
        if (mo.ProductDomain == ProductDomain.WindowsUpdate)
            return "Affects how and when this PC receives Windows quality and feature updates, and whether the user can interact with Windows Update UI.";
        if (mo.ProductDomain == ProductDomain.Defender)
            return "Affects built-in malware protection behavior, sample submission, and cloud-delivered protection on this PC.";
        if (mo.ProductDomain == ProductDomain.Telemetry)
            return "Affects how much diagnostic and usage data this device may send to Microsoft, and whether that data is reused for personalization.";
        if (mo.ProductDomain == ProductDomain.Advertising)
            return "Affects whether Windows supplies an Advertising ID that applications can use for cross-app advertising correlation.";
        if (mo.ProductDomain == ProductDomain.ActivityHistory)
            return "Affects local retention and optional cloud upload of recent activity used by Timeline and cross-device resume scenarios.";
        if (mo.ProductDomain == ProductDomain.Edge)
            return "Affects Microsoft Edge privacy defaults for tracking prevention, suggestions, metrics, and credential storage.";
        return "May change available features, privacy exposure, or management behavior for this account or PC.";
    }

    private static string BuildEnterpriseImpact(ManagedObject mo, string controlHint)
    {
        if (mo.ControlLevel == ControlLevel.AdministratorControlled)
            return $"{controlHint} In managed environments this is often set once via Group Policy or MDM and enforced for all users on the device.";
        return controlHint;
    }

    private static string BuildUseCases(ManagedObject mo) => mo.ProductDomain switch
    {
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy =>
            "Personal privacy hardening, shared PCs, kiosks, compliance reviews of app capabilities, and environments where camera/microphone/location must be restricted.",
        ProductDomain.Telemetry =>
            "Reducing diagnostic data sharing, meeting enterprise data-handling requirements, or clarifying which diagnostic level is currently effective.",
        ProductDomain.WindowsUpdate =>
            "Controlling patch cadence, bandwidth use, offline/WSUS servicing models, or locking down end-user update UI on managed devices.",
        ProductDomain.Defender =>
            "Ensuring antivirus posture, temporarily isolating protection during support scenarios, or balancing cloud sample submission against data sensitivity.",
        ProductDomain.Edge =>
            "Browser privacy defaults for tracking prevention, search suggestions, metrics, and password manager behavior.",
        ProductDomain.Advertising =>
            "Reducing cross-app advertising correlation while leaving other Windows and application telemetry surfaces unchanged.",
        ProductDomain.ActivityHistory =>
            "Disabling Timeline or preventing activity upload when cross-device resume is not required.",
        ProductDomain.Location =>
            "Machine-wide location control for high-privacy hosts, air-gapped systems, or environments that must not report physical location.",
        _ => "Understanding current configuration before deciding whether a change is appropriate."
    };

    private static string BuildGuidance(ManagedObject mo, string controlHint)
    {
        var parts = new List<string> { controlHint };
        if (mo.RiskLevel == RiskLevel.High)
            parts.Add("Prefer the least privilege that still supports required applications and workflows.");
        if (!string.IsNullOrWhiteSpace(mo.Rationale))
            parts.Add("Read the rationale and related settings before treating any single value as definitive.");
        parts.Add("This guidance is informational only; the platform does not change the system.");
        return string.Join(" ", parts);
    }

    private static string BuildPrivacyImpact(ManagedObject mo)
    {
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission)
            return "Directly governs whether applications may access a sensitive capability (camera, microphone, location, files, contacts, etc.). " +
                   "Broad Allow values increase the set of apps that can collect sensor or personal data.";
        if (mo.ProductDomain == ProductDomain.Telemetry)
            return "Controls volume and reuse of diagnostic data that may include usage patterns, device identifiers, and error reports sent to Microsoft.";
        if (mo.ProductDomain == ProductDomain.Advertising)
            return "The Advertising ID enables cross-application advertising correlation. Disabling it reduces that specific tracking vector; it does not disable all forms of telemetry or in-app tracking.";
        if (mo.ProductDomain == ProductDomain.ActivityHistory)
            return "Activity history can reconstruct recent application and document usage. Cloud upload increases exposure beyond the local device.";
        if (mo.ProductDomain == ProductDomain.Speech)
            return "Online speech recognition sends audio to cloud services. Local-only recognition avoids that transfer when available.";
        if (mo.ProductDomain == ProductDomain.Edge)
            return "Affects how Edge shares query fragments, metrics, and personalization data, and how aggressively it blocks trackers.";
        if (mo.ProductDomain == ProductDomain.Location)
            return "Location data reveals physical movement and habitual places. Machine-wide disable is stronger than per-app ConsentStore deny.";
        return string.Empty;
    }

    private static string BuildSecurityImpact(ManagedObject mo)
    {
        if (mo.ProductDomain == ProductDomain.Defender)
            return "Directly affects host malware protection. Disabling real-time monitoring or the antivirus engine significantly increases exposure unless an equivalent third-party product is active.";
        if (mo.ProductDomain == ProductDomain.WindowsUpdate)
            return "Delaying or blocking updates increases the window of vulnerability to publicly known exploits. Appropriate only when an alternative patch process exists.";
        if (mo.ProductDomain == ProductDomain.Firewall)
            return "Firewall rules control network exposure of local services. Misconfiguration can open attack surface or break required connectivity.";
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission &&
            (mo.ObjectId.Contains("webcam", StringComparison.OrdinalIgnoreCase) ||
             mo.ObjectId.Contains("microphone", StringComparison.OrdinalIgnoreCase) ||
             mo.ObjectId.Contains("camera", StringComparison.OrdinalIgnoreCase)))
            return "Unauthorized sensor access is both a privacy and a safety risk (surveillance, eavesdropping). Prefer Deny or Prompt on high-security hosts.";
        return string.Empty;
    }

    private static string BuildSideEffects(ManagedObject mo)
    {
        if (!string.IsNullOrWhiteSpace(mo.KnownSideEffects))
            return mo.KnownSideEffects;

        if (mo.ProductDomain is ProductDomain.ConsentStore or ProductDomain.AppPrivacy)
            return "Denying a capability can break applications that legitimately require it (for example, video conferencing without camera/microphone access, or maps without location).";
        if (mo.ProductDomain == ProductDomain.WindowsUpdate)
            return "Restrictive update policies can leave the device unpatched if no alternative servicing channel (WSUS, Intune, offline media) is configured.";
        if (mo.ProductDomain == ProductDomain.Telemetry && mo.ObjectId.Contains("allowtelemetry", StringComparison.OrdinalIgnoreCase))
            return "Very low diagnostic levels can limit some enterprise analytics, optional diagnostic features, and Microsoft support scenarios that rely on richer telemetry.";
        return string.Empty;
    }

    private static string BuildExceptions(ManagedObject mo)
    {
        if (mo.ControlLevel == ControlLevel.AdministratorControlled)
            return "On domain-joined or MDM-managed devices, a higher policy layer may force a value regardless of the user preference or local UI setting.";
        if (mo.ProductDomain is ProductDomain.ConsentStore)
            return "Machine AppPrivacy Group Policy (LetApps*) can force-allow or force-deny the capability, overriding the per-user ConsentStore value.";
        if (mo.ProductDomain == ProductDomain.Advertising)
            return "A Group Policy that disables Advertising ID overrides the per-user AdvertisingInfo toggle and prevents re-enablement by the user.";
        return string.Empty;
    }

    private static string BuildMisconceptions(ManagedObject mo)
    {
        if (mo.ProductDomain == ProductDomain.Advertising)
            return "Disabling the Advertising ID does not disable Windows diagnostic data, Microsoft account activity, or tracking performed inside individual applications or websites.";
        if (mo.ProductDomain == ProductDomain.Telemetry)
            return "Setting a low diagnostic data level does not by itself disable all network communication with Microsoft (Windows Update, Store, licensing, and some feature endpoints remain separate).";
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission)
            return "A per-app ConsentStore Deny does not always equal machine-wide disable; Group Policy AppPrivacy settings and other platform components can still influence effective access.";
        if (mo.ProductDomain == ProductDomain.Defender)
            return "Disabling Defender is not a reliable privacy improvement; it primarily reduces malware protection. Privacy and security controls should be considered separately.";
        return string.Empty;
    }

    private static string BuildUnknowns(ManagedObject mo)
    {
        var parts = new List<string>();

        parts.Add("MDM (Intune/CSP) precedence is ranked in the model but not fully collected in this prototype; effective state may be incomplete on MDM-managed devices.");

        if (mo.ProductDomain == ProductDomain.Firewall)
            parts.Add("Firewall catalog coverage is partial; many rule sets and profiles are not yet modeled.");

        if (mo.Observation?.Resolution?.Confidence == EffectiveConfidence.Unknown ||
            mo.Observation?.Effective?.Confidence == EffectiveConfidence.Unknown)
            parts.Add("Effective configuration confidence is Unknown for this setting on the current scan.");

        return string.Join(" ", parts);
    }

    private static List<string> InferRelatedApplications(ManagedObject mo)
    {
        var name = (mo.ObjectName ?? string.Empty).ToLowerInvariant();
        var id = (mo.ObjectId ?? string.Empty).ToLowerInvariant();
        var list = new List<string>();

        if (name.Contains("camera") || id.Contains("webcam") || id.Contains("camera"))
        {
            list.Add("Camera app");
            list.Add("Microsoft Teams");
            list.Add("Zoom and other video apps");
            list.Add("Browsers with WebRTC");
        }
        else if (name.Contains("microphone") || id.Contains("microphone"))
        {
            list.Add("Voice recorders");
            list.Add("Microsoft Teams");
            list.Add("Assistants and conferencing apps");
            list.Add("Browsers with WebRTC");
        }
        else if (name.Contains("location") || id.Contains("location"))
        {
            list.Add("Maps and navigation apps");
            list.Add("Weather apps");
            list.Add("Find My Device scenarios");
        }
        else if (mo.ProductDomain == ProductDomain.Edge)
        {
            list.Add("Microsoft Edge");
        }
        else if (mo.ProductDomain == ProductDomain.WindowsUpdate)
        {
            list.Add("Windows Update");
            list.Add("WSUS / update management tools");
        }
        else if (mo.ProductDomain == ProductDomain.Defender)
        {
            list.Add("Microsoft Defender Antivirus");
            list.Add("Third-party antivirus (if present)");
        }

        return list;
    }
}
