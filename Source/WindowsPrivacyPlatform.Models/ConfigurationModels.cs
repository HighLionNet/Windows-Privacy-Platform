namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// One observed value from a single Windows configuration layer.
/// Pure data — no resolution logic.
/// Carries explicit provenance so explanations can answer "How do you know?".
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

    // --- Evidence / provenance ---

    /// <summary>Name of the collector that produced this observation.</summary>
    public string CollectorName { get; set; } = string.Empty;

    /// <summary>Human-readable evidence source (e.g. "Registry HKLM\\...", "WMI Win32_OperatingSystem", "ServiceController").</summary>
    public string EvidenceSource { get; set; } = string.Empty;

    /// <summary>Additional sources that were consulted or agreed/disagreed.</summary>
    public List<string> AlternativeSources { get; set; } = new();

    /// <summary>Notes about collection quality, conflicts, or limitations (never invents certainty).</summary>
    public string CollectionNotes { get; set; } = string.Empty;

    /// <summary>Mapped confidence for presentation; derived from ConfidenceScore or cross-validation.</summary>
    public EffectiveConfidence EffectiveConfidence { get; set; } = EffectiveConfidence.Unknown;
}

/// <summary>
/// Result of effective-configuration reasoning.
/// Always includes a reason; conflicts are explicit.
/// SemanticValue is the knowledge-layer interpretation of the effective raw token when a map exists.
/// </summary>
public class ConfigurationResolution
{
    public List<ConfigurationObservation> RawObservations { get; set; } = new();
    public string? EffectiveValue { get; set; }
    public ConfigurationLayer EffectiveSource { get; set; } = ConfigurationLayer.Unknown;
    public EffectiveConfidence Confidence { get; set; } = EffectiveConfidence.Unknown;

    /// <summary>Educational explanation of why this layer/value is effective.</summary>
    public string ResolutionReason { get; set; } = string.Empty;

    /// <summary>Why the confidence level was chosen (evidence quality, map presence, agreement).</summary>
    public string ConfidenceReason { get; set; } = string.Empty;

    /// <summary>Canonical meaning of EffectiveValue when a ValueSemantics map exists; otherwise null.</summary>
    public string? SemanticValue { get; set; }

    /// <summary>Display label from knowledge map when available.</summary>
    public string? SemanticDisplay { get; set; }

    public bool HasConflict { get; set; }

    /// <summary>Compatibility projection used by existing Observation.Effective consumers.</summary>
    public EffectiveState ToEffectiveState() => new()
    {
        EffectiveValue = EffectiveValue,
        EffectiveSource = EffectiveSource,
        Confidence = Confidence,
        Explanation = ResolutionReason,
        ConfidenceReason = ConfidenceReason,
        SemanticValue = SemanticValue,
        SemanticDisplay = SemanticDisplay,
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
    public string ConfidenceReason { get; set; } = string.Empty;
    public string? SemanticValue { get; set; }
    public string? SemanticDisplay { get; set; }
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
