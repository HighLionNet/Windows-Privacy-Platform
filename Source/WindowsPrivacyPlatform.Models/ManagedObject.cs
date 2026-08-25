namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Catalog + runtime view of a managed Windows setting.
///
/// Conceptual separation:
/// - Definition fields come from the static catalog.
/// - Observation holds live discovered values after binding.
/// - WritableTarget (optional) is the ONLY authorization for Modify mode.
/// - Absence of WritableTarget means the setting is observation-only.
/// </summary>
public class ManagedObject
{
    // ========== DEFINITION (static catalog) ==========

    public string ObjectId { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public string CanonicalPath { get; set; } = string.Empty;

    public FeatureCategory FeatureCategory { get; set; }
    public ProductDomain ProductDomain { get; set; }
    public string? SubCategory { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public ImpactLevel ImpactLevel { get; set; }

    public int MinimumBuild { get; set; }
    public int? MaximumBuild { get; set; }
    public List<string>? SupportedEditions { get; set; }
    public List<string>? SupportedWindowsVersions { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? Rationale { get; set; }
    public List<string>? References { get; set; }

    public string WhenIgnored { get; set; } = string.Empty;
    public string CommonMisconception { get; set; } = string.Empty;
    public string TypicalEnterpriseUse { get; set; } = string.Empty;
    public string ConsumerImpact { get; set; } = string.Empty;

    /// <summary>
    /// Complete, setting-specific decision-support content. Catalog finalization must populate this
    /// with authored material; presentation fallbacks are not accepted for catalog entries.
    /// </summary>
    public SettingNarrative Narrative { get; set; } = new();

    public List<ValueMeaning> ValueSemantics { get; set; } = new();

    public InterfaceName InterfaceName { get; set; }
    public string? InterfaceScope { get; set; }

    public ConfigurationType ConfigurationType { get; set; }
    public string TargetValue { get; set; } = string.Empty;

    public string? BuildConstraint { get; set; }
    public string? EditionConstraint { get; set; }
    public string? ComponentConstraint { get; set; }
    public string? HardwareConstraint { get; set; }
    public string? SoftwareConstraint { get; set; }
    public string? VirtualizationConstraint { get; set; }

    public string DiscoveryMethod { get; set; } = string.Empty;
    public string ComplianceMethod { get; set; } = string.Empty;

    public string RemediationMethod { get; set; } = string.Empty;
    public string? RemediationScope { get; set; }

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

    public string SchemaVersion { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedTimestamp { get; set; }
    public string? LogLevel { get; set; }
    public bool? AuditRequired { get; set; }
    public LifecycleState LifecycleState { get; set; }

    public int ConfidenceScore { get; set; }
    public string? ConfidenceSource { get; set; }

    public string VerificationMethod { get; set; } = string.Empty;
    public string? ExpectedResult { get; set; }
    public int VerificationReliability { get; set; }

    public string? EvidenceType { get; set; }
    public string? EvidenceLocation { get; set; }
    public string? EvidenceHash { get; set; }

    // ========== WRITE AUTHORIZATION (explicit, deny-by-default) ==========

    /// <summary>
    /// When non-null and complete, this setting may be modified under Modify mode.
    /// When null, modification is refused regardless of DiscoveryMethod or Observation paths.
    /// </summary>
    public WritableTarget? WritableTarget { get; set; }

    /// <summary>Convenience: true only when an explicit complete WritableTarget is present.</summary>
    public bool IsWritable => WritableTarget is { IsComplete: true };

    // ========== RELATIONSHIPS ==========

    public List<string>? Requires { get; set; }
    public List<string>? Recommended { get; set; }
    public List<string>? ConflictsWith { get; set; }
    public List<string>? Ordering { get; set; }
    public List<string>? RebootDependency { get; set; }
    public List<string>? RelatedFeature { get; set; }
    public List<string>? Replacement { get; set; }
    public List<string>? Alternative { get; set; }
    public string? ConflictExplanation { get; set; }

    public List<SettingRelationship> StructuredRelationships { get; set; } = new();

    // ========== OBSERVATION (runtime) ==========

    public string? CurrentState { get; set; }
    public DateTime? LastVerified { get; set; }

    public SettingObservation Observation { get; set; } = new();

    // ========== DESIRED STATE ==========

    public string DesiredState { get; set; } = string.Empty;
    public DesiredStateInfo? Desired { get; set; }
}
