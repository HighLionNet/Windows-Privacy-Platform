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
