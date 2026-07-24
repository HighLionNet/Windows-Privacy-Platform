namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Navigation / presentation models for a future terminal UI (and optional GUI).
/// Pure data — no input handling, no rendering, no system calls.
/// Trusted catalog metadata is kept separate from untrusted discovered values.
/// </summary>

/// <summary>
/// One node in a domain → feature → setting navigation tree.
/// </summary>
public class NavigationNode
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public ProductDomain? Domain { get; set; }
    public string? ObjectId { get; set; }
    public List<NavigationNode> Children { get; set; } = new();
}

/// <summary>
/// Detail view model for a single setting. Safe for display only.
/// Discovered values are treated as untrusted display text — never executed or interpreted as code.
/// </summary>
public class SettingDetailView
{
    // Trusted catalog metadata
    public string ObjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Rationale { get; set; }
    public ProductDomain Domain { get; set; }
    public string? SubCategory { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public ControlLevel ControlLevel { get; set; }

    // Untrusted / discovered (display only)
    public string? CurrentStateDisplay { get; set; }
    public string? EffectiveValueDisplay { get; set; }
    public string? EffectiveSourceDisplay { get; set; }
    public string? EffectiveExplanation { get; set; }
    public bool HasConflict { get; set; }
    public List<LayerDisplay> Layers { get; set; } = new();

    // Relationships (ids + names from trusted catalog)
    public List<RelatedSettingDisplay> Related { get; set; } = new();
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
}

/// <summary>
/// Builds navigation trees and detail views from a bound catalog.
/// No UI framework dependency. TUI/GUI hosts consume these models.
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
                Title = domainGroup.Key.ToString(),
                Domain = domainGroup.Key
            };

            foreach (var subGroup in domainGroup
                         .GroupBy(m => m.SubCategory ?? domainGroup.Key.ToString())
                         .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var featureNode = new NavigationNode
                {
                    Id = $"feature:{domainGroup.Key}:{subGroup.Key}",
                    Title = subGroup.Key,
                    Domain = domainGroup.Key
                };

                foreach (var mo in subGroup.OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase))
                {
                    featureNode.Children.Add(new NavigationNode
                    {
                        Id = $"setting:{mo.ObjectId}",
                        Title = mo.ObjectName,
                        Subtitle = mo.CurrentState,
                        Domain = mo.ProductDomain,
                        ObjectId = mo.ObjectId
                    });
                }

                domainNode.Children.Add(featureNode);
            }

            root.Children.Add(domainNode);
        }

        return root;
    }

    public static SettingDetailView? BuildDetail(ManagedObject mo, SettingsQuery? query = null)
    {
        if (mo is null)
            return null;

        var view = new SettingDetailView
        {
            ObjectId = mo.ObjectId,
            Title = mo.ObjectName,
            Description = mo.Description,
            Rationale = mo.Rationale,
            Domain = mo.ProductDomain,
            SubCategory = mo.SubCategory,
            RiskLevel = mo.RiskLevel,
            ControlLevel = mo.ControlLevel,
            CurrentStateDisplay = mo.CurrentState,
            EffectiveValueDisplay = mo.Observation?.Effective?.EffectiveValue,
            EffectiveSourceDisplay = mo.Observation?.Effective?.EffectiveSource.ToString(),
            EffectiveExplanation = mo.Observation?.Effective?.Explanation,
            HasConflict = mo.Observation?.Effective?.HasConflict == true
        };

        if (mo.Observation?.Layers is not null)
        {
            foreach (var layer in mo.Observation.Layers)
            {
                view.Layers.Add(new LayerDisplay
                {
                    LayerName = layer.Layer.ToString(),
                    ValueDisplay = layer.RawValue,
                    SourcePathDisplay = layer.SourcePath
                });
            }
        }

        if (query is not null)
        {
            foreach (var related in query.RelatedTo(mo.ObjectId))
            {
                view.Related.Add(new RelatedSettingDisplay
                {
                    ObjectId = related.ObjectId,
                    Title = related.ObjectName,
                    Relationship = "Related"
                });
            }
        }

        return view;
    }
}
