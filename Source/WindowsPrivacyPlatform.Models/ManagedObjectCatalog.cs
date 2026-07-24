// Source/WindowsPrivacyPlatform.Models/ManagedObjectCatalog.cs
using System.Collections.Generic;
using System.Linq;

namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Static catalog of predefined ManagedObjects for high-value privacy and security settings.
/// Pure data only — no business logic. Knowledge owner of ValueSemantics maps.
/// ObjectId values align with PolicyCollector probe ids and PrivacyCollector names.
/// Every entry has exactly one primary ProductDomain for navigation/report grouping.
/// v0.9.5: Full probe coverage, expanded ValueSemantics, compatibility metadata, WhenIgnored/CommonMisconception/TypicalEnterpriseUse.
/// </summary>
public static class ManagedObjectCatalog
{
    public static IReadOnlyList<ManagedObject> PrivacySettings { get; } = AttachSemantics(CreatePrivacyBatch());

    public static IReadOnlyList<ManagedObject> PolicySettings { get; } = AttachSemantics(CreatePolicyBatch());

    public static IReadOnlyList<ManagedObject> FirewallSettings { get; } = AttachSemantics(CreateFirewallBatch());

    /// <summary>Combined catalog (privacy + policy + firewall).</summary>
    public static IReadOnlyList<ManagedObject> All { get; } =
        PrivacySettings.Concat(PolicySettings).Concat(FirewallSettings).ToList().AsReadOnly();

    private static IReadOnlyList<ManagedObject> AttachSemantics(IReadOnlyList<ManagedObject> batch)
    {
        foreach (var mo in batch)
        {
            if (mo is null) continue;
            mo.SchemaVersion = "0.9.5";
            mo.ConfidenceSource = "Catalog-v0.9.5";
            ApplyKnownSemantics(mo);
        }
        return batch;
    }

    private static void ApplyKnownSemantics(ManagedObject mo)
    {
        // ConsentStore: Allow / Deny / Prompt
        if (mo.ObjectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("Allow", "Allow", "Allow", "Applications may use this capability when they request it (subject to higher policy)."),
                V("Deny", "Deny", "Deny", "Applications are blocked from this capability under the current user."),
                V("Prompt", "Prompt", "Prompt", "Windows prompts the user when an application requests this capability.")
            ];
            if (string.IsNullOrWhiteSpace(mo.WhenIgnored))
                mo.WhenIgnored = "Machine AppPrivacy (LetApps*) policy can force allow or force deny and override this ConsentStore value.";
            if (string.IsNullOrWhiteSpace(mo.CommonMisconception))
                mo.CommonMisconception = "A ConsentStore Deny is not always the whole story; machine AppPrivacy policy can still force access.";
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11"];
            return;
        }

