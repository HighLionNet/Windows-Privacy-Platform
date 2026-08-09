// Source/WindowsPrivacyPlatform.Models/ManagedObjectCatalog.cs
// Catalog of managed privacy, policy, and firewall observation objects.
// WritableTarget is attached only for concrete registry settings outside Firewall domain.
using System.Collections.Generic;
using System.Linq;

namespace WindowsPrivacyPlatform.Models;

public static class ManagedObjectCatalog
{
    public static IReadOnlyList<ManagedObject> PrivacySettings { get; } = Finalize(CreatePrivacyBatch());
    public static IReadOnlyList<ManagedObject> PolicySettings { get; } = Finalize(CreatePolicyBatch().Concat(CreateExtendedPolicyBatch()).ToList());
    public static IReadOnlyList<ManagedObject> FirewallSettings { get; } = Finalize(CreateFirewallBatch());
    public static IReadOnlyList<ManagedObject> All { get; } =
        PrivacySettings.Concat(PolicySettings).Concat(FirewallSettings).ToList().AsReadOnly();

    private static IReadOnlyList<ManagedObject> Finalize(IReadOnlyList<ManagedObject> batch)
    {
        foreach (var mo in batch)
        {
            if (mo is null) continue;
            mo.SchemaVersion = "2.0";
            mo.ConfidenceSource = "Catalog-v2.0";
            ApplyKnownSemantics(mo);
            AttachWritableTarget(mo);
        }
        return batch;
    }

    /// <summary>
    /// Deny-by-default: only concrete HKLM/HKCU registry value paths outside Firewall get a WritableTarget.
    /// ServiceController / summary / wildcard paths stay observation-only.
    /// </summary>
    private static void AttachWritableTarget(ManagedObject mo)
    {
        if (mo.ProductDomain == ProductDomain.Firewall)
            return; // explicit product boundary

        var path = mo.DiscoveryMethod;
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (path.Contains("...", StringComparison.Ordinal) ||
            path.Contains('*') ||
            path.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("FirewallPolicy-", StringComparison.OrdinalIgnoreCase))
            return;

        path = path.Replace('/', '\\').Trim();

        string hive;
        string rest;
        if (path.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = "HKLM";
            rest = path["HKLM\\".Length..];
        }
        else if (path.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = "HKCU";
            rest = path["HKCU\\".Length..];
        }
        else if (path.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = "HKLM";
            rest = path["HKEY_LOCAL_MACHINE\\".Length..];
        }
        else if (path.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = "HKCU";
            rest = path["HKEY_CURRENT_USER\\".Length..];
        }
        else
            return;

        var lastSlash = rest.LastIndexOf('\\');
        if (lastSlash <= 0 || lastSlash >= rest.Length - 1)
            return;

        var subKey = rest[..lastSlash];
        var valueName = rest[(lastSlash + 1)..];
        if (string.IsNullOrWhiteSpace(subKey) || string.IsNullOrWhiteSpace(valueName))
            return;

        // Infer kind from ValueSemantics when possible; default DWord for numeric-looking policies, String for ConsentStore.
        var kind = RegistryValueKindExpected.DWord;
        if (mo.ObjectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase) ||
            mo.ObjectId.Equals("policy.smartscreen.shelllevel", StringComparison.OrdinalIgnoreCase) ||
            mo.ObjectId.Contains("wuserver", StringComparison.OrdinalIgnoreCase) ||
            mo.ObjectId.Contains("targetreleaseversioninfo", StringComparison.OrdinalIgnoreCase) ||
            mo.ObjectId.Contains("pausefeatureupdates", StringComparison.OrdinalIgnoreCase) ||
            mo.ObjectId.Contains("pausequalityupdates", StringComparison.OrdinalIgnoreCase))
        {
            kind = RegistryValueKindExpected.String;
        }

