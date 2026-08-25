// Source/WindowsPrivacyPlatform.Scanner/PolicyCollector.cs
using System;
using System.Globalization;
using System.Linq;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for high-value privacy and security policy/preference registry values.
    /// Uses RegistryView.Registry64 so observations match PolicyChangeService writes on 64-bit Windows.
    /// Missing values are recorded as "Not configured". Never writes. Never elevates.
    /// </summary>
    public sealed class PolicyCollector : IInventoryCollector
    {
        public string Name => "PolicyCollector";

        private sealed record Probe(
            string Id,
            string Category,
            RegistryHive Hive,
            string SubKey,
            string ValueName);

        private static readonly Probe[] Probes =
        {
            new("policy.telemetry.allowtelemetry", "Telemetry", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry"),
            new("policy.telemetry.allowtelemetry.currentversion", "Telemetry", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry"),
            new("policy.telemetry.donotshowfeedback", "Telemetry", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "DoNotShowFeedbackNotifications"),
            new("policy.telemetry.allowdevicename", "Telemetry", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowDeviceNameInTelemetry"),
            new("policy.telemetry.limitdiagnosticlogs", "Telemetry", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "LimitDiagnosticLogCollection"),

            new("policy.update.noautoupdate", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate"),
            new("policy.update.auoptions", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions"),
            new("policy.update.scheduledinstallday", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "ScheduledInstallDay"),
            new("policy.update.scheduledinstalltime", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "ScheduledInstallTime"),
            new("policy.update.autoinstallminor", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AutoInstallMinorUpdates"),
            new("policy.update.detectionfrequency", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "DetectionFrequency"),
            new("policy.update.disableuxwuaccess", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "SetDisableUXWUAccess"),
            new("policy.update.disablewuaccess", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableWindowsUpdateAccess"),
            new("policy.update.donotconnectinternet", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DoNotConnectToWindowsUpdateInternetLocations"),
            new("policy.update.excludewudrivers", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate"),
            new("policy.update.ux.branchreadiness", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "BranchReadinessLevel"),
            new("policy.update.ux.flightsettings", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "FlightSettingsMaxPauseDays"),
            new("policy.update.ux.pausefeatureupdatesstart", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseFeatureUpdatesStartTime"),
            new("policy.update.ux.pausequalityupdatesstart", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseQualityUpdatesStartTime"),
            new("policy.deliveryopt.downloadmode", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode"),
            new("policy.update.deferfeatureupdates", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays"),
            new("policy.update.deferqualityupdates", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferQualityUpdatesPeriodInDays"),
            new("policy.update.wuserver", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUServer"),
            new("policy.update.wustatusserver", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUStatusServer"),
            new("policy.update.targetreleaseversion", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "TargetReleaseVersion"),
            new("policy.update.targetreleaseversioninfo", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "TargetReleaseVersionInfo"),
            new("policy.update.managepreviewbuilds", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ManagePreviewBuilds"),
            new("policy.update.allowmuupdateservice", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "AllowMUUpdateService"),
            new("policy.update.elevatednonadmins", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "ElevateNonAdmins"),
            new("policy.update.setedurestart", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "SetEDURestart"),
            new("policy.update.disabledualscan", "WindowsUpdate", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DisableDualScan"),

            new("policy.defender.disableantispyware", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware"),
            new("policy.defender.disablerealtime", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring"),
            new("policy.defender.disablebehaviormonitor", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableBehaviorMonitoring"),
            new("policy.defender.disableioav", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableIOAVProtection"),
            new("policy.defender.spynetreporting", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SpynetReporting"),
            new("policy.defender.submitsamples", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SubmitSamplesConsent"),
            new("policy.defender.puaprotection", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "PUAProtection"),
            new("policy.defender.enablenetworkprotection", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection", "EnableNetworkProtection"),
            new("policy.defender.enablecontrolledfolderaccess", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access", "EnableControlledFolderAccess"),
            new("policy.defender.cloudblocklevel", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine", "MpCloudBlockLevel"),
            new("policy.defender.disableblockatfirstseen", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "DisableBlockAtFirstSeen"),
            new("policy.defender.disablescriptscanning", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableScriptScanning"),
            new("policy.defender.disablecatchupfullscan", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Scan", "DisableCatchupFullScan"),
            new("policy.defender.disablecatchupquickscan", "Defender", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Scan", "DisableCatchupQuickScan"),

            new("policy.smartscreen.enable", "SmartScreen", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableSmartScreen"),
            new("policy.smartscreen.shelllevel", "SmartScreen", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "ShellSmartScreenLevel"),

            new("policy.search.allowcortana", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana"),
            new("policy.search.disablewebsearch", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch"),
            new("policy.search.connectedsearchuseweb", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb"),
            new("policy.search.allowsearchlocation", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowSearchToUseLocation"),
            new("policy.search.allowcloudsearch", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCloudSearch"),
            new("policy.search.disableremovabledriveindexing", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableRemovableDriveIndexing"),
            new("policy.search.highlights", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "EnableDynamicContentInWSB"),
            new("policy.search.disablesearch", "Search", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableSearch"),

            new("policy.activity.enableactivityfeed", "ActivityHistory", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed"),
            new("policy.activity.publishuseractivities", "ActivityHistory", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities"),
            new("policy.activity.uploaduseractivities", "ActivityHistory", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities"),

            new("policy.cloud.disableconsumerfeatures", "CloudContent", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures"),
            new("policy.cloud.disablesoftlanding", "CloudContent", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding"),
            new("policy.cloud.disablecloudoptimized", "CloudContent", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableCloudOptimizedContent"),
            new("policy.cloud.disablewindowsspotlight.hkcu", "CloudContent", RegistryHive.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures"),
            new("policy.cloud.disabletailored.hkcu", "CloudContent", RegistryHive.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableTailoredExperiencesWithDiagnosticData"),

            new("policy.advertising.disabledbygpo", "Advertising", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy"),

            new("policy.location.disablelocation", "Location", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation"),
            new("policy.location.disablelocationscripting", "Location", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocationScripting"),
            new("policy.location.disablewindowslocationsupplier", "Location", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableWindowsLocationProvider"),

            new("policy.appprivacy.location", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessLocation"),
            new("policy.appprivacy.camera", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCamera"),
            new("policy.appprivacy.microphone", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMicrophone"),
            new("policy.appprivacy.accountinfo", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessAccountInfo"),
            new("policy.appprivacy.contacts", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessContacts"),
            new("policy.appprivacy.calendar", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCalendar"),
            new("policy.appprivacy.email", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessEmail"),
            new("policy.appprivacy.callhistory", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessCallHistory"),
            new("policy.appprivacy.messaging", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessMessaging"),
            new("policy.appprivacy.radios", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessRadios"),
            new("policy.appprivacy.documents", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessDocuments"),
            new("policy.appprivacy.pictures", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessPictures"),
            new("policy.appprivacy.videos", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessVideos"),
            new("policy.appprivacy.filesystem", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsAccessFileSystem"),
            new("policy.appprivacy.appdiagnostics", "AppPrivacy", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsGetDiagnosticInfo"),

            new("policy.findmydevice.allow", "Device", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\FindMyDevice", "AllowFindMyDevice"),
            new("policy.device.metadataretrieval", "Device", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork"),

            new("policy.feedback.numberoffeedbacksiuf", "Feedback", RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod"),
            new("policy.feedback.periodinsiuf", "Feedback", RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Siuf\Rules", "PeriodInNanoSeconds"),

            new("policy.onedrive.disablefilesyncngsc", "CloudContent", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\OneDrive", "DisableFileSyncNGSC"),
            new("policy.onedrive.filesondemand", "CloudContent", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\OneDrive", "FilesOnDemandEnabled"),
            new("policy.explorer.allowonlinecontent", "Explorer", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "AllowOnlineTips"),
            new("policy.explorer.norecentserverdocs", "Explorer", RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoRecentDocsHistory"),

            new("policy.biometrics.enabled", "Biometrics", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Biometrics", "Enabled"),
            new("policy.biometrics.facialfeatures", "Biometrics", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Biometrics\FacialFeatures", "EnhancedAntiSpoofing"),

            new("policy.uac.enablelua", "UAC", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA"),
            new("policy.uac.consentpromptbehavioradmin", "UAC", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin"),
            new("policy.uac.filteradministratortoken", "UAC", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "FilterAdministratorToken"),
            new("policy.uac.promptonsecuredesktop", "UAC", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop"),

            new("policy.bitlocker.enablob", "BitLocker", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\FVE", "UseAdvancedStartup"),
            new("policy.bitlocker.preventdeviceencryption", "BitLocker", RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption"),
            new("policy.bitlocker.encryptionmethod", "BitLocker", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\FVE", "EncryptionMethodWithXtsOs"),
            new("policy.bitlocker.recoverypassword", "BitLocker", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\FVE", "OSRecoveryPassword"),
            new("policy.bitlocker.activeDirectoryBackup", "BitLocker", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\FVE", "OSActiveDirectoryBackup"),

            new("policy.clipboard.allowhistory", "Clipboard", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory"),
            new("policy.clipboard.allowcrossdevice", "Clipboard", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard"),

            new("policy.edge.autofilladdress", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "AutofillAddressEnabled"),
            new("policy.edge.autofillcreditcard", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "AutofillCreditCardEnabled"),
            new("policy.edge.passwordmanager", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "PasswordManagerEnabled"),
            new("policy.edge.searchsuggest", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "SearchSuggestEnabled"),
            new("policy.edge.alternateerrorpages", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "AlternateErrorPagesEnabled"),
            new("policy.edge.paymentmethods", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "PaymentMethodQueryEnabled"),
            new("policy.edge.personalizationreporting", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "PersonalizationReportingEnabled"),
            new("policy.edge.metricsreporting", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "MetricsReportingEnabled"),
            new("policy.edge.sendsitinfo", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "SendSiteInfoToImproveServices"),
            new("policy.edge.trackingprevention", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "TrackingPrevention")
            ,new("policy.edge.m365copiloticon", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "Microsoft365CopilotChatIconEnabled")
            ,new("policy.edge.familysafety", "Edge", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "FamilySafetySettingsEnabled")

            ,new("policy.recall.disableaidataanalysis", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis")
            ,new("policy.recall.allowenablement", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "AllowRecallEnablement")
            ,new("policy.recall.allowexport", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "AllowRecallExport")
            ,new("policy.recall.denyapplist", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "SetDenyAppListForRecall")
            ,new("policy.recall.denyurilist", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "SetDenyUriListForRecall")
            ,new("policy.recall.maxstorage", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "SetMaximumStorageSpaceForRecallSnapshots")
            ,new("policy.recall.maxduration", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "SetMaximumStorageDurationForRecallSnapshots")
            ,new("policy.recall.disableclicktodo", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableClickToDo")
            ,new("policy.copilot.turnoffwindowscopilot", "WindowsAI", RegistryHive.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot")
            ,new("policy.copilot.removemicrosoftcopilotapp", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "RemoveMicrosoftCopilotApp")
            ,new("policy.copilot.disablesettingsagent", "WindowsAI", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableSettingsAgent")

            ,new("policy.asr.vulnerablesigneddrivers", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "56a863a9-875e-4185-98a7-b882c64b5ce5")
            ,new("policy.asr.lsasscredentialtheft", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "9e6c4e1f-7d60-472f-ba1a-a39ef669e4b2")
            ,new("policy.asr.officechildprocess", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "d4f940ab-401b-4efc-aadc-ad5f3c50688a")
            ,new("policy.asr.officeexecutablecontent", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "3b576869-a4ec-4529-8536-b80a7769e899")
            ,new("policy.asr.officecodeinjection", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "75668c1f-73b5-4cf0-bb93-3ecf5cb7cc84")
            ,new("policy.asr.officemacrowin32", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "92e97fa1-2edf-4476-bdd6-9dd0b4dddc7b")
            ,new("policy.asr.scriptdownload", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "d3e037e1-3eb8-44c8-a917-57927947596d")
            ,new("policy.asr.emailwebmailcontent", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "be9ba2d9-53ea-4cdc-84e5-9b1eeee46550")
            ,new("policy.asr.advancedransomware", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "c1db55ab-c21a-4637-bb3f-a12568109d35")
            ,new("policy.asr.prevalenceage", "DefenderASR", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules", "01443614-cd74-433a-b99e-2ecdc07bfc25")

            ,new("policy.hello.enabled", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\PassportForWork", "Enabled")
            ,new("policy.hello.requiresecuritydevice", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\PassportForWork", "RequireSecurityDevice")
            ,new("policy.hello.usebiometrics", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WinBio\Credential Provider", "Domain Accounts")
            ,new("policy.hello.enhancedsigninsecurity", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Policies\PassportForWork\Biometrics", "EnableESSwithSupportedPeripherals")
            ,new("policy.hello.pinrecovery", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\PassportForWork", "EnablePinRecovery")
            ,new("policy.hello.minpinlength", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\PassportForWork\PINComplexity", "MinimumPINLength")
            ,new("policy.hello.pinexpiration", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\PassportForWork\PINComplexity", "Expiration")
            ,new("policy.hello.cloudtrust", "WindowsHello", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\PassportForWork", "UseCloudTrustForOnPremAuth")

            ,new("policy.storage.senseglobal", "StorageSense", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseGlobal")
            ,new("policy.storage.temporaryfiles", "StorageSense", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "AllowStorageSenseTemporaryFilesCleanup")
            ,new("policy.storage.cadence", "StorageSense", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "ConfigStorageSenseGlobalCadence")
            ,new("policy.storage.recyclebinthreshold", "StorageSense", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "ConfigStorageSenseRecycleBinCleanupThreshold")
            ,new("policy.storage.downloadsthreshold", "StorageSense", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "ConfigStorageSenseDownloadsCleanupThreshold")
            ,new("policy.storage.clouddehydration", "StorageSense", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\StorageSense", "ConfigStorageSenseCloudContentDehydrationThreshold")
            ,new("policy.widgets.allow", "Widgets", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests")

            ,new("policy.accessibility.disablesettingssync", "Accessibility", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SettingSync", "DisableAccessibilitySettingSync")

            ,new("policy.network.dohmode", "Network", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "DoHPolicy")
            ,new("policy.network.dohsetting", "Network", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient", "DohPolicySetting")
            ,new("policy.network.blocknondomain", "Network", RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WcmSvc\GroupPolicy", "fBlockNonDomain")
        };

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            foreach (var probe in Probes)
            {
                try
                {
                    snapshot.PolicySettings.Add(ReadProbe(probe));
                }
                catch (Exception ex)
                {
                    snapshot.PolicySettings.Add(new PolicySettingInfo
                    {
                        Name = probe.Id,
                        Category = probe.Category,
                        Hive = probe.Hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU",
                        Path = probe.SubKey,
                        ValueName = probe.ValueName,
                        Value = "Error reading: " + ex.Message
                    });
                }
            }

            snapshot.PolicySettings.Add(ReadRandomMacState());
        }

        private static PolicySettingInfo ReadRandomMacState()
        {
            const string subKey = @"SOFTWARE\Microsoft\WlanSvc\Interfaces";
            var info = new PolicySettingInfo
            {
                Name = "network.wifi.randommac",
                Category = "Network",
                Hive = "HKLM",
                Path = subKey + @"\*",
                ValueName = "RandomMacState",
                Value = "Not configured"
            };

            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                using var interfaces = baseKey.OpenSubKey(subKey, writable: false);
                if (interfaces is null)
                    return info;

                var observed = interfaces.GetSubKeyNames()
                    .Select(interfaceName =>
                    {
                        using var key = interfaces.OpenSubKey(interfaceName, writable: false);
                        if (key is null || !key.GetValueNames().Any(name =>
                                name.Equals("RandomMacState", StringComparison.OrdinalIgnoreCase)))
                            return null;

                        var raw = key.GetValue("RandomMacState", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        return raw is null
                            ? null
                            : $"{interfaceName}={FormatValue(raw, key.GetValueKind("RandomMacState"))}";
                    })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                if (observed.Count > 0)
                    info.Value = string.Join("; ", observed!);
            }
            catch (Exception ex)
            {
                info.Value = "Error reading: " + ex.Message;
            }

            return info;
        }

        private static PolicySettingInfo ReadProbe(Probe probe)
        {
            var info = new PolicySettingInfo
            {
                Name = probe.Id,
                Category = probe.Category,
                Hive = probe.Hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU",
                Path = probe.SubKey,
                ValueName = probe.ValueName,
                Value = "Not configured"
            };

            // Registry64 matches PolicyChangeService write view — critical for post-change scan consistency.
            using var baseKey = RegistryKey.OpenBaseKey(probe.Hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(probe.SubKey, writable: false);
            if (key is null)
                return info;

            var names = key.GetValueNames();
            var exists = names.Any(n => string.Equals(n, probe.ValueName, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                return info;

            var raw = key.GetValue(probe.ValueName, defaultValue: null, options: RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is null)
                return info;

            var kind = key.GetValueKind(probe.ValueName);
            info.Value = FormatValue(raw, kind);
            return info;
        }

        private static string FormatValue(object raw, RegistryValueKind kind)
        {
            return kind switch
            {
                RegistryValueKind.DWord => Convert.ToInt32(raw, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture),
                RegistryValueKind.QWord => Convert.ToInt64(raw, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture),
                RegistryValueKind.Binary when raw is byte[] bytes => BitConverter.ToString(bytes),
                RegistryValueKind.MultiString when raw is string[] arr => string.Join(";", arr),
                _ => raw.ToString()?.Trim() ?? "Not configured"
            };
        }
    }
}
