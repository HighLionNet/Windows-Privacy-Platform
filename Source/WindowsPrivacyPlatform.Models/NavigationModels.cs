namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Navigation / presentation models for the GUI.
/// Pure data — no input handling, no rendering, no system calls.
/// </summary>

public class NavigationNode
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public ProductDomain? Domain { get; set; }
    public string? ObjectId { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public bool HasConflict { get; set; }
    public int ChildCount { get; set; }
    public int ConflictCount { get; set; }
    public int HighRiskCount { get; set; }
    public List<NavigationNode> Children { get; set; } = new();
}

public class SettingDetailView
{
    public string ObjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DomainPath { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public ControlLevel ControlLevel { get; set; }
    public string TechnicalLocation { get; set; } = string.Empty;
    public bool IsWritable { get; set; }
    public string ExclusionReasonText { get; set; } = string.Empty;
    public CatalogBucket Bucket { get; set; }
    public ApplicabilityState Applicability { get; set; }
    public string ApplicabilityReason { get; set; } = string.Empty;
    public string RestartExpectation { get; set; } = string.Empty;
    public NativeToolLink? NativeTool { get; set; }
    public SettingNarrative Narrative { get; set; } = new();

    public SettingExplanation Explanation { get; set; } = new();

    public string? CurrentStateDisplay { get; set; }
    public string? EffectiveValueDisplay { get; set; }
    public string? EffectiveSourceDisplay { get; set; }
    public string? ResolutionReason { get; set; }
    public EffectiveConfidence Confidence { get; set; }
    public bool HasConflict { get; set; }
    public List<LayerDisplay> Layers { get; set; } = new();
    public List<RelatedSettingDisplay> Related { get; set; } = new();
    public List<OptionDisplay> Options { get; set; } = new();
}

public class OptionDisplay
{
    public string RawValue { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsApplicable { get; set; } = true;
}

public class LayerDisplay
{
    public string LayerName { get; set; } = string.Empty;
    public string ValueDisplay { get; set; } = string.Empty;
    public string SourcePathDisplay { get; set; } = string.Empty;
}

public class RelatedSettingDisplay
{
    public string ObjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}

public static class NavigationBuilder
{
    public static NavigationNode BuildDomainTree(IReadOnlyList<ManagedObject> catalog)
    {
        var root = new NavigationNode { Id = "root", Title = "Settings" };

        foreach (var domainGroup in catalog.GroupBy(m => m.ProductDomain).OrderBy(g => g.Key))
        {
            var domainNode = new NavigationNode
            {
                Id = $"domain:{domainGroup.Key}",
                Title = HumanizeDomain(domainGroup.Key),
                Domain = domainGroup.Key,
                ChildCount = domainGroup.Count(),
                ConflictCount = domainGroup.Count(m => m.Observation?.Effective?.HasConflict == true ||
                                                       m.Observation?.Resolution?.HasConflict == true),
                HighRiskCount = domainGroup.Count(m => m.RiskLevel == RiskLevel.High)
            };

            foreach (var subGroup in domainGroup
                         .GroupBy(m => m.SubCategory ?? domainGroup.Key.ToString())
                         .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var featureNode = new NavigationNode
                {
                    Id = $"feature:{domainGroup.Key}:{subGroup.Key}",
                    Title = subGroup.Key,
                    Domain = domainGroup.Key,
                    ChildCount = subGroup.Count(),
                    ConflictCount = subGroup.Count(m => m.Observation?.Effective?.HasConflict == true ||
                                                        m.Observation?.Resolution?.HasConflict == true),
                    HighRiskCount = subGroup.Count(m => m.RiskLevel == RiskLevel.High)
                };

                foreach (var mo in subGroup.OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase))
                {
                    featureNode.Children.Add(new NavigationNode
                    {
                        Id = $"setting:{mo.ObjectId}",
                        Title = mo.ObjectName,
                        Subtitle = DisplayValue(
                                       mo.Observation?.Effective?.EffectiveValue
                                       ?? mo.Observation?.Resolution?.EffectiveValue
                                       ?? mo.CurrentState),
                        Domain = mo.ProductDomain,
                        ObjectId = mo.ObjectId,
                        RiskLevel = mo.RiskLevel,
                        HasConflict = mo.Observation?.Effective?.HasConflict == true ||
                                      mo.Observation?.Resolution?.HasConflict == true
                    });
                }

                domainNode.Children.Add(featureNode);
            }

            root.Children.Add(domainNode);
        }

        root.ChildCount = root.Children.Count;
        root.ConflictCount = root.Children.Sum(c => c.ConflictCount);
        root.HighRiskCount = root.Children.Sum(c => c.HighRiskCount);
        return root;
    }

