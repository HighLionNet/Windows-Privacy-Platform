namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Human decision-support content for a setting. Presentation code formats this object but does
/// not invent Windows behavior or registry meaning.
/// </summary>
public sealed class SettingExplanation
{
    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DomainPath { get; set; } = string.Empty;
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
/// Projects the required per-setting catalog narrative into the UI model. The fallback exists only
/// to keep the UI fail-soft for non-catalog objects; catalog validation rejects fallback content.
/// </summary>
public static class SettingExplanationFactory
{
    public static SettingExplanation FromDefinition(ManagedObject setting)
    {
        if (setting is null)
            throw new ArgumentNullException(nameof(setting));

        var narrative = setting.Narrative is { IsComplete: true }
            ? setting.Narrative
            : CatalogNarrativeAuthoring.CreateFallback(setting);

        var domain = NavigationBuilder.HumanizeDomain(setting.ProductDomain);
        var domainPath = string.IsNullOrWhiteSpace(setting.SubCategory)
            ? domain
            : $"{domain} › {setting.SubCategory}";

        return new SettingExplanation
        {
            ObjectId = setting.ObjectId,
            DisplayName = setting.ObjectName,
            DomainPath = domainPath,
            ImpactLabel = setting.RiskLevel switch
            {
                RiskLevel.High => "High privacy or security impact",
                RiskLevel.Medium => "Medium configuration impact",
                _ => "Lower configuration impact"
            },
            RiskSummary = narrative.SecurityImpact,
            WhatIsIt = narrative.Mechanics,
            WhyItMatters = narrative.WhyItMatters,
            UserImpact = narrative.ConsumerImpact,
            EnterpriseImpact = narrative.TypicalEnterpriseUse,
            TypicalUseCases = narrative.TypicalEnterpriseUse,
            DecisionGuidance = narrative.DecisionGuidance,
            PrivacyImpactText = narrative.PrivacyImpact,
            SecurityImpactText = narrative.SecurityImpact,
            SideEffects = narrative.KnownSideEffects,
            Exceptions = narrative.WhenIgnored,
            CommonMisconceptions = narrative.CommonMisconception,
            Unknowns = BuildUnknowns(setting, narrative),
            RelatedApplications = InferRelatedApplications(setting)
        };
    }

    private static string BuildUnknowns(ManagedObject setting, SettingNarrative narrative)
    {
        var parts = new List<string>();
        if (narrative.FallbackUsed)
            parts.Add("This object is using emergency fallback content and must not be treated as a finalized catalog explanation.");
        if (setting.ValueSemantics.Count == 0)
            parts.Add("This setting has no finite semantic map; the raw or inventory value is preserved without inventing meaning.");
        if (string.IsNullOrWhiteSpace(setting.CurrentState) ||
            setting.CurrentState.Contains("Not observed", StringComparison.OrdinalIgnoreCase))
            parts.Add("The current scan did not produce an observation for this object.");
        if (setting.CurrentState?.Contains("Not configured", StringComparison.OrdinalIgnoreCase) == true)
            parts.Add("The probed location was present or reachable, but the named value was not configured.");
        parts.Add("MDM/CSP coverage remains partial; a higher management layer can exist even when no local registry policy was observed.");
        return string.Join(" ", parts);
    }

    private static List<string> InferRelatedApplications(ManagedObject setting)
    {
        var token = (setting.ObjectName + " " + setting.ObjectId).ToLowerInvariant();
        if (token.Contains("camera") || token.Contains("webcam"))
            return ["Windows Camera", "Microsoft Teams", "WebRTC browsers and conferencing applications"];
        if (token.Contains("microphone") || token.Contains("speech"))
            return ["Voice Recorder", "Microsoft Teams", "Speech and conferencing applications"];
        if (token.Contains("location"))
            return ["Windows Maps", "Weather", "Find My Device", "Location-aware applications"];
        if (setting.ProductDomain == ProductDomain.Edge)
            return ["Microsoft Edge"];
        if (setting.ProductDomain == ProductDomain.WindowsUpdate)
            return ["Windows Update", "WSUS", "Microsoft Intune and update-management tools"];
        if (setting.ProductDomain == ProductDomain.Defender)
            return ["Microsoft Defender Antivirus", "Microsoft Defender for Endpoint", "Third-party security software"];
        return [];
    }
}
