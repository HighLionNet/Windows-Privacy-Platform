namespace WindowsPrivacyPlatform.Models;

public enum FeatureCategory
{
    RegistryPolicy,
    RegistryPreference,
    WindowsService,
    ScheduledTask,
    OptionalFeature,
    WindowsCapability,
    AppxPackage,
    ProvisionedPackage,
    EdgePolicy,
    DefenderSetting,
    NetworkSetting,
    FirewallProfile,
    FirewallRule,
    PrivacyPermission,
    WindowsComponent,
    AIComponent,
    CloudComponent,
    ExplorerFeature,
    StartMenuFeature,
    Widgets,
    StoreFeature,
    OfficeFeature
}

/// <summary>
/// Primary product/feature domain for navigation and report grouping.
/// Every catalog entry is assigned exactly one primary domain.
/// </summary>
public enum ProductDomain
{
    ConsentStore,
    AppPrivacy,
    Telemetry,
    WindowsUpdate,
    Defender,
    Search,
    Edge,
    ActivityHistory,
    CloudContent,
    Advertising,
    Location,
    Biometrics,
    Device,
    Speech,
    Firewall,
    Network,
    RemoteAccess,
    LocalSecurity,
    Copilot,
    Recall,
    Widgets,
    OneDrive,
    Storage,
    Other
}

/// <summary>Why a catalog entry is deliberately unavailable to the write pipeline.</summary>
public enum ExclusionReason
{
    None = 0,
    UnsupportedValueKind,
    RequiresMultiKeyCoordination,
    HighRiskIrreversible,
    ReadOnlyByDesign,
    NotYetCatalogued
}

/// <summary>Primary product surface on which an entry belongs.</summary>
public enum CatalogBucket
{
    Settings = 0,
    SystemInventory,
    InternalReference
}

public enum ApplicabilityState
{
    Unknown = 0,
    Applicable,
    NotAvailableOnEdition,
    NotAvailableOnBuild,
    NotAvailableOnWindowsVersion,
    NotPresentOnDevice
}

/// <summary>
/// Windows configuration layer that produced an observed value.
/// Ordered conceptually from weakest (user) to strongest (baseline) for documentation;
/// actual resolution lives in PolicyPrecedenceResolver (Scanner).
/// </summary>
public enum ConfigurationLayer
{
    Unknown = 0,
    UserPreference,
    ApplicationPreference,
    AlternatePolicyStore,
    MachinePolicy,
    MDMPolicy,
    SecurityBaseline
}

public enum EffectiveConfidence
{
    Unknown = 0,
    Low,
    Medium,
    High
}

/// <summary>
/// Kind of relationship between two managed settings (graph edge type).
/// </summary>
public enum RelationshipKind
{
    Related = 0,
    Overrides,
    OverriddenBy,
    ConflictsWith,
    DependsOn,
    Requires,
    Affects,
    SameFeatureAlternatePath,
    /// <summary>This setting is ignored when the target condition/setting is present.</summary>
    IgnoredWhen,
    /// <summary>Alternate registry or store path for the same semantic.</summary>
    AlternativeStorage,
    /// <summary>Legacy equivalent of a modern setting.</summary>
    LegacyEquivalent,
    /// <summary>Modern replacement for a legacy setting.</summary>
    ModernReplacement,
    /// <summary>Usually configured together in enterprise baselines.</summary>
    UsuallyConfiguredWith,
    /// <summary>Change may require reboot, sign-out, or service restart (see RebootRequirement on object).</summary>
    RequiresRestart
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public enum ImpactLevel
{
    User,
    System,
    Application,
    Security
}

public enum LifecycleState
{
    Draft,
    Validated,
    Active,
    NonCompliant,
    Deprecated,
    Retired,
    Incompatible
}

public enum InterfaceName
{
    Registry,
    GroupPolicy,
    ServiceControlManager,
    TaskScheduler,
    DISM,
    AppX,
    WindowsCapability,
    Firewall,
    Defender,
    WMICIM,
    NetworkStack
}

public enum ConfigurationType
{
    RegistryValue,
    ServiceState,
    PackageState,
    PolicyState,
    FeatureState,
    CapabilityState,
    FirewallRuleState,
    DefenderSettingValue,
    NetworkSettingValue,
    TaskState
}

public enum Reversibility
{
    Reversible,
    PartiallyReversible,
    Irreversible
}

public enum RebootRequirement
{
    None,
    ApplicationRestart,
    ExplorerRestart,
    ServiceRestart,
    Logout,
    RebootRequired
}

public enum PriorityLevel
{
    Core,
    Recommended,
    Optional,
    Experimental
}

public enum ControlLevel
{
    Locked,
    AdministratorControlled,
    UserControlled,
    Advisory
}

public enum ComponentOwner
{
    MicrosoftEdge,
    WindowsSearch,
    Defender,
    Explorer,
    Store,
    AI,
    Telemetry,
    Networking,
    WindowsUpdate,
    Other
}

public enum UpdatePersistenceBehavior
{
    Persists,
    MayReset,
    UsuallyReset,
    AlwaysReset
}
