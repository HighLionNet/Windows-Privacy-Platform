namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Human decision-support content for a setting.
/// Trusted catalog-derived text only — no registry paths as the primary message.
/// Presentation layers (CLI/TUI/GUI) format this object; they do not invent meaning.
/// </summary>
public class SettingExplanation
{
    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DomainPath { get; set; } = string.Empty;
    public string RiskSummary { get; set; } = string.Empty;

    public string WhatIsIt { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
    public string UserImpact { get; set; } = string.Empty;
    public string EnterpriseImpact { get; set; } = string.Empty;
    public string TypicalUseCases { get; set; } = string.Empty;
    public string DecisionGuidance { get; set; } = string.Empty;

    public List<string> RelatedApplications { get; set; } = new();
}

/// <summary>
/// Builds SettingExplanation from catalog definition fields.
/// Pure composition — no system access, no observation logic.
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

        return new SettingExplanation
        {
            ObjectId = mo.ObjectId,
            DisplayName = mo.ObjectName,
            DomainPath = domainPath,
            RiskSummary = riskSummary,
            WhatIsIt = string.IsNullOrWhiteSpace(mo.Description)
                ? mo.ObjectName
                : mo.Description,
            WhyItMatters = string.IsNullOrWhiteSpace(mo.Rationale)
                ? riskSummary
                : mo.Rationale,
            UserImpact = BuildUserImpact(mo),
            EnterpriseImpact = BuildEnterpriseImpact(mo, controlHint),
            TypicalUseCases = BuildUseCases(mo),
            DecisionGuidance = BuildGuidance(mo, controlHint),
            RelatedApplications = InferRelatedApplications(mo)
        };
    }

    private static string BuildUserImpact(ManagedObject mo)
    {
        if (mo.FeatureCategory == FeatureCategory.PrivacyPermission)
            return "Affects whether apps on this device can use this capability for the current user.";
        if (mo.ProductDomain == ProductDomain.WindowsUpdate)
            return "Affects how and when this PC receives Windows updates.";
        if (mo.ProductDomain == ProductDomain.Defender)
            return "Affects built-in malware protection behavior on this PC.";
        if (mo.ProductDomain == ProductDomain.Telemetry)
            return "Affects how much diagnostic data this device may send to Microsoft.";
        return "May change available features, privacy exposure, or management behavior for this account or PC.";
    }

    private static string BuildEnterpriseImpact(ManagedObject mo, string controlHint)
    {
        if (mo.ControlLevel == ControlLevel.AdministratorControlled)
            return $"{controlHint} In managed environments this is often set once and enforced.";
        return controlHint;
    }

    private static string BuildUseCases(ManagedObject mo) => mo.ProductDomain switch
    {
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy =>
            "Personal privacy hardening, shared PCs, kiosks, or compliance reviews of app capabilities.",
        ProductDomain.Telemetry =>
            "Reducing diagnostic data sharing, or meeting enterprise data-handling requirements.",
        ProductDomain.WindowsUpdate =>
            "Controlling patch cadence, bandwidth, or offline/WSUS servicing models.",
        ProductDomain.Defender =>
            "Ensuring antivirus posture or temporarily isolating protection during support scenarios.",
        ProductDomain.Edge =>
            "Browser privacy defaults for tracking, suggestions, and credential storage.",
        _ => "Understanding current configuration before deciding whether a change is appropriate."
    };

    private static string BuildGuidance(ManagedObject mo, string controlHint)
    {
        var parts = new List<string> { controlHint };
        if (mo.RiskLevel == RiskLevel.High)
            parts.Add("Prefer the least privilege that still supports required apps and workflows.");
        if (!string.IsNullOrWhiteSpace(mo.Rationale))
            parts.Add("Read the rationale and related settings before treating any single value as definitive.");
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
        }
        else if (name.Contains("microphone") || id.Contains("microphone"))
        {
            list.Add("Voice recorders");
            list.Add("Microsoft Teams");
            list.Add("Assistants and conferencing apps");
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

        return list;
    }
}
