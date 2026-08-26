namespace WindowsPrivacyPlatform.Models;

/// <summary>Coverage and curated native-surface entries introduced for the current catalog.</summary>
internal static class CatalogV22Expansion
{
    internal static IReadOnlyList<ManagedObject> CreateCoverageBatch()
    {
        var list = new List<ManagedObject>
        {
            // App permission policy categories present on supported Windows releases.
            Reg("policy.appprivacy.appointments", "Let Apps Access Appointments", "Controls whether apps can read or update appointment data.", "Calendar-linked records can disclose schedules and contacts.", ProductDomain.AppPrivacy, "App permissions", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessAppointments", RiskLevel.High),
            Reg("policy.appprivacy.phonecall", "Let Apps Make Phone Calls", "Controls whether apps can initiate phone calls through connected capabilities.", "Unrestricted calling can create privacy and cost exposure.", ProductDomain.AppPrivacy, "App permissions", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessPhone", RiskLevel.High),
            Reg("policy.appprivacy.tasks", "Let Apps Access Tasks", "Controls whether apps can read and update task-list data.", "Task data can expose projects, deadlines, and personal routines.", ProductDomain.AppPrivacy, "App permissions", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessTasks", RiskLevel.Medium),
            Reg("policy.appprivacy.motion", "Let Apps Access Motion", "Controls whether apps can use motion and activity sensor data.", "Motion history can reveal behavior and device use patterns.", ProductDomain.AppPrivacy, "App permissions", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessMotion", RiskLevel.High),
            Reg("policy.appprivacy.trusteddevices", "Let Apps Access Trusted Devices", "Controls whether apps can communicate with paired trusted devices.", "Paired-device access can expose data beyond this computer.", ProductDomain.AppPrivacy, "App permissions", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessTrustedDevices", RiskLevel.High),
            Reg("policy.appprivacy.bluetooth", "Let Apps Control Bluetooth Radios", "Controls whether apps can manage Bluetooth radio state.", "Radio control affects nearby-device visibility and connectivity.", ProductDomain.AppPrivacy, "App permissions", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessBluetooth", RiskLevel.Medium),
            Reg("policy.appprivacy.voiceactivation", "Let Apps Use Voice Activation", "Controls whether apps can listen for voice activation while in use.", "Voice activation may keep microphone-dependent features available in the background.", ProductDomain.AppPrivacy, "Voice activation", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsActivateWithVoice", RiskLevel.High),
            Reg("policy.appprivacy.voiceactivationlocked", "Voice Activation Above Lock", "Controls whether voice-activated apps may respond while the device is locked.", "Lock-screen activation can expose commands or responses without an unlocked session.", ProductDomain.AppPrivacy, "Voice activation", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsActivateWithVoiceAboveLock", RiskLevel.High),
            Reg("policy.appprivacy.backgroundapps", "Let Apps Run in Background", "Controls whether apps may continue background work.", "Background execution affects notifications, network use, and battery life.", ProductDomain.AppPrivacy, "Background activity", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsRunInBackground", RiskLevel.Medium),

            // Defender attack-surface reduction, exploit protection, and ransomware controls.
            Reg("policy.defender.asr.officechild", "Block Office Child Processes", "Controls the attack-surface rule that blocks Office applications from creating child processes.", "Malicious documents commonly use child processes to launch payloads.", ProductDomain.Defender, "Attack surface reduction", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\D4F940AB-401B-4EFC-AADC-AD5F3C50688A", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.asr.credentials", "Block Credential Theft from LSASS", "Controls the attack-surface rule that blocks credential stealing from the local security authority.", "Credential theft enables lateral movement and account takeover.", ProductDomain.Defender, "Attack surface reduction", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\9E6C4E1F-7D60-472F-BA1A-A39EF669E4B2", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.asr.emailcontent", "Block Executable Email Content", "Controls the attack-surface rule for executable content launched from email clients and webmail.", "Email attachments remain a common malware delivery channel.", ProductDomain.Defender, "Attack surface reduction", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\BE9BA2D9-53EA-4CDC-84E5-9B1EEEE46550", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.asr.obfuscatedscripts", "Block Obfuscated Scripts", "Controls the attack-surface rule for suspiciously obfuscated script execution.", "Obfuscation is often used to conceal malicious command behavior.", ProductDomain.Defender, "Attack surface reduction", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\5BEB7EFE-FD9A-4556-801D-275E5FFC04CC", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.asr.officecode", "Block Office Code Injection", "Controls the attack-surface rule that blocks Office applications from injecting code into other processes.", "Code injection can hide document-borne malware inside trusted processes.", ProductDomain.Defender, "Attack surface reduction", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\75668C1F-73B5-4CF0-BB93-3ECF5CB7CC84", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.asr.wmi", "Block WMI Event Persistence", "Controls the attack-surface rule that blocks persistence through Windows Management Instrumentation events.", "Event subscriptions can relaunch malicious code without a normal startup entry.", ProductDomain.Defender, "Attack surface reduction", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\ASR\Rules\E6DB77E5-3DF2-4CF1-B95A-636979351E5B", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.cfa.allowedapps", "Controlled Folder Access Allowlist", "Reports the policy surface for applications allowed through controlled folder access.", "An overly broad allowlist weakens ransomware protection for protected folders.", ProductDomain.Defender, "Controlled folder access", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access\AllowedApplications", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.exploit.system", "System Exploit Protection Configuration", "Reports whether a managed system-wide exploit protection configuration is assigned.", "Exploit mitigations can stop memory-corruption techniques but require application compatibility testing.", ProductDomain.Defender, "Exploit protection", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender ExploitGuard\Exploit Protection\ExploitProtectionSettings", RiskLevel.High, FeatureCategory.DefenderSetting),
            Reg("policy.defender.networkprotection.downlevel", "Network Protection on Older Builds", "Controls whether network protection is available on supported older Windows builds.", "Network protection can block connections to malicious or untrusted destinations.", ProductDomain.Defender, "Network protection", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection\EnableNetworkProtection", RiskLevel.High, FeatureCategory.DefenderSetting),

            // Copilot, Recall, Widgets, Search and cloud-backed shell features.
            Reg("policy.copilot.turnoff", "Legacy Copilot Integration", "Controls the legacy Windows Copilot integration for this user.", "This policy does not control the newer Copilot app.", ProductDomain.Copilot, "Copilot", @"HKCU\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot\TurnOffWindowsCopilot", RiskLevel.Medium, FeatureCategory.AIComponent, 22621),
            Reg("policy.recall.disableaidataanalysis", "Disable Recall Snapshots", "Controls whether Recall can save and analyze screen snapshots.", "Snapshots can contain sensitive content from applications and websites.", ProductDomain.Recall, "Recall", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableAIDataAnalysis", RiskLevel.High, FeatureCategory.AIComponent, 26100),
            Reg("policy.recall.disableclicktodo", "Disable Click to Do", "Controls whether Click to Do analyzes screen content for contextual actions.", "Screen analysis can include information displayed by other applications.", ProductDomain.Recall, "Click to Do", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI\DisableClickToDo", RiskLevel.High, FeatureCategory.AIComponent, 26100),
            Reg("policy.widgets.allow", "Allow Widgets", "Controls whether the Windows widgets board is available.", "Widgets can retrieve personalized news, weather, and account-linked content.", ProductDomain.Widgets, "Widgets", @"HKLM\SOFTWARE\Policies\Microsoft\Dsh\AllowNewsAndInterests", RiskLevel.Medium, FeatureCategory.Widgets),
            Reg("policy.widgets.disableboard", "Disable Widgets Board", "Controls whether users can open the widgets board.", "Disabling the board removes its interactive content surface.", ProductDomain.Widgets, "Widgets", @"HKLM\SOFTWARE\Policies\Microsoft\Dsh\DisableWidgetsBoard", RiskLevel.Low, FeatureCategory.Widgets, 22621),
            Reg("policy.search.highlights", "Allow Search Highlights", "Controls rotating cloud-backed highlights in Windows Search.", "Search highlights may retrieve personalized or regional content.", ProductDomain.Search, "Search content", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\EnableDynamicContentInWSB", RiskLevel.Medium),
            Reg("policy.search.safe", "Safe Search Mode", "Controls filtering applied to web results in Windows Search.", "Filtering changes which web content can appear in search results.", ProductDomain.Search, "Web search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\ConnectedSearchSafeSearch", RiskLevel.Medium),

            // OneDrive, Storage Sense, network sharing, and remote administration.
            Reg("policy.onedrive.disablefilesync", "Disable OneDrive File Sync", "Controls whether the OneDrive sync client can connect and synchronize files.", "Disabling sync stops cloud file updates and can affect applications that use OneDrive paths.", ProductDomain.OneDrive, "Synchronization", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive\DisableFileSyncNGSC", RiskLevel.High, FeatureCategory.CloudComponent),
            Reg("policy.onedrive.disablepersonal", "Disable Personal OneDrive Sync", "Controls whether personal Microsoft accounts may synchronize through OneDrive.", "Organizations may restrict personal sync to reduce unmanaged data transfer.", ProductDomain.OneDrive, "Account scope", @"HKLM\SOFTWARE\Policies\Microsoft\OneDrive\DisablePersonalSync", RiskLevel.Medium, FeatureCategory.CloudComponent),
            Reg("policy.onedrive.filesondemand", "OneDrive Files On-Demand", "Controls whether cloud files can appear locally without full download.", "Changing this affects storage use and offline file availability.", ProductDomain.OneDrive, "Synchronization", @"HKLM\SOFTWARE\Policies\Microsoft\OneDrive\FilesOnDemandEnabled", RiskLevel.Medium, FeatureCategory.CloudComponent),
            Reg("policy.storage.allow", "Allow Storage Sense", "Controls whether Storage Sense can remove eligible temporary and cloud-backed content.", "Automatic cleanup can recover space but may remove files users expected to remain local.", ProductDomain.Storage, "Storage Sense", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense\AllowStorageSenseGlobal", RiskLevel.Medium, FeatureCategory.ExplorerFeature),
            Reg("policy.storage.cadence", "Storage Sense Cadence", "Controls how often Storage Sense evaluates cleanup rules.", "More frequent cleanup changes how quickly temporary content is removed.", ProductDomain.Storage, "Storage Sense", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense\ConfigStorageSenseGlobalCadence", RiskLevel.Medium, FeatureCategory.ExplorerFeature),
            Reg("policy.storage.onedriveage", "Cloud Content Dehydration Age", "Controls when inactive cloud files may become online-only.", "Dehydrated files need network access before they can be opened again.", ProductDomain.Storage, "Cloud content", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense\ConfigStorageSenseCloudContentDehydrationThreshold", RiskLevel.Medium, FeatureCategory.ExplorerFeature),
            Reg("policy.network.llmnr", "Turn Off Multicast Name Resolution", "Controls fallback name resolution on local networks.", "Multicast name resolution can expose queries and enable credential-relay attacks on hostile networks.", ProductDomain.Network, "Name resolution", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\EnableMulticast", RiskLevel.High, FeatureCategory.NetworkSetting),
            Reg("policy.network.netbios", "Disable NetBIOS Name Resolution", "Controls legacy NetBIOS name resolution behavior.", "Legacy broadcast resolution increases attack surface on untrusted networks.", ProductDomain.Network, "Name resolution", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\DNSClient\EnableNetbios", RiskLevel.High, FeatureCategory.NetworkSetting),
            Reg("policy.network.bridge", "Allow Network Bridge", "Controls whether users can create a network bridge.", "Bridging can join network segments and bypass expected routing controls.", ProductDomain.Network, "Network sharing", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Network Connections\NC_AllowNetBridge_NLA", RiskLevel.High, FeatureCategory.NetworkSetting),
            Reg("policy.remote.rdp", "Allow Remote Desktop Connections", "Controls whether Remote Desktop accepts inbound sessions.", "Remote Desktop exposes an interactive logon surface to reachable networks.", ProductDomain.RemoteAccess, "Remote Desktop", @"HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\fDenyTSConnections", RiskLevel.High, FeatureCategory.NetworkSetting),
            Reg("policy.remote.assistance", "Allow Solicited Remote Assistance", "Controls whether users can request interactive Remote Assistance.", "Remote assistance permits another person to view or control the session after approval.", ProductDomain.RemoteAccess, "Remote Assistance", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services\fAllowToGetHelp", RiskLevel.High, FeatureCategory.NetworkSetting),
            Reg("policy.remote.unsolicited", "Allow Unsolicited Remote Assistance", "Controls whether approved helpers can offer Remote Assistance without a user-created invitation.", "Unsolicited assistance is appropriate only in tightly managed support environments.", ProductDomain.RemoteAccess, "Remote Assistance", @"HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services\fAllowUnsolicited", RiskLevel.High, FeatureCategory.NetworkSetting),

            // Local security highlights remain observation-only because safe changes coordinate multiple policies.
            Reg("policy.security.runasppl", "LSA Protected Process", "Controls protected-process isolation for the local security authority.", "Protection makes credential theft and code injection into the security authority harder.", ProductDomain.LocalSecurity, "Credential protection", @"HKLM\SYSTEM\CurrentControlSet\Control\Lsa\RunAsPPL", RiskLevel.High, FeatureCategory.RegistryPolicy),
            Reg("policy.security.nolmhash", "Do Not Store LAN Manager Hash", "Controls storage of legacy password hashes after password changes.", "Legacy hashes are substantially weaker than modern credential representations.", ProductDomain.LocalSecurity, "Credentials", @"HKLM\SYSTEM\CurrentControlSet\Control\Lsa\NoLMHash", RiskLevel.High, FeatureCategory.RegistryPolicy),
            Reg("policy.security.blankpassword", "Limit Blank Passwords to Console", "Controls whether local accounts with blank passwords can log on remotely.", "Remote use of blank passwords is a direct account-compromise risk.", ProductDomain.LocalSecurity, "Accounts", @"HKLM\SYSTEM\CurrentControlSet\Control\Lsa\LimitBlankPasswordUse", RiskLevel.High, FeatureCategory.RegistryPolicy),
            Reg("policy.security.restrictanonymous", "Restrict Anonymous Enumeration", "Controls anonymous access to account and share information.", "Anonymous enumeration can help an attacker map users and resources.", ProductDomain.LocalSecurity, "Anonymous access", @"HKLM\SYSTEM\CurrentControlSet\Control\Lsa\RestrictAnonymous", RiskLevel.High, FeatureCategory.RegistryPolicy),
            Reg("policy.security.smbsigning", "Require SMB Client Signing", "Controls whether the SMB client requires signed sessions.", "Signing helps prevent tampering and relay attacks on file-sharing traffic.", ProductDomain.LocalSecurity, "Network authentication", @"HKLM\SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters\RequireSecuritySignature", RiskLevel.High, FeatureCategory.NetworkSetting),

            // Curated scheduled tasks. Curated services already exist in the earlier catalog batch;
            // their write contracts are attached centrally by CuratedWriteAuthorizations.
            Task("task.applicationexperience.compatibilityappraiser", "Microsoft Compatibility Appraiser", "Reports whether the compatibility appraisal task is enabled.", "The task inventories compatibility data used during Windows servicing.", "Application Experience", @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"),
            Task("task.applicationexperience.programdataupdater", "Program Data Updater", "Reports whether the application compatibility data update task is enabled.", "The task refreshes program inventory used for compatibility decisions.", "Application Experience", @"\Microsoft\Windows\Application Experience\ProgramDataUpdater"),
            Task("task.ceip.consolidator", "Customer Experience Consolidator", "Reports whether the customer-experience aggregation task is enabled.", "The task consolidates participation data before reporting.", "Customer Experience", @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"),
            Task("task.ceip.usbceip", "USB Customer Experience Task", "Reports whether the USB experience participation task is enabled.", "The task gathers USB-related participation information rather than controlling device drivers.", "Customer Experience", @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"),
            Task("task.maps.mapstoasttask", "Offline Maps Notification Task", "Reports whether offline Maps can schedule maintenance notifications.", "The task supports offline-map status messages.", "Maps", @"\Microsoft\Windows\Maps\MapsToastTask"),
            Task("task.maps.mapsupdatetask", "Offline Maps Update Task", "Reports whether offline Maps can refresh downloaded map data automatically.", "The task keeps offline map packages current.", "Maps", @"\Microsoft\Windows\Maps\MapsUpdateTask"),
            Task("task.wer.queuereporting", "Windows Error Reporting Queue", "Reports whether queued error reports may be processed on schedule.", "The task submits queued reliability information when reporting is enabled.", "Error reporting", @"\Microsoft\Windows\Windows Error Reporting\QueueReporting"),

            // Curated, per-user removable inbox applications.
            Appx("appx.bingnews", "Microsoft Start News", "Reports whether the consumer news application is installed for the current user.", "The package is optional and can retrieve personalized news content.", "Microsoft.BingNews"),
            Appx("appx.bingweather", "MSN Weather", "Reports whether the consumer weather application is installed for the current user.", "The package can use location and retrieve forecast content.", "Microsoft.BingWeather"),
            Appx("appx.gethelp", "Get Help", "Reports whether the Microsoft support application is installed for the current user.", "The package provides cloud-backed support workflows.", "Microsoft.GetHelp"),
            Appx("appx.getstarted", "Windows Tips", "Reports whether the Windows tips application is installed for the current user.", "The package presents feature guidance and suggested content.", "Microsoft.Getstarted"),
            Appx("appx.solitaire", "Microsoft Solitaire Collection", "Reports whether the bundled consumer game package is installed for the current user.", "The package is independent from core Windows components.", "Microsoft.MicrosoftSolitaireCollection"),
            Appx("appx.feedbackhub", "Feedback Hub", "Reports whether the Windows feedback application is installed for the current user.", "The package is used to submit feedback and diagnostic attachments.", "Microsoft.WindowsFeedbackHub"),
            Appx("appx.xboxoverlay", "Xbox Game Bar", "Reports whether the gaming overlay package is installed for the current user.", "The package provides recording, social, and performance overlays.", "Microsoft.XboxGamingOverlay"),

            // Curated optional features.
            Feature("feature.xpsservices", "XPS Services", "Reports whether legacy XPS document services are enabled.", "XPS support is optional on devices that do not use that document format.", "Printing-XPSServices-Features"),
            Feature("feature.workfolders", "Work Folders Client", "Reports whether the enterprise Work Folders client is enabled.", "Work Folders is optional when the organization does not deploy that synchronization service.", "WorkFolders-Client"),
            Feature("feature.mediaplayback", "Media Playback", "Reports whether Windows media playback components are enabled.", "Some media applications depend on these shared codecs and playback services.", "MediaPlayback"),
            Feature("feature.windowsmediaplayer", "Windows Media Player", "Reports whether the classic Windows Media Player component is enabled.", "The classic player is optional and can be restored through Windows Features.", "WindowsMediaPlayer")
        };

        return list.AsReadOnly();
    }

    private static ManagedObject Reg(string id, string name, string description, string rationale,
        ProductDomain domain, string subCategory, string path, RiskLevel risk,
        FeatureCategory category = FeatureCategory.RegistryPolicy, int minimumBuild = 10240) =>
        Base(id, name, description, rationale, domain, subCategory, risk, category,
            category == FeatureCategory.DefenderSetting ? InterfaceName.Defender : InterfaceName.GroupPolicy,
            category == FeatureCategory.DefenderSetting ? ConfigurationType.DefenderSettingValue : ConfigurationType.PolicyState,
            path, minimumBuild);

    private static ManagedObject Service(string id, string name, string description, string rationale,
        ProductDomain domain, string subCategory, string serviceName) =>
        Base(id, name, description, rationale, domain, subCategory, RiskLevel.Medium,
            FeatureCategory.WindowsService, InterfaceName.ServiceControlManager, ConfigurationType.ServiceState,
            "ServiceController:" + serviceName);

    private static ManagedObject Task(string id, string name, string description, string rationale,
        string subCategory, string taskPath) =>
        Base(id, name, description, rationale, ProductDomain.Telemetry, subCategory, RiskLevel.Medium,
            FeatureCategory.ScheduledTask, InterfaceName.TaskScheduler, ConfigurationType.TaskState,
            "ScheduledTask:" + taskPath);

    private static ManagedObject Appx(string id, string name, string description, string rationale, string packageName) =>
        Base(id, name, description, rationale, ProductDomain.Other, "Curated applications", RiskLevel.Low,
            FeatureCategory.AppxPackage, InterfaceName.AppX, ConfigurationType.PackageState, "App package: " + packageName);

    private static ManagedObject Feature(string id, string name, string description, string rationale, string featureName) =>
        Base(id, name, description, rationale, ProductDomain.Other, "Curated optional features", RiskLevel.Medium,
            FeatureCategory.OptionalFeature, InterfaceName.DISM, ConfigurationType.FeatureState, "Optional feature: " + featureName);

    private static ManagedObject Base(string id, string name, string description, string rationale,
        ProductDomain domain, string subCategory, RiskLevel risk, FeatureCategory category,
        InterfaceName iface, ConfigurationType configuration, string discovery, int minimumBuild = 10240) => new()
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = category.ToString(),
            Description = description,
            Rationale = rationale,
            FeatureCategory = category,
            ProductDomain = domain,
            SubCategory = subCategory,
            RiskLevel = risk,
            ImpactLevel = ImpactLevel.Security,
            LifecycleState = LifecycleState.Active,
            InterfaceName = iface,
            ConfigurationType = configuration,
            DiscoveryMethod = discovery,
            CanonicalPath = id,
            ControlLevel = category is FeatureCategory.AppxPackage ? ControlLevel.UserControlled : ControlLevel.AdministratorControlled,
            ComponentOwner = domain == ProductDomain.Defender ? ComponentOwner.Defender : ComponentOwner.Other,
            PriorityLevel = PriorityLevel.Recommended,
            Reversibility = Reversibility.Reversible,
            RebootRequirement = category == FeatureCategory.OptionalFeature ? RebootRequirement.RebootRequired : RebootRequirement.None,
            CreatedBy = nameof(CatalogV22Expansion),
            CreatedTimestamp = DateTime.UnixEpoch,
            ConfidenceScore = 80,
            MinimumBuild = minimumBuild,
            SupportedWindowsVersions = ["Windows 10", "Windows 11"]
        };
}
