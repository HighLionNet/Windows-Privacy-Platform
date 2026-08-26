namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Explicit, catalog-backed authorization to modify a setting.
/// DEFAULT is no WritableTarget = NOT WRITABLE.
/// DiscoveryMethod / Observation paths never authorize writes by themselves.
/// </summary>
public sealed class WritableTarget
{
    /// <summary>The typed system surface authorized by this target.</summary>
    public WritableTargetKind Kind { get; set; } = WritableTargetKind.Registry;

    /// <summary>HKLM or HKCU (or full hive name).</summary>
    public string Hive { get; set; } = string.Empty;

    /// <summary>Registry view to use for both read and write.</summary>
    public RegistryViewKind View { get; set; } = RegistryViewKind.Registry64;

    /// <summary>Subkey path without hive prefix (e.g. SOFTWARE\Policies\Microsoft\Windows\System).</summary>
    public string SubKey { get; set; } = string.Empty;

    /// <summary>Value name inside the key.</summary>
    public string ValueName { get; set; } = string.Empty;

    /// <summary>Expected registry value kind. Writes must use this kind; no guessing.</summary>
    public RegistryValueKindExpected ValueKind { get; set; } = RegistryValueKindExpected.DWord;

    /// <summary>Raw values that may be written. Empty = any value matching ValueKind is allowed (rare).</summary>
    public List<string> SupportedRawValues { get; set; } = new();

    /// <summary>Whether deleting the value (Not configured) is allowed.</summary>
    public bool SupportsDeletion { get; set; } = true;

    /// <summary>Whether elevation is required (almost always true for HKLM).</summary>
    public bool RequiresElevation { get; set; } = true;

    /// <summary>Optional human note shown in confirmation.</summary>
    public string? Notes { get; set; }

    /// <summary>Service name, task path, package name, or optional-feature name for non-registry targets.</summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>Optional end-user recovery/reinstall guidance included in confirmation.</summary>
    public string? RecoveryHint { get; set; }

    /// <summary>True when this target is fully specified and usable.</summary>
    public bool IsComplete => Kind switch
    {
        WritableTargetKind.Registry =>
            !string.IsNullOrWhiteSpace(Hive) &&
            !string.IsNullOrWhiteSpace(SubKey) &&
            !string.IsNullOrWhiteSpace(ValueName) &&
            ValueKind != RegistryValueKindExpected.Unsupported,
        WritableTargetKind.Service or
        WritableTargetKind.ScheduledTask or
        WritableTargetKind.AppxPackage or
        WritableTargetKind.OptionalFeature =>
            !string.IsNullOrWhiteSpace(Identifier) && SupportedRawValues.Count > 0,
        _ => false
    };
}

public enum WritableTargetKind
{
    Registry = 0,
    Service,
    ScheduledTask,
    AppxPackage,
    OptionalFeature
}

public enum RegistryViewKind
{
    Registry64 = 0,
    Registry32 = 1,
    Default = 2
}

public enum RegistryValueKindExpected
{
    DWord = 0,
    QWord = 1,
    String = 2,
    ExpandString = 3,
    /// <summary>Refused cleanly because this registry kind has no write contract.</summary>
    Unsupported = 99
}

/// <summary>Safe native hand-off for operations intentionally outside the product write boundary.</summary>
public sealed class NativeToolLink
{
    public string Label { get; set; } = string.Empty;
    public string Executable { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Label) &&
        !string.IsNullOrWhiteSpace(Executable);
}
