namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Human decision-support content for a setting.
/// Trusted catalog-derived text only — presentation layers format this object.
/// Written as technical documentation, not registry metadata.
/// </summary>
public class SettingExplanation
{
    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DomainPath { get; set; } = string.Empty;

    /// <summary>Neutral impact significance label (not a judgment or score).</summary>
    public string ImpactLabel { get; set; } = string.Empty;

    public string RiskSummary { get; set; } = string.Empty;

    public string WhatIsIt { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string UserImpact { get; set; } = string.Empty;
    public string EnterpriseImpact { get; set; } = string.Empty;
    public string TypicalUseCases { get; set; } = string.Empty;
    public string DecisionGuidance { get; set; } = string.Empty;

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
/// Tone: professional, neutral, technical, educational.
/// </summary>
public static class SettingExplanationFactory
{
    public static SettingExplanation FromDefinition(ManagedObject mo)
    {
        if (mo is null)
            throw new ArgumentNullException(nameof(mo));

        var domainPath = string.IsNullOrWhiteSpace(mo.SubCategory)
            ? HumanDomain(mo.ProductDomain)
            : $"{HumanDomain(mo.ProductDomain)} › {mo.SubCategory}";

        var impactLabel = mo.RiskLevel switch
        {
            RiskLevel.High => "High privacy or security impact",
            RiskLevel.Medium => "Medium configuration impact",
            _ => "Lower configuration impact"
        };

        var riskSummary = mo.RiskLevel switch
        {
            RiskLevel.High =>
                "This setting sits on a high-impact surface: sensor access, identity data, diagnostic volume, or host protection. Understanding it matters more than treating the label as a score.",
            RiskLevel.Medium =>
                "This setting has moderate effect on privacy, management, or day-to-day behavior. Context from related layers is often useful.",
            _ =>
                "This setting has a narrower effect. It is still part of the wider configuration map."
        };

        var controlHint = mo.ControlLevel switch
        {
            ControlLevel.AdministratorControlled =>
                "On managed devices this is commonly set by administrators through Group Policy or MDM and applied for every user on the machine.",
            ControlLevel.UserControlled =>
                "The signed-in user can usually change this unless a higher policy layer overrides it.",
            ControlLevel.Locked =>
                "Windows treats this as a fixed platform behavior; local change may be blocked.",
            _ =>
                "Control is advisory in the catalog model; the live system may still enforce higher layers."
        };

        return new SettingExplanation
        {
            ObjectId = mo.ObjectId,
            DisplayName = mo.ObjectName,
            DomainPath = domainPath,
            ImpactLabel = impactLabel,
            RiskSummary = riskSummary,
            WhatIsIt = BuildWhatIsIt(mo),
            WhyItMatters = BuildWhyItMatters(mo),
            UserImpact = BuildUserImpact(mo),
            EnterpriseImpact = BuildEnterpriseImpact(mo, controlHint),
            TypicalUseCases = BuildUseCases(mo),
            DecisionGuidance = BuildContextNotes(mo, controlHint),
            PrivacyImpactText = BuildPrivacyImpact(mo),
            SecurityImpactText = BuildSecurityImpact(mo),
            SideEffects = BuildSideEffects(mo),
            Exceptions = BuildExceptions(mo),
            CommonMisconceptions = BuildMisconceptions(mo),
            Unknowns = BuildUnknowns(mo),
            RelatedApplications = InferRelatedApplications(mo)
        };
    }

    private static string HumanDomain(ProductDomain domain) => domain switch
    {
        ProductDomain.ConsentStore => "App permissions",
        ProductDomain.AppPrivacy => "App policy overrides",
        ProductDomain.Telemetry => "Telemetry and diagnostics",
        ProductDomain.WindowsUpdate => "Windows Update",
        ProductDomain.Defender => "Microsoft Defender",
        ProductDomain.Search => "Search",
        ProductDomain.Edge => "Microsoft Edge",
        ProductDomain.ActivityHistory => "Activity History",
        ProductDomain.CloudContent => "Cloud content",
        ProductDomain.Advertising => "Advertising",
        ProductDomain.Location => "Location",
        ProductDomain.Biometrics => "Biometrics",
        ProductDomain.Device => "Device recovery",
        ProductDomain.Speech => "Speech",
        ProductDomain.Firewall => "Firewall",
        _ => domain.ToString()
    };

    private static string BuildWhatIsIt(ManagedObject mo)
    {
        var baseText = string.IsNullOrWhiteSpace(mo.Description) ? mo.ObjectName : mo.Description.Trim();

        // Domain framing turns short catalog lines into documentation-style overview.
        return mo.ProductDomain switch
        {
            ProductDomain.Advertising when mo.ObjectId.Contains("advertising", StringComparison.OrdinalIgnoreCase) =>
                "Windows can create a unique Advertising ID for each user account. Applications may request this identifier to personalize advertising, measure engagement, and distinguish one user from another without relying only on traditional web cookies. " +
                baseText,

            ProductDomain.ConsentStore =>
                "This is a per-user capability permission stored in the Windows ConsentStore. It governs whether applications running under the current account may use a specific device or data capability. " +
                baseText,

            ProductDomain.AppPrivacy =>
                "This is a machine-level AppPrivacy policy. It can force-allow, force-deny, or leave app access under user control for a given capability, independent of the per-user ConsentStore value. " +
                baseText,

            ProductDomain.Telemetry =>
                "This setting participates in Windows diagnostic data configuration. Diagnostic data may include device, reliability, and usage information that Windows components send to Microsoft under the configured level. " +
                baseText,

            ProductDomain.WindowsUpdate =>
                "This setting influences how Windows Update discovers, downloads, or presents updates on the device, including automatic update behavior and access to update UI. " +
                baseText,

            ProductDomain.Defender =>
                "This setting belongs to Microsoft Defender Antivirus configuration. It can affect real-time protection, cloud-delivered protection, sample submission, or related antivirus policy. " +
                baseText,

            ProductDomain.ActivityHistory =>
                "Activity History records recent app and document activity so features such as Timeline can help users resume work. Some related policies also control whether that history is uploaded for roaming. " +
                baseText,

            ProductDomain.Edge =>
                "This setting configures Microsoft Edge browser policy. It can change tracking prevention, suggestions, metrics reporting, or credential features inside Edge. " +
                baseText,

            ProductDomain.Location =>
                "This setting participates in Windows location services. Location data can reveal physical movement and habitual places and is consumed by apps, system features, and recovery scenarios. " +
                baseText,

            ProductDomain.Speech =>
                "This setting controls whether speech input may be processed by online speech services. Online recognition can improve accuracy by using cloud models; local recognition keeps audio on the device when available. " +
                baseText,

            ProductDomain.Search =>
                "This setting influences Windows Search behavior, including cloud-backed results, assistant features, or whether search may use location. " +
                baseText,

            ProductDomain.CloudContent =>
                "This setting relates to consumer cloud content surfaces such as suggestions, Spotlight imagery, or soft-landing experiences after updates. " +
                baseText,

            ProductDomain.Biometrics =>
                "This setting relates to the Windows biometric framework used by features such as Windows Hello face or fingerprint unlock. " +
                baseText,

            ProductDomain.Device =>
                "This setting relates to device recovery or findability features that may depend on location and account services. " +
                baseText,

            _ => baseText
        };
    }

    private static string BuildWhyItMatters(ManagedObject mo)
    {
        if (!string.IsNullOrWhiteSpace(mo.Rationale))
            return mo.Rationale.Trim();

        return mo.ProductDomain switch
        {
            ProductDomain.ConsentStore or ProductDomain.AppPrivacy =>
                "Capability permissions decide which applications can reach sensors and personal data. Understanding both the user preference and any machine policy override is necessary to know what is actually effective.",
            ProductDomain.Telemetry =>
                "Diagnostic level and related personalization controls affect how much operational data leaves the device and whether that data is reused for tips or recommendations.",
            ProductDomain.Advertising =>
                "The Advertising ID is a specific personalization surface. It is narrower than general Windows diagnostics, but it is one of the clearer cross-app advertising correlation points Windows exposes to applications.",
            ProductDomain.Defender =>
                "Antivirus posture is part of host security. Changes here alter detection and protection behavior rather than privacy labeling alone.",
            ProductDomain.WindowsUpdate =>
                "Update configuration determines whether the device stays current with security fixes and how much control users or administrators retain over timing and sources.",
            _ =>
                "This setting is part of the broader Windows privacy and security configuration map. Seeing its effective layer helps explain real system behavior."
        };
    }

    private static string BuildUserImpact(ManagedObject mo)
    {
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission)
            return "For the signed-in user, this decides whether apps may use the capability when they request it. " +
                   "Allow permits requesting apps (subject to higher policy). Prompt asks each time where supported. Deny blocks the capability for apps under this account unless a machine policy forces otherwise.";

        return mo.ProductDomain switch
        {
            ProductDomain.WindowsUpdate =>
                "Affects when updates arrive, whether the Windows Update interface remains available, and whether servicing follows local policy or public update endpoints.",
            ProductDomain.Defender =>
                "Affects real-time protection, cloud intelligence, and sample handling on this PC.",
            ProductDomain.Telemetry =>
                "Affects how much diagnostic and usage data this device may send, and whether diagnostic data is reused for tailored tips.",
            ProductDomain.Advertising =>
                "When disabled, applications should not receive the Advertising ID for advertising personalization. Other Windows and application telemetry channels remain separate.",
            ProductDomain.ActivityHistory =>
                "Affects whether recent activity is kept locally for Timeline-style resume, and whether that history may be uploaded for roaming.",
            ProductDomain.Edge =>
                "Affects Edge tracking prevention, suggestions, metrics, and related browser privacy defaults.",
            ProductDomain.Location =>
                "Affects whether the location platform is available to apps and system features on this machine.",
            ProductDomain.Speech =>
                "Affects whether speech audio may be sent to online recognition services.",
            _ =>
                "May change available features, data exposure, or management behavior for this account or PC."
        };
    }

    private static string BuildEnterpriseImpact(ManagedObject mo, string controlHint)
    {
        if (mo.ControlLevel == ControlLevel.AdministratorControlled)
            return controlHint + " Enterprise deployments often set this once and rely on precedence so users cannot locally reopen the surface.";
        return controlHint;
    }

    private static string BuildUseCases(ManagedObject mo) => mo.ProductDomain switch
    {
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy =>
            "Shared PCs, kiosks, privacy reviews of app capabilities, and environments that restrict camera, microphone, location, or broad filesystem access.",
        ProductDomain.Telemetry =>
            "Clarifying the effective diagnostic level, meeting data-handling requirements, or separating diagnostic volume from advertising and activity features.",
        ProductDomain.WindowsUpdate =>
            "Managed patch cadence, WSUS or Intune servicing, bandwidth control, or locking update UI on administered devices.",
        ProductDomain.Defender =>
            "Confirming antivirus policy, understanding sample submission, or reviewing temporary support changes to protection.",
        ProductDomain.Edge =>
            "Browser privacy defaults for tracking prevention, suggestions, metrics, and password storage.",
        ProductDomain.Advertising =>
            "Understanding cross-app advertising correlation independently of Windows diagnostic data level.",
        ProductDomain.ActivityHistory =>
            "Environments that do not use Timeline or do not want activity history uploaded.",
        ProductDomain.Location =>
            "Hosts that must not report physical location, including some high-privacy or offline scenarios.",
        _ =>
            "Understanding current configuration before interpreting related settings or policy layers."
    };

    private static string BuildContextNotes(ManagedObject mo, string controlHint)
    {
        // Informational only — never framed as an instruction to change the system.
        var parts = new List<string> { controlHint };
        if (!string.IsNullOrWhiteSpace(mo.Rationale))
            parts.Add("Related settings and layer precedence often matter more than any single raw value.");
        parts.Add("This platform only explains configuration; it does not change Windows.");
        return string.Join(" ", parts);
    }

    private static string BuildPrivacyImpact(ManagedObject mo)
    {
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission)
            return "Governs whether applications may access a sensitive capability such as camera, microphone, location, files, or contacts. " +
                   "Wide Allow values enlarge the set of apps that can collect sensor or personal data under this account.";

        return mo.ProductDomain switch
        {
            ProductDomain.Telemetry =>
                "Influences the volume and reuse of diagnostic data, which can include usage patterns, device identifiers, and error reports sent to Microsoft.",
            ProductDomain.Advertising =>
                "The Advertising ID supports cross-application advertising correlation. Disabling it narrows that specific vector; it does not turn off Windows diagnostics, Microsoft account activity, or in-app tracking.",
            ProductDomain.ActivityHistory =>
                "Activity history can reconstruct recent application and document use. Cloud upload extends that history beyond the local device.",
            ProductDomain.Speech =>
                "Online speech recognition may send audio to cloud services. Local recognition avoids that transfer when the platform supports it.",
            ProductDomain.Edge =>
                "Affects how Edge handles trackers, query suggestions, metrics, and personalization data.",
            ProductDomain.Location =>
                "Location reveals physical movement and habitual places. A machine-wide disable is stronger than denying individual apps in ConsentStore.",
            _ => string.Empty
        };
    }