    public static SettingDetailView? BuildDetail(ManagedObject mo, SettingsQuery? query = null)
    {
        if (mo is null)
            return null;

        var explanation = SettingExplanationFactory.FromDefinition(mo);
        var resolution = mo.Observation?.Resolution;
        var effective = mo.Observation?.Effective;

        var current = DisplayValue(mo.CurrentState);
        var effectiveValue = DisplayValue(resolution?.EffectiveValue ?? effective?.EffectiveValue ?? mo.CurrentState);
        var source = resolution?.EffectiveSource ?? effective?.EffectiveSource;
        var sourceDisplay = source is null or ConfigurationLayer.Unknown
            ? (IsAbsent(current) ? "No policy value at probed path" : "Unknown")
            : source.ToString();

        var confidence = resolution?.Confidence ?? effective?.Confidence ?? EffectiveConfidence.Unknown;
        if (IsAbsent(effectiveValue) && confidence == EffectiveConfidence.Unknown)
            confidence = EffectiveConfidence.Medium;

        var view = new SettingDetailView
        {
            ObjectId = mo.ObjectId,
            Title = mo.ObjectName,
            DomainPath = explanation.DomainPath,
            RiskLevel = mo.RiskLevel,
            ControlLevel = mo.ControlLevel,
            TechnicalLocation = mo.TechnicalLocation,
            IsWritable = mo.IsWritable,
            ExclusionReasonText = CatalogPolicy.ExclusionReasonText(mo.ExclusionReason),
            Bucket = mo.Bucket,
            Applicability = mo.Applicability,
            ApplicabilityReason = mo.ApplicabilityReason,
            RestartExpectation = mo.RebootRequirement.ToString(),
            NativeTool = mo.NativeTool,
            Narrative = mo.Narrative,
            Explanation = explanation,
            CurrentStateDisplay = current,
            EffectiveValueDisplay = effectiveValue,
            EffectiveSourceDisplay = sourceDisplay,
            ResolutionReason = resolution?.ResolutionReason ?? effective?.Explanation,
            Confidence = confidence,
            HasConflict = resolution?.HasConflict == true || effective?.HasConflict == true
        };

        if (mo.ValueSemantics is { Count: > 0 })
        {
            foreach (var v in mo.ValueSemantics)
            {
                if (v is null) continue;
                var copy = SettingOptionLanguage.For(mo, v);
                view.Options.Add(new OptionDisplay
                {
                    RawValue = v.RawValue ?? string.Empty,
                    Label = copy.Action,
                    Description = copy.Effect
                });
            }
        }

        var layers = resolution?.RawObservations ?? mo.Observation?.Layers;
        if (layers is not null)
        {
            foreach (var layer in layers)
            {
                view.Layers.Add(new LayerDisplay
                {
                    LayerName = layer.Layer.ToString(),
                    ValueDisplay = SanitizeDisplay(DisplayValue(layer.RawValue)),
                    SourcePathDisplay = SanitizeDisplay(layer.SourcePath)
                });
            }
        }

        if (query is not null)
        {
            var edges = query.GetRelationshipEdges(mo.ObjectId)
                .GroupBy(e => e.ToObjectId + "|" + e.FromObjectId + "|" + e.Kind)
                .Select(g => g.First());

            foreach (var edge in edges)
            {
                var otherId = edge.FromObjectId.Equals(mo.ObjectId, StringComparison.OrdinalIgnoreCase)
                    ? edge.ToObjectId
                    : edge.FromObjectId;
                var other = query.GetById(otherId);
                if (other is null)
                    continue;

                view.Related.Add(new RelatedSettingDisplay
                {
                    ObjectId = other.ObjectId,
                    Title = other.ObjectName,
                    Relationship = edge.Kind.ToString(),
                    Explanation = edge.Explanation
                });
            }
        }

        return view;
    }

    /// <summary>
    /// Normalize display of observed values while preserving the trust model:
    /// Unknown, Not configured, Not observed, and Error are distinct states.
    /// Never collapse Unknown into an absence state.
    /// </summary>
    public static string DisplayValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Not configured";

        var trimmed = value.Trim();

        // Preserve exact semantic tokens
        if (trimmed.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return "Unknown";
        if (trimmed.Equals("Not observed", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("Not observed in this scan", StringComparison.OrdinalIgnoreCase))
            return "Not observed";
        if (trimmed.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            return trimmed;
        if (trimmed.Equals("Not configured", StringComparison.OrdinalIgnoreCase))
            return "Not configured";

        return trimmed;
    }

    /// <summary>
    /// True only when the value represents proven absence (successfully checked and missing).
    /// Unknown and Not observed are NOT treated as absence.
    /// </summary>
    public static bool IsAbsent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        return value.Trim().Equals("Not configured", StringComparison.OrdinalIgnoreCase);
    }

    public static string HumanizeDomain(ProductDomain domain) => domain switch
    {
        ProductDomain.ConsentStore => "App permissions",
        ProductDomain.AppPrivacy => "App policy controls",
        ProductDomain.Telemetry => "Diagnostics & feedback",
        ProductDomain.WindowsUpdate => "Windows Update",
        ProductDomain.Defender => "Microsoft Defender",
        ProductDomain.Search => "Search privacy",
        ProductDomain.Edge => "Microsoft Edge",
        ProductDomain.ActivityHistory => "Activity",
        ProductDomain.CloudContent => "Personalization",
        ProductDomain.Advertising => "Advertising",
        ProductDomain.Location => "Location services",
        ProductDomain.Biometrics => "Biometrics",
        ProductDomain.Device => "Device privacy",
        ProductDomain.Speech => "Online speech",
        ProductDomain.Firewall => "Windows Firewall",
        ProductDomain.Network => "Network security",
        ProductDomain.RemoteAccess => "Remote access",
        ProductDomain.LocalSecurity => "Local security",
        ProductDomain.Copilot => "Copilot",
        ProductDomain.Recall => "Recall & Click to Do",
        ProductDomain.Widgets => "Widgets",
        ProductDomain.OneDrive => "OneDrive",
        ProductDomain.Storage => "Storage Sense",
        ProductDomain.Other => "Clipboard",
        _ => domain.ToString()
    };

    private static string SanitizeDisplay(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        Span<char> buffer = value.Length <= 512 ? stackalloc char[value.Length] : new char[value.Length];
        var n = 0;
        foreach (var c in value)
        {
            if (char.IsControl(c) && c != '\t')
                continue;
            buffer[n++] = c;
            if (n >= 512)
                break;
        }
        return new string(buffer[..n]);
    }
}
