namespace WindowsPrivacyPlatform.Models;

/// <summary>Current Microsoft Copilot app and Windows AI policies documented for v2.5.</summary>
public static class CatalogV25Expansion
{
    private const string CopilotPolicyReference =
        "https://learn.microsoft.com/windows/client-management/configure-microsoft-copilot-policies";
    private const string WindowsAiReference =
        "https://learn.microsoft.com/windows/client-management/mdm/policy-csp-windowsai";

    public static IReadOnlyList<ManagedObject> CreateCoverageBatch()
    {
        const string CopilotApp = @"HKLM\SOFTWARE\Policies\Microsoft\Copilot";
        const string WindowsAi = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
        const string Paint = @"HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\Paint";

        return new List<ManagedObject>
        {
            Binary("policy.copilot.app.browsing", "Copilot App Web Browsing",
                "Controls whether the current Microsoft Copilot app can browse the web.",
                "Blocking browsing reduces live web access inside Copilot but also removes web-grounded answers.",
                ProductDomain.Copilot, "Copilot app", CopilotApp + @"\BrowsingEnabled", 22621,
                "Block browsing", "Allow browsing", CopilotPolicyReference),
            Binary("policy.copilot.app.componentupdates", "Copilot App Component Updates",
                "Controls noncritical component updates in the current Microsoft Copilot app.",
                "Disabling component updates can delay fixes; Microsoft still permits security-critical component updates.",
                ProductDomain.Copilot, "Copilot app", CopilotApp + @"\ComponentUpdatesEnabled", 22621,
                "Block component updates", "Allow component updates", CopilotPolicyReference,
                RebootRequirement.ApplicationRestart),
            Binary("policy.copilot.app.coworkactions", "Copilot Cowork Tool Actions",
                "Controls whether Cowork can take actions on behalf of the user.",
                "Tool actions can affect files, applications, and services reached through Copilot.",
                ProductDomain.Copilot, "Copilot app", CopilotApp + @"\CopilotCoworkToolActionsEnabled", 22621,
                "Block tool actions", "Allow tool actions", CopilotPolicyReference),

            Binary("policy.copilot.settingsagent", "Settings Agentic Search",
                "Controls natural-language agentic search suggestions in Windows Settings.",
                "Disabling it retains conventional and semantic Settings search while removing the agentic experience.",
                ProductDomain.Copilot, "Windows AI", WindowsAi + @"\DisableSettingsAgent", 26100,
                "Leave agentic search on", "Turn agentic search off", WindowsAiReference,
                editions: EnterpriseEditions()),
            Binary("policy.copilot.paint.cocreator", "Paint Cocreator",
                "Controls whether Cocreator is available in Microsoft Paint.",
                "Cocreator is a generative image feature and may use connected AI services.",
                ProductDomain.Copilot, "AI features in Paint", Paint + @"\DisableCocreator", 22621,
                "Leave Cocreator on", "Turn Cocreator off", WindowsAiReference),
            Binary("policy.copilot.paint.generativefill", "Paint Generative Fill",
                "Controls whether generative fill is available in Microsoft Paint.",
                "Generative fill changes image content with an AI-assisted workflow.",
                ProductDomain.Copilot, "AI features in Paint", Paint + @"\DisableGenerativeFill", 22621,
                "Leave generative fill on", "Turn generative fill off", WindowsAiReference),
            Binary("policy.copilot.paint.imagecreator", "Paint Image Creator",
                "Controls whether Image Creator is available in Microsoft Paint.",
                "Image Creator is a generative image surface and may use connected AI services.",
                ProductDomain.Copilot, "AI features in Paint", Paint + @"\DisableImageCreator", 22621,
                "Leave Image Creator on", "Turn Image Creator off", WindowsAiReference),

            MonitoredBinary("policy.recall.allowenablement", "Recall Component Availability",
                "Reports whether the Recall optional component is available for users to enable.",
                "Disabling this policy removes Recall bits and deletes saved snapshots, so WPP monitors it without offering a write.",
                "Recall", WindowsAi + @"\AllowRecallEnablement", 26100,
                "Recall unavailable", "Recall available", WindowsAiReference),
            MonitoredBinary("policy.recall.allowexport", "Recall Snapshot Export",
                "Reports whether users can export their Recall snapshot information on supported EEA devices.",
                "Exported snapshot information can contain sensitive screen content and is protected by a user-held export code.",
                "Data safeguards", WindowsAi + @"\AllowRecallExport", 26100,
                "Deny export", "Allow export", WindowsAiReference, EnterpriseEditions()),
            MonitoredString("policy.recall.denyapplist", "Recall App Exclusion List",
                "Reports the managed list of apps excluded from Recall snapshots.",
                "A managed exclusion list helps keep sensitive application content out of captured snapshots.",
                WindowsAi + @"\SetDenyAppListForRecall", WindowsAiReference),
            MonitoredString("policy.recall.denyurilist", "Recall Website Exclusion List",
                "Reports the managed list of website addresses excluded from Recall snapshots.",
                "A managed exclusion list helps keep sensitive browser content out of captured snapshots.",
                WindowsAi + @"\SetDenyUriListForRecall", WindowsAiReference),
            Choice("policy.recall.maxduration", "Recall Snapshot Retention",
                "Controls the maximum time Recall snapshots are retained.",
                "Shorter retention reduces the historical screen-content window but removes older Recall history sooner.",
                "Storage limits", WindowsAi + @"\SetMaximumStorageDurationForRecallSnapshots", WindowsAiReference,
                [V("0", "Windows managed", "Let Windows choose the retention limit."),
                 V("30", "30 days", "Keep snapshots for up to 30 days."),
                 V("60", "60 days", "Keep snapshots for up to 60 days."),
                 V("90", "90 days", "Keep snapshots for up to 90 days."),
                 V("180", "180 days", "Keep snapshots for up to 180 days.")]),
            Choice("policy.recall.maxstorage", "Recall Snapshot Storage Limit",
                "Controls the maximum disk space available for Recall snapshots.",
                "A lower limit bounds stored screen history but causes older snapshots to be removed sooner.",
                "Storage limits", WindowsAi + @"\SetMaximumStorageSpaceForRecallSnapshots", WindowsAiReference,
                [V("0", "Windows managed", "Let Windows size snapshot storage for the device."),
                 V("10240", "10 GB", "Limit snapshot storage to 10 GB."),
                 V("25600", "25 GB", "Limit snapshot storage to 25 GB."),
                 V("51200", "50 GB", "Limit snapshot storage to 50 GB."),
                 V("76800", "75 GB", "Limit snapshot storage to 75 GB."),
                 V("102400", "100 GB", "Limit snapshot storage to 100 GB."),
                 V("153600", "150 GB", "Limit snapshot storage to 150 GB.")])
        }.AsReadOnly();
    }

