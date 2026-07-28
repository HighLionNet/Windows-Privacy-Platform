namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Navigation / presentation models for TUI (and optional GUI).
/// Pure data — no input handling, no rendering, no system calls.
/// Trusted catalog metadata is kept separate from untrusted discovered values.
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

/// <summary>
/// Full decision-support card for one setting (UI-independent).
/// </summary>
public class SettingDetailView
{
    public string ObjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DomainPath { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public ControlLevel ControlLevel { get; set; }

    public SettingExplanation Explanation { get; set; } = new();

    // Untrusted / discovered (display only — never execute)
    public string? CurrentStateDisplay { get; set; }
    public string? EffectiveValueDisplay { get; set; }
    public string? EffectiveSourceDisplay { get; set; }
    public string? ResolutionReason { get; set; }
    public EffectiveConfidence Confidence { get; set; }
    public bool HasConflict { get; set; }
    public List<LayerDisplay> Layers { get; set; } = new();
    public List<RelatedSettingDisplay> Related { get; set; } = new();

    /// <summary>Catalog ValueSemantics mapped for the options table (trusted).</summary>
    public List<OptionDisplay> Options { get; set; } = new();
}

public class OptionDisplay
{
    public string RawValue { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
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

/// <summary>
/// Builds navigation trees and detail cards from a bound catalog + SettingsQuery.
/// No UI framework dependency.
/// </summary>
public static class NavigationBuilder
{
    public static NavigationNode BuildDomainTree(IReadOnlyList<ManagedObject> catalog)
    {
        var root = new NavigationNode { Id = "root", Title = "Windows Privacy Platform" };

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
                        Subtitle = mo.Observation?.Effective?.EffectiveValue
                                   ?? mo.Observation?.Resolution?.EffectiveValue
                                   ?? mo.CurrentState,
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

        var view = new SettingDetailView
        {
            ObjectId = mo.ObjectId,
            Title = mo.ObjectName,
            DomainPath = explanation.DomainPath,
            RiskLevel = mo.RiskLevel,
            ControlLevel = mo.ControlLevel,
            Explanation = explanation,
            CurrentStateDisplay = mo.CurrentState,
            EffectiveValueDisplay = resolution?.EffectiveValue ?? effective?.EffectiveValue,
            EffectiveSourceDisplay = (resolution?.EffectiveSource ?? effective?.EffectiveSource)?.ToString(),
            ResolutionReason = resolution?.ResolutionReason ?? effective?.Explanation,
            Confidence = resolution?.Confidence ?? effective?.Confidence ?? EffectiveConfidence.Unknown,
            HasConflict = resolution?.HasConflict == true || effective?.HasConflict == true
        };

        if (mo.ValueSemantics is { Count: > 0 })
        {
            foreach (var v in mo.ValueSemantics)
            {
                if (v is null) continue;
                view.Options.Add(new OptionDisplay
                {
                    RawValue = v.RawValue ?? string.Empty,
                    Label = string.IsNullOrWhiteSpace(v.DisplayLabel) ? v.Canonical : v.DisplayLabel,
                    Description = string.IsNullOrWhiteSpace(v.Description) ? null : v.Description
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
                    // Display-only: treat as opaque text, never execute
                    ValueDisplay = SanitizeDisplay(layer.RawValue),
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
    /// Human-readable domain title for navigation and page headers.
    /// </summary>
    public static string HumanizeDomain(ProductDomain domain) => domain switch
    {
        ProductDomain.ConsentStore => "Privacy — App permissions",
        ProductDomain.AppPrivacy => "Privacy — App policy overrides",
        ProductDomain.Telemetry => "Telemetry & diagnostics",
        ProductDomain.WindowsUpdate => "Windows Update",
        ProductDomain.Defender => "Microsoft Defender",
        ProductDomain.Search => "Search & Cortana",
        ProductDomain.Edge => "Microsoft Edge",
        ProductDomain.ActivityHistory => "Activity History",
        ProductDomain.CloudContent => "Cloud content & suggestions",
        ProductDomain.Advertising => "Advertising ID",
        ProductDomain.Location => "Location services",
        ProductDomain.Biometrics => "Biometrics",
        ProductDomain.Device => "Device find & recovery",
        ProductDomain.Speech => "Speech recognition",
        ProductDomain.Firewall => "Windows Firewall",
        _ => domain.ToString()
    };

    /// <summary>
    /// Strip control characters from discovered strings before UI display.
    /// Does not interpret content as code.
    /// </summary>
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
