// Source/WindowsPrivacyPlatform.Models/CatalogExpansion.cs
// Additional catalog entries for v2.1 coverage expansion.
// All entries are observation-capable; WritableTarget is assigned only via explicit whitelist in ManagedObjectCatalog.
using System.Collections.Generic;

namespace WindowsPrivacyPlatform.Models;

internal static class CatalogExpansion
{
    internal static IReadOnlyList<ManagedObject> CreateCoverageBatch()
    {
        var list = new List<ManagedObject>();

        // ---- AppPrivacy (machine policy overrides ConsentStore) ----
        list.Add(Pol("policy.appprivacy.accountinfo", "Let Apps Access Account Info (GPO)", "Machine force for account info capability.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessAccountInfo"));
        list.Add(Pol("policy.appprivacy.contacts", "Let Apps Access Contacts (GPO)", "Machine force for contacts.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessContacts"));
        list.Add(Pol("policy.appprivacy.calendar", "Let Apps Access Calendar (GPO)", "Machine force for calendar.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessCalendar"));
        list.Add(Pol("policy.appprivacy.email", "Let Apps Access Email (GPO)", "Machine force for email.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessEmail"));
        list.Add(Pol("policy.appprivacy.callhistory", "Let Apps Access Call History (GPO)", "Machine force for call history.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessCallHistory"));
        list.Add(Pol("policy.appprivacy.messaging", "Let Apps Access Messaging (GPO)", "Machine force for messaging.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessMessaging"));
        list.Add(Pol("policy.appprivacy.radios", "Let Apps Access Radios (GPO)", "Machine force for radios.", "Overrides ConsentStore.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessRadios"));
        list.Add(Pol("policy.appprivacy.documents", "Let Apps Access Documents (GPO)", "Machine force for documents library.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessDocuments"));
        list.Add(Pol("policy.appprivacy.pictures", "Let Apps Access Pictures (GPO)", "Machine force for pictures library.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessPictures"));
        list.Add(Pol("policy.appprivacy.videos", "Let Apps Access Videos (GPO)", "Machine force for videos library.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessVideos"));
        list.Add(Pol("policy.appprivacy.filesystem", "Let Apps Access File System (GPO)", "Machine force for broad filesystem access.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessFileSystem"));
        list.Add(Pol("policy.appprivacy.appdiagnostics", "Let Apps Get Diagnostic Info (GPO)", "Machine force for app diagnostics.", "Overrides ConsentStore.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsGetDiagnosticInfo"));

        // ---- Defender additional ----
        list.Add(Pol("policy.defender.disablebehaviormonitor", "Disable Behavior Monitoring", "Defender behavior monitoring policy.", "Reduces behavioral detection.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableBehaviorMonitoring"));
        list.Add(Pol("policy.defender.disableioav", "Disable IOAV Protection", "Scans downloaded files and attachments.", "Reduces download scanning.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableIOAVProtection"));
        list.Add(Pol("policy.defender.puaprotection", "PUA Protection", "Potentially unwanted application protection.", "Blocks unwanted software.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\PUAProtection"));
        list.Add(Pol("policy.defender.disableblockatfirstseen", "Disable Block at First Seen", "Cloud block-at-first-seen behavior.", "Affects zero-day response.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\DisableBlockAtFirstSeen"));
        list.Add(Pol("policy.defender.disablescriptscanning", "Disable Script Scanning", "Real-time script scanning.", "Reduces script-based detection.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableScriptScanning"));
        list.Add(Pol("policy.defender.disablecatchupfullscan", "Disable Catch-up Full Scan", "Missed full scan catch-up.", "Scan scheduling.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Scan\DisableCatchupFullScan"));
        list.Add(Pol("policy.defender.disablecatchupquickscan", "Disable Catch-up Quick Scan", "Missed quick scan catch-up.", "Scan scheduling.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Scan\DisableCatchupQuickScan"));

        // ---- Windows Update additional (observation; selective write) ----
        list.Add(Pol("policy.update.disablewuaccess", "Disable Windows Update Access", "Blocks Windows Update access.", "Can strand the device without patches.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DisableWindowsUpdateAccess"));
        list.Add(Pol("policy.update.donotconnectinternet", "Do Not Connect to Windows Update Internet Locations", "Blocks public update endpoints.", "Often paired with WSUS.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DoNotConnectToWindowsUpdateInternetLocations"));
        list.Add(Pol("policy.update.excludewudrivers", "Exclude WU Drivers in Quality Update", "Driver updates via WU.", "Driver delivery policy.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\ExcludeWUDriversInQualityUpdate"));
        list.Add(Pol("policy.update.disableuxwuaccess", "Disable UX WU Access", "Blocks Windows Update Settings UI.", "Users cannot open update settings.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\SetDisableUXWUAccess"));
        list.Add(Pol("policy.update.managepreviewbuilds", "Manage Preview Builds", "Controls Insider/preview builds.", "Preview channel policy.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\ManagePreviewBuilds"));
        list.Add(Pol("policy.update.allowmuupdateservice", "Allow Microsoft Update Service", "Microsoft Update (non-Windows products).", "Office and other MU content.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AllowMUUpdateService"));
        list.Add(Pol("policy.update.elevatednonadmins", "Elevate Non-Admins for Updates", "Non-admins can install updates.", "Privilege boundary.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\ElevateNonAdmins"));
        list.Add(Pol("policy.update.deferfeatureupdates", "Defer Feature Updates (Days)", "Feature update deferral period.", "Enterprise deferral; value is days.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DeferFeatureUpdatesPeriodInDays"));
        list.Add(Pol("policy.update.deferqualityupdates", "Defer Quality Updates (Days)", "Quality update deferral period.", "Enterprise deferral; value is days.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DeferQualityUpdatesPeriodInDays"));
        list.Add(Pol("policy.update.targetreleaseversioninfo", "Target Release Version Info", "Named release pin (e.g. 24H2).", "Works with TargetReleaseVersion.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\TargetReleaseVersionInfo"));

        // ---- Search / Activity / Cloud ----
        list.Add(Pol("policy.search.connectedsearchuseweb", "Connected Search Use Web", "Web results in search.", "Keeps search more local when disabled.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\ConnectedSearchUseWeb"));
        list.Add(Pol("policy.search.allowsearchlocation", "Allow Search to Use Location", "Search location access.", "Location in search.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowSearchToUseLocation"));
        list.Add(Pol("policy.search.allowcloudsearch", "Allow Cloud Search", "Cloud search content.", "Cloud-backed search.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowCloudSearch"));
        list.Add(Pol("policy.activity.publishuseractivities", "Publish User Activities", "Local activity publication.", "Timeline surface.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.ActivityHistory, "ActivityHistory", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\PublishUserActivities"));
        list.Add(Pol("policy.cloud.disablesoftlanding", "Disable Soft Landing", "Post-upgrade tips.", "Consumer experience.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableSoftLanding"));
        list.Add(Pol("policy.cloud.disablecloudoptimized", "Disable Cloud Optimized Content", "Cloud-optimized content.", "Consumer experience.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableCloudOptimizedContent"));
        list.Add(Pol("policy.cloud.disablewindowsspotlight.hkcu", "Disable Windows Spotlight (User Policy)", "Spotlight features for user.", "User-scoped policy.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKCU\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableWindowsSpotlightFeatures"));
        list.Add(Pol("policy.cloud.disabletailored.hkcu", "Disable Tailored Experiences (User Policy)", "Diagnostic personalization for user.", "User-scoped policy.", RiskLevel.Medium, FeatureCategory.CloudComponent, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKCU\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableTailoredExperiencesWithDiagnosticData"));

        // ---- Location / Device / Biometrics ----
        list.Add(Pol("policy.location.disablelocationscripting", "Disable Location Scripting", "Script access to location.", "Scripting surface.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Location, "Location", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableLocationScripting"));
        list.Add(Pol("policy.location.disablewindowslocationsupplier", "Disable Windows Location Provider", "Built-in location provider.", "Location stack.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Location, "Location", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableWindowsLocationProvider"));
        list.Add(Pol("policy.findmydevice.allow", "Allow Find My Device", "Find My Device policy.", "Device tracking.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "Device", @"HKLM\SOFTWARE\Policies\Microsoft\FindMyDevice\AllowFindMyDevice"));
        list.Add(Pol("policy.device.metadataretrieval", "Prevent Device Metadata From Network", "Device metadata download.", "Network metadata.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "Device", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Device Metadata\PreventDeviceMetadataFromNetwork"));
        list.Add(Pol("policy.biometrics.enabled", "Biometrics Enabled", "Windows biometrics service policy.", "Fingerprint/face.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Biometrics, "Biometrics", @"HKLM\SOFTWARE\Policies\Microsoft\Biometrics\Enabled"));
        list.Add(Pol("policy.biometrics.facialfeatures", "Enhanced Anti-Spoofing (Facial)", "Facial anti-spoofing.", "Windows Hello face.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Biometrics, "Biometrics", @"HKLM\SOFTWARE\Policies\Microsoft\Biometrics\FacialFeatures\EnhancedAntiSpoofing"));

        // ---- UAC (observation + selective write for ConsentPromptBehaviorAdmin) ----
        list.Add(Pol("policy.uac.consentpromptbehavioradmin", "UAC Consent Prompt Behavior (Admin)", "Elevation prompt behavior for administrators.", "Core UAC posture.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "UAC", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\ConsentPromptBehaviorAdmin"));
        list.Add(Pol("policy.uac.enablelua", "Enable LUA (UAC)", "User Account Control master switch.", "Disabling weakens the security boundary.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "UAC", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\EnableLUA"));
        list.Add(Pol("policy.uac.promptonsecuredesktop", "Prompt on Secure Desktop", "UAC prompts on secure desktop.", "Reduces prompt spoofing when enabled.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "UAC", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\PromptOnSecureDesktop"));
        list.Add(Pol("policy.uac.filteradministratortoken", "Filter Administrator Token", "Admin Approval Mode filtering.", "Token filtering.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "UAC", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\FilterAdministratorToken"));

        // ---- BitLocker / device encryption policies (OBSERVATION PRIMARY; writes only where explicitly authorized later) ----
        list.Add(Pol("policy.bitlocker.enablob", "Require Additional Authentication at Startup", "BitLocker startup authentication policy.", "TPM/PIN/USB startup auth.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "BitLocker", @"HKLM\SOFTWARE\Policies\Microsoft\FVE\UseAdvancedStartup"));
        list.Add(Pol("policy.bitlocker.preventdeviceencryption", "Prevent Automatic Device Encryption", "Controls whether Windows may automatically enable device encryption during eligible device setup.", "This is not the live BitLocker protection state and does not decrypt an already protected volume.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "BitLocker", @"HKLM\SYSTEM\CurrentControlSet\Control\BitLocker\PreventDeviceEncryption"));
        list.Add(Pol("policy.bitlocker.encryptionmethod", "OS Volume Encryption Method", "BitLocker encryption method for OS volume.", "Cipher strength policy.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "BitLocker", @"HKLM\SOFTWARE\Policies\Microsoft\FVE\EncryptionMethodWithXtsOs"));
        list.Add(Pol("policy.bitlocker.recoverypassword", "OS Recovery Password Requirement", "Require recovery password for OS drive.", "Recovery key policy.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "BitLocker", @"HKLM\SOFTWARE\Policies\Microsoft\FVE\OSRecoveryPassword"));
        list.Add(Pol("policy.bitlocker.activeDirectoryBackup", "Store Recovery Info in AD DS", "Backup BitLocker recovery to AD.", "Enterprise recovery.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Device, "BitLocker", @"HKLM\SOFTWARE\Policies\Microsoft\FVE\OSActiveDirectoryBackup"));

        // ---- Edge privacy (observation; selective write via existing tracking prevention) ----
        list.Add(Pol("policy.edge.metricsreporting", "Edge Metrics Reporting", "Usage/metrics reporting.", "Privacy.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\MetricsReportingEnabled"));
        list.Add(Pol("policy.edge.personalizationreporting", "Edge Personalization Reporting", "Personalization data reporting.", "Privacy.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\PersonalizationReportingEnabled"));
        list.Add(Pol("policy.edge.searchsuggest", "Edge Search Suggestions", "Search suggestions.", "Privacy.", RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\SearchSuggestEnabled"));
        list.Add(Pol("policy.edge.passwordmanager", "Edge Password Manager", "Built-in password manager.", "Credential storage.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\PasswordManagerEnabled"));
        list.Add(Pol("policy.edge.autofilladdress", "Edge Autofill Addresses", "Address autofill.", "Personal data.", RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\AutofillAddressEnabled"));
        list.Add(Pol("policy.edge.autofillcreditcard", "Edge Autofill Credit Cards", "Payment autofill.", "Financial data.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\AutofillCreditCardEnabled"));
        list.Add(Pol("policy.edge.sendsitinfo", "Edge Send Site Info to Improve Services", "Site info for service improvement.", "Privacy.", RiskLevel.Low, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\SendSiteInfoToImproveServices"));

        // ---- Services (observation-only catalog anchors; DiscoveryMethod is not a write path) ----
        list.Add(Svc("service.diagtrack", "Connected User Experiences and Telemetry (DiagTrack)", "Telemetry service state.", "Privacy-related service.", RiskLevel.High, ProductDomain.Telemetry, "Services", "ServiceController:DiagTrack"));
        list.Add(Svc("service.dmwappushservice", "Device Management Wireless Application Protocol (dmwappushservice)", "WAP push / device management related.", "Often tied to telemetry discussions.", RiskLevel.Medium, ProductDomain.Telemetry, "Services", "ServiceController:dmwappushservice"));
        list.Add(Svc("service.wuaserv", "Windows Update (wuauserv)", "Windows Update service.", "Patch delivery.", RiskLevel.High, ProductDomain.WindowsUpdate, "Services", "ServiceController:wuauserv"));
        list.Add(Svc("service.windefend", "Microsoft Defender Antivirus Service (WinDefend)", "Defender service.", "Core host protection.", RiskLevel.High, ProductDomain.Defender, "Services", "ServiceController:WinDefend"));
        list.Add(Svc("service.sense", "Windows Defender Advanced Threat Protection Service (Sense)", "MDE / Sense service when present.", "Enterprise detection.", RiskLevel.High, ProductDomain.Defender, "Services", "ServiceController:Sense"));
        list.Add(Svc("service.mpssvc", "Windows Defender Firewall (MpsSvc)", "Firewall service.", "Network boundary.", RiskLevel.High, ProductDomain.Firewall, "Services", "ServiceController:MpsSvc"));

        return list.AsReadOnly();
    }

    private static ManagedObject Pol(string id, string name, string description, string rationale, RiskLevel risk, FeatureCategory category, ComponentOwner owner, ControlLevel control, ProductDomain domain, string subCategory, string discovery)
    {
        var iface = category == FeatureCategory.EdgePolicy ? InterfaceName.GroupPolicy : category == FeatureCategory.DefenderSetting ? InterfaceName.Defender : InterfaceName.GroupPolicy;
        var cfg = category == FeatureCategory.DefenderSetting ? ConfigurationType.DefenderSettingValue : ConfigurationType.PolicyState;
        return new ManagedObject
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = "PolicySetting",
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
            SchemaVersion = "2.1",
            CreatedBy = "CatalogExpansion",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 75,
            ConfidenceSource = "Catalog-v2.1",
            SupportedWindowsVersions = ["Windows 10", "Windows 11"]
        };
    }

    private static ManagedObject Svc(string id, string name, string description, string rationale, RiskLevel risk, ProductDomain domain, string subCategory, string discovery)
    {
        return new ManagedObject
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = "ServiceSetting",
            Description = description,
            Rationale = rationale,
            FeatureCategory = FeatureCategory.WindowsService,
            ProductDomain = domain,
            SubCategory = subCategory,
            RiskLevel = risk,
            ImpactLevel = ImpactLevel.Security,
            LifecycleState = LifecycleState.Active,
            InterfaceName = InterfaceName.ServiceControlManager,
            ConfigurationType = ConfigurationType.ServiceState,
            DiscoveryMethod = discovery,
            CanonicalPath = id,
            ControlLevel = ControlLevel.AdministratorControlled,
            ComponentOwner = ComponentOwner.Other,
            PriorityLevel = PriorityLevel.Recommended,
            Reversibility = Reversibility.PartiallyReversible,
            RebootRequirement = RebootRequirement.ServiceRestart,
            SchemaVersion = "2.1",
            CreatedBy = "CatalogExpansion",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 70,
            ConfidenceSource = "Catalog-v2.1",
            SupportedWindowsVersions = ["Windows 10", "Windows 11"],
            CommonMisconception = "Service state alone does not prove policy intent. Do not disable security services without understanding dependencies."
            // No WritableTarget — observation only.
        };
    }
}
