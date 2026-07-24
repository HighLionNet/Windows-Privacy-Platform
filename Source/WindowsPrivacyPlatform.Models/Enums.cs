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
/// SubCategory remains available for finer classification within a domain.
/// </summary>
public enum ProductDomain
{
    /// <summary>Per-user ConsentStore app capability permissions.</summary>
    ConsentStore,
    /// <summary>Machine AppPrivacy GPO overrides for app capabilities.</summary>
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
    /// <summary>Reserved for future firewall collector and catalog entries.</summary>
    Firewall,
    Other
}

/// <summary>
/// Windows configuration layer that produced an observed value.
/// Used for effective-state resolution (User vs MachinePolicy vs alternate stores).
/// </summary>
public enum ConfigurationLayer
{
    Unknown = 0,
    UserPreference,
    MachinePolicy,
    AlternatePolicyStore,
    MDM,
    SecurityBaseline
}

/// <summary>
/// Confidence that an EffectiveState was determined correctly.
/// </summary>
public enum EffectiveConfidence
{
    Unknown = 0,
    Low,
    Medium,
    High
}

/// <summary>
/// Kind of relationship between two managed settings.
/// </summary>
public enum RelationshipKind
{
    Related = 0,
    Overrides,
    OverriddenBy,
    ConflictsWith,
    Requires,
    DependsOn,
    SameFeatureAlternatePath
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