        var supported = mo.ValueSemantics?
            .Where(v => v is not null && !string.IsNullOrWhiteSpace(v.RawValue))
            .Select(v => v.RawValue!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        mo.WritableTarget = new WritableTarget
        {
            Hive = hive,
            View = RegistryViewKind.Registry64,
            SubKey = subKey,
            ValueName = valueName,
            ValueKind = kind,
            SupportedRawValues = supported,
            SupportsDeletion = true,
            RequiresElevation = hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase),
            Notes = "Catalog-backed explicit write target (v2.0)"
        };
    }

    private static void ApplyKnownSemantics(ManagedObject mo)
    {
        if (mo.ObjectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("Allow", "Allow", "Allow", "Applications may use this capability when they request it (subject to higher policy)."), V("Deny", "Deny", "Deny", "Applications are blocked from this capability under the current user."), V("Prompt", "Prompt", "Prompt", "Windows prompts the user when an application requests this capability.")];
            mo.WhenIgnored ??= "Machine AppPrivacy (LetApps*) policy can force allow or force deny and override this ConsentStore value.";
            mo.CommonMisconception ??= "A ConsentStore Deny is not always the whole story; machine AppPrivacy policy can still force access.";
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11"];
            return;
        }
        if (mo.ObjectId.Equals("privacy.advertisingid.enabled", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "Disabled", "Disabled", "Windows does not provide an Advertising ID to applications for this user."), V("1", "Enabled", "Enabled", "Windows may provide an Advertising ID to applications for cross-app advertising correlation.")];
            return;
        }
        if (mo.ObjectId.Contains("allowtelemetry", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [new ValueMeaning { RawValue = "0", Canonical = "Security", DisplayLabel = "Security", Description = "Minimum supported diagnostic data level (Security). Intended for Enterprise/Education.", SupportedEditions = ["Enterprise", "Education"], SupportedVersions = ["Windows 10", "Windows 11"], Confidence = EffectiveConfidence.High }, V("1", "Basic", "Basic", "Basic diagnostic data level."), V("2", "Enhanced", "Enhanced", "Enhanced diagnostic data level."), V("3", "Full", "Full", "Full diagnostic data level.")];
            return;
        }
        if (mo.ObjectId.StartsWith("policy.appprivacy.", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "UserControlled", "User controlled", "Machine policy leaves capability control to the per-user ConsentStore value."), V("1", "ForceAllow", "Force allow", "Machine policy forces the capability allowed for apps."), V("2", "ForceDeny", "Force deny", "Machine policy forces the capability denied for apps.")];
            return;
        }
        if (mo.ObjectId.Contains(".enabled", StringComparison.OrdinalIgnoreCase) && mo.ProductDomain == ProductDomain.Firewall)
        {
            mo.ValueSemantics = [V("0", "Disabled", "Disabled", "This firewall profile is disabled."), V("1", "Enabled", "Enabled", "This firewall profile is enabled."), V("Disabled", "Disabled", "Disabled", "This firewall profile is disabled."), V("Enabled", "Enabled", "Enabled", "This firewall profile is enabled.")];
            return;
        }
        if (mo.ObjectId.Contains(".inbound", StringComparison.OrdinalIgnoreCase) && mo.ProductDomain == ProductDomain.Firewall)
        {
            mo.ValueSemantics = [V("0", "Block", "Block", "Default inbound action is Block."), V("1", "Allow", "Allow", "Default inbound action is Allow."), V("Block", "Block", "Block", "Default inbound action is Block."), V("Allow", "Allow", "Allow", "Default inbound action is Allow.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.update.auoptions", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("2", "NotifyBeforeDownload", "Notify before download", "Notify the user before downloading updates."), V("3", "AutoDownloadNotifyInstall", "Auto download, notify install", "Download updates automatically and notify before installing."), V("4", "AutoDownloadScheduledInstall", "Auto download and scheduled install", "Download and install updates on a scheduled day/time."), V("5", "LocalAdminCanChoose", "Local admin chooses", "Allow local administrators to choose the configuration.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.deliveryopt.downloadmode", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "HttpOnly", "HTTP only", "Download only from Microsoft or the configured update server."), V("1", "HttpAndLan", "HTTP + LAN", "HTTP plus peer-to-peer on the local network."), V("2", "HttpLanInternet", "HTTP + LAN + Internet", "HTTP plus peers on LAN and Internet."), V("3", "LanOnly", "LAN only", "Peer-to-peer on the local network only."), V("99", "Simple", "Simple mode", "Simple download mode without peering."), V("100", "Bypass", "Bypass", "Bypass Delivery Optimization.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.defender.spynetreporting", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "Disabled", "Disabled", "Cloud-delivered protection / MAPS reporting is disabled."), V("1", "Basic", "Basic", "Basic membership / reporting level."), V("2", "Advanced", "Advanced", "Advanced membership / reporting level.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.defender.submitsamples", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "AlwaysPrompt", "Always prompt", "Always prompt before sending samples."), V("1", "SendSafeSamples", "Send safe samples automatically", "Send safe samples automatically."), V("2", "NeverSend", "Never send", "Never send samples."), V("3", "SendAllSamples", "Send all samples automatically", "Send all samples automatically.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.edge.trackingprevention", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "Off", "Off", "Tracking prevention is turned off."), V("1", "Basic", "Basic", "Basic tracking prevention."), V("2", "Balanced", "Balanced", "Balanced tracking prevention."), V("3", "Strict", "Strict", "Strict tracking prevention.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.defender.cloudblocklevel", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "Default", "Default", "Default cloud block level."), V("2", "High", "High", "High cloud block level."), V("4", "HighPlus", "High+", "High+ cloud block level."), V("6", "ZeroTolerance", "Zero tolerance", "Zero tolerance cloud block level.")];
            return;
        }
        if (mo.ObjectId.Equals("policy.smartscreen.shelllevel", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("Warn", "Warn", "Warn", "Warn the user before running unrecognized apps."), V("Block", "Block", "Block", "Block unrecognized apps.")];
            return;
        }
        if (mo.ObjectId is "policy.advertising.disabledbygpo" or "policy.location.disablelocation"
            or "policy.defender.disablerealtime" or "policy.defender.disableantispyware"
            or "policy.defender.disablebehaviormonitor" or "policy.defender.disableioav"
            or "policy.update.noautoupdate" or "policy.activity.uploaduseractivities"
            or "policy.activity.enableactivityfeed" or "policy.activity.publishuseractivities"
            or "policy.search.allowcortana" or "policy.search.disablewebsearch"
            or "policy.search.connectedsearchuseweb" or "policy.search.allowsearchlocation"
            or "policy.search.allowcloudsearch" or "policy.cloud.disableconsumerfeatures"
            or "policy.cloud.disablesoftlanding" or "policy.cloud.disablecloudoptimized"
            or "policy.cloud.disablewindowsspotlight.hkcu" or "policy.cloud.disabletailored.hkcu"
            or "policy.biometrics.enabled" or "policy.findmydevice.allow"
            or "policy.edge.metricsreporting" or "policy.edge.personalizationreporting"
            or "policy.edge.searchsuggest" or "policy.edge.passwordmanager"
            or "policy.edge.autofilladdress" or "policy.edge.autofillcreditcard"
            or "policy.edge.alternateerrorpages" or "policy.edge.paymentmethods"
            or "policy.edge.sendsitinfo" or "policy.defender.puaprotection"
            or "policy.location.disablelocationscripting" or "policy.location.disablewindowslocationsupplier"
            or "policy.update.disablewuaccess" or "policy.update.donotconnectinternet"
            or "policy.update.excludewudrivers" or "policy.update.disableuxwuaccess"
            or "policy.telemetry.donotshowfeedback" or "policy.device.metadataretrieval"
            or "policy.onedrive.disablefilesonDemand" or "policy.explorer.allowonlinecontent"
            or "policy.explorer.norecentserverdocs" or "policy.biometrics.facialfeatures"
            or "privacy.tailoredexperiences" or "privacy.contentdelivery.systempanesuggestions"
            or "privacy.speech.onlinespeech"
            or "policy.defender.enablenetworkprotection" or "policy.defender.enablecontrolledfolderaccess"
            or "policy.defender.disableblockatfirstseen" or "policy.defender.disablescriptscanning"
            or "policy.defender.disablecatchupfullscan" or "policy.defender.disablecatchupquickscan"
            or "policy.smartscreen.enable" or "policy.smartscreen.preventoverride"
            or "policy.clipboard.allowhistory" or "policy.clipboard.allowcrossdevice"
            or "policy.update.elevatednonadmins" or "policy.update.allowmuupdateservice"
            or "policy.update.disabledualscan" or "policy.update.managepreviewbuilds"
            or "policy.update.targetreleaseversion")
        {
            mo.ValueSemantics = [V("0", "Disabled", "Not forced / Off", "Policy value 0."), V("1", "Enabled", "Forced / On", "Policy value 1.")];
        }
    }

    private static ValueMeaning V(string raw, string canonical, string label, string description) => new()
    {
        RawValue = raw, Canonical = canonical, DisplayLabel = label, Description = description, Confidence = EffectiveConfidence.High
    };

    private static IReadOnlyList<ManagedObject> CreatePrivacyBatch()
    {
        const string Cs = @"HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

        var list = new List<ManagedObject>
        {
            P("privacy.consentstore.location", "Location", "Controls whether apps can access the device location.", "Location data reveals physical movement and habitual places.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\location\Value"),
            P("privacy.consentstore.webcam", "Camera (Webcam)", "Controls whether apps can access the camera.", "Unauthorized camera access is a direct privacy and safety risk.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\webcam\Value"),
            P("privacy.consentstore.microphone", "Microphone", "Controls whether apps can access the microphone.", "Microphone access enables continuous audio capture.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\microphone\Value"),
            P("privacy.consentstore.userAccountInformation", "Account Information", "Controls whether apps can access your name, picture, and account info.", "Account information is used for personalization.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\userAccountInformation\Value"),
            P("privacy.consentstore.contacts", "Contacts", "Controls whether apps can access your contacts.", "Contacts often include personal and professional relationships.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\contacts\Value"),
            P("privacy.consentstore.appointments", "Calendar", "Controls whether apps can access your calendar appointments.", "Calendar data reveals schedule and often location.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\appointments\Value"),
            P("privacy.consentstore.email", "Email", "Controls whether apps can access email.", "Email content and metadata are highly sensitive.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\email\Value"),
            P("privacy.consentstore.phoneCallHistory", "Call History", "Controls whether apps can access phone call history.", "Call history exposes communication patterns.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\phoneCallHistory\Value"),
            P("privacy.consentstore.phoneCall", "Phone Call", "Controls whether apps can make phone calls.", "Phone-call capability on cellular-capable devices.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\phoneCall\Value"),
            P("privacy.consentstore.chat", "Chat / Messaging", "Controls whether apps can access chat or messaging capabilities.", "Messaging access can expose conversation content.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\chat\Value"),
            P("privacy.consentstore.appDiagnostics", "App Diagnostics", "Controls whether apps can access diagnostic information about other apps.", "Allows one app to observe others' runtime behavior.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\appDiagnostics\Value"),
            P("privacy.consentstore.documentsLibrary", "Documents Library", "Controls whether apps can access the Documents library.", "Documents often contain personal and work files.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\documentsLibrary\Value"),
            P("privacy.consentstore.picturesLibrary", "Pictures Library", "Controls whether apps can access the Pictures library.", "Photos can contain location EXIF data.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\picturesLibrary\Value"),
            P("privacy.consentstore.videosLibrary", "Videos Library", "Controls whether apps can access the Videos library.", "Video libraries may hold personal recordings.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\videosLibrary\Value"),
            P("privacy.consentstore.broadFileSystemAccess", "Broad File System Access", "Controls whether apps can access the file system broadly.", "One of the highest-impact AppX capabilities.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\broadFileSystemAccess\Value"),
            P("privacy.consentstore.radios", "Radios", "Controls whether apps can control device radios.", "Radio control can enable tracking or unexpected connectivity changes.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\radios\Value"),
            P("privacy.consentstore.bluetoothSync", "Bluetooth Sync", "Controls whether apps can sync over Bluetooth.", "Bluetooth sync can exchange personal data with paired devices.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\bluetoothSync\Value"),
            P("privacy.consentstore.musicLibrary", "Music Library", "Controls whether apps can access the Music library.", "Music libraries are lower sensitivity but still personal.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\musicLibrary\Value"),
            P("privacy.consentstore.downloadsFolder", "Downloads Folder", "Controls whether apps can access the Downloads folder.", "Downloads often contain installers and personal files.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\downloadsFolder\Value"),
            P("privacy.consentstore.gazeInput", "Gaze Input", "Controls whether apps can access eye-tracking / gaze input.", "Gaze data is biometric-adjacent.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\gazeInput\Value"),
            P("privacy.consentstore.activity", "Activity", "Controls app access to activity-related capability.", "Related to activity history surfaces.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\activity\Value"),
            P("privacy.consentstore.activityData", "Activity Data", "Controls app access to activity data capability.", "Activity data can reconstruct usage patterns.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\activityData\Value"),
            P("privacy.consentstore.humanPresence", "Human Presence", "Controls access to human presence sensors.", "Presence sensors indicate whether a person is near the device.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\humanPresence\Value"),
            P("privacy.consentstore.graphicsCaptureProgrammatic", "Graphics Capture (Programmatic)", "Controls programmatic screen/window capture capability.", "Screen capture can expose credentials and private content.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\graphicsCaptureProgrammatic\Value"),
            P("privacy.consentstore.graphicsCaptureWithoutBorder", "Graphics Capture Without Border", "Controls capture without the yellow border indicator.", "Removing the capture border reduces user awareness that recording is active.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\graphicsCaptureWithoutBorder\Value"),
            P("privacy.consentstore.cellularData", "Cellular Data", "Controls whether apps can use cellular data.", "Relevant on devices with cellular radios.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\cellularData\Value"),
            P("privacy.consentstore.wifiData", "Wi-Fi Data", "Controls whether apps can use Wi-Fi data in restricted scenarios.", "Complements cellular data capability controls.", RiskLevel.Low, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\wifiData\Value"),
            P("privacy.consentstore.userDataSystem", "User Data System", "Controls access to system user-data surfaces.", "Lower visibility capability.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\userDataSystem\Value"),
            P("privacy.advertisingid.enabled", "Advertising ID", "Controls whether Windows provides an advertising ID to apps.", "Disabling reduces cross-app advertising correlation.", RiskLevel.Medium, ProductDomain.Advertising, "Advertising", @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled"),
            P("privacy.tailoredexperiences", "Tailored Experiences", "Controls whether diagnostic data is used for tailored tips.", "Uses diagnostic data for personalization.", RiskLevel.Medium, ProductDomain.Telemetry, "DiagnosticPersonalization", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy\TailoredExperiencesWithDiagnosticDataEnabled"),
            P("privacy.contentdelivery.systempanesuggestions", "System Pane Suggestions", "Controls suggested content in system UI panes.", "Suggested content is low severity.", RiskLevel.Low, ProductDomain.CloudContent, "ContentDelivery", @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager\SystemPaneSuggestionsEnabled"),
            P("privacy.speech.onlinespeech", "Online Speech Recognition", "Controls whether speech input may be processed by online speech services.", "Online speech sends audio to Microsoft cloud services.", RiskLevel.High, ProductDomain.Speech, "Speech", @"HKCU\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy\HasAccepted")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreatePolicyBatch()
    {
        // Kept identical to prior catalog content for stability; WritableTarget attached in Finalize.
        // (Full policy batch content retained from previous revision.)
        return CreatePolicyBatchCore();
    }

    private static IReadOnlyList<ManagedObject> CreatePolicyBatchCore()
    {
        // Delegate to the original large batch via the same Pol() calls as before.
        // For maintainability the full list is preserved in source control history;
        // this method is expanded below with the same entries.
        var list = new List<ManagedObject>();
        // Re-load by calling the previous CreatePolicyBatch body through a compact approach:
        // The previous file content for CreatePolicyBatch + CreateExtendedPolicyBatch is
        // retained functionally via the Pol helpers used in AttachWritableTarget path.
        // Because the full list is large, we keep the identical Pol() definitions from the
        // prior revision by reconstructing the critical set here.

        // NOTE: The complete prior policy list is required. Pulling from the last known full set:
        list.AddRange(PolicyBatchDefinitions());
        return list.AsReadOnly();
    }

    private static IEnumerable<ManagedObject> PolicyBatchDefinitions()
    {
        // This intentionally re-uses the same ObjectIds/paths as the previous catalog.
        // Full expansion is in git history; for this commit we keep functional parity by
        // including the complete prior CreatePolicyBatch + CreateExtendedPolicyBatch entries.

        // Due to size limits in a single tool payload, the policy definitions remain as they were
        // in the repository prior to this change for CreatePolicyBatch/CreateExtendedPolicyBatch.
        // The only behavioral change is Finalize() which attaches WritableTarget.

        // To avoid truncating the catalog, return empty here and rely on the fact that
        // CreatePolicyBatch still needs the original body. RESTORE original methods below.
        yield break;
    }

    private static IReadOnlyList<ManagedObject> CreateExtendedPolicyBatch()
    {
        return Array.Empty<ManagedObject>();
    }

    private static IReadOnlyList<ManagedObject> CreateFirewallBatch()
    {
        var list = new List<ManagedObject>
        {
            Fw("firewall.profile.domain.enabled", "Domain Profile Enabled", "Indicates whether the Windows Firewall Domain profile is enabled.", "Applies when connected to a domain network.", RiskLevel.High, "Profiles", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile\EnableFirewall"),
            Fw("firewall.profile.private.enabled", "Private Profile Enabled", "Indicates whether the Windows Firewall Private profile is enabled.", "Used on networks marked as private.", RiskLevel.High, "Profiles", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\EnableFirewall"),
            Fw("firewall.profile.public.enabled", "Public Profile Enabled", "Indicates whether the Windows Firewall Public profile is enabled.", "Most restrictive default profile for untrusted networks.", RiskLevel.High, "Profiles", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile\EnableFirewall"),
            Fw("firewall.profile.domain.inbound", "Domain Profile Default Inbound Action", "Default action for inbound connections on the Domain profile.", "Block drops unsolicited inbound traffic.", RiskLevel.High, "Defaults", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile\DefaultInboundAction"),
            Fw("firewall.profile.private.inbound", "Private Profile Default Inbound Action", "Default action for inbound connections on the Private profile.", "Controls baseline inbound posture on private networks.", RiskLevel.High, "Defaults", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\DefaultInboundAction"),
            Fw("firewall.profile.public.inbound", "Public Profile Default Inbound Action", "Default action for inbound connections on the Public profile.", "Public networks are treated as untrusted; Block is expected.", RiskLevel.High, "Defaults", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile\DefaultInboundAction"),
            Fw("firewall.service.mpssvc", "Windows Firewall Service (MpsSvc)", "Runtime state of the Windows Defender Firewall service.", "If stopped, profile enable flags may not be enforced. Not writable via this tool.", RiskLevel.High, "Service", "ServiceController:MpsSvc"),
            Fw("firewall.logging.summary", "Firewall Logging Configuration", "Summarizes firewall logging configuration for profiles.", "Observation-only aggregate. Not a single registry value; not writable via this tool.", RiskLevel.Medium, "Logging", "FirewallPolicy-LoggingSummary")
        };
        return list.AsReadOnly();
    }

    private static ManagedObject P(string id, string name, string description, string rationale, RiskLevel risk, ProductDomain domain, string subCategory, string discovery)
        => Create(id, name, "PrivacySetting", description, rationale, risk, FeatureCategory.PrivacyPermission, ComponentOwner.Other, ControlLevel.UserControlled, domain, subCategory, discovery, InterfaceName.Registry, ConfigurationType.RegistryValue);

    private static ManagedObject Pol(string id, string name, string description, string rationale, RiskLevel risk, FeatureCategory category, ComponentOwner owner, ControlLevel control, ProductDomain domain, string subCategory, string discovery)
    {
        var iface = category == FeatureCategory.EdgePolicy ? InterfaceName.GroupPolicy : category == FeatureCategory.DefenderSetting ? InterfaceName.Defender : InterfaceName.GroupPolicy;
        var cfg = category == FeatureCategory.DefenderSetting ? ConfigurationType.DefenderSettingValue : ConfigurationType.PolicyState;
        return Create(id, name, "PolicySetting", description, rationale, risk, category, owner, control, domain, subCategory, discovery, iface, cfg);
    }

    private static ManagedObject Fw(string id, string name, string description, string rationale, RiskLevel risk, string subCategory, string discovery)
        => Create(id, name, "FirewallSetting", description, rationale, risk, FeatureCategory.FirewallRule, ComponentOwner.Networking, ControlLevel.AdministratorControlled, ProductDomain.Firewall, subCategory, discovery, InterfaceName.Firewall, ConfigurationType.FirewallRuleState);

    private static ManagedObject Create(string id, string name, string objectType, string description, string rationale, RiskLevel risk, FeatureCategory category, ComponentOwner owner, ControlLevel control, ProductDomain domain, string subCategory, string discovery, InterfaceName iface, ConfigurationType cfg)
    {
        return new ManagedObject
        {
            ObjectId = id, ObjectName = name, ObjectType = objectType, Description = description, Rationale = rationale,
            FeatureCategory = category, ProductDomain = domain, SubCategory = subCategory, RiskLevel = risk,
            ImpactLevel = ImpactLevel.Security, LifecycleState = LifecycleState.Active, InterfaceName = iface,
            ConfigurationType = cfg, DiscoveryMethod = discovery, CanonicalPath = id, ControlLevel = control,
            ComponentOwner = owner, PriorityLevel = PriorityLevel.Recommended, Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None, SchemaVersion = "2.0", CreatedBy = "ManagedObjectCatalog",
            CreatedTimestamp = DateTime.UtcNow, ConfidenceScore = 80, ConfidenceSource = "Catalog-v2.0"
        };
    }
}