    private static ManagedObject Binary(string id, string name, string description, string rationale,
        ProductDomain domain, string category, string path, int minimumBuild, string zeroLabel, string oneLabel,
        string reference, RebootRequirement reboot = RebootRequirement.None, List<string>? editions = null)
    {
        var item = Base(id, name, description, rationale, domain, category, path, minimumBuild, reference);
        item.ValueSemantics =
        [
            V("0", zeroLabel, zeroLabel + "."),
            V("1", oneLabel, oneLabel + ".")
        ];
        item.RebootRequirement = reboot;
        item.SupportedEditions = editions;
        if (id.StartsWith("policy.copilot.app.", StringComparison.OrdinalIgnoreCase))
        {
            item.SoftwareConstraint = "Microsoft Copilot version 152 or later";
            item.WhenIgnored = "Microsoft Copilot versions earlier than 152 do not apply this policy.";
        }
        return item;
    }

    private static ManagedObject MonitoredBinary(string id, string name, string description, string rationale,
        string category, string path, int minimumBuild, string zeroLabel, string oneLabel, string reference,
        List<string>? editions = null)
    {
        var item = Binary(id, name, description, rationale, ProductDomain.Recall, category, path, minimumBuild,
            zeroLabel, oneLabel, reference, editions: editions);
        item.ExclusionReason = ExclusionReason.ReadOnlyByDesign;
        return item;
    }

    private static ManagedObject MonitoredString(string id, string name, string description, string rationale,
        string path, string reference)
    {
        var item = Base(id, name, description, rationale, ProductDomain.Recall, "Data safeguards", path, 26100, reference);
        item.SupportedEditions = EnterpriseEditions();
        item.ExclusionReason = ExclusionReason.ReadOnlyByDesign;
        return item;
    }

    private static ManagedObject Choice(string id, string name, string description, string rationale,
        string category, string path, string reference, List<ValueMeaning> values)
    {
        var item = Base(id, name, description, rationale, ProductDomain.Recall, category, path, 26100, reference);
        item.SupportedEditions = EnterpriseEditions();
        item.ValueSemantics = values;
        return item;
    }

    private static ManagedObject Base(string id, string name, string description, string rationale,
        ProductDomain domain, string category, string path, int minimumBuild, string reference) => new()
    {
        ObjectId = id,
        ObjectName = name,
        ObjectType = "PolicySetting",
        CanonicalPath = id,
        Description = description,
        Rationale = rationale,
        FeatureCategory = FeatureCategory.AIComponent,
        ProductDomain = domain,
        SubCategory = category,
        RiskLevel = RiskLevel.High,
        ImpactLevel = ImpactLevel.Security,
        MinimumBuild = minimumBuild,
        SupportedWindowsVersions = ["Windows 11"],
        References = [reference],
        InterfaceName = InterfaceName.GroupPolicy,
        ConfigurationType = ConfigurationType.PolicyState,
        DiscoveryMethod = path,
        ControlLevel = ControlLevel.AdministratorControlled,
        ComponentOwner = ComponentOwner.AI,
        PriorityLevel = PriorityLevel.Recommended,
        Reversibility = Reversibility.Reversible,
        RebootRequirement = RebootRequirement.None,
        LifecycleState = LifecycleState.Active,
        CreatedBy = "CatalogV25Expansion",
        CreatedTimestamp = DateTime.UnixEpoch,
        ConfidenceScore = 95,
        ConfidenceSource = "Microsoft policy documentation"
    };

    private static ValueMeaning V(string raw, string label, string description) => new()
    {
        RawValue = raw,
        Canonical = label.Replace(" ", string.Empty),
        DisplayLabel = label,
        Description = description,
        Confidence = EffectiveConfidence.High
    };

    private static List<string> EnterpriseEditions() =>
        ["Enterprise", "Education", "IoT Enterprise", "IoT Enterprise LTSC"];
}
