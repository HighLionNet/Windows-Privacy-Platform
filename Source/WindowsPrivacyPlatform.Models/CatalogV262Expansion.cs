namespace WindowsPrivacyPlatform.Models;

/// <summary>Microsoft-documented Edge policy coverage added for the focused privacy cage.</summary>
public static class CatalogV262Expansion
{
    public static IReadOnlyList<ManagedObject> CreateCoverageBatch() =>
    [
        EdgeBinary("policy.edge.hidefirstrun", "Hide Edge First-run Experience",
            "Controls whether Edge shows its first-run experience and splash screen.",
            "Hiding first run avoids promotional setup prompts but does not disable browser sign-in or sync policy.",
            "First run", "HideFirstRunExperience",
            "https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/hidefirstrunexperience"),
        EdgeBinary("policy.edge.hubssidebar", "Edge Sidebar",
            "Controls whether the Microsoft Edge sidebar is shown.",
            "The sidebar can surface connected services and app shortcuts beside browsing content.",
            "Sidebar", "HubsSidebarEnabled",
            "https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/hubssidebarenabled"),
        EdgeBinary("policy.edge.shoppingassistant", "Edge Shopping Assistant",
            "Controls shopping features such as coupons, price comparison, rebates, and express checkout.",
            "Shopping features contact services for retail-domain offers and price information.",
            "Shopping", "EdgeShoppingAssistantEnabled",
            "https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/edgeshoppingassistantenabled"),
        EdgeChoice("policy.edge.diagnosticdata", "Edge Diagnostic Data",
            "Controls whether Edge sends no, required, or optional browser diagnostic data to Microsoft.",
            "Optional diagnostic data can include browser usage, visited sites, and crash information; turning off required data is not recommended by Microsoft.",
            "Diagnostic data", "DiagnosticData",
            "https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/diagnosticdata",
            [
                new ValueMeaning { RawValue = "0", Canonical = "Off", DisplayLabel = "Off (not recommended)", Description = "Turn off required and optional Edge diagnostic data.", Confidence = EffectiveConfidence.High },
                new ValueMeaning { RawValue = "1", Canonical = "RequiredData", DisplayLabel = "Required data", Description = "Send required Edge diagnostic data without optional data.", Confidence = EffectiveConfidence.High },
                new ValueMeaning { RawValue = "2", Canonical = "OptionalData", DisplayLabel = "Optional data", Description = "Send required and optional Edge diagnostic data.", Confidence = EffectiveConfidence.High }
            ], "Microsoft Edge 122 or later"),
        EdgeBinary("policy.edge.urldiagnosticdata", "URLs in Edge Diagnostic Data",
            "Controls whether URLs and per-page usage are included in optional Edge diagnostic data.",
            "This policy applies only when Edge Diagnostic Data is set to Optional data.",
            "Diagnostic data", "UrlDiagnosticDataEnabled",
            "https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/urldiagnosticdataenabled"),
        EdgeBinary("policy.edge.userfeedback", "Edge User Feedback",
            "Controls whether users can invoke Edge feedback, suggestions, surveys, and issue reporting.",
            "Work or school account feedback can be associated with the account and organization.",
            "Diagnostic data", "UserFeedbackAllowed",
            "https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/userfeedbackallowed")
    ];

    private static ManagedObject EdgeBinary(string id, string name, string description, string rationale,
        string category, string valueName, string reference) => new()
    {
        ObjectId = id,
        ObjectName = name,
        ObjectType = "PolicySetting",
        CanonicalPath = id,
        Description = description,
        Rationale = rationale,
        FeatureCategory = FeatureCategory.EdgePolicy,
        ProductDomain = ProductDomain.Edge,
        SubCategory = category,
        RiskLevel = RiskLevel.Medium,
        ImpactLevel = ImpactLevel.Application,
        InterfaceName = InterfaceName.GroupPolicy,
        ConfigurationType = ConfigurationType.PolicyState,
        DiscoveryMethod = @"HKLM\SOFTWARE\Policies\Microsoft\Edge\" + valueName,
        ControlLevel = ControlLevel.AdministratorControlled,
        ComponentOwner = ComponentOwner.MicrosoftEdge,
        PriorityLevel = PriorityLevel.Recommended,
        Reversibility = Reversibility.Reversible,
        RebootRequirement = RebootRequirement.ApplicationRestart,
        LifecycleState = LifecycleState.Active,
        CreatedBy = nameof(CatalogV262Expansion),
        CreatedTimestamp = DateTime.UnixEpoch,
        ConfidenceScore = 100,
        ConfidenceSource = "Microsoft Edge policy reference",
        References = [reference],
        ValueSemantics =
        [
            new ValueMeaning { RawValue = "0", Canonical = "Disabled", DisplayLabel = "Disabled", Description = "Disable this Edge feature.", Confidence = EffectiveConfidence.High },
            new ValueMeaning { RawValue = "1", Canonical = "Enabled", DisplayLabel = "Enabled", Description = "Enable this Edge feature.", Confidence = EffectiveConfidence.High }
        ]
    };

    private static ManagedObject EdgeChoice(string id, string name, string description, string rationale,
        string category, string valueName, string reference, List<ValueMeaning> meanings, string softwareConstraint)
    {
        var item = EdgeBinary(id, name, description, rationale, category, valueName, reference);
        item.ValueSemantics = meanings;
        item.SoftwareConstraint = softwareConstraint;
        return item;
    }
}
