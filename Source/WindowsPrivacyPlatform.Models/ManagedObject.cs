namespace WindowsPrivacyPlatform.Models;

public class ManagedObject
{
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

    // State
    public string DesiredState { get; set; } = string.Empty;
    public string? CurrentState { get; set; }
    public LifecycleState LifecycleState { get; set; }

    // Dependencies
    public List<string>? Requires { get; set; }
    public List<string>? Recommended { get; set; }
    public List<string>? ConflictsWith { get; set; }
    public List<string>? Ordering { get; set; }
    public List<string>? RebootDependency { get; set; }

    // Confidence
    public int ConfidenceScore { get; set; }
    public string? ConfidenceSource { get; set; }

    // Verification
    public string VerificationMethod { get; set; } = string.Empty;
    public string? ExpectedResult { get; set; }
    public DateTime? LastVerified { get; set; }
    public int VerificationReliability { get; set; }

    // Evidence
    public string? EvidenceType { get; set; }
    public string? EvidenceLocation { get; set; }
    public string? EvidenceHash { get; set; }

    // Documentation
    public string Description { get; set; } = string.Empty;
    public string? Rationale { get; set; }
    public List<string>? References { get; set; }

    // Logging
    public string? LogLevel { get; set; }
    public bool? AuditRequired { get; set; }

    // Metadata
    public string SchemaVersion { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedTimestamp { get; set; }

    // ManagementInterface
    public InterfaceName InterfaceName { get; set; }
    public string? InterfaceScope { get; set; }

    // Configuration
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

    // Remediation
    public string RemediationMethod { get; set; } = string.Empty;
    public string? RemediationScope { get; set; }

    // Recovery
    public Reversibility Reversibility { get; set; }
    public bool? BackupRequired { get; set; }

    // RebootImpact
    public RebootRequirement RebootRequirement { get; set; }

    // Priority
    public PriorityLevel PriorityLevel { get; set; }

    // UserControl
    public ControlLevel ControlLevel { get; set; }

    // Ownership
    public ComponentOwner ComponentOwner { get; set; }

    // RelatedObjects
    public List<string>? RelatedFeature { get; set; }
    public List<string>? Replacement { get; set; }
    public List<string>? Alternative { get; set; }
    public string? ConflictExplanation { get; set; }

    // ImpactScoring
    public int PrivacyImpact { get; set; }
    public int SecurityImpact { get; set; }
    public int PerformanceImpact { get; set; }
    public int UserExperienceImpact { get; set; }
    public int SystemStabilityImpact { get; set; }

    // SideEffects
    public string KnownSideEffects { get; set; } = string.Empty;

    // UpdatePersistence
    public UpdatePersistenceBehavior CumulativeUpdateBehavior { get; set; }
    public UpdatePersistenceBehavior FeatureUpdateBehavior { get; set; }
    public UpdatePersistenceBehavior ApplicationUpdateBehavior { get; set; }
}
