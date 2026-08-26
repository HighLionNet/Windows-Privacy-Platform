namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// The complete non-registry allowlist. Nothing discovered at runtime can add an entry here.
/// </summary>
public static class CuratedWriteAuthorizations
{
    public static IReadOnlyDictionary<string, WritableTarget> Targets { get; } = BuildTargets();

    public static bool TryCreateTarget(string objectId, out WritableTarget? target)
    {
        if (Targets.TryGetValue(objectId, out var found))
        {
            target = Clone(found);
            return true;
        }

        target = null;
        return false;
    }

    private static IReadOnlyDictionary<string, WritableTarget> BuildTargets()
    {
        var targets = new Dictionary<string, WritableTarget>(StringComparer.OrdinalIgnoreCase);

        // Non-core telemetry service; startup and running state are individually reversible.
        targets["service.diagtrack"] = Native(WritableTargetKind.Service, "DiagTrack",
            ["Startup:Automatic", "Startup:Manual", "Startup:Disabled", "State:Running", "State:Stopped"]);
        // Device-management messaging service; disabling it does not remove device-management configuration and is reversible.
        targets["service.dmwappushservice"] = Native(WritableTargetKind.Service, "dmwappushservice",
            ["Startup:Automatic", "Startup:Manual", "Startup:Disabled", "State:Running", "State:Stopped"]);

        // Microsoft compatibility telemetry task; enable/disable preserves the task definition.
        targets["task.applicationexperience.compatibilityappraiser"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser", ["Enabled", "Disabled"]);
        // Program inventory telemetry task; enable/disable is reversible and leaves its action untouched.
        targets["task.applicationexperience.programdataupdater"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Application Experience\ProgramDataUpdater", ["Enabled", "Disabled"]);
        // Customer Experience Improvement Program aggregation task; not required for core OS stability.
        targets["task.ceip.consolidator"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator", ["Enabled", "Disabled"]);
        // USB participation telemetry task; not required for device enumeration or driver installation.
        targets["task.ceip.usbceip"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip", ["Enabled", "Disabled"]);
        // Offline Maps notification task; disabling affects Maps maintenance only and is reversible.
        targets["task.maps.mapstoasttask"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Maps\MapsToastTask", ["Enabled", "Disabled"]);
        // Offline Maps update task; disabling affects automatic map refresh only and is reversible.
        targets["task.maps.mapsupdatetask"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Maps\MapsUpdateTask", ["Enabled", "Disabled"]);
        // Windows Error Reporting queue task; disabling does not disable local crash handling.
        targets["task.wer.queuereporting"] = Native(WritableTargetKind.ScheduledTask, @"\Microsoft\Windows\Windows Error Reporting\QueueReporting", ["Enabled", "Disabled"]);

        // Consumer inbox apps below are per-user, independently removable, and reinstallable through Microsoft Store.
        targets["appx.bingnews"] = Appx("Microsoft.BingNews", "Reinstall Microsoft Start from Microsoft Store.");
        targets["appx.bingweather"] = Appx("Microsoft.BingWeather", "Reinstall MSN Weather from Microsoft Store.");
        targets["appx.gethelp"] = Appx("Microsoft.GetHelp", "Reinstall Get Help from Microsoft Store.");
        targets["appx.getstarted"] = Appx("Microsoft.Getstarted", "Reinstall Tips from Microsoft Store when available.");
        targets["appx.solitaire"] = Appx("Microsoft.MicrosoftSolitaireCollection", "Reinstall Microsoft Solitaire Collection from Microsoft Store.");
        targets["appx.feedbackhub"] = Appx("Microsoft.WindowsFeedbackHub", "Reinstall Feedback Hub from Microsoft Store.");
        targets["appx.xboxoverlay"] = Appx("Microsoft.XboxGamingOverlay", "Reinstall Xbox Game Bar from Microsoft Store.");

        // Optional client components below are independently reversible through Windows servicing.
        targets["feature.xpsservices"] = Native(WritableTargetKind.OptionalFeature, "Printing-XPSServices-Features", ["Enabled", "Disabled"]);
        targets["feature.workfolders"] = Native(WritableTargetKind.OptionalFeature, "WorkFolders-Client", ["Enabled", "Disabled"]);
        targets["feature.mediaplayback"] = Native(WritableTargetKind.OptionalFeature, "MediaPlayback", ["Enabled", "Disabled"]);
        targets["feature.windowsmediaplayer"] = Native(WritableTargetKind.OptionalFeature, "WindowsMediaPlayer", ["Enabled", "Disabled"]);

        AddFirewallTargets(targets, "domain", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile");
        AddFirewallTargets(targets, "private", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile");
        AddFirewallTargets(targets, "public", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile");

        return targets;
    }

    private static WritableTarget Native(WritableTargetKind kind, string identifier, List<string> values) => new()
    {
        Kind = kind,
        Identifier = identifier,
        SupportedRawValues = values,
        SupportsDeletion = false,
        RequiresElevation = kind != WritableTargetKind.AppxPackage,
        Notes = "Explicit curated authorization; runtime discovery cannot expand this allowlist."
    };

    private static WritableTarget Appx(string identifier, string hint)
    {
        var target = Native(WritableTargetKind.AppxPackage, identifier, ["Remove"]);
        target.RecoveryHint = hint;
        return target;
    }

    private static void AddFirewallTargets(IDictionary<string, WritableTarget> targets, string profile, string subKey)
    {
        targets[$"firewall.profile.{profile}.enabled"] = Registry(subKey, "EnableFirewall", ["0", "1"]);
        targets[$"firewall.profile.{profile}.inbound"] = Registry(subKey, "DefaultInboundAction", ["0", "1"]);
        targets[$"firewall.profile.{profile}.outbound"] = Registry(subKey, "DefaultOutboundAction", ["0", "1"]);
        targets[$"firewall.profile.{profile}.notifications"] = Registry(subKey, "DisableNotifications", ["0", "1"]);
    }

    private static WritableTarget Registry(string subKey, string valueName, List<string> values) => new()
    {
        Kind = WritableTargetKind.Registry,
        Hive = "HKLM",
        View = RegistryViewKind.Registry64,
        SubKey = subKey,
        ValueName = valueName,
        ValueKind = RegistryValueKindExpected.DWord,
        SupportedRawValues = values,
        SupportsDeletion = true,
        RequiresElevation = true,
        Notes = "Explicit firewall-profile authorization; individual rules remain excluded."
    };

    private static WritableTarget Clone(WritableTarget source) => new()
    {
        Kind = source.Kind,
        Hive = source.Hive,
        View = source.View,
        SubKey = source.SubKey,
        ValueName = source.ValueName,
        ValueKind = source.ValueKind,
        SupportedRawValues = source.SupportedRawValues.ToList(),
        SupportsDeletion = source.SupportsDeletion,
        RequiresElevation = source.RequiresElevation,
        Notes = source.Notes,
        Identifier = source.Identifier,
        RecoveryHint = source.RecoveryHint
    };
}
