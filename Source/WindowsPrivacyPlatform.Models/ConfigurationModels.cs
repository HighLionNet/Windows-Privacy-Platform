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
/// Result of effective-configuration reasoning.
/// Always includes a reason; conflicts are explicit.
/// </summary>
public class ConfigurationResolution
{
    public List<ConfigurationObservation> RawObservations { get; set; } = new();
    public string? EffectiveValue { get; set; }
    public ConfigurationLayer EffectiveSource { get; set; } = ConfigurationLayer.Unknown;
    public EffectiveConfidence Confidence { get; set; } = EffectiveConfidence.Unknown;
    public string ResolutionReason { get; set; } = string.Empty;
    public bool HasConflict { get; set; }

    /// <summary>Compatibility projection used by existing Observation.Effective consumers.</summary>
    public EffectiveState ToEffectiveState() => new()
    {
        EffectiveValue = EffectiveValue,
        EffectiveSource = EffectiveSource,
        Confidence = Confidence,
        Explanation = ResolutionReason,
        HasConflict = HasConflict,
        ContributingLayers = RawObservations
    };
}

/// <summary>
/// Best-effort effective configuration (legacy shape; prefer ConfigurationResolution).
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

public class DesiredStateInfo
{
    public string? DesiredValue { get; set; }
    public string? BaselineProfileId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Structured relationship edge between two catalog settings.
/// Supports later graph traversal (no UI yet).
/// </summary>
public class SettingRelationship
{
    public string FromObjectId { get; set; } = string.Empty;
    public string ToObjectId { get; set; } = string.Empty;
    public RelationshipKind Kind { get; set; } = RelationshipKind.Related;
    public string? Explanation { get; set; }
}

/// <summary>
/// Runtime observation envelope after binding.
/// </summary>
public class SettingObservation
{
    public string? CurrentValue { get; set; }
    public string? SourceSummary { get; set; }
    public DateTime? ObservedAt { get; set; }
    public int ConfidenceScore { get; set; }
    public List<ConfigurationObservation> Layers { get; set; } = new();
    public EffectiveState? Effective { get; set; }
    public ConfigurationResolution? Resolution { get; set; }
}
