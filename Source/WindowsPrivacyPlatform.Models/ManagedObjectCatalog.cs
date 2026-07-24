// Source/WindowsPrivacyPlatform.Models/ManagedObjectCatalog.cs
using System.Collections.Generic;
using System.Linq;

namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Static catalog of predefined ManagedObjects for high-value privacy and security settings.
/// Pure data only — no business logic. Used by report layer to explain inventory.
/// ObjectId values align with PolicyCollector probe ids and PrivacyCollector names.
/// Every entry has exactly one primary ProductDomain for navigation/report grouping.
/// v0.8: Firewall domain entries added (curated, read-only understanding only).
/// </summary>
public static class ManagedObjectCatalog
{
    public static IReadOnlyList<ManagedObject> PrivacySettings { get; } = CreatePrivacyBatch();

    public static IReadOnlyList<ManagedObject> PolicySettings { get; } = CreatePolicyBatch();

    public static IReadOnlyList<ManagedObject> FirewallSettings { get; } = CreateFirewallBatch();

    /// <summary>Combined catalog (privacy + policy + firewall).</summary>
    public static IReadOnlyList<ManagedObject> All { get; } =
        PrivacySettings.Concat(PolicySettings).Concat(FirewallSettings).ToList().AsReadOnly();

    private static IReadOnlyList<ManagedObject> CreatePrivacyBatch()
    {
        var list = new List<ManagedObject>
        {
            P("privacy.consentstore.location", "Location", "Controls whether apps can access the device location.",
                "Location data reveals physical movement and habitual places. Denying access reduces tracking risk while breaking navigation and weather apps that need it.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\location\\Value"),
            P("privacy.consentstore.webcam", "Camera (Webcam)", "Controls whether apps can access the camera.",
                "Unauthorized camera access is a direct privacy and safety risk. Prefer Deny or Prompt unless a trusted app requires it.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\webcam\\Value"),
            P("privacy.consentstore.microphone", "Microphone", "Controls whether apps can access the microphone.",
                "Microphone access enables continuous audio capture. High risk if granted broadly; keep at Prompt or Deny for untrusted apps.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\microphone\\Value"),
            P("privacy.consentstore.userAccountInformation", "Account Information", "Controls whether apps can access your name, picture, and account info.",
                "Account information is used for personalization and can aid profiling. Medium risk; review which apps truly need it.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\userAccountInformation\\Value"),
            P("privacy.consentstore.contacts", "Contacts", "Controls whether apps can access your contacts.",
                "Contacts often include personal and professional relationships. Exposing them increases social-graph leakage risk.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\contacts\\Value"),
            P("privacy.consentstore.appointments", "Calendar", "Controls whether apps can access your calendar appointments.",
                "Calendar data reveals schedule, meetings, and often location. High sensitivity for professional and personal privacy.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\appointments\\Value"),
            P("privacy.consentstore.email", "Email", "Controls whether apps can access email.",
                "Email content and metadata are highly sensitive. Restrict to apps that explicitly require mail access.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\email\\Value"),
            P("privacy.consentstore.phoneCallHistory", "Call History", "Controls whether apps can access phone call history.",
                "Call history exposes communication patterns and contacts. Rarely needed by desktop apps; prefer Deny.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\phoneCallHistory\\Value"),
            P("privacy.consentstore.appDiagnostics", "App Diagnostics", "Controls whether apps can access diagnostic information about other apps.",
                "Allows one app to observe others' runtime behavior. Useful for system tools; unnecessary for most apps.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\appDiagnostics\\Value"),
            P("privacy.consentstore.documentsLibrary", "Documents Library", "Controls whether apps can access the Documents library.",
                "Documents often contain personal and work files. Prefer Prompt so the user approves each access pattern.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\documentsLibrary\\Value"),
            P("privacy.consentstore.picturesLibrary", "Pictures Library", "Controls whether apps can access the Pictures library.",
                "Photos can contain location EXIF data and private imagery. High privacy impact if broadly allowed.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\picturesLibrary\\Value"),
            P("privacy.consentstore.videosLibrary", "Videos Library", "Controls whether apps can access the Videos library.",
                "Video libraries may hold personal recordings. Same risk profile as pictures; restrict unless needed.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\videosLibrary\\Value"),
            P("privacy.consentstore.broadFileSystemAccess", "Broad File System Access", "Controls whether apps can access the file system broadly beyond known folders.",
                "Broad filesystem access is one of the highest-impact AppX capabilities. Prefer Deny unless a trusted tool requires full access.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\broadFileSystemAccess\\Value"),
            P("privacy.consentstore.radios", "Radios", "Controls whether apps can control device radios (Bluetooth, Wi-Fi, etc.).",
                "Radio control can enable tracking or unexpected connectivity changes. Medium risk; limit to system-like apps.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\radios\\Value"),
            P("privacy.consentstore.musicLibrary", "Music Library", "Controls whether apps can access the Music library.",
                "Music libraries are lower sensitivity than documents/photos but still personal. Prefer Prompt.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\musicLibrary\\Value"),
            P("privacy.consentstore.downloadsFolder", "Downloads Folder", "Controls whether apps can access the Downloads folder.",
                "Downloads often contain installers and personal files from the web. Restrict broad access.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\downloadsFolder\\Value"),
            P("privacy.consentstore.gazeInput", "Gaze Input", "Controls whether apps can access eye-tracking / gaze input.",
                "Gaze data is biometric-adjacent. Disable unless a specific accessibility or research app needs it.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\gazeInput\\Value"),
            P("privacy.consentstore.activity", "Activity", "Controls app access to activity-related capability.",
                "Related to activity history surfaces. Prefer Deny if Timeline/activity features are unused.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\activity\\Value"),
            P("privacy.consentstore.activityData", "Activity Data", "Controls app access to activity data capability.",
                "Activity data can reconstruct usage patterns. High privacy impact if broadly allowed.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\activityData\\Value"),
            P("privacy.consentstore.humanPresence", "Human Presence", "Controls access to human presence sensors.",
                "Presence sensors indicate whether a person is near the device. Relevant for privacy in shared spaces.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\humanPresence\\Value"),
            P("privacy.consentstore.graphicsCaptureProgrammatic", "Graphics Capture (Programmatic)", "Controls programmatic screen/window capture capability.",
                "Screen capture can expose credentials and private content. Prefer Prompt or Deny for untrusted apps.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\graphicsCaptureProgrammatic\\Value"),
            P("privacy.consentstore.graphicsCaptureWithoutBorder", "Graphics Capture Without Border", "Controls capture without the yellow border indicator.",
                "Removing the capture border reduces user awareness that recording is active. Prefer Deny.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\graphicsCaptureWithoutBorder\\Value"),
            P("privacy.advertisingid.enabled", "Advertising ID", "Controls whether Windows provides an advertising ID to apps for cross-app tracking.",
                "Disabling the advertising ID reduces cross-app advertising correlation. Low functional impact for most users.",
                RiskLevel.Medium, ProductDomain.Advertising, "Advertising", "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\\Enabled"),
            P("privacy.tailoredexperiences", "Tailored Experiences", "Controls whether diagnostic data is used to offer tailored tips and recommendations.",
                "Uses diagnostic data for personalization. Disabling reduces data reuse for recommendations with minimal feature loss.",
                RiskLevel.Medium, ProductDomain.Telemetry, "DiagnosticPersonalization", "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\\TailoredExperiencesWithDiagnosticDataEnabled"),
            P("privacy.contentdelivery.systempanesuggestions", "System Pane Suggestions", "Controls suggested content in system UI panes (Settings tips, etc.).",
                "Suggested content is low severity but contributes to attention and soft telemetry. Optional to disable for a quieter UI.",
                RiskLevel.Low, ProductDomain.CloudContent, "ContentDelivery", "HKCU\\...\\ContentDeliveryManager\\SystemPaneSuggestionsEnabled"),
            P("privacy.speech.onlinespeech", "Online Speech Recognition", "Controls whether speech input may be processed by online (cloud) speech services.",
                "Online speech sends audio to Microsoft cloud services. Prefer local-only recognition when available if cloud processing is undesirable.",
                RiskLevel.High, ProductDomain.Speech, "Speech", "HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy\\HasAccepted")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreatePolicyBatch()
    {
        var list = new List<ManagedObject>
        {
            Pol("policy.telemetry.allowtelemetry", "Allow Telemetry (GPO)", "Sets the diagnostic data level via Group Policy (0=Security, 1=Basic, 2=Enhanced, 3=Full).",
                "This is the primary enterprise control for how much diagnostic data leaves the device. Lower values reduce data sent to Microsoft; some enterprise features require higher levels.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.Telemetry, "Telemetry",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\\AllowTelemetry"),
            Pol("policy.telemetry.allowtelemetry.currentversion", "Allow Telemetry (CurrentVersion Policies)", "Alternate path for diagnostic data level under CurrentVersion Policies DataCollection.",
                "Same semantic as AllowTelemetry GPO; present on some images as the effective machine policy store.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.Telemetry, "Telemetry",
                "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\DataCollection\\AllowTelemetry"),
            Pol("policy.telemetry.donotshowfeedback", "Do Not Show Feedback Notifications", "Suppresses feedback reminder notifications.",
                "Reduces interruption and feedback prompts. Does not by itself change diagnostic data level.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.Telemetry, "Telemetry",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\\DoNotShowFeedbackNotifications"),

            Pol("policy.update.noautoupdate", "No Auto Update", "When set, disables automatic Windows Update checking/install behavior controlled by AU policy.",
                "Stopping automatic updates increases security risk from unpatched systems. Use only with a deliberate alternative patch process.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\NoAutoUpdate"),
            Pol("policy.update.auoptions", "AU Options", "Configures automatic update mode (notify, download, scheduled install, etc.).",
                "AUOptions controls how aggressively updates are downloaded and installed. Scheduled install enables predictable maintenance windows.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\AUOptions"),
            Pol("policy.update.scheduledinstallday", "Scheduled Install Day", "Day of week for scheduled update installation when AU is in scheduled mode.",
                "Pairs with ScheduledInstallTime to control when reboots and installs occur.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\ScheduledInstallDay"),
            Pol("policy.update.scheduledinstalltime", "Scheduled Install Time", "Hour of day for scheduled update installation.",
                "Use off-hours to reduce user disruption while keeping systems patched.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\ScheduledInstallTime"),
            Pol("policy.update.disablewuaccess", "Disable Windows Update Access", "Prevents user access to Windows Update.",
                "Locks down end-user update UI. Appropriate in managed environments with WSUS/Intune; risky if no other update channel exists.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\DisableWindowsUpdateAccess"),
            Pol("policy.update.donotconnectinternet", "Do Not Connect to Windows Update Internet Locations", "Blocks contact with public Windows Update endpoints.",
                "Used with WSUS/offline servicing. Misconfiguration can leave devices unpatched.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\DoNotConnectToWindowsUpdateInternetLocations"),
            Pol("policy.update.excludewudrivers", "Exclude WU Drivers in Quality Update", "Excludes drivers from quality update offers.",
                "Useful when drivers are managed separately. May delay hardware fixes delivered via Windows Update.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\ExcludeWUDriversInQualityUpdate"),
            Pol("policy.deliveryopt.downloadmode", "Delivery Optimization Download Mode", "Controls peer-to-peer and cloud delivery of updates (0=HTTP only, 1=LAN, 2=Group, 3=Internet, etc.).",
                "Restricting to HTTP-only reduces LAN/Internet sharing of update content; may increase bandwidth from Microsoft or WSUS.",
                RiskLevel.Medium, FeatureCategory.NetworkSetting, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization\\DODownloadMode"),

            Pol("policy.defender.disableantispyware", "Disable AntiSpyware (legacy)", "Legacy policy that can disable Microsoft Defender Antivirus.",
                "Setting this is a severe security risk on systems relying on Defender. Prefer leaving Defender enabled unless a third-party AV is active.",
                RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\DisableAntiSpyware"),
            Pol("policy.defender.disablerealtime", "Disable Real-Time Monitoring", "Disables Defender real-time protection via policy.",
                "Real-time protection is core host security. Disable only for short troubleshooting windows.",
                RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\\DisableRealtimeMonitoring"),
            Pol("policy.defender.spynetreporting", "MAPS / Spynet Reporting", "Controls cloud-delivered protection / Microsoft Active Protection Service reporting level.",
                "Cloud reporting improves detection but sends sample metadata to Microsoft. Balance threat intel benefit vs data sharing.",
                RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet\\SpynetReporting"),
            Pol("policy.defender.submitsamples", "Submit Samples Consent", "Controls automatic sample submission to Microsoft.",
                "Sample submission can include suspicious files. Restrict in high-sensitivity environments; may reduce cloud detection quality.",
                RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet\\SubmitSamplesConsent"),
            Pol("policy.defender.puaprotection", "PUA Protection", "Enables blocking of potentially unwanted applications.",
                "PUA protection reduces adware/bundleware. Low privacy impact; recommended for most users.",
                RiskLevel.Low, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\PUAProtection"),

            Pol("policy.search.allowcortana", "Allow Cortana", "Enables or disables Cortana via policy.",
                "Cortana/cloud assistant features increase cloud interaction. Disable on systems that do not need the assistant.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\AllowCortana"),
            Pol("policy.search.disablewebsearch", "Disable Web Search", "Prevents search from querying the web.",
                "Keeps Start/search queries local. Reduces query leakage to Microsoft; removes web result convenience.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\DisableWebSearch"),
            Pol("policy.search.connectedsearchuseweb", "Connected Search Use Web", "Controls whether search uses the web.",
                "Related to cloud search. Disable alongside DisableWebSearch for local-only search behavior.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\ConnectedSearchUseWeb"),
            Pol("policy.search.allowsearchlocation", "Allow Search To Use Location", "Allows Windows Search to use location.",
                "Search + location can refine local results but adds another location consumer. Prefer off if location is restricted.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\AllowSearchToUseLocation"),

            Pol("policy.activity.enableactivityfeed", "Enable Activity Feed", "Enables the Timeline / activity feed feature.",
                "Activity feed stores recent activity for resume scenarios. Disable if Timeline is unused to reduce local activity retention.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.ActivityHistory, "ActivityHistory",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\\EnableActivityFeed"),
            Pol("policy.activity.publishuseractivities", "Publish User Activities", "Allows activities to be published for Timeline.",
                "Publishing activities creates a local (and potentially synced) history of app usage.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.ActivityHistory, "ActivityHistory",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\\PublishUserActivities"),
            Pol("policy.activity.uploaduseractivities", "Upload User Activities", "Allows activities to be uploaded to the cloud for roaming Timeline.",
                "Cloud upload of activities is higher privacy impact than local-only. Prefer disabled unless cross-device Timeline is required.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.ActivityHistory, "ActivityHistory",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\\UploadUserActivities"),

            Pol("policy.cloud.disableconsumerfeatures", "Disable Windows Consumer Features", "Turns off consumer experiences (suggested apps, etc.) via policy.",
                "Reduces Store suggestions and consumer upsell surfaces. Common hardening setting for managed PCs.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableWindowsConsumerFeatures"),
            Pol("policy.cloud.disablesoftlanding", "Disable Soft Landing", "Disables post-update soft landing tips and experiences.",
                "Quietens first-run/post-update experiences. Low functional impact.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableSoftLanding"),
            Pol("policy.cloud.disablewindowsspotlight.hkcu", "Disable Windows Spotlight (User)", "Disables Spotlight lock screen and related consumer content for the user.",
                "Spotlight fetches online imagery and suggestions. Disable for a static lock screen and less cloud content.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.UserControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableWindowsSpotlightFeatures"),

            Pol("policy.advertising.disabledbygpo", "Advertising ID Disabled by Group Policy", "Forces advertising ID off via GPO.",
                "Stronger than the per-user AdvertisingInfo toggle. Use in managed environments to prevent re-enablement.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Advertising, "Advertising",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo\\DisabledByGroupPolicy"),

            Pol("policy.location.disablelocation", "Disable Location", "Disables the Windows location feature via policy.",
                "Machine-wide location kill switch. Breaks location-dependent apps and Find My Device scenarios.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Location, "Location",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\\DisableLocation"),

            Pol("policy.appprivacy.location", "Let Apps Access Location (GPO)", "Force-allows, force-denies, or user-controls app location access.",
                "GPO override for ConsentStore location. Value semantics: 0=User, 1=Force allow, 2=Force deny (typical).",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessLocation"),
            Pol("policy.appprivacy.camera", "Let Apps Access Camera (GPO)", "Force policy for app camera access.",
                "Machine policy override for webcam ConsentStore. Prefer force-deny on kiosks and high-security hosts.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessCamera"),
            Pol("policy.appprivacy.microphone", "Let Apps Access Microphone (GPO)", "Force policy for app microphone access.",
                "Machine policy override for microphone ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessMicrophone"),
            Pol("policy.appprivacy.filesystem", "Let Apps Access File System (GPO)", "Force policy for broad filesystem access by apps.",
                "High-impact capability. Force-deny unless line-of-business apps require it.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessFileSystem"),

            Pol("policy.edge.trackingprevention", "Edge Tracking Prevention", "Configures Microsoft Edge tracking prevention level via policy.",
                "Higher tracking prevention reduces cross-site tracking at the cost of some site compatibility.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\TrackingPrevention"),
            Pol("policy.edge.metricsreporting", "Edge Metrics Reporting", "Controls Edge metrics/telemetry reporting.",
                "Disabling reduces Edge diagnostic data shared with Microsoft.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\MetricsReportingEnabled"),
            Pol("policy.edge.personalizationreporting", "Edge Personalization Reporting", "Controls Edge personalization data reporting.",
                "Related to personalized experiences in Edge. Disable to limit behavioral data sharing.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\PersonalizationReportingEnabled"),
            Pol("policy.edge.searchsuggest", "Edge Search Suggestions", "Enables or disables search suggestions in Edge.",
                "Search suggestions send partial queries to the suggestion service. Disable for less query leakage.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\SearchSuggestEnabled"),
            Pol("policy.edge.passwordmanager", "Edge Password Manager", "Enables or disables the built-in Edge password manager.",
                "Security/privacy trade-off: convenient storage vs reliance on browser vault. Pair with OS-level credential hygiene.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\PasswordManagerEnabled"),

            Pol("policy.biometrics.enabled", "Biometrics Enabled", "Enables or disables Windows biometric framework via policy.",
                "Disabling biometrics removes Windows Hello face/fingerprint unlock. Security vs convenience trade-off.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Biometrics, "Biometrics",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\\Enabled"),

            Pol("policy.findmydevice.allow", "Allow Find My Device", "Allows the Find My Device feature.",
                "Find My Device uses location and device registration with Microsoft account services. Disable on air-gapped or high-privacy hosts.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Device, "Device",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice\\AllowFindMyDevice")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreateFirewallBatch()
    {
        var list = new List<ManagedObject>
        {
            Fw("firewall.profile.domain.enabled", "Domain Profile Enabled",
                "Indicates whether the Windows Firewall Domain profile is enabled.",
                "The Domain profile applies when the computer is connected to a network that is authenticated to a domain controller. Enabling it applies the domain network firewall policy; disabling it leaves that network segment without this host firewall profile.",
                RiskLevel.High, "Profiles",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile\\EnableFirewall"),

            Fw("firewall.profile.private.enabled", "Private Profile Enabled",
                "Indicates whether the Windows Firewall Private profile is enabled.",
                "The Private profile is used on networks the user or administrator has marked as private (home or work). It typically allows more inbound connectivity than the Public profile while still filtering unsolicited traffic.",
                RiskLevel.High, "Profiles",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile\\EnableFirewall"),

            Fw("firewall.profile.public.enabled", "Public Profile Enabled",
                "Indicates whether the Windows Firewall Public profile is enabled.",
                "The Public profile is the most restrictive default profile and is used on networks not identified as domain or private. It is intended to reduce exposure on untrusted networks such as cafes and airports.",
                RiskLevel.High, "Profiles",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile\\EnableFirewall"),

            Fw("firewall.profile.domain.inbound", "Domain Profile Default Inbound Action",
                "Default action for inbound connections that do not match an allow rule on the Domain profile.",
                "When set to Block, unsolicited inbound traffic is dropped unless an explicit allow rule exists. Allow is uncommon for inbound defaults and increases exposure of local services.",
                RiskLevel.High, "Defaults",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile\\DefaultInboundAction"),

            Fw("firewall.profile.private.inbound", "Private Profile Default Inbound Action",
                "Default action for inbound connections that do not match an allow rule on the Private profile.",
                "Controls the baseline inbound posture on private networks. Block is the typical secure default; Allow widens the attack surface for local services.",
                RiskLevel.High, "Defaults",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile\\DefaultInboundAction"),

            Fw("firewall.profile.public.inbound", "Public Profile Default Inbound Action",
                "Default action for inbound connections that do not match an allow rule on the Public profile.",
                "Public networks are treated as untrusted. A Block default is the expected posture; an Allow default on Public is a significant exposure signal.",
                RiskLevel.High, "Defaults",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile\\DefaultInboundAction"),

            Fw("firewall.service.mpssvc", "Windows Firewall Service (MpsSvc)",
                "Runtime state of the Windows Defender Firewall service (MpsSvc).",
                "If the firewall service is stopped, profile enable flags may not be enforced. The service state is observed via ServiceController and does not by itself describe individual rules.",
                RiskLevel.High, "Service",
                "ServiceController:MpsSvc"),

            Fw("firewall.logging.summary", "Firewall Logging Configuration",
                "Summarizes whether firewall logging paths and dropped/successful connection logging are configured for profiles.",
                "Logging supports forensic and operational review of blocked or allowed traffic. Presence of a log path does not imply continuous high-volume capture; configuration varies by profile and policy.",
                RiskLevel.Medium, "Logging",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\*\\Logging")
        };
        return list.AsReadOnly();
    }

    private static ManagedObject P(
        string id, string name, string description, string rationale,
        RiskLevel risk, ProductDomain domain, string subCategory, string discovery)
    {
        return Create(id, name, "PrivacySetting", description, rationale, risk,
            FeatureCategory.PrivacyPermission, ComponentOwner.Other, ControlLevel.UserControlled,
            domain, subCategory, discovery, InterfaceName.Registry, ConfigurationType.RegistryValue);
    }

    private static ManagedObject Pol(
        string id, string name, string description, string rationale,
        RiskLevel risk, FeatureCategory category, ComponentOwner owner, ControlLevel control,
        ProductDomain domain, string subCategory, string discovery)
    {
        var iface = category == FeatureCategory.EdgePolicy ? InterfaceName.GroupPolicy
            : category == FeatureCategory.DefenderSetting ? InterfaceName.Defender
            : InterfaceName.GroupPolicy;
        var cfg = category == FeatureCategory.DefenderSetting
            ? ConfigurationType.DefenderSettingValue
            : ConfigurationType.PolicyState;
        return Create(id, name, "PolicySetting", description, rationale, risk,
            category, owner, control, domain, subCategory, discovery, iface, cfg);
    }

    private static ManagedObject Fw(
        string id, string name, string description, string rationale,
        RiskLevel risk, string subCategory, string discovery)
    {
        return Create(id, name, "FirewallSetting", description, rationale, risk,
            FeatureCategory.FirewallRule, ComponentOwner.Networking, ControlLevel.AdministratorControlled,
            ProductDomain.Firewall, subCategory, discovery, InterfaceName.Firewall, ConfigurationType.FirewallRuleState);
    }

    private static ManagedObject Create(
        string id,
        string name,
        string objectType,
        string description,
        string rationale,
        RiskLevel risk,
        FeatureCategory category,
        ComponentOwner owner,
        ControlLevel control,
        ProductDomain domain,
        string subCategory,
        string discovery,
        InterfaceName iface,
        ConfigurationType cfg)
    {
        return new ManagedObject
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = objectType,
            Description = description,
            Rationale = rationale,
            FeatureCategory = category,
            ProductDomain = domain,
            SubCategory = subCategory,
            RiskLevel = risk,
            ImpactLevel = ImpactLevel.Security,
            LifecycleState = LifecycleState.Active,
            InterfaceName = iface,
            ConfigurationType = cfg,
            DiscoveryMethod = discovery,
            CanonicalPath = id,
            ControlLevel = control,
            ComponentOwner = owner,
            PriorityLevel = PriorityLevel.Recommended,
            Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None,
            SchemaVersion = "0.8",
            CreatedBy = "ManagedObjectCatalog",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 80,
            ConfidenceSource = "Catalog-v0.8"
        };
    }
}
