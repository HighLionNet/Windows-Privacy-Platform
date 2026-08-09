// Source/WindowsPrivacyPlatform.Models/ManagedObjectCatalog.cs
// Catalog of managed privacy, policy, and firewall observation objects.
// DiscoveryMethod values must be concrete (hive + subkey + value) for any writable setting.
using System.Collections.Generic;
using System.Linq;

namespace WindowsPrivacyPlatform.Models;

public static class ManagedObjectCatalog
{
    public static IReadOnlyList<ManagedObject> PrivacySettings { get; } = AttachSemantics(CreatePrivacyBatch());
    public static IReadOnlyList<ManagedObject> PolicySettings { get; } = AttachSemantics(CreatePolicyBatch().Concat(CreateExtendedPolicyBatch()).ToList());
    public static IReadOnlyList<ManagedObject> FirewallSettings { get; } = AttachSemantics(CreateFirewallBatch());
    public static IReadOnlyList<ManagedObject> All { get; } =
        PrivacySettings.Concat(PolicySettings).Concat(FirewallSettings).ToList().AsReadOnly();

    private static IReadOnlyList<ManagedObject> AttachSemantics(IReadOnlyList<ManagedObject> batch)
    {
        foreach (var mo in batch)
        {
            if (mo is null) continue;
            mo.SchemaVersion = "1.6";
            mo.ConfidenceSource = "Catalog-v1.6";
            ApplyKnownSemantics(mo);
        }
        return batch;
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
        // Binary 0/1 polarity for many policies
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
        // Concrete ConsentStore path prefix (HKCU, current user, no elevation required for read).
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
        var list = new List<ManagedObject>
        {
            Pol("policy.telemetry.allowtelemetry", "Allow Telemetry (GPO)", "Sets the diagnostic data level via Group Policy.", "Primary enterprise control for diagnostic data.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowTelemetry"),
            Pol("policy.telemetry.allowtelemetry.currentversion", "Allow Telemetry (CurrentVersion Policies)", "Alternate path for diagnostic data level.", "Same semantic as AllowTelemetry GPO.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection\AllowTelemetry"),
            Pol("policy.telemetry.donotshowfeedback", "Do Not Show Feedback Notifications", "Suppresses feedback reminder notifications.", "Reduces interruption.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\DoNotShowFeedbackNotifications"),
            Pol("policy.telemetry.disablecommercialid", "Allow Device Name In Telemetry", "Controls whether the device name may be included in telemetry.", "Device name can aid correlation.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowDeviceNameInTelemetry"),
            Pol("policy.update.noautoupdate", "No Auto Update", "Disables automatic Windows Update checking/install.", "Stopping automatic updates increases exposure unless another channel exists.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\NoAutoUpdate"),
            Pol("policy.update.auoptions", "AU Options", "Configures automatic update mode.", "Controls how aggressively updates are downloaded and installed.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\AUOptions"),
            Pol("policy.update.scheduledinstallday", "Scheduled Install Day", "Day of week for scheduled update installation.", "Pairs with ScheduledInstallTime.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\ScheduledInstallDay"),
            Pol("policy.update.scheduledinstalltime", "Scheduled Install Time", "Hour of day for scheduled update installation.", "Use off-hours to reduce disruption.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\ScheduledInstallTime"),
            Pol("policy.update.autoinstallminor", "Auto Install Minor Updates", "Controls automatic installation of minor updates.", "Minor updates are often lower risk.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\AutoInstallMinorUpdates"),
            Pol("policy.update.detectionfrequency", "Detection Frequency", "Hours between Windows Update detection cycles.", "Lower values increase check frequency.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\DetectionFrequency"),
            Pol("policy.update.disablewuaccess", "Disable Windows Update Access", "Prevents user access to Windows Update.", "Locks down end-user update UI.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DisableWindowsUpdateAccess"),
            Pol("policy.update.disableuxwuaccess", "Disable UX WU Access", "Blocks access to Windows Update via Settings UX.", "Related to DisableWindowsUpdateAccess.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\SetDisableUXWUAccess"),
            Pol("policy.update.donotconnectinternet", "Do Not Connect to Windows Update Internet Locations", "Blocks contact with public Windows Update endpoints.", "Used with WSUS/offline servicing.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DoNotConnectToWindowsUpdateInternetLocations"),
            Pol("policy.update.excludewudrivers", "Exclude WU Drivers in Quality Update", "Excludes drivers from quality update offers.", "Useful when drivers are managed separately.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\ExcludeWUDriversInQualityUpdate"),
            Pol("policy.update.ux.branchreadiness", "Branch Readiness Level (UX)", "Controls feature update readiness / channel preference.", "Influences when feature updates are offered.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\BranchReadinessLevel"),
            Pol("policy.update.ux.flightsettings", "Flight Settings Max Pause Days", "Maximum days feature/quality updates may be paused.", "Bounds how long updates can be paused.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\FlightSettingsMaxPauseDays"),
            Pol("policy.update.ux.pausefeatureupdatesstart", "Pause Feature Updates Start Time", "Timestamp when feature updates were paused.", "Informational observation of a local pause.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.UserControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\PauseFeatureUpdatesStartTime"),
            Pol("policy.update.ux.pausequalityupdatesstart", "Pause Quality Updates Start Time", "Timestamp when quality updates were paused.", "Informational observation of a local pause.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.UserControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\PauseQualityUpdatesStartTime"),
            Pol("policy.deliveryopt.downloadmode", "Delivery Optimization Download Mode", "Controls peer-to-peer and cloud delivery of updates.", "Restricting to HTTP-only reduces LAN/Internet sharing.", RiskLevel.Medium, FeatureCategory.NetworkSetting, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization\DODownloadMode"),
            Pol("policy.defender.disableantispyware", "Disable AntiSpyware (legacy)", "Legacy policy that can disable Microsoft Defender Antivirus.", "Severe security impact.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\DisableAntiSpyware"),
            Pol("policy.defender.disablerealtime", "Disable Real-Time Monitoring", "Disables Defender real-time protection via policy.", "Real-time protection is core host security.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableRealtimeMonitoring"),
            Pol("policy.defender.disablebehaviormonitor", "Disable Behavior Monitoring", "Disables Defender behavior monitoring via policy.", "Behavior monitoring catches suspicious process activity.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableBehaviorMonitoring"),
            Pol("policy.defender.disableioav", "Disable IOAV Protection", "Disables scanning of downloaded files and attachments.", "IOAV protection scans content as it is written or opened.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableIOAVProtection"),
            Pol("policy.defender.spynetreporting", "MAPS / Spynet Reporting", "Controls cloud-delivered protection reporting level.", "Cloud reporting improves detection but sends metadata.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SpynetReporting"),
            Pol("policy.defender.submitsamples", "Submit Samples Consent", "Controls automatic sample submission to Microsoft.", "Sample submission can include suspicious files.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SubmitSamplesConsent"),
            Pol("policy.defender.puaprotection", "PUA Protection", "Enables blocking of potentially unwanted applications.", "PUA protection reduces adware/bundleware.", RiskLevel.Low, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\PUAProtection"),
            Pol("policy.search.allowcortana", "Allow Cortana", "Enables or disables Cortana via policy.", "Cortana/cloud assistant features increase cloud interaction.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowCortana"),
            Pol("policy.search.disablewebsearch", "Disable Web Search", "Prevents search from querying the web.", "Keeps Start/search queries local.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\DisableWebSearch"),
            Pol("policy.search.connectedsearchuseweb", "Connected Search Use Web", "Controls whether search uses the web.", "Related to cloud search.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\ConnectedSearchUseWeb"),
            Pol("policy.search.allowsearchlocation", "Allow Search To Use Location", "Allows Windows Search to use location.", "Search + location can refine results.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowSearchToUseLocation"),
            Pol("policy.search.allowcloudsearch", "Allow Cloud Search", "Allows Windows Search to use cloud search features.", "Cloud search can return online results.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowCloudSearch"),
            Pol("policy.search.disableindexedlocationsinlib", "Disable Indexed Search", "Policy affecting indexed search behavior.", "Indexing improves local search performance.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\DisableIndexedSearch"),
            Pol("policy.activity.enableactivityfeed", "Enable Activity Feed", "Enables the Timeline / activity feed feature.", "Activity feed stores recent activity.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.ActivityHistory, "ActivityHistory", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableActivityFeed"),
            Pol("policy.activity.publishuseractivities", "Publish User Activities", "Allows activities to be published for Timeline.", "Publishing activities creates local history.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.ActivityHistory, "ActivityHistory", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\PublishUserActivities"),
            Pol("policy.activity.uploaduseractivities", "Upload User Activities", "Allows activities to be uploaded to the cloud.", "Cloud upload of activities is higher privacy impact.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.ActivityHistory, "ActivityHistory", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\UploadUserActivities"),
            Pol("policy.cloud.disableconsumerfeatures", "Disable Windows Consumer Features", "Turns off consumer experiences via policy.", "Reduces Store suggestions and consumer upsell.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableWindowsConsumerFeatures"),
            Pol("policy.cloud.disablesoftlanding", "Disable Soft Landing", "Disables post-update soft landing tips.", "Quietens first-run/post-update experiences.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableSoftLanding"),
            Pol("policy.cloud.disablecloudoptimized", "Disable Cloud Optimized Content", "Disables cloud-optimized content experiences.", "Reduces online content optimizations.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableCloudOptimizedContent"),
            Pol("policy.cloud.disablewindowsspotlight.hkcu", "Disable Windows Spotlight (User)", "Disables Spotlight lock screen for the user.", "Spotlight fetches online imagery.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.UserControlled, ProductDomain.CloudContent, "CloudContent", @"HKCU\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableWindowsSpotlightFeatures"),
            Pol("policy.cloud.disabletailored.hkcu", "Disable Tailored Experiences (User Policy)", "User-policy path to disable tailored experiences.", "Related to the per-user privacy toggle.", RiskLevel.Medium, FeatureCategory.CloudComponent, ComponentOwner.Telemetry, ControlLevel.UserControlled, ProductDomain.CloudContent, "CloudContent", @"HKCU\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableTailoredExperiencesWithDiagnosticData"),
            Pol("policy.advertising.disabledbygpo", "Advertising ID Disabled by Group Policy", "Forces advertising ID off via GPO.", "Stronger than the per-user toggle.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Advertising, "Advertising", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo\DisabledByGroupPolicy"),
            Pol("policy.location.disablelocation", "Disable Location", "Disables the Windows location feature via policy.", "Machine-wide location kill switch.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Location, "Location", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableLocation"),
            Pol("policy.location.disablelocationscripting", "Disable Location Scripting", "Disables location scripting interfaces.", "Limits scripted access to location APIs.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Location, "Location", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableLocationScripting"),
            Pol("policy.location.disablewindowslocationsupplier", "Disable Windows Location Provider", "Disables the Windows location provider.", "Stops the built-in location provider.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Location, "Location", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableWindowsLocationProvider"),
            Pol("policy.appprivacy.location", "Let Apps Access Location (GPO)", "Force-allows, force-denies, or user-controls app location access.", "GPO override for ConsentStore location.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessLocation"),
            Pol("policy.appprivacy.camera", "Let Apps Access Camera (GPO)", "Force policy for app camera access.", "Machine policy override for webcam ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessCamera"),
            Pol("policy.appprivacy.microphone", "Let Apps Access Microphone (GPO)", "Force policy for app microphone access.", "Machine policy override for microphone ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessMicrophone"),
            Pol("policy.appprivacy.accountinfo", "Let Apps Access Account Info (GPO)", "Force policy for app access to account information.", "Machine policy override for userAccountInformation ConsentStore.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessAccountInfo"),
            Pol("policy.appprivacy.contacts", "Let Apps Access Contacts (GPO)", "Force policy for app contacts access.", "Machine policy override for contacts ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessContacts"),
            Pol("policy.appprivacy.calendar", "Let Apps Access Calendar (GPO)", "Force policy for app calendar access.", "Machine policy override for appointments ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessCalendar"),
            Pol("policy.appprivacy.email", "Let Apps Access Email (GPO)", "Force policy for app email access.", "Machine policy override for email ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessEmail"),
            Pol("policy.appprivacy.callhistory", "Let Apps Access Call History (GPO)", "Force policy for app call-history access.", "Machine policy override for phoneCallHistory ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessCallHistory"),
            Pol("policy.appprivacy.messaging", "Let Apps Access Messaging (GPO)", "Force policy for app messaging access.", "Machine policy override for messaging capabilities.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessMessaging"),
            Pol("policy.appprivacy.radios", "Let Apps Access Radios (GPO)", "Force policy for app radio control.", "Machine policy override for radios ConsentStore.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessRadios"),
            Pol("policy.appprivacy.documents", "Let Apps Access Documents (GPO)", "Force policy for app Documents library access.", "Machine policy override for documentsLibrary ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessDocuments"),
            Pol("policy.appprivacy.pictures", "Let Apps Access Pictures (GPO)", "Force policy for app Pictures library access.", "Machine policy override for picturesLibrary ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessPictures"),
            Pol("policy.appprivacy.videos", "Let Apps Access Videos (GPO)", "Force policy for app Videos library access.", "Machine policy override for videosLibrary ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessVideos"),
            Pol("policy.appprivacy.filesystem", "Let Apps Access File System (GPO)", "Force policy for broad filesystem access by apps.", "High-impact capability.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessFileSystem"),
            Pol("policy.appprivacy.appdiagnostics", "Let Apps Get Diagnostic Info (GPO)", "Force policy for app diagnostic information.", "Machine policy override for appDiagnostics ConsentStore.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsGetDiagnosticInfo"),
            Pol("policy.findmydevice.allow", "Allow Find My Device", "Allows the Find My Device feature.", "Find My Device uses location and device registration.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "Device", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice\AllowFindMyDevice"),
            Pol("policy.device.metadataretrieval", "Prevent Device Metadata From Network", "Prevents retrieval of device metadata from the network.", "Stops automatic download of device icons.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "Device", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Device Metadata\PreventDeviceMetadataFromNetwork"),
            Pol("policy.feedback.numberoffeedbacksiuf", "Number of SIUF In Period", "User preference controlling how many feedback prompts appear.", "Affects interruption frequency.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.UserControlled, ProductDomain.Telemetry, "Feedback", @"HKCU\SOFTWARE\Microsoft\Siuf\Rules\NumberOfSIUFInPeriod"),
            Pol("policy.feedback.periodinsiuf", "SIUF Period In Nano Seconds", "User preference for the feedback prompt period.", "Works with NumberOfSIUFInPeriod.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.UserControlled, ProductDomain.Telemetry, "Feedback", @"HKCU\SOFTWARE\Microsoft\Siuf\Rules\PeriodInNanoSeconds"),
            Pol("policy.onedrive.disablefilesonDemand", "Disable OneDrive File Sync (NGSC)", "Disables OneDrive Next Generation Sync Client file sync.", "Used in environments that prohibit consumer OneDrive.", RiskLevel.Medium, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "OneDrive", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive\DisableFileSyncNGSC"),
            Pol("policy.explorer.allowonlinecontent", "Allow Online Tips", "Controls online tips in Explorer / shell.", "Online tips fetch content from Microsoft services.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Explorer, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "Explorer", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\AllowOnlineTips"),
            Pol("policy.explorer.norecentserverdocs", "No Recent Docs History", "Disables recent documents history for the user.", "Reduces local tracking of recently opened documents.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Explorer, ControlLevel.UserControlled, ProductDomain.Other, "Explorer", @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\NoRecentDocsHistory"),
            Pol("policy.biometrics.enabled", "Biometrics Enabled", "Enables or disables Windows biometric framework via policy.", "Disabling biometrics removes Windows Hello face/fingerprint unlock.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Biometrics, "Biometrics", @"HKLM\SOFTWARE\Policies\Microsoft\Biometrics\Enabled"),
            Pol("policy.biometrics.facialfeatures", "Enhanced Anti-Spoofing (Facial)", "Controls enhanced anti-spoofing for facial biometrics.", "Strengthens Windows Hello face resistance to presentation attacks.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Biometrics, "Biometrics", @"HKLM\SOFTWARE\Policies\Microsoft\Biometrics\FacialFeatures\EnhancedAntiSpoofing"),
            Pol("policy.edge.trackingprevention", "Edge Tracking Prevention", "Configures Microsoft Edge tracking prevention level.", "Higher tracking prevention reduces cross-site tracking.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\TrackingPrevention"),
            Pol("policy.edge.metricsreporting", "Edge Metrics Reporting", "Controls Edge metrics/telemetry reporting.", "Disabling reduces Edge diagnostic data shared with Microsoft.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\MetricsReportingEnabled"),
            Pol("policy.edge.personalizationreporting", "Edge Personalization Reporting", "Controls Edge personalization data reporting.", "Related to personalized experiences in Edge.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\PersonalizationReportingEnabled"),
            Pol("policy.edge.searchsuggest", "Edge Search Suggestions", "Enables or disables search suggestions in Edge.", "Search suggestions send partial queries to the suggestion service.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\SearchSuggestEnabled"),
            Pol("policy.edge.passwordmanager", "Edge Password Manager", "Enables or disables the built-in Edge password manager.", "Security/privacy trade-off.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\PasswordManagerEnabled"),
            Pol("policy.edge.autofilladdress", "Edge Autofill Address", "Enables or disables address autofill in Edge.", "Address autofill stores form data for convenience.", RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\AutofillAddressEnabled"),
            Pol("policy.edge.autofillcreditcard", "Edge Autofill Credit Card", "Enables or disables credit-card autofill in Edge.", "Credit-card autofill is higher sensitivity.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\AutofillCreditCardEnabled"),
            Pol("policy.edge.alternateerrorpages", "Edge Alternate Error Pages", "Enables or disables alternate error pages.", "Alternate error pages can send navigation failure context.", RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\AlternateErrorPagesEnabled"),
            Pol("policy.edge.paymentmethods", "Edge Payment Method Query", "Controls whether Edge may query available payment methods.", "Payment method queries can expose payment capability presence.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\PaymentMethodQueryEnabled"),
            Pol("policy.edge.sendsitinfo", "Edge Send Site Info To Improve Services", "Controls whether Edge sends site information to improve Microsoft services.", "Optional diagnostic channel specific to Edge.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\SendSiteInfoToImproveServices")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreateExtendedPolicyBatch()
    {
        var list = new List<ManagedObject>
        {
            Pol("policy.update.deferfeatureupdates", "Defer Feature Updates (Days)", "Number of days to defer feature updates.", "Allows staged feature update rollout. 0–365 typical.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DeferFeatureUpdatesPeriodInDays"),
            Pol("policy.update.deferqualityupdates", "Defer Quality Updates (Days)", "Number of days to defer quality updates.", "Allows staged quality update rollout. 0–30 typical.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DeferQualityUpdatesPeriodInDays"),
            Pol("policy.update.wuserver", "WUServer", "Intranet Microsoft update service location (WSUS).", "Points clients at a WSUS or other intranet update server.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\WUServer"),
            Pol("policy.update.wustatusserver", "WUStatusServer", "Intranet statistics server for Windows Update.", "Usually matches WUServer in WSUS deployments.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\WUStatusServer"),
            Pol("policy.update.targetreleaseversion", "Target Release Version", "Enables targeting of a specific Windows feature release.", "When enabled, TargetReleaseVersionInfo selects the version.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\TargetReleaseVersion"),
            Pol("policy.update.targetreleaseversioninfo", "Target Release Version Info", "Specifies the target Windows feature release (e.g. 23H2).", "Used with TargetReleaseVersion to pin feature updates.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\TargetReleaseVersionInfo"),
            Pol("policy.update.managepreviewbuilds", "Manage Preview Builds", "Controls access to Windows Insider / preview builds.", "Disable to prevent optional preview channel enrollment.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\ManagePreviewBuilds"),
            Pol("policy.update.allowmuupdateservice", "Allow Microsoft Update Service", "Allows updates from Microsoft Update (non-Windows products).", "Enables Office and other Microsoft product updates via WU.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AllowMUUpdateService"),
            Pol("policy.update.elevatednonadmins", "Elevate Non-Admins (AU)", "Allows non-administrators to approve or install updates.", "Relevant in kiosk or shared scenarios.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\ElevateNonAdmins"),
            Pol("policy.update.disabledualscan", "Disable Dual Scan", "Prevents Windows Update from scanning public Microsoft Update when WSUS is configured.", "Keeps clients on the intranet update path only.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DisableDualScan"),

            Pol("policy.defender.enablenetworkprotection", "Enable Network Protection", "Enables Windows Defender Network Protection.", "Blocks outbound connections to malicious hosts.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection\EnableNetworkProtection"),
            Pol("policy.defender.enablecontrolledfolderaccess", "Enable Controlled Folder Access", "Enables Controlled Folder Access (ransomware protection).", "Protects specified folders from unauthorized changes.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access\EnableControlledFolderAccess"),
            Pol("policy.defender.cloudblocklevel", "Cloud Block Level", "Sets the cloud-delivered protection block level.", "Higher levels block more aggressively based on cloud reputation.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine\MpCloudBlockLevel"),
            Pol("policy.defender.disableblockatfirstseen", "Disable Block at First Sight", "Disables Block at First Sight (cloud-based first-seen blocking).", "Reduces cloud-driven blocking of newly observed files.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\DisableBlockAtFirstSeen"),
            Pol("policy.defender.disablescriptscanning", "Disable Script Scanning", "Disables real-time script scanning.", "Script scanning inspects scripts before execution.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableScriptScanning"),
            Pol("policy.defender.disablecatchupfullscan", "Disable Catch-up Full Scan", "Disables catch-up full scans after missed schedules.", "Catch-up scans run when a scheduled scan was missed.", RiskLevel.Low, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Scan\DisableCatchupFullScan"),
            Pol("policy.defender.disablecatchupquickscan", "Disable Catch-up Quick Scan", "Disables catch-up quick scans after missed schedules.", "Catch-up quick scans run when a scheduled quick scan was missed.", RiskLevel.Low, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Scan\DisableCatchupQuickScan"),

            Pol("policy.smartscreen.enable", "Enable SmartScreen", "Enables Windows SmartScreen.", "SmartScreen checks apps and files against reputation services.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Defender, "SmartScreen", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableSmartScreen"),
            Pol("policy.smartscreen.shelllevel", "Shell SmartScreen Level", "Sets the SmartScreen level for the shell (Warn or Block).", "Block is more restrictive than Warn.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Defender, "SmartScreen", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\ShellSmartScreenLevel"),

            Pol("policy.clipboard.allowhistory", "Allow Clipboard History", "Enables or disables Clipboard history.", "Clipboard history stores multiple items and can sync content.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Other, "Clipboard", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\AllowClipboardHistory"),
            Pol("policy.clipboard.allowcrossdevice", "Allow Cross-Device Clipboard", "Enables or disables cross-device clipboard sync.", "Cross-device clipboard can sync clipboard content via cloud.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Other, "Clipboard", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\AllowCrossDeviceClipboard")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreateFirewallBatch()
    {
        // Profile enable/inbound paths are concrete and readable.
        // Service and logging summary are observation-only (not writable through this tool).
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
            RebootRequirement = RebootRequirement.None, SchemaVersion = "1.6", CreatedBy = "ManagedObjectCatalog",
            CreatedTimestamp = DateTime.UtcNow, ConfidenceScore = 80, ConfidenceSource = "Catalog-v1.6"
        };
    }
}