    private static string BuildSecurityImpact(ManagedObject mo)
    {
        if (mo.ProductDomain == ProductDomain.Defender)
            return "Affects host malware protection. Turning off real-time monitoring or the antivirus engine increases exposure unless another product provides equivalent protection.";

        if (mo.ProductDomain == ProductDomain.WindowsUpdate)
            return "Restricting updates can lengthen the window during which known vulnerabilities remain unpatched. That trade-off only makes sense when another servicing channel is intentional and working.";

        if (mo.ProductDomain == ProductDomain.Firewall)
            return "Firewall configuration controls network exposure of local services. Errors can open attack surface or interrupt required connectivity.";

        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission &&
            (mo.ObjectId.Contains("webcam", StringComparison.OrdinalIgnoreCase) ||
             mo.ObjectId.Contains("microphone", StringComparison.OrdinalIgnoreCase) ||
             mo.ObjectId.Contains("camera", StringComparison.OrdinalIgnoreCase)))
            return "Camera and microphone access can enable surveillance or eavesdropping if granted more broadly than intended. Effective policy may come from ConsentStore, AppPrivacy, or both.";

        return string.Empty;
    }

    private static string BuildSideEffects(ManagedObject mo)
    {
        if (!string.IsNullOrWhiteSpace(mo.KnownSideEffects))
            return mo.KnownSideEffects;

        if (mo.ProductDomain is ProductDomain.ConsentStore or ProductDomain.AppPrivacy)
            return "Denying a capability can stop legitimate applications that need it—for example, video calls without camera or microphone access, or maps without location.";

        if (mo.ProductDomain == ProductDomain.WindowsUpdate)
            return "Aggressive update restrictions can leave the device unpatched if WSUS, Intune, offline media, or another channel is not configured.";

        if (mo.ProductDomain == ProductDomain.Telemetry &&
            mo.ObjectId.Contains("allowtelemetry", StringComparison.OrdinalIgnoreCase))
            return "Very low diagnostic levels can limit some enterprise analytics and support scenarios that rely on richer telemetry.";

        if (mo.ProductDomain == ProductDomain.Advertising)
            return "Some applications may fall back to other identifiers or first-party accounts when the Advertising ID is unavailable.";

        return string.Empty;
    }

    private static string BuildExceptions(ManagedObject mo)
    {
        if (mo.ProductDomain is ProductDomain.ConsentStore)
            return "Machine AppPrivacy policy (LetApps*) can force-allow or force-deny a capability and override the per-user ConsentStore value.";

        if (mo.ProductDomain == ProductDomain.Advertising)
            return "A Group Policy that disables Advertising ID overrides the per-user toggle and can prevent the user from turning it back on.";

        if (mo.ControlLevel == ControlLevel.AdministratorControlled)
            return "On domain-joined or MDM-managed devices, a higher policy layer may force a value regardless of local UI or user preference.";

        return string.Empty;
    }

    private static string BuildMisconceptions(ManagedObject mo)
    {
        return mo.ProductDomain switch
        {
            ProductDomain.Advertising =>
                "Turning off the Advertising ID does not disable Windows diagnostic data, Microsoft account activity, website tracking, or tracking performed inside individual applications.",
            ProductDomain.Telemetry =>
                "A low diagnostic data level does not stop all communication with Microsoft. Windows Update, Store, licensing, and some feature endpoints remain separate channels.",
            ProductDomain.ConsentStore or ProductDomain.AppPrivacy =>
                "A ConsentStore Deny is not always the whole story. Machine AppPrivacy policy and other platform components can still change effective access.",
            ProductDomain.Defender =>
                "Disabling Defender is not a privacy improvement by itself; it primarily reduces malware protection. Privacy and security controls should be read as separate concerns.",
            ProductDomain.ActivityHistory =>
                "Disabling upload does not always clear local activity history; local publish and feed settings are separate controls.",
            _ => string.Empty
        };
    }

    private static string BuildUnknowns(ManagedObject mo)
    {
        var parts = new List<string>
        {
            "MDM (Intune/CSP) precedence is modeled in layer ranking but not fully collected in this prototype, so effective state on MDM-managed devices may be incomplete."
        };

        if (mo.ProductDomain == ProductDomain.Firewall)
            parts.Add("Firewall coverage in the catalog is partial; many profiles and rule sets are not yet modeled.");

        if (mo.Observation?.Resolution?.Confidence == EffectiveConfidence.Unknown ||
            mo.Observation?.Effective?.Confidence == EffectiveConfidence.Unknown)
            parts.Add("On this scan, effective configuration confidence for this setting is Unknown.");

        if (string.IsNullOrWhiteSpace(mo.CurrentState) ||
            mo.CurrentState.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ||
            mo.CurrentState.Contains("Not configured", StringComparison.OrdinalIgnoreCase))
            parts.Add("No configured value was observed for this setting in the current scan, or the collector reported it as not configured.");

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
            list.Add("WSUS and update management tools");
        }
        else if (mo.ProductDomain == ProductDomain.Defender)
        {
            list.Add("Microsoft Defender Antivirus");
            list.Add("Third-party antivirus (if present)");
        }

        return list;
    }
}
