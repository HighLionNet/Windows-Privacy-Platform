namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// One observed value from a single Windows configuration layer.
/// Pure data — no resolution logic.
/// </summary>
public class ConfigurationObservation
{
    public string ObjectId { get; set; } = string.Empty;
    public ConfigurationLayer Layer { get; set; } = ConfigurationLayer.Unknown;
    public string RawValue { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string? Hive { get; set; }
    public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
    public int ConfidenceScore { get; set; } = 80;
}

/// <summary>
/// Best-effort effective configuration for a setting after considering layers.
/// Conflicts are explicit — never hidden.
/// </summary>
public class EffectiveState
{
    public string? EffectiveValue { get; set; }
    public ConfigurationLayer EffectiveSource { get; set; } = ConfigurationLayer.Unknown;
    public EffectiveConfidence Confidence { get; set; } = EffectiveConfidence.Unknown;
    public string Explanation { get; set; } = string.Empty;
    public bool HasConflict { get; set; }
    public List<ConfigurationObservation> ContributingLayers { get; set; } = new();
}

/// <summary>
/// Optional baseline / desired-state comparison data (compare-only; no enforcement).
/// </summary>
public class DesiredStateInfo
{
    public string? DesiredValue { get; set; }
    public string? BaselineProfileId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Structured relationship between two catalog settings.
/// Replaces ad-hoc string lists for navigation and effective-layer work.
/// </summary>
public class SettingRelationship
{
    public string FromObjectId { get; set; } = string.Empty;
    public string ToObjectId { get; set; } = string.Empty;
    public RelationshipKind Kind { get; set; } = RelationshipKind.Related;
    public string? Explanation { get; set; }
}

/// <summary>
/// Runtime observation envelope attached to a ManagedObject after binding.
/// Separates live discovered data from static catalog definition fields.
/// </summary>
public class SettingObservation
{
    public string? CurrentValue { get; set; }
    public string? SourceSummary { get; set; }
    public DateTime? ObservedAt { get; set; }
    public int ConfidenceScore { get; set; }
    public List<ConfigurationObservation> Layers { get; set; } = new();
    public EffectiveState? Effective { get; set; }
}