        // Advertising ID user toggle (0/1)
        if (mo.ObjectId.Equals("privacy.advertisingid.enabled", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "Disabled", "Disabled", "Windows does not provide an Advertising ID to applications for this user."),
                V("1", "Enabled", "Enabled", "Windows may provide an Advertising ID to applications for cross-app advertising correlation.")
            ];
            mo.WhenIgnored ??= "Group Policy DisabledByGroupPolicy forces Advertising ID off regardless of this user toggle.";
            mo.CommonMisconception ??= "Turning off the Advertising ID does not disable Windows diagnostic data or in-app tracking.";
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11"];
            return;
        }

        // AllowTelemetry (both paths)
        if (mo.ObjectId.Contains("allowtelemetry", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                new ValueMeaning
                {
                    RawValue = "0", Canonical = "Security", DisplayLabel = "Security",
                    Description = "Minimum supported diagnostic data level (Security). Intended for Enterprise/Education.",
                    SupportedEditions = ["Enterprise", "Education"], SupportedVersions = ["Windows 10", "Windows 11"],
                    Confidence = EffectiveConfidence.High,
                    Notes = "On Home/Pro, Windows may not honor Security level the same way; treat as enterprise-oriented."
                },
                V("1", "Basic", "Basic", "Basic diagnostic data level."),
                V("2", "Enhanced", "Enhanced", "Enhanced diagnostic data level (legacy naming on some builds)."),
                V("3", "Full", "Full", "Full diagnostic data level.")
            ];
            mo.WhenIgnored ??= "If neither policy store is configured, the effective diagnostic level may come from Setup or default behavior not collected here.";
            mo.CommonMisconception ??= "A low diagnostic level does not stop Windows Update, Store, or licensing traffic.";
            mo.TypicalEnterpriseUse ??= "Enterprises often set AllowTelemetry to 0 or 1 via Group Policy or MDM.";
            mo.SupportedEditions ??= ["Enterprise", "Education", "Pro", "Home"];
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11"];
            return;
        }

        // AppPrivacy LetApps* force codes
        if (mo.ObjectId.StartsWith("policy.appprivacy.", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "UserControlled", "User controlled", "Machine policy leaves capability control to the per-user ConsentStore value."),
                V("1", "ForceAllow", "Force allow", "Machine policy forces the capability allowed for apps; user ConsentStore is ignored."),
                V("2", "ForceDeny", "Force deny", "Machine policy forces the capability denied for apps; user ConsentStore is ignored.")
            ];
            mo.WhenIgnored ??= "Not configured means Windows falls back to user ConsentStore (or other platform defaults).";
            mo.CommonMisconception ??= "AppPrivacy 0 is not the same as Force Deny; 0 means user control.";
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11"];
            return;
        }

        // Firewall profile enable (0/1)
        if (mo.ObjectId.Contains(".enabled", StringComparison.OrdinalIgnoreCase) &&
            mo.ProductDomain == ProductDomain.Firewall)
        {
            mo.ValueSemantics =
            [
                V("0", "Disabled", "Disabled", "This firewall profile is disabled."),
                V("1", "Enabled", "Enabled", "This firewall profile is enabled."),
                V("Disabled", "Disabled", "Disabled", "This firewall profile is disabled."),
                V("Enabled", "Enabled", "Enabled", "This firewall profile is enabled.")
            ];
            mo.WhenIgnored ??= "Profile flags are not enforced if the Windows Firewall service (MpsSvc) is stopped.";
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11", "Server"];
            return;
        }

        // Firewall inbound defaults (0 Block / 1 Allow common)
        if (mo.ObjectId.Contains(".inbound", StringComparison.OrdinalIgnoreCase) &&
            mo.ProductDomain == ProductDomain.Firewall)
        {
            mo.ValueSemantics =
            [
                V("0", "Block", "Block", "Default inbound action is Block (unsolicited inbound dropped unless allowed by rule)."),
                V("1", "Allow", "Allow", "Default inbound action is Allow (uncommon for secure defaults)."),
                V("Block", "Block", "Block", "Default inbound action is Block."),
                V("Allow", "Allow", "Allow", "Default inbound action is Allow.")
            ];
            return;
        }

        // AUOptions
        if (mo.ObjectId.Equals("policy.update.auoptions", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("2", "NotifyBeforeDownload", "Notify before download", "Notify the user before downloading updates."),
                V("3", "AutoDownloadNotifyInstall", "Auto download, notify install", "Download updates automatically and notify before installing."),
                V("4", "AutoDownloadScheduledInstall", "Auto download and scheduled install", "Download and install updates on a scheduled day/time."),
                V("5", "LocalAdminCanChoose", "Local admin chooses", "Allow local administrators to choose the configuration.")
            ];
            mo.WhenIgnored ??= "Only meaningful when NoAutoUpdate is not forcing updates off and an AU policy is in effect.";
            mo.TypicalEnterpriseUse ??= "Enterprises commonly use 4 with ScheduledInstallDay/Time or manage via WSUS/Intune instead.";
            return;
        }

        // Delivery Optimization download mode
        if (mo.ObjectId.Equals("policy.deliveryopt.downloadmode", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "HttpOnly", "HTTP only", "Download only from Microsoft or the configured update server; no peer sharing."),
                V("1", "HttpAndLan", "HTTP + LAN", "HTTP plus peer-to-peer on the local network."),
                V("2", "HttpLanInternet", "HTTP + LAN + Internet", "HTTP plus peers on LAN and Internet (Group)."),
                V("3", "LanOnly", "LAN only", "Peer-to-peer on the local network only."),
                V("99", "Simple", "Simple mode", "Simple download mode without peering."),
                V("100", "Bypass", "Bypass", "Bypass Delivery Optimization; use BITS/HTTP directly.")
            ];
            mo.TypicalEnterpriseUse ??= "Many enterprises set 0 or 1 to control bandwidth and avoid Internet peering.";
            return;
        }

        // MAPS / Spynet reporting
        if (mo.ObjectId.Equals("policy.defender.spynetreporting", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "Disabled", "Disabled", "Cloud-delivered protection / MAPS reporting is disabled."),
                V("1", "Basic", "Basic", "Basic membership / reporting level."),
                V("2", "Advanced", "Advanced", "Advanced membership / reporting level.")
            ];
            return;
        }

        // Sample submission consent
        if (mo.ObjectId.Equals("policy.defender.submitsamples", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "AlwaysPrompt", "Always prompt", "Always prompt the user before sending samples."),
                V("1", "SendSafeSamples", "Send safe samples automatically", "Send safe samples automatically without prompting."),
                V("2", "NeverSend", "Never send", "Never send samples."),
                V("3", "SendAllSamples", "Send all samples automatically", "Send all samples automatically.")
            ];
            return;
        }

        // Edge Tracking Prevention
        if (mo.ObjectId.Equals("policy.edge.trackingprevention", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "Off", "Off", "Tracking prevention is turned off."),
                V("1", "Basic", "Basic", "Basic tracking prevention."),
                V("2", "Balanced", "Balanced", "Balanced tracking prevention (default on many installs)."),
                V("3", "Strict", "Strict", "Strict tracking prevention.")
            ];
            return;
        }

        // Binary enable-style / polarity policies (0/1)
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
            or "privacy.speech.onlinespeech")
        {
            mo.ValueSemantics =
            [
                V("0", "Disabled", "Not forced / Off", "Policy value 0 (feature not forced by this policy, or disabled depending on the policy polarity)."),
                V("1", "Enabled", "Forced / On", "Policy value 1 (feature forced or enabled depending on the policy polarity).")
            ];
        }
    }

    private static ValueMeaning V(string raw, string canonical, string label, string description) => new()
    {
        RawValue = raw,
        Canonical = canonical,
        DisplayLabel = label,
        Description = description,
        Confidence = EffectiveConfidence.High
    };

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
            P("privacy.consentstore.phoneCall", "Phone Call", "Controls whether apps can make phone calls.",
                "Phone-call capability is primarily relevant on cellular-capable devices and phone-link scenarios.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\phoneCall\\Value"),
            P("privacy.consentstore.chat", "Chat / Messaging", "Controls whether apps can access chat or messaging capabilities.",
                "Messaging access can expose conversation content and contacts.",
                RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\chat\\Value"),
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
            P("privacy.consentstore.bluetoothSync", "Bluetooth Sync", "Controls whether apps can sync over Bluetooth.",
                "Bluetooth sync can exchange personal data with paired devices.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\bluetoothSync\\Value"),
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
            P("privacy.consentstore.cellularData", "Cellular Data", "Controls whether apps can use cellular data.",
                "Relevant on devices with cellular radios; limits background cellular usage by apps.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\cellularData\\Value"),
            P("privacy.consentstore.wifiData", "Wi-Fi Data", "Controls whether apps can use Wi-Fi data in restricted scenarios.",
                "Complements cellular data capability controls for network usage visibility.",
                RiskLevel.Low, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\wifiData\\Value"),
            P("privacy.consentstore.userDataSystem", "User Data System", "Controls access to system user-data surfaces used by some platform components.",
                "Lower visibility capability; review if unexpected apps request it.",
                RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", "HKCU\\...\\ConsentStore\\userDataSystem\\Value"),

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
            // Telemetry
            Pol("policy.telemetry.allowtelemetry", "Allow Telemetry (GPO)", "Sets the diagnostic data level via Group Policy.",
                "Primary enterprise control for how much diagnostic data leaves the device. Meaning of 0/1/2/3 is defined only in ValueSemantics.",
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
            Pol("policy.telemetry.disablecommercialid", "Allow Device Name In Telemetry", "Controls whether the device name may be included in telemetry.",
                "Device name can aid correlation of diagnostic events to a specific machine in enterprise analytics.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.Telemetry, "Telemetry",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\\AllowDeviceNameInTelemetry"),

            // Windows Update
            Pol("policy.update.noautoupdate", "No Auto Update", "When set, disables automatic Windows Update checking/install behavior controlled by AU policy.",
                "Stopping automatic updates increases exposure to unpatched systems unless another servicing channel is intentional.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\NoAutoUpdate"),
            Pol("policy.update.auoptions", "AU Options", "Configures automatic update mode (notify, download, scheduled install, etc.).",
                "AUOptions controls how aggressively updates are downloaded and installed. Values mapped in ValueSemantics.",
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
            Pol("policy.update.autoinstallminor", "Auto Install Minor Updates", "Controls automatic installation of minor updates.",
                "Minor updates are often lower risk; policy polarity depends on the AU configuration in force.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\AutoInstallMinorUpdates"),
            Pol("policy.update.detectionfrequency", "Detection Frequency", "Hours between Windows Update detection cycles when policy is active.",
                "Lower values increase check frequency; higher values reduce chatter against update endpoints.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU\\DetectionFrequency"),
            Pol("policy.update.disablewuaccess", "Disable Windows Update Access", "Prevents user access to Windows Update.",
                "Locks down end-user update UI. Appropriate in managed environments with WSUS/Intune; risky if no other update channel exists.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\DisableWindowsUpdateAccess"),
            Pol("policy.update.disableuxwuaccess", "Disable UX WU Access", "Blocks access to Windows Update via the Settings UX when set.",
                "Related to DisableWindowsUpdateAccess; both reduce end-user servicing control.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\SetDisableUXWUAccess"),
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
            Pol("policy.update.ux.branchreadiness", "Branch Readiness Level (UX)", "Controls feature update readiness / channel preference exposed in Windows Update UX settings.",
                "Influences when feature updates are offered relative to the selected readiness level.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\\BranchReadinessLevel"),
            Pol("policy.update.ux.flightsettings", "Flight Settings Max Pause Days", "Maximum days feature/quality updates may be paused from the UX settings path.",
                "Bounds how long a user or local admin can pause updates when the control is available.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\\FlightSettingsMaxPauseDays"),
            Pol("policy.update.ux.pausefeatureupdatesstart", "Pause Feature Updates Start Time", "Timestamp when feature updates were paused (UX settings path).",
                "Informational observation of a local pause; not a policy force by itself.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.UserControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\\PauseFeatureUpdatesStartTime"),
            Pol("policy.update.ux.pausequalityupdatesstart", "Pause Quality Updates Start Time", "Timestamp when quality updates were paused (UX settings path).",
                "Informational observation of a local pause; not a policy force by itself.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.UserControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings\\PauseQualityUpdatesStartTime"),
            Pol("policy.deliveryopt.downloadmode", "Delivery Optimization Download Mode", "Controls peer-to-peer and cloud delivery of updates.",
                "Restricting to HTTP-only reduces LAN/Internet sharing of update content; may increase bandwidth from Microsoft or WSUS. Values mapped in ValueSemantics.",
                RiskLevel.Medium, FeatureCategory.NetworkSetting, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled,
                ProductDomain.WindowsUpdate, "WindowsUpdate",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization\\DODownloadMode"),

            // Defender
            Pol("policy.defender.disableantispyware", "Disable AntiSpyware (legacy)", "Legacy policy that can disable Microsoft Defender Antivirus.",
                "Severe security impact on systems relying on Defender. Prefer leaving Defender enabled unless a third-party AV is active.",
                RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\DisableAntiSpyware"),
            Pol("policy.defender.disablerealtime", "Disable Real-Time Monitoring", "Disables Defender real-time protection via policy.",
                "Real-time protection is core host security. Disable only for short troubleshooting windows.",
                RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\\DisableRealtimeMonitoring"),
            Pol("policy.defender.disablebehaviormonitor", "Disable Behavior Monitoring", "Disables Defender behavior monitoring via policy.",
                "Behavior monitoring catches suspicious process activity beyond signature matches.",
                RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\\DisableBehaviorMonitoring"),
            Pol("policy.defender.disableioav", "Disable IOAV Protection", "Disables scanning of downloaded files and attachments (IOAV).",
                "IOAV protection scans content as it is written or opened from the internet.",
                RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection\\DisableIOAVProtection"),
            Pol("policy.defender.spynetreporting", "MAPS / Spynet Reporting", "Controls cloud-delivered protection / Microsoft Active Protection Service reporting level.",
                "Cloud reporting improves detection but sends sample metadata to Microsoft. Values mapped in ValueSemantics.",
                RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet\\SpynetReporting"),
            Pol("policy.defender.submitsamples", "Submit Samples Consent", "Controls automatic sample submission to Microsoft.",
                "Sample submission can include suspicious files. Restrict in high-sensitivity environments. Values mapped in ValueSemantics.",
                RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet\\SubmitSamplesConsent"),
            Pol("policy.defender.puaprotection", "PUA Protection", "Enables blocking of potentially unwanted applications.",
                "PUA protection reduces adware/bundleware. Low privacy impact.",
                RiskLevel.Low, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled,
                ProductDomain.Defender, "Defender",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\PUAProtection"),

            // Search
            Pol("policy.search.allowcortana", "Allow Cortana", "Enables or disables Cortana via policy.",
                "Cortana/cloud assistant features increase cloud interaction.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\AllowCortana"),
            Pol("policy.search.disablewebsearch", "Disable Web Search", "Prevents search from querying the web.",
                "Keeps Start/search queries local.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\DisableWebSearch"),
            Pol("policy.search.connectedsearchuseweb", "Connected Search Use Web", "Controls whether search uses the web.",
                "Related to cloud search.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\ConnectedSearchUseWeb"),
            Pol("policy.search.allowsearchlocation", "Allow Search To Use Location", "Allows Windows Search to use location.",
                "Search + location can refine local results but adds another location consumer.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\AllowSearchToUseLocation"),
            Pol("policy.search.allowcloudsearch", "Allow Cloud Search", "Allows Windows Search to use cloud search features.",
                "Cloud search can return results backed by online services and account content.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\AllowCloudSearch"),
            Pol("policy.search.disableindexedlocationsinlib", "Disable Indexed Search", "Policy affecting indexed search behavior / locations.",
                "Indexing improves local search performance; disabling changes discovery of local content.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled,
                ProductDomain.Search, "Search",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search\\DisableIndexedSearch"),

            // Activity History
            Pol("policy.activity.enableactivityfeed", "Enable Activity Feed", "Enables the Timeline / activity feed feature.",
                "Activity feed stores recent activity for resume scenarios.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.ActivityHistory, "ActivityHistory",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\\EnableActivityFeed"),
            Pol("policy.activity.publishuseractivities", "Publish User Activities", "Allows activities to be published for Timeline.",
                "Publishing activities creates a local (and potentially synced) history of app usage.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.ActivityHistory, "ActivityHistory",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\\PublishUserActivities"),
            Pol("policy.activity.uploaduseractivities", "Upload User Activities", "Allows activities to be uploaded to the cloud for roaming Timeline.",
                "Cloud upload of activities is higher privacy impact than local-only.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled,
                ProductDomain.ActivityHistory, "ActivityHistory",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\\UploadUserActivities"),

            // Cloud Content
            Pol("policy.cloud.disableconsumerfeatures", "Disable Windows Consumer Features", "Turns off consumer experiences (suggested apps, etc.) via policy.",
                "Reduces Store suggestions and consumer upsell surfaces.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableWindowsConsumerFeatures"),
            Pol("policy.cloud.disablesoftlanding", "Disable Soft Landing", "Disables post-update soft landing tips and experiences.",
                "Quietens first-run/post-update experiences.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableSoftLanding"),
            Pol("policy.cloud.disablecloudoptimized", "Disable Cloud Optimized Content", "Disables cloud-optimized content experiences.",
                "Reduces certain online content optimizations in the shell.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableCloudOptimizedContent"),
            Pol("policy.cloud.disablewindowsspotlight.hkcu", "Disable Windows Spotlight (User)", "Disables Spotlight lock screen and related consumer content for the user.",
                "Spotlight fetches online imagery and suggestions.",
                RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.UserControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableWindowsSpotlightFeatures"),
            Pol("policy.cloud.disabletailored.hkcu", "Disable Tailored Experiences (User Policy)", "User-policy path to disable tailored experiences with diagnostic data.",
                "Related to the per-user privacy toggle; policy path can lock the preference.",
                RiskLevel.Medium, FeatureCategory.CloudComponent, ComponentOwner.Telemetry, ControlLevel.UserControlled,
                ProductDomain.CloudContent, "CloudContent",
                "HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent\\DisableTailoredExperiencesWithDiagnosticData"),

            // Advertising
            Pol("policy.advertising.disabledbygpo", "Advertising ID Disabled by Group Policy", "Forces advertising ID off via GPO.",
                "Stronger than the per-user AdvertisingInfo toggle.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Advertising, "Advertising",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo\\DisabledByGroupPolicy"),

            // Location
            Pol("policy.location.disablelocation", "Disable Location", "Disables the Windows location feature via policy.",
                "Machine-wide location kill switch. Breaks location-dependent apps and Find My Device scenarios.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Location, "Location",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\\DisableLocation"),
            Pol("policy.location.disablelocationscripting", "Disable Location Scripting", "Disables location scripting interfaces.",
                "Limits scripted access to location APIs.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Location, "Location",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\\DisableLocationScripting"),
            Pol("policy.location.disablewindowslocationsupplier", "Disable Windows Location Provider", "Disables the Windows location provider.",
                "Stops the built-in location provider used by many location consumers.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Location, "Location",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors\\DisableWindowsLocationProvider"),

            // App Privacy (full LetApps* set)
            Pol("policy.appprivacy.location", "Let Apps Access Location (GPO)", "Force-allows, force-denies, or user-controls app location access.",
                "GPO override for ConsentStore location. Codes defined only in ValueSemantics.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessLocation"),
            Pol("policy.appprivacy.camera", "Let Apps Access Camera (GPO)", "Force policy for app camera access.",
                "Machine policy override for webcam ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessCamera"),
            Pol("policy.appprivacy.microphone", "Let Apps Access Microphone (GPO)", "Force policy for app microphone access.",
                "Machine policy override for microphone ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessMicrophone"),
            Pol("policy.appprivacy.accountinfo", "Let Apps Access Account Info (GPO)", "Force policy for app access to account information.",
                "Machine policy override for userAccountInformation ConsentStore.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessAccountInfo"),
            Pol("policy.appprivacy.contacts", "Let Apps Access Contacts (GPO)", "Force policy for app contacts access.",
                "Machine policy override for contacts ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessContacts"),
            Pol("policy.appprivacy.calendar", "Let Apps Access Calendar (GPO)", "Force policy for app calendar access.",
                "Machine policy override for appointments ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessCalendar"),
            Pol("policy.appprivacy.email", "Let Apps Access Email (GPO)", "Force policy for app email access.",
                "Machine policy override for email ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessEmail"),
            Pol("policy.appprivacy.callhistory", "Let Apps Access Call History (GPO)", "Force policy for app call-history access.",
                "Machine policy override for phoneCallHistory ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessCallHistory"),
            Pol("policy.appprivacy.messaging", "Let Apps Access Messaging (GPO)", "Force policy for app messaging access.",
                "Machine policy override for messaging/chat-related capabilities.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessMessaging"),
            Pol("policy.appprivacy.radios", "Let Apps Access Radios (GPO)", "Force policy for app radio control.",
                "Machine policy override for radios ConsentStore.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessRadios"),
            Pol("policy.appprivacy.documents", "Let Apps Access Documents (GPO)", "Force policy for app Documents library access.",
                "Machine policy override for documentsLibrary ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessDocuments"),
            Pol("policy.appprivacy.pictures", "Let Apps Access Pictures (GPO)", "Force policy for app Pictures library access.",
                "Machine policy override for picturesLibrary ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessPictures"),
            Pol("policy.appprivacy.videos", "Let Apps Access Videos (GPO)", "Force policy for app Videos library access.",
                "Machine policy override for videosLibrary ConsentStore.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessVideos"),
            Pol("policy.appprivacy.filesystem", "Let Apps Access File System (GPO)", "Force policy for broad filesystem access by apps.",
                "High-impact capability.",
                RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsAccessFileSystem"),
            Pol("policy.appprivacy.appdiagnostics", "Let Apps Get Diagnostic Info (GPO)", "Force policy for app diagnostic information about other apps.",
                "Machine policy override for appDiagnostics ConsentStore.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.AppPrivacy, "AppPrivacy",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy\\LetAppsGetDiagnosticInfo"),

            // Device / Find My Device
            Pol("policy.findmydevice.allow", "Allow Find My Device", "Allows the Find My Device feature.",
                "Find My Device uses location and device registration with Microsoft account services.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Device, "Device",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\FindMyDevice\\AllowFindMyDevice"),
            Pol("policy.device.metadataretrieval", "Prevent Device Metadata From Network", "Prevents retrieval of device metadata from the network.",
                "Stops automatic download of device icons and descriptive metadata.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Device, "Device",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Device Metadata\\PreventDeviceMetadataFromNetwork"),

            // Feedback (user SIUF)
            Pol("policy.feedback.numberoffeedbacksiuf", "Number of SIUF In Period", "User preference controlling how many feedback prompts appear in a period.",
                "Affects interruption frequency of Windows feedback (SIUF) prompts.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.UserControlled,
                ProductDomain.Telemetry, "Feedback",
                "HKCU\\SOFTWARE\\Microsoft\\Siuf\\Rules\\NumberOfSIUFInPeriod"),
            Pol("policy.feedback.periodinsiuf", "SIUF Period In Nano Seconds", "User preference for the feedback prompt period.",
                "Works with NumberOfSIUFInPeriod to throttle feedback UI.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.UserControlled,
                ProductDomain.Telemetry, "Feedback",
                "HKCU\\SOFTWARE\\Microsoft\\Siuf\\Rules\\PeriodInNanoSeconds"),

            // OneDrive
            Pol("policy.onedrive.disablefilesonDemand", "Disable OneDrive File Sync (NGSC)", "Disables OneDrive Next Generation Sync Client file sync via policy.",
                "Used in environments that prohibit consumer OneDrive sync on managed devices.",
                RiskLevel.Medium, FeatureCategory.CloudComponent, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "OneDrive",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive\\DisableFileSyncNGSC"),

            // Explorer
            Pol("policy.explorer.allowonlinecontent", "Allow Online Tips", "Controls online tips in Explorer / shell.",
                "Online tips fetch content from Microsoft services.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Explorer, ControlLevel.AdministratorControlled,
                ProductDomain.CloudContent, "Explorer",
                "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\AllowOnlineTips"),
            Pol("policy.explorer.norecentserverdocs", "No Recent Docs History", "Disables recent documents history for the user when set.",
                "Reduces local tracking of recently opened documents in the shell.",
                RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Explorer, ControlLevel.UserControlled,
                ProductDomain.Other, "Explorer",
                "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\NoRecentDocsHistory"),

            // Biometrics
            Pol("policy.biometrics.enabled", "Biometrics Enabled", "Enables or disables Windows biometric framework via policy.",
                "Disabling biometrics removes Windows Hello face/fingerprint unlock.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Biometrics, "Biometrics",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\\Enabled"),
            Pol("policy.biometrics.facialfeatures", "Enhanced Anti-Spoofing (Facial)", "Controls enhanced anti-spoofing for facial biometrics.",
                "Strengthens Windows Hello face resistance to presentation attacks when supported by hardware.",
                RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled,
                ProductDomain.Biometrics, "Biometrics",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Biometrics\\FacialFeatures\\EnhancedAntiSpoofing"),

            // Edge
            Pol("policy.edge.trackingprevention", "Edge Tracking Prevention", "Configures Microsoft Edge tracking prevention level via policy.",
                "Higher tracking prevention reduces cross-site tracking at the cost of some site compatibility. Values mapped in ValueSemantics.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\TrackingPrevention"),
            Pol("policy.edge.metricsreporting", "Edge Metrics Reporting", "Controls Edge metrics/telemetry reporting.",
                "Disabling reduces Edge diagnostic data shared with Microsoft.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\MetricsReportingEnabled"),
            Pol("policy.edge.personalizationreporting", "Edge Personalization Reporting", "Controls Edge personalization data reporting.",
                "Related to personalized experiences in Edge.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\PersonalizationReportingEnabled"),
            Pol("policy.edge.searchsuggest", "Edge Search Suggestions", "Enables or disables search suggestions in Edge.",
                "Search suggestions send partial queries to the suggestion service.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\SearchSuggestEnabled"),
            Pol("policy.edge.passwordmanager", "Edge Password Manager", "Enables or disables the built-in Edge password manager.",
                "Security/privacy trade-off: convenient storage vs reliance on browser vault.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\PasswordManagerEnabled"),
            Pol("policy.edge.autofilladdress", "Edge Autofill Address", "Enables or disables address autofill in Edge.",
                "Address autofill stores form data for convenience.",
                RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\AutofillAddressEnabled"),
            Pol("policy.edge.autofillcreditcard", "Edge Autofill Credit Card", "Enables or disables credit-card autofill in Edge.",
                "Credit-card autofill is higher sensitivity than address autofill.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\AutofillCreditCardEnabled"),
            Pol("policy.edge.alternateerrorpages", "Edge Alternate Error Pages", "Enables or disables alternate error pages that may contact Microsoft services.",
                "Alternate error pages can send navigation failure context to improve suggestions.",
                RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\AlternateErrorPagesEnabled"),
            Pol("policy.edge.paymentmethods", "Edge Payment Method Query", "Controls whether Edge may query available payment methods.",
                "Payment method queries can expose payment capability presence to sites.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\PaymentMethodQueryEnabled"),
            Pol("policy.edge.sendsitinfo", "Edge Send Site Info To Improve Services", "Controls whether Edge sends site information to improve Microsoft services.",
                "Optional diagnostic channel specific to Edge browsing.",
                RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled,
                ProductDomain.Edge, "Edge",
                "HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge\\SendSiteInfoToImproveServices")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreateFirewallBatch()
    {
        var list = new List<ManagedObject>
        {
            Fw("firewall.profile.domain.enabled", "Domain Profile Enabled",
                "Indicates whether the Windows Firewall Domain profile is enabled.",
                "The Domain profile applies when the computer is connected to a network authenticated to a domain controller.",
                RiskLevel.High, "Profiles",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile\\EnableFirewall"),

            Fw("firewall.profile.private.enabled", "Private Profile Enabled",
                "Indicates whether the Windows Firewall Private profile is enabled.",
                "The Private profile is used on networks marked as private (home or work).",
                RiskLevel.High, "Profiles",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile\\EnableFirewall"),

            Fw("firewall.profile.public.enabled", "Public Profile Enabled",
                "Indicates whether the Windows Firewall Public profile is enabled.",
                "The Public profile is the most restrictive default profile for untrusted networks.",
                RiskLevel.High, "Profiles",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile\\EnableFirewall"),

            Fw("firewall.profile.domain.inbound", "Domain Profile Default Inbound Action",
                "Default action for inbound connections that do not match an allow rule on the Domain profile.",
                "Block drops unsolicited inbound traffic unless an explicit allow rule exists.",
                RiskLevel.High, "Defaults",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\DomainProfile\\DefaultInboundAction"),

            Fw("firewall.profile.private.inbound", "Private Profile Default Inbound Action",
                "Default action for inbound connections that do not match an allow rule on the Private profile.",
                "Controls baseline inbound posture on private networks.",
                RiskLevel.High, "Defaults",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\StandardProfile\\DefaultInboundAction"),

            Fw("firewall.profile.public.inbound", "Public Profile Default Inbound Action",
                "Default action for inbound connections that do not match an allow rule on the Public profile.",
                "Public networks are treated as untrusted; Block is the expected default.",
                RiskLevel.High, "Defaults",
                "HKLM\\SYSTEM\\CurrentControlSet\\Services\\SharedAccess\\Parameters\\FirewallPolicy\\PublicProfile\\DefaultInboundAction"),

            Fw("firewall.service.mpssvc", "Windows Firewall Service (MpsSvc)",
                "Runtime state of the Windows Defender Firewall service (MpsSvc).",
                "If the firewall service is stopped, profile enable flags may not be enforced.",
                RiskLevel.High, "Service",
                "ServiceController:MpsSvc"),

            Fw("firewall.logging.summary", "Firewall Logging Configuration",
                "Summarizes whether firewall logging paths and dropped/successful connection logging are configured for profiles.",
                "Logging supports forensic and operational review of blocked or allowed traffic.",
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
            SchemaVersion = "0.9.5",
            CreatedBy = "ManagedObjectCatalog",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 80,
            ConfidenceSource = "Catalog-v0.9.5"
        };
    }
}
