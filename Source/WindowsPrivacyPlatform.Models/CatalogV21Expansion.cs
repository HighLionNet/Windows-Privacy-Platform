namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// v2.1 coverage additions. Registry-backed entries are grounded in Microsoft ADMX/Policy CSP
/// mappings; inventory anchors are presence/state observations only and intentionally non-writable.
/// </summary>
public static class CatalogV21Expansion
{
    public static IReadOnlyList<ManagedObject> CreateBatch()
    {
        var entries = new List<ManagedObject>();

        AddAlreadyCollectedPolicies(entries);
        AddWindowsAi(entries);
        AddDefenderAsr(entries);
        AddWindowsHello(entries);
        AddStorageAndShell(entries);
        AddNetwork(entries);
        AddLocalSecurity(entries);
        AddInventoryAnchors(entries);

        return entries.AsReadOnly();
    }

    private static void AddAlreadyCollectedPolicies(List<ManagedObject> entries)
    {
        entries.Add(Policy("policy.telemetry.allowdevicename", "Allow Device Name in Diagnostic Data", "Controls whether the device name can be included in Windows diagnostic data sent under the configured telemetry level.", "Device names can contain organization or user-identifying information.", ProductDomain.Telemetry, "Diagnostic data", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowDeviceNameInTelemetry"));
        entries.Add(Policy("policy.telemetry.limitdiagnosticlogs", "Limit Diagnostic Log Collection", "Limits optional diagnostic log collection when Windows requests extra troubleshooting data in addition to structured diagnostic events.", "The policy narrows supplemental logs but does not turn off required diagnostic events or Windows Error Reporting by itself.", ProductDomain.Telemetry, "Diagnostic data", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\LimitDiagnosticLogCollection"));
        entries.Add(Policy("policy.update.scheduledinstallday", "Scheduled Install Day", "Selects the weekday used by the legacy scheduled automatic-update installation mode.", "The value is effective only with a compatible AUOptions schedule mode.", ProductDomain.WindowsUpdate, "Installation schedule", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\ScheduledInstallDay"));
        entries.Add(Policy("policy.update.scheduledinstalltime", "Scheduled Install Time", "Selects the hour used by the legacy scheduled automatic-update installation mode.", "Schedule timing affects restart planning and exposure before installation.", ProductDomain.WindowsUpdate, "Installation schedule", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\ScheduledInstallTime"));
        entries.Add(Policy("policy.update.autoinstallminor", "Install Minor Updates Immediately", "Controls immediate installation of updates classified by Windows Update as minor and restart-free.", "The legacy policy can change update timing without configuring the complete servicing strategy.", ProductDomain.WindowsUpdate, "Installation behavior", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\AutoInstallMinorUpdates"));
        entries.Add(Policy("policy.update.detectionfrequency", "Update Detection Frequency", "Defines the legacy Windows Update detection interval in hours when its companion enable policy is active.", "Short intervals increase update-service activity; long intervals delay detection.", ProductDomain.WindowsUpdate, "Detection", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\DetectionFrequency"));
        entries.Add(Policy("policy.update.ux.branchreadiness", "Branch Readiness Level", "Records the Windows Update UX readiness channel selected for feature-update offers.", "This preference is build-dependent and should not be confused with a target release pin.", ProductDomain.WindowsUpdate, "UX state", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\BranchReadinessLevel"));
        entries.Add(Policy("policy.update.ux.flightsettings", "Maximum Pause Days", "Records the maximum update-pause duration exposed by the Windows Update UX on this image.", "The setting affects pause limits rather than proving that updates are currently paused.", ProductDomain.WindowsUpdate, "UX state", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\FlightSettingsMaxPauseDays"));
        entries.Add(Policy("policy.update.ux.pausefeatureupdatesstart", "Feature Update Pause Start", "Records the timestamp from which feature updates are paused in the Windows Update UX state store.", "A timestamp documents a pause window; it is not a permanent feature-update block.", ProductDomain.WindowsUpdate, "UX state", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\PauseFeatureUpdatesStartTime"));
        entries.Add(Policy("policy.update.ux.pausequalityupdatesstart", "Quality Update Pause Start", "Records the timestamp from which quality updates are paused in the Windows Update UX state store.", "Quality-update pauses can defer security fixes and expire automatically.", ProductDomain.WindowsUpdate, "UX state", @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings\PauseQualityUpdatesStartTime", RiskLevel.High));
        entries.Add(Policy("policy.update.wuserver", "WSUS Update Service URL", "Specifies the intranet update-service endpoint used when client-side WSUS policy is enabled.", "An unreachable or untrusted endpoint can prevent managed update discovery.", ProductDomain.WindowsUpdate, "WSUS", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\WUServer", RiskLevel.High));
        entries.Add(Policy("policy.update.wustatusserver", "WSUS Statistics Server URL", "Specifies the intranet endpoint to which the Windows Update client reports status when WSUS policy is active.", "Reporting may be separated from content service, so both URLs must be evaluated.", ProductDomain.WindowsUpdate, "WSUS", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\WUStatusServer"));
        entries.Add(Policy("policy.update.setedurestart", "Engaged Restart Transition", "Enables the Windows Update engaged-restart transition policy for managed restart scheduling.", "Restart policy affects availability and user disruption after update installation.", ProductDomain.WindowsUpdate, "Restart", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\SetEDURestart"));
        entries.Add(Policy("policy.search.disableremovabledriveindexing", "Prevent Removable Drive Indexing", "Prevents removable-drive locations from being added to libraries and the Windows Search index.", "Removable media can contain sensitive data and can also expand index scope.", ProductDomain.Search, "Indexing", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\DisableRemovableDriveIndexing"));
        entries.Add(Policy("policy.feedback.numberoffeedbacksiuf", "Feedback Prompts per Period", "Stores how many feedback prompts Windows may show during the configured SIUF feedback period.", "Together with the period value, this controls feedback prompt frequency rather than diagnostic collection.", ProductDomain.Telemetry, "Feedback", @"HKCU\SOFTWARE\Microsoft\Siuf\Rules\NumberOfSIUFInPeriod"));
        entries.Add(Policy("policy.feedback.periodinsiuf", "Feedback Prompt Period", "Stores the SIUF feedback prompt period in 100-nanosecond units for the current user.", "The period must be interpreted with NumberOfSIUFInPeriod; zero values conventionally represent never prompt.", ProductDomain.Telemetry, "Feedback", @"HKCU\SOFTWARE\Microsoft\Siuf\Rules\PeriodInNanoSeconds"));
        entries.Add(Policy("policy.onedrive.disablefilesyncngsc", "Prevent OneDrive File Sync", "Prevents the modern OneDrive sync client from providing file storage and synchronization.", "This policy disables the sync client; it is distinct from Files On-Demand hydration behavior.", ProductDomain.CloudContent, "OneDrive", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive\DisableFileSyncNGSC"));
        entries.Add(Policy("policy.onedrive.filesondemand", "Use OneDrive Files On-Demand", "Controls whether OneDrive can expose online-only placeholders and hydrate file content when opened.", "Files On-Demand reduces disk use but depends on the OneDrive client and Cloud Files filter driver.", ProductDomain.Storage, "OneDrive", @"HKLM\SOFTWARE\Policies\Microsoft\OneDrive\FilesOnDemandEnabled"));
        entries.Add(Policy("policy.explorer.allowonlinecontent", "Allow Online Tips", "Controls whether Windows Settings retrieves online tips and help content from Microsoft content services.", "Disabling online tips reduces this content request without disabling Windows Update or Store traffic.", ProductDomain.CloudContent, "Settings content", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\AllowOnlineTips"));
        entries.Add(Policy("policy.explorer.norecentserverdocs", "Do Not Keep Recent Document History", "Controls whether Explorer records recently opened documents for the current user.", "Recent-item history improves convenience but reveals local document activity.", ProductDomain.ActivityHistory, "Explorer history", @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer\NoRecentDocsHistory"));
        entries.Add(Policy("policy.edge.alternateerrorpages", "Edge Similar Page Suggestions", "Controls whether Edge contacts a web service to suggest alternatives when a requested page cannot be found.", "Service-backed error suggestions disclose the failed navigation context to the browser service.", ProductDomain.Edge, "Navigation services", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\AlternateErrorPagesEnabled"));
        entries.Add(Policy("policy.edge.paymentmethods", "Edge Payment Method Availability Query", "Controls whether websites can ask Edge if the user has an enrolled payment method.", "Disabling the query reduces passive payment-profile capability disclosure to websites.", ProductDomain.Edge, "Payments", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\PaymentMethodQueryEnabled"));
    }

    private static void AddWindowsAi(List<ManagedObject> entries)
    {
        entries.Add(Policy("policy.recall.disableaidataanalysis", "Turn Off Recall Snapshot Saving", "Controls whether Windows may save screen snapshots for Recall; enabling the policy disables snapshot saving and removes existing snapshots.", "Recall snapshots can contain visible application, document, and web content.", ProductDomain.Recall, "Snapshots", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableAIDataAnalysis", RiskLevel.High));
        entries.Add(Policy("policy.recall.allowenablement", "Allow Recall Enablement", "Controls whether the Recall optional component is available for a user to enable on supported Windows 11 devices.", "Availability does not itself opt the user into snapshot saving.", ProductDomain.Recall, "Availability", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\AllowRecallEnablement", RiskLevel.High));
        entries.Add(Policy("policy.recall.allowexport", "Allow Recall Export", "Controls user-initiated export of Recall snapshots and related information where export is supported.", "Export creates a portable copy of highly sensitive activity data and is EEA-specific in current Windows releases.", ProductDomain.Recall, "Data portability", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\AllowRecallExport", RiskLevel.High));
        entries.Add(Policy("policy.recall.denyapplist", "Recall App Exclusion List", "Specifies executable names or application identifiers whose content Recall must exclude from snapshots.", "The deny list supplements user exclusions but does not disable Recall globally.", ProductDomain.Recall, "Filtering", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\SetDenyAppListForRecall", RiskLevel.High));
        entries.Add(Policy("policy.recall.denyurilist", "Recall URI Exclusion List", "Specifies URI patterns that supported browsers must filter from Recall snapshots.", "Filtering depends on browser support and does not cover content displayed outside supported URI-aware integrations.", ProductDomain.Recall, "Filtering", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\SetDenyUriListForRecall", RiskLevel.High));
        entries.Add(Policy("policy.recall.maxstorage", "Recall Maximum Snapshot Storage", "Sets the maximum disk space allocated to Recall snapshots on supported devices.", "A storage cap limits retention volume but does not determine which content is captured.", ProductDomain.Recall, "Retention", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\SetMaximumStorageSpaceForRecallSnapshots"));
        entries.Add(Policy("policy.recall.maxduration", "Recall Maximum Snapshot Retention", "Sets the maximum duration for retaining Recall snapshots on supported devices.", "Shorter retention reduces historical exposure while limiting how far back Recall can search.", ProductDomain.Recall, "Retention", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\SetMaximumStorageDurationForRecallSnapshots"));
        entries.Add(Policy("policy.recall.disableclicktodo", "Disable Click to Do", "Controls the Windows AI Click to Do experience that analyzes selected on-screen content for contextual actions.", "Disabling Click to Do is separate from disabling Recall snapshot saving.", ProductDomain.Recall, "On-screen analysis", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableClickToDo"));
        entries.Add(Policy("policy.copilot.turnoffwindowscopilot", "Turn Off Legacy Windows Copilot", "Controls the deprecated in-box Windows Copilot pane and its taskbar entry for the current user.", "Microsoft documents that this policy does not govern every newer Copilot app experience.", ProductDomain.Copilot, "Windows Copilot", @"HKCU\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot\TurnOffWindowsCopilot"));
        entries.Add(Policy("policy.copilot.removemicrosoftcopilotapp", "Remove Microsoft Copilot App", "Controls removal of the Microsoft Copilot app for users on Windows builds that support this Windows AI policy.", "App removal is distinct from tenant-level Microsoft 365 Copilot service policy.", ProductDomain.Copilot, "Copilot app", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\RemoveMicrosoftCopilotApp"));
        entries.Add(Policy("policy.copilot.disablesettingsagent", "Disable Settings Agent", "Controls the Windows AI agent that can assist with Settings tasks on supported builds.", "This policy affects the Settings agent rather than every Copilot or Windows AI capability.", ProductDomain.Copilot, "Windows AI agents", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableSettingsAgent"));
        entries.Add(Policy("policy.edge.m365copiloticon", "Microsoft 365 Copilot Chat Icon in Edge", "Controls whether Microsoft 365 Copilot Chat appears in the Edge for Business toolbar for signed-in work profiles.", "Hiding the icon does not revoke tenant licensing or block Copilot service access through other entry points.", ProductDomain.Copilot, "Edge for Business", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\Microsoft365CopilotChatIconEnabled"));
    }

    private static void AddDefenderAsr(List<ManagedObject> entries)
    {
        var root = @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\";
        var modes = new List<ValueMeaning>
        {
            Meaning("0", "Disabled", "Disabled"),
            Meaning("1", "Block", "Block"),
            Meaning("2", "Audit", "Audit"),
            Meaning("6", "Warn", "Warn")
        };

        entries.Add(Policy("policy.asr.vulnerablesigneddrivers", "ASR: Block Abused Vulnerable Signed Drivers", "Prevents applications from writing known vulnerable signed drivers to disk; it does not unload drivers already present.", "Vulnerable kernel drivers can be abused to disable security controls.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "56a863a9-875e-4185-98a7-b882c64b5ce5", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.lsasscredentialtheft", "ASR: Block Credential Theft from LSASS", "Restricts process access patterns commonly used to read credentials from LSASS memory.", "The rule overlaps with LSA protection and Credential Guard and can generate compatibility events.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "9e6c4e1f-7d60-472f-ba1a-a39ef669e4b2", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.officechildprocess", "ASR: Block Office Child Processes", "Blocks Office applications from creating child processes used by many document-borne attacks.", "Legitimate macros and add-ins that launch helper processes require evaluation before block mode.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "d4f940ab-401b-4efc-aadc-ad5f3c50688a", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.officeexecutablecontent", "ASR: Block Office Executable Content", "Blocks Office applications from creating executable content on disk.", "The rule limits payload staging through Word, Excel, and PowerPoint documents.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "3b576869-a4ec-4529-8536-b80a7769e899", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.officecodeinjection", "ASR: Block Office Code Injection", "Blocks Office applications from injecting code into other processes.", "Code injection can evade application boundaries but some automation software may depend on similar techniques.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "75668c1f-73b5-4cf0-bb93-3ecf5cb7cc84", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.officemacrowin32", "ASR: Block Win32 API from Office Macros", "Blocks Office macro code from making Win32 API calls that are frequently used to execute or inject payloads.", "Complex signed business macros should be tested in audit mode before enforcement.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "92e97fa1-2edf-4476-bdd6-9dd0b4dddc7b", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.scriptdownload", "ASR: Block Script-Downloaded Executables", "Blocks JavaScript and VBScript from launching executable content downloaded from the Internet.", "The rule interrupts a common script-to-payload infection chain.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "d3e037e1-3eb8-44c8-a917-57927947596d", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.emailwebmailcontent", "ASR: Block Executable Email Content", "Blocks executable content launched from email client and webmail contexts supported by Defender.", "The rule reduces attachment-borne execution but may affect specialized mail workflows.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "be9ba2d9-53ea-4cdc-84e5-9b1eeee46550", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.advancedransomware", "ASR: Advanced Ransomware Protection", "Uses Defender cloud and behavioral signals to block activity associated with ransomware.", "Cloud protection dependencies and false-positive testing matter before broad block deployment.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "c1db55ab-c21a-4637-bb3f-a12568109d35", RiskLevel.High, modes));
        entries.Add(Policy("policy.asr.prevalenceage", "ASR: Block Low-Prevalence Executables", "Blocks executable files that do not meet Defender prevalence, age, or trusted-list criteria.", "New or line-of-business binaries can be blocked until reputation is established.", ProductDomain.ExploitProtection, "Attack Surface Reduction", root + "01443614-cd74-433a-b99e-2ecdc07bfc25", RiskLevel.High, modes));
    }

    private static void AddWindowsHello(List<ManagedObject> entries)
    {
        entries.Add(Policy("policy.hello.enabled", "Use Windows Hello for Business", "Controls Windows Hello for Business provisioning through the device Group Policy store.", "The GPO value can conflict with PassportForWork CSP policy and does not by itself prove enrollment.", ProductDomain.WindowsHello, "Provisioning", @"HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\Enabled", RiskLevel.High));
        entries.Add(Policy("policy.hello.requiresecuritydevice", "Require Hardware Security Device", "Requires Windows Hello for Business credentials to use a hardware security device when available under the policy definition.", "TPM-backed credentials strengthen key protection but impose hardware requirements.", ProductDomain.WindowsHello, "Key protection", @"HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\RequireSecurityDevice", RiskLevel.High));
        entries.Add(Policy("policy.hello.usebiometrics", "Allow Biometrics for Domain Accounts", "Controls whether domain users may use biometric gestures with Windows Hello for Business.", "Biometric gesture policy is separate from enabling the Windows biometric framework globally.", ProductDomain.WindowsHello, "Biometrics", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WinBio\Credential Provider\Domain Accounts", RiskLevel.High));
        entries.Add(Policy("policy.hello.enhancedsigninsecurity", "Enhanced Sign-in Security", "Controls whether Windows Hello uses enhanced sign-in security with supported biometric peripherals.", "The control only applies when hardware and drivers support the enhanced security path.", ProductDomain.WindowsHello, "Biometrics", @"HKLM\SOFTWARE\Microsoft\Policies\PassportForWork\Biometrics\EnableESSwithSupportedPeripherals", RiskLevel.High));
        entries.Add(Policy("policy.hello.pinrecovery", "Windows Hello PIN Recovery", "Controls use of the Microsoft PIN recovery service for Windows Hello for Business.", "Cloud-assisted recovery improves supportability and introduces a service dependency.", ProductDomain.WindowsHello, "PIN", @"HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\EnablePinRecovery"));
        entries.Add(Policy("policy.hello.minpinlength", "Minimum Windows Hello PIN Length", "Sets the minimum number of characters permitted for a Windows Hello for Business PIN.", "A Hello PIN unlocks a device-bound credential and is not transmitted like an account password.", ProductDomain.WindowsHello, "PIN complexity", @"HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\PINComplexity\MinimumPINLength", RiskLevel.High));
        entries.Add(Policy("policy.hello.pinexpiration", "Windows Hello PIN Expiration", "Sets the number of days before a Windows Hello for Business PIN expires; zero disables expiration.", "Frequent forced changes can reduce usability without changing the device-bound key material.", ProductDomain.WindowsHello, "PIN complexity", @"HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\PINComplexity\Expiration"));
        entries.Add(Policy("policy.hello.cloudtrust", "Use Cloud Trust for On-Premises Authentication", "Controls Windows Hello for Business cloud Kerberos trust for on-premises authentication.", "Cloud trust changes the authentication deployment model and depends on Microsoft Entra and Kerberos configuration.", ProductDomain.WindowsHello, "Trust model", @"HKLM\SOFTWARE\Policies\Microsoft\PassportForWork\UseCloudTrustForOnPremAuth", RiskLevel.High));
    }

    private static void AddStorageAndShell(List<ManagedObject> entries)
    {
        var storage = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense\";
        entries.Add(Policy("policy.storage.senseglobal", "Allow Storage Sense", "Controls whether Storage Sense may automatically reclaim disk space.", "The global switch gates configured cleanup schedules and thresholds.", ProductDomain.Storage, "Storage Sense", storage + "AllowStorageSenseGlobal"));
        entries.Add(Policy("policy.storage.temporaryfiles", "Storage Sense Temporary File Cleanup", "Controls whether Storage Sense may delete temporary files that applications are not using.", "Temporary cleanup can reclaim space but should not be mistaken for secure data erasure.", ProductDomain.Storage, "Storage Sense", storage + "AllowStorageSenseTemporaryFilesCleanup"));
        entries.Add(Policy("policy.storage.cadence", "Storage Sense Cadence", "Sets Storage Sense to run during low disk space, daily, weekly, or monthly.", "Cadence controls cleanup frequency rather than which data classes are eligible.", ProductDomain.Storage, "Storage Sense", storage + "ConfigStorageSenseGlobalCadence", RiskLevel.Medium,
            [Meaning("0", "LowDiskSpace", "During low disk space"), Meaning("1", "Daily", "Daily"), Meaning("7", "Weekly", "Weekly"), Meaning("30", "Monthly", "Monthly")]));
        entries.Add(Policy("policy.storage.recyclebinthreshold", "Recycle Bin Cleanup Threshold", "Sets the minimum age in days before Storage Sense permanently removes Recycle Bin items.", "A low threshold shortens the user's recovery window for deleted files.", ProductDomain.Storage, "Storage Sense", storage + "ConfigStorageSenseRecycleBinCleanupThreshold"));
        entries.Add(Policy("policy.storage.downloadsthreshold", "Downloads Cleanup Threshold", "Sets the minimum time in days that an unaccessed Downloads file remains before Storage Sense can delete it.", "Downloads may contain important installers or documents, so automated deletion requires careful communication.", ProductDomain.Storage, "Storage Sense", storage + "ConfigStorageSenseDownloadsCleanupThreshold", RiskLevel.High));
        entries.Add(Policy("policy.storage.clouddehydration", "Cloud Content Dehydration Threshold", "Sets how long locally cached cloud content remains unused before Storage Sense can return it to online-only state.", "Dehydration saves space but makes later access depend on network and cloud availability.", ProductDomain.Storage, "Storage Sense", storage + "ConfigStorageSenseCloudContentDehydrationThreshold"));
        entries.Add(Policy("policy.widgets.allow", "Allow Widgets", "Controls the Windows 11 Widgets experience, including its taskbar content surface.", "Widgets retrieve personalized web content and can expose account or interest information on screen.", ProductDomain.Widgets, "Widgets", @"HKLM\SOFTWARE\Policies\Microsoft\Dsh\AllowNewsAndInterests"));
        entries.Add(Policy("policy.accessibility.disablesettingssync", "Do Not Sync Accessibility Settings", "Prevents the Windows accessibility-settings group from synchronizing to or from the device through Windows backup and settings sync.", "Disabling synchronization keeps accessibility preferences local but can force users to reconfigure assistive features on each device.", ProductDomain.Accessibility, "Settings synchronization", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync\DisableAccessibilitySettingSync"));
        entries.Add(Policy("policy.search.highlights", "Allow Search Highlights", "Controls dynamic search highlights in Search home and the taskbar or Start search surface.", "Highlights introduce service-provided content into a local search entry point.", ProductDomain.Search, "Search content", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\EnableDynamicContentInWSB"));
        entries.Add(Policy("policy.search.disablesearch", "Disable Search Interface", "Disables the Windows Search UI and its entry points on supported Windows 11 builds.", "This does not necessarily stop indexing services or remove existing index data.", ProductDomain.Search, "Search interface", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\DisableSearch", RiskLevel.High));
        entries.Add(Policy("policy.edge.familysafety", "Edge Family Safety Settings", "Controls whether Edge exposes Family Safety settings and Kids Mode.", "This browser policy does not establish Microsoft Family group membership or prove that Windows screen-time enforcement is active.", ProductDomain.FamilySafety, "Microsoft Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\FamilySafetySettingsEnabled"));
    }

    private static void AddNetwork(List<ManagedObject> entries)
    {
        entries.Add(Policy("policy.network.dohmode", "DNS over HTTPS Policy Mode", "Controls whether the Windows DNS client prohibits, allows, or requires DNS over HTTPS where configured DNS servers support it.", "Requiring DoH can break name resolution when the selected resolver or enterprise DNS path cannot use HTTPS.", ProductDomain.Network, "DNS encryption", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\DoHPolicy", RiskLevel.High,
            [Meaning("1", "Prohibit", "Prohibit DoH"), Meaning("2", "Allow", "Allow DoH"), Meaning("3", "Require", "Require DoH")]));
        entries.Add(Policy("policy.network.dohsetting", "Encrypted DNS Fallback Setting", "Stores the companion encrypted-name-resolution option used by the DNS Client administrative template.", "The value refines DoH behavior and must be interpreted with the main DoHPolicy value.", ProductDomain.Network, "DNS encryption", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\DohPolicySetting"));
        entries.Add(Policy("policy.network.blocknondomain", "Block Simultaneous Domain and Non-Domain Networks", "Prevents concurrent connections to domain-authenticated and non-domain networks under defined Windows Connection Manager conditions.", "The control reduces network-bridging exposure but can interrupt legitimate multi-homed workflows.", ProductDomain.Network, "Connection isolation", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WcmSvc\GroupPolicy\fBlockNonDomain", RiskLevel.High));
        entries.Add(Policy("network.wifi.randommac", "Wi-Fi Random Hardware Address Default", "Aggregates the RandomMacState preference under installed Wi-Fi interface records.", "Randomized addresses reduce passive correlation across Wi-Fi networks; individual network profiles can still have their own behavior.", ProductDomain.Network, "Wi-Fi privacy", @"HKLM\SOFTWARE\Microsoft\WlanSvc\Interfaces\*\RandomMacState"));
    }

    private static void AddLocalSecurity(List<ManagedObject> entries)
    {
        entries.Add(SecurityPolicy("policy.security.passwordcomplexity", "Password Complexity Requirement", "Reports whether the local account password policy requires Windows complexity rules.", "Local policy applies to local accounts and does not replace domain password policy.", "System Access/PasswordComplexity", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.minpasswordlength", "Minimum Password Length", "Reports the minimum password length in the local security policy database.", "Length is one component of authentication policy and may be superseded for domain accounts.", "System Access/MinimumPasswordLength", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.passwordhistory", "Password History Size", "Reports how many prior local-account passwords Windows remembers to prevent immediate reuse.", "History is effective only when passwords are changed and account policy applies.", "System Access/PasswordHistorySize"));
        entries.Add(SecurityPolicy("policy.security.maxpasswordage", "Maximum Password Age", "Reports the maximum password age for local accounts in days.", "Forced expiration has usability and modern password-policy trade-offs.", "System Access/MaximumPasswordAge"));
        entries.Add(SecurityPolicy("policy.security.lockoutthreshold", "Account Lockout Threshold", "Reports the failed sign-in count that triggers local account lockout.", "A low threshold resists guessing but can enable denial of service through deliberate failures.", "System Access/LockoutBadCount", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.lockoutduration", "Account Lockout Duration", "Reports how long a locally locked account remains locked before automatic release.", "Duration and reset counter must be considered with the lockout threshold.", "System Access/LockoutDuration"));
        entries.Add(SecurityPolicy("policy.security.auditlogons", "Audit Logon Events", "Reports the legacy local audit-policy value for logon events from the security policy export.", "Advanced audit subcategory policy can override legacy category settings.", "Event Audit/AuditLogonEvents", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.auditpolicychange", "Audit Policy Change", "Reports the legacy local audit-policy value for policy-change events.", "Auditing policy changes helps establish when security configuration was modified.", "Event Audit/AuditPolicyChange", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.debugprograms", "User Right: Debug Programs", "Reports principals granted SeDebugPrivilege in the local security policy.", "Debug privilege can open protected processes and is highly sensitive.", "Privilege Rights/SeDebugPrivilege", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.impersonate", "User Right: Impersonate a Client", "Reports principals granted SeImpersonatePrivilege in the local security policy.", "Service-account misuse of impersonation privilege is a common privilege-escalation path.", "Privilege Rights/SeImpersonatePrivilege", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.remotedesktoplogon", "User Right: Remote Desktop Logon", "Reports principals granted SeRemoteInteractiveLogonRight.", "The right defines who may sign in through Remote Desktop when other RDP controls permit it.", "Privilege Rights/SeRemoteInteractiveLogonRight", RiskLevel.High));
        entries.Add(SecurityPolicy("policy.security.denynetworklogon", "User Right: Deny Network Logon", "Reports principals assigned SeDenyNetworkLogonRight.", "Deny rights override corresponding allow rights and can intentionally block remote access for sensitive accounts.", "Privilege Rights/SeDenyNetworkLogonRight", RiskLevel.High));
    }

    private static void AddInventoryAnchors(List<ManagedObject> entries)
    {
        var services = new (string Id, string Name, string Service, ProductDomain Domain, string Description, RiskLevel Risk)[]
        {
            ("service.wsearch", "Windows Search Service", "WSearch", ProductDomain.Search, "Indexes local content and services Windows Search queries.", RiskLevel.Medium),
            ("service.location", "Geolocation Service", "lfsvc", ProductDomain.Location, "Provides the Windows location service used by location-aware apps and features.", RiskLevel.High),
            ("service.biometric", "Windows Biometric Service", "WbioSrvc", ProductDomain.Biometrics, "Brokers biometric capture and matching for Windows sign-in and applications.", RiskLevel.High),
            ("service.securityhealth", "Windows Security Health Service", "SecurityHealthService", ProductDomain.Defender, "Reports Windows Security provider health and notification state.", RiskLevel.High),
            ("service.deliveryoptimization", "Delivery Optimization", "DoSvc", ProductDomain.WindowsUpdate, "Downloads update and Store content and can exchange content with peers under policy.", RiskLevel.Medium),
            ("service.updateorchestrator", "Update Orchestrator Service", "UsoSvc", ProductDomain.WindowsUpdate, "Coordinates Windows Update scan, download, installation, and restart tasks.", RiskLevel.High),
            ("service.waasmedic", "Windows Update Medic Service", "WaaSMedicSvc", ProductDomain.WindowsUpdate, "Repairs Windows Update components to preserve servicing reliability.", RiskLevel.High),
            ("service.appreadiness", "App Readiness", "AppReadiness", ProductDomain.CloudContent, "Prepares Store applications when users sign in and after application updates.", RiskLevel.Low),
            ("service.capsvc", "Capability Access Manager Service", "camsvc", ProductDomain.ConsentStore, "Mediates application access to protected capabilities such as camera and location.", RiskLevel.High),
            ("service.clipboarduser", "Clipboard User Service", "cbdhsvc", ProductDomain.Clipboard, "Provides per-user clipboard history and synchronization support; deployed instances can have a suffix.", RiskLevel.Medium),
            ("service.pushnotifications", "Windows Push Notifications System Service", "WpnService", ProductDomain.CloudContent, "Maintains the system push-notification platform used by applications and Windows experiences.", RiskLevel.Medium),
            ("service.ngccontainer", "Microsoft Passport Container", "NgcCtnrSvc", ProductDomain.WindowsHello, "Manages Windows Hello key containers used for device-bound credentials.", RiskLevel.High),
            ("service.credentials", "Credential Manager", "VaultSvc", ProductDomain.WindowsHello, "Provides secure storage and retrieval for credentials used by applications and Windows.", RiskLevel.High),
            ("service.onedrive.sync", "OneSync User Service", "OneSyncSvc", ProductDomain.CloudContent, "Synchronizes mail, contacts, calendar, and related user data; deployed instances can have a suffix.", RiskLevel.Medium),
            ("service.familymonitor", "Parental Controls Service", "WpcMonSvc", ProductDomain.FamilySafety, "Supports Windows parental-control and Family Safety monitoring when installed.", RiskLevel.Medium)
        };
        foreach (var service in services)
            entries.Add(Inventory(service.Id, service.Name, service.Description, "Service presence and runtime state explain whether the related Windows feature can operate.", service.Domain, "Services", FeatureCategory.WindowsService, InterfaceName.ServiceControlManager, ConfigurationType.ServiceState, "ServiceController:" + service.Service, service.Risk));

        var tasks = new (string Id, string Name, string Path, ProductDomain Domain, string Description)[]
        {
            ("task.compat.appraiser", "Microsoft Compatibility Appraiser", @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser", ProductDomain.Telemetry, "Collects compatibility inventory used for Windows upgrade and application assessment."),
            ("task.ceip.consolidator", "CEIP Consolidator", @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator", ProductDomain.Telemetry, "Consolidates Customer Experience Improvement Program data when that Windows component is active."),
            ("task.ceip.kernel", "Kernel CEIP Task", @"\Microsoft\Windows\Customer Experience Improvement Program\KernelCeipTask", ProductDomain.Telemetry, "Collects kernel-related CEIP information on systems where the task is present and enabled."),
            ("task.ceip.usb", "USB CEIP Task", @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip", ProductDomain.Telemetry, "Collects USB reliability information for CEIP on systems where the task is present."),
            ("task.feedback.dmclient", "Feedback SIUF DmClient", @"\Microsoft\Windows\Feedback\Siuf\DmClient", ProductDomain.Telemetry, "Supports Windows feedback notification and SIUF scheduling."),
            ("task.feedback.scenario", "Feedback Scenario Download", @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload", ProductDomain.Telemetry, "Supports scenario-driven Windows feedback prompts."),
            ("task.family.monitor", "Family Safety Monitor Task", @"\Microsoft\Windows\Shell\FamilySafetyMonitor", ProductDomain.FamilySafety, "Runs Family Safety monitoring integration for configured family accounts."),
            ("task.family.refresh", "Family Safety Refresh Task", @"\Microsoft\Windows\Shell\FamilySafetyRefreshTask", ProductDomain.FamilySafety, "Refreshes Family Safety configuration and enforcement data."),
            ("task.wer.queue", "Windows Error Reporting Queue", @"\Microsoft\Windows\Windows Error Reporting\QueueReporting", ProductDomain.Telemetry, "Processes queued Windows Error Reporting items under configured consent and diagnostic policy."),
            ("task.maps.update", "Maps Update Task", @"\Microsoft\Windows\Maps\MapsUpdateTask", ProductDomain.Location, "Updates offline map data used by Windows mapping features.")
        };
        foreach (var task in tasks)
            entries.Add(Inventory(task.Id, task.Name, task.Description, "Task state is useful evidence of periodic background activity but does not prove that a run transmitted data.", task.Domain, "Scheduled tasks", FeatureCategory.ScheduledTask, InterfaceName.TaskScheduler, ConfigurationType.TaskState, "ScheduledTask:" + task.Path));

        var packages = new (string Id, string Name, string Pattern, ProductDomain Domain, string Description)[]
        {
            ("package.widgets.webexperience", "Windows Web Experience Pack", "MicrosoftWindows.Client.WebExperience", ProductDomain.Widgets, "Provides the Windows Widgets web experience on supported Windows 11 builds."),
            ("package.copilot", "Microsoft Copilot Package", "Microsoft.Copilot", ProductDomain.Copilot, "Represents an installed Microsoft Copilot application package when present."),
            ("package.cortana", "Cortana Package", "Microsoft.549981C3F5F10", ProductDomain.Search, "Represents the separate Cortana application package used on supported builds."),
            ("package.search", "Windows Search Package", "Microsoft.Windows.Search", ProductDomain.Search, "Represents the packaged Windows Search experience when present."),
            ("package.sechealth", "Windows Security Interface", "Microsoft.SecHealthUI", ProductDomain.Defender, "Provides the Windows Security user interface; provider protection is separate."),
            ("package.cloudexperience", "Cloud Experience Host", "Microsoft.Windows.CloudExperienceHost", ProductDomain.CloudContent, "Hosts account- and cloud-connected Windows setup experiences."),
            ("package.peopleexperience", "People Experience Host", "Microsoft.Windows.PeopleExperienceHost", ProductDomain.CloudContent, "Hosts people and contact integration surfaces in Windows."),
            ("package.startmenu", "Start Menu Experience Host", "Microsoft.Windows.StartMenuExperienceHost", ProductDomain.Search, "Hosts the Start menu experience and related suggestions surface."),
            ("package.edge", "Microsoft Edge Package", "Microsoft.MicrosoftEdge", ProductDomain.Edge, "Represents an installed packaged Edge component where exposed through AppX inventory."),
            ("package.family", "Microsoft Family Package", "MicrosoftCorporationII.MicrosoftFamily", ProductDomain.FamilySafety, "Represents the Microsoft Family application package when installed.")
        };
        foreach (var package in packages)
            entries.Add(Inventory(package.Id, package.Name, package.Description, "Package presence establishes component availability, not account configuration or active data transfer.", package.Domain, "Packages", FeatureCategory.AppxPackage, InterfaceName.AppX, ConfigurationType.PackageState, "AppxPackage:" + package.Pattern));

        var capabilities = new (string Id, string Name, string Pattern, ProductDomain Domain, string Description)[]
        {
            ("capability.hello.face", "Windows Hello Face Capability", "Hello.Face", ProductDomain.WindowsHello, "Installs face-recognition components used by Windows Hello on compatible hardware."),
            ("capability.mathrecognizer", "Math Recognizer", "MathRecognizer", ProductDomain.Accessibility, "Provides handwriting-based mathematical expression recognition."),
            ("capability.handwriting", "Language Handwriting", "Language.Handwriting", ProductDomain.Accessibility, "Adds handwriting recognition resources for installed languages."),
            ("capability.speech", "Language Speech Recognition", "Language.Speech", ProductDomain.Speech, "Adds speech recognition resources for installed languages."),
            ("capability.texttospeech", "Language Text to Speech", "Language.TextToSpeech", ProductDomain.Accessibility, "Adds text-to-speech voices used by Narrator and other applications."),
            ("capability.opensshclient", "OpenSSH Client", "OpenSSH.Client", ProductDomain.Network, "Adds the Microsoft OpenSSH client used for encrypted remote sessions."),
            ("capability.stepsrecorder", "Steps Recorder", "StepsRecorder", ProductDomain.Telemetry, "Adds the Steps Recorder diagnostic capture utility on builds where available."),
            ("capability.internetexplorer", "Internet Explorer Mode Capability", "Browser.InternetExplorer", ProductDomain.Edge, "Represents legacy Internet Explorer optional capability components where retained.")
        };
        foreach (var capability in capabilities)
            entries.Add(Inventory(capability.Id, capability.Name, capability.Description, "Capability presence helps explain available Windows behavior but does not indicate current use.", capability.Domain, "Capabilities", FeatureCategory.WindowsCapability, InterfaceName.WindowsCapability, ConfigurationType.CapabilityState, "WindowsCapability:" + capability.Pattern));
    }

    private static ManagedObject Policy(
        string id,
        string name,
        string description,
        string rationale,
        ProductDomain domain,
        string subCategory,
        string discovery,
        RiskLevel risk = RiskLevel.Medium,
        List<ValueMeaning>? semantics = null) =>
        new()
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = "PolicySetting",
            CanonicalPath = id,
            Description = description,
            Rationale = rationale,
            FeatureCategory = domain is ProductDomain.Defender or ProductDomain.ExploitProtection ? FeatureCategory.DefenderSetting : FeatureCategory.RegistryPolicy,
            ProductDomain = domain,
            SubCategory = subCategory,
            RiskLevel = risk,
            ImpactLevel = ImpactLevel.Security,
            LifecycleState = LifecycleState.Active,
            InterfaceName = InterfaceName.GroupPolicy,
            ConfigurationType = ConfigurationType.PolicyState,
            DiscoveryMethod = discovery,
            ControlLevel = ControlLevel.AdministratorControlled,
            ComponentOwner = Owner(domain),
            PriorityLevel = PriorityLevel.Recommended,
            Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None,
            CreatedBy = "CatalogV21Expansion",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 90,
            SupportedWindowsVersions = ["Windows 10", "Windows 11"],
            References = References(domain),
            ValueSemantics = semantics ?? []
        };

    private static ManagedObject SecurityPolicy(string id, string name, string description, string rationale, string field, RiskLevel risk = RiskLevel.Medium) =>
        Policy(id, name, description, rationale, ProductDomain.LocalSecurity, "Local Security Policy", "Secedit:" + field, risk);

    private static ManagedObject Inventory(
        string id,
        string name,
        string description,
        string rationale,
        ProductDomain domain,
        string subCategory,
        FeatureCategory feature,
        InterfaceName iface,
        ConfigurationType configuration,
        string discovery,
        RiskLevel risk = RiskLevel.Medium) =>
        new()
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = "InventorySetting",
            CanonicalPath = id,
            Description = description,
            Rationale = rationale,
            FeatureCategory = feature,
            ProductDomain = domain,
            SubCategory = subCategory,
            RiskLevel = risk,
            ImpactLevel = ImpactLevel.System,
            LifecycleState = LifecycleState.Active,
            InterfaceName = iface,
            ConfigurationType = configuration,
            DiscoveryMethod = discovery,
            ControlLevel = ControlLevel.Advisory,
            ComponentOwner = Owner(domain),
            PriorityLevel = PriorityLevel.Optional,
            Reversibility = Reversibility.PartiallyReversible,
            RebootRequirement = RebootRequirement.None,
            CreatedBy = "CatalogV21Expansion",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 80,
            SupportedWindowsVersions = ["Windows 10", "Windows 11"],
            References = References(domain)
        };

    private static List<string> References(ProductDomain domain) => domain switch
    {
        ProductDomain.Recall or ProductDomain.Copilot => ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-windowsai"],
        ProductDomain.ExploitProtection => ["https://learn.microsoft.com/defender-endpoint/attack-surface-reduction-rules-reference", "https://learn.microsoft.com/defender-endpoint/enable-attack-surface-reduction"],
        ProductDomain.WindowsHello => ["https://learn.microsoft.com/windows/security/identity-protection/hello-for-business/configure", "https://learn.microsoft.com/windows/security/identity-protection/hello-for-business/policy-settings"],
        ProductDomain.Storage => ["https://learn.microsoft.com/windows/configuration/storage/storage-sense"],
        ProductDomain.Widgets => ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-newsandinterests"],
        ProductDomain.Search => ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-search"],
        ProductDomain.Network => ["https://learn.microsoft.com/windows-server/networking/dns/doh-client-support", "https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11"],
        ProductDomain.Edge => ["https://learn.microsoft.com/deployedge/microsoft-edge-policies/"],
        ProductDomain.LocalSecurity => ["https://learn.microsoft.com/windows-server/administration/windows-commands/secedit-export"],
        ProductDomain.Clipboard => ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-privacy"],
        ProductDomain.Accessibility => ["https://learn.microsoft.com/windows/client-management/mdm/policy-csp-settingssync", "https://learn.microsoft.com/windows/privacy/optional-diagnostic-data"],
        ProductDomain.WindowsUpdate => ["https://learn.microsoft.com/windows/deployment/update/waas-wu-settings"],
        ProductDomain.Telemetry => ["https://learn.microsoft.com/windows/privacy/configure-windows-diagnostic-data-in-your-organization"],
        _ => ["https://learn.microsoft.com/windows/"]
    };

    private static ComponentOwner Owner(ProductDomain domain) => domain switch
    {
        ProductDomain.Defender or ProductDomain.ExploitProtection => ComponentOwner.Defender,
        ProductDomain.Edge => ComponentOwner.MicrosoftEdge,
        ProductDomain.Search => ComponentOwner.WindowsSearch,
        ProductDomain.WindowsUpdate => ComponentOwner.WindowsUpdate,
        ProductDomain.Telemetry => ComponentOwner.Telemetry,
        ProductDomain.Network or ProductDomain.Firewall => ComponentOwner.Networking,
        ProductDomain.Recall or ProductDomain.Copilot => ComponentOwner.AI,
        _ => ComponentOwner.Other
    };

    private static ValueMeaning Meaning(string raw, string canonical, string label) => new()
    {
        RawValue = raw,
        Canonical = canonical,
        DisplayLabel = label,
        Description = $"Raw value {raw} represents {label}."
    };
}
