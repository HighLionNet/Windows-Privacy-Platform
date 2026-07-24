namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Catalog + runtime view of a managed Windows setting.
///
/// Conceptual separation (v0.6.5):
/// - Definition fields (identity, description, domain, risk, documentation) come from the static catalog.
/// - Observation holds live discovered values and layer data after binding.
/// - Desired holds optional baseline comparison data (compare-only).
/// - StructuredRelationships describe overrides / related settings.
///
/// Existing flat properties are retained for backward compatibility with catalog, validator, and CLI.
/// Models remain pure data — no business logic.
/// </summary>
public class ManagedObject
{
    // ========== DEFINITION (static catalog) ==========

    // Identity
    public string ObjectId { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string CanonicalPath { get; set; } = string.Empty;

    // Classification
    public FeatureCategory FeatureCategory { get; set; }
    /// <summary>Primary product domain for navigation and report grouping.</summary>
    public ProductDomain ProductDomain { get; set; }
    public string? SubCategory { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public ImpactLevel ImpactLevel { get; set; }

    // VersionCompatibility
    public int MinimumBuild { get; set; }
    public int? MaximumBuild { get; set; }
    public List<string>? SupportedEditions { get; set; }

    // Documentation
    public string Description { get; set; } = string.Empty;
    public string? Rationale { get; set; }
    public List<string>? References { get; set; }

    // ManagementInterface
    public InterfaceName InterfaceName { get; set; }
    public string? InterfaceScope { get; set; }

    // Configuration definition
    public ConfigurationType ConfigurationType { get; set; }
    public string TargetValue { get; set; } = string.Empty;

    // Applicability
    public string? BuildConstraint { get; set; }
    public string? EditionConstraint { get; set; }
    public string? ComponentConstraint { get; set; }
    public string? HardwareConstraint { get; set; }
    public string? SoftwareConstraint { get; set; }
    public string? VirtualizationConstraint { get; set; }

    // Detection
    public string DiscoveryMethod { get; set; } = string.Empty;
    public string ComplianceMethod { get; set; } = string.Empty;

    // Remediation metadata (definition only — no execution in current phase)
    public string RemediationMethod { get; set; } = string.Empty;
    public string? RemediationScope { get; set; }

    // Recovery / impact definition
    public Reversibility Reversibility { get; set; }
    public bool? BackupRequired { get; set; }
    public RebootRequirement RebootRequirement { get; set; }
    public PriorityLevel PriorityLevel { get; set; }
    public ControlLevel ControlLevel { get; set; }
    public ComponentOwner ComponentOwner { get; set; }

    public int PrivacyImpact { get; set; }
    public int SecurityImpact { get; set; }
    public int PerformanceImpact { get; set; }
    public int UserExperienceImpact { get; set; }
    public int SystemStabilityImpact { get; set; }
    public string KnownSideEffects { get; set; } = string.Empty;

    public UpdatePersistenceBehavior CumulativeUpdateBehavior { get; set; }
    public UpdatePersistenceBehavior FeatureUpdateBehavior { get; set; }
    public UpdatePersistenceBehavior ApplicationUpdateBehavior { get; set; }

    // Catalog metadata
    public string SchemaVersion { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedTimestamp { get; set; }
    public string? LogLevel { get; set; }
    public bool? AuditRequired { get; set; }
    public LifecycleState LifecycleState { get; set; }

    // Confidence of the definition itself
    public int ConfidenceScore { get; set; }
    public string? ConfidenceSource { get; set; }

    // Verification definition
    public string VerificationMethod { get; set; } = string.Empty;
    public string? ExpectedResult { get; set; }
    public int VerificationReliability { get; set; }

    // Evidence definition
    public string? EvidenceType { get; set; }
    public string? EvidenceLocation { get; set; }
    public string? EvidenceHash { get; set; }

    // ========== RELATIONSHIPS ==========

    /// <summary>Legacy string lists — still populated where useful; prefer StructuredRelationships.</summary>
    public List<string>? Requires { get; set; }
    public List<string>? Recommended { get; set; }
    public List<string>? ConflictsWith { get; set; }
    public List<string>? Ordering { get; set; }
    public List<string>? RebootDependency { get; set; }
    public List<string>? RelatedFeature { get; set; }
    public List<string>? Replacement { get; set; }
    public List<string>? Alternative { get; set; }
    public string? ConflictExplanation { get; set; }

    /// <summary>Structured relationships for navigation and effective-layer resolution.</summary>
    public List<SettingRelationship> StructuredRelationships { get; set; } = new();

    // ========== OBSERVATION (runtime, filled by binders) ==========

    /// <summary>Legacy flat current value — kept for CLI/report compatibility.</summary>
    public string? CurrentState { get; set; }
    public DateTime? LastVerified { get; set; }

    /// <summary>Structured runtime observation (layers + effective state).</summary>
    public SettingObservation Observation { get; set; } = new();

    // ========== DESIRED STATE (optional baseline, compare-only) ==========

    /// <summary>Legacy desired value string.</summary>
    public string DesiredState { get; set; } = string.Empty;

    /// <summary>Structured desired/baseline info (not enforced).</summary>
    public DesiredStateInfo? Desired { get; set; }
}
