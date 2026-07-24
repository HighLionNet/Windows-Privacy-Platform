// Source/WindowsPrivacyPlatform.Scanner/PolicyCollector.cs
using System;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for high-value privacy and security policy/preference registry values.
    /// Covers telemetry, Windows Update, Defender, Search, Cloud Content, Activity History,
    /// Advertising, Location, App Privacy, Delivery Optimization, and related GPO paths.
    /// Never writes. Never requests elevation. Missing values are recorded as "Not configured".
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

        // Table-driven probes. Ids align with ManagedObjectCatalog ObjectId values.
        private static readonly Probe[] Probes =
        {
            // --- Telemetry / Diagnostic data ---
            new("policy.telemetry.allowtelemetry",
                "Telemetry",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "AllowTelemetry"),
            new("policy.telemetry.allowtelemetry.currentversion",
                "Telemetry",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                "AllowTelemetry"),
            new("policy.telemetry.donotshowfeedback",
                "Telemetry",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "DoNotShowFeedbackNotifications"),
            new("policy.telemetry.disablecommercialid",
                "Telemetry",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "AllowDeviceNameInTelemetry"),

            // --- Windows Update ---
            new("policy.update.noautoupdate",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                "NoAutoUpdate"),
            new("policy.update.auoptions",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                "AUOptions"),
            new("policy.update.scheduledinstallday",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                "ScheduledInstallDay"),
            new("policy.update.scheduledinstalltime",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                "ScheduledInstallTime"),
            new("policy.update.autoinstallminor",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                "AutoInstallMinorUpdates"),
            new("policy.update.detectionfrequency",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
                "DetectionFrequency"),
            new("policy.update.disableuxwuaccess",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                "SetDisableUXWUAccess"),
            new("policy.update.disablewuaccess",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                "DisableWindowsUpdateAccess"),
            new("policy.update.donotconnectinternet",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                "DoNotConnectToWindowsUpdateInternetLocations"),
            new("policy.update.excludewudrivers",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate",
                "ExcludeWUDriversInQualityUpdate"),
            new("policy.update.ux.branchreadiness",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings",
                "BranchReadinessLevel"),
            new("policy.update.ux.flightsettings",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings",
                "FlightSettingsMaxPauseDays"),
            new("policy.update.ux.pausefeatureupdatesstart",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings",
                "PauseFeatureUpdatesStartTime"),
            new("policy.update.ux.pausequalityupdatesstart",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings",
                "PauseQualityUpdatesStartTime"),
            new("policy.deliveryopt.downloadmode",
                "WindowsUpdate",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
                "DODownloadMode"),

            // --- Defender ---
            new("policy.defender.disableantispyware",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender",
                "DisableAntiSpyware"),
            new("policy.defender.disablerealtime",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                "DisableRealtimeMonitoring"),
            new("policy.defender.disablebehaviormonitor",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                "DisableBehaviorMonitoring"),
            new("policy.defender.disableioav",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
                "DisableIOAVProtection"),
            new("policy.defender.spynetreporting",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
                "SpynetReporting"),
            new("policy.defender.submitsamples",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
                "SubmitSamplesConsent"),
            new("policy.defender.puaprotection",
                "Defender",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows Defender",
                "PUAProtection"),

            // --- Search / Cortana ---
            new("policy.search.allowcortana",
                "Search",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "AllowCortana"),
            new("policy.search.disablewebsearch",
                "Search",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "DisableWebSearch"),
            new("policy.search.connectedsearchuseweb",
                "Search",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "ConnectedSearchUseWeb"),
            new("policy.search.allowsearchlocation",
                "Search",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "AllowSearchToUseLocation"),
            new("policy.search.allowcloudsearch",
                "Search",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "AllowCloudSearch"),
            new("policy.search.disableindexedlocationsinlib",
                "Search",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Windows Search",
                "DisableIndexedSearch"),

            // --- Activity History / Timeline ---
            new("policy.activity.enableactivityfeed",
                "ActivityHistory",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System",
                "EnableActivityFeed"),
            new("policy.activity.publishuseractivities",
                "ActivityHistory",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System",
                "PublishUserActivities"),
            new("policy.activity.uploaduseractivities",
                "ActivityHistory",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\System",
                "UploadUserActivities"),

            // --- Cloud Content / Consumer experiences ---
            new("policy.cloud.disableconsumerfeatures",
                "CloudContent",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                "DisableWindowsConsumerFeatures"),
            new("policy.cloud.disablesoftlanding",
                "CloudContent",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                "DisableSoftLanding"),
            new("policy.cloud.disablecloudoptimized",
                "CloudContent",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                "DisableCloudOptimizedContent"),
            new("policy.cloud.disablewindowsspotlight.hkcu",
                "CloudContent",
                RegistryHive.CurrentUser,
                @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                "DisableWindowsSpotlightFeatures"),
            new("policy.cloud.disabletailored.hkcu",
                "CloudContent",
                RegistryHive.CurrentUser,
                @"SOFTWARE\Policies\Microsoft\Windows\CloudContent",
                "DisableTailoredExperiencesWithDiagnosticData"),

            // --- Advertising ---
            new("policy.advertising.disabledbygpo",
                "Advertising",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                "DisabledByGroupPolicy"),

            // --- Location ---
            new("policy.location.disablelocation",
                "Location",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                "DisableLocation"),
            new("policy.location.disablelocationscripting",
                "Location",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                "DisableLocationScripting"),
            new("policy.location.disablewindowslocationsupplier",
                "Location",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors",
                "DisableWindowsLocationProvider"),

            // --- App Privacy (GPO LetApps* ) ---
            new("policy.appprivacy.location",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessLocation"),
            new("policy.appprivacy.camera",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessCamera"),
            new("policy.appprivacy.microphone",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessMicrophone"),
            new("policy.appprivacy.accountinfo",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessAccountInfo"),
            new("policy.appprivacy.contacts",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessContacts"),
            new("policy.appprivacy.calendar",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessCalendar"),
            new("policy.appprivacy.email",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessEmail"),
            new("policy.appprivacy.callhistory",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessCallHistory"),
            new("policy.appprivacy.messaging",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessMessaging"),
            new("policy.appprivacy.radios",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessRadios"),
            new("policy.appprivacy.documents",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessDocuments"),
            new("policy.appprivacy.pictures",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessPictures"),
            new("policy.appprivacy.videos",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessVideos"),
            new("policy.appprivacy.filesystem",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsAccessFileSystem"),
            new("policy.appprivacy.appdiagnostics",
                "AppPrivacy",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy",
                "LetAppsGetDiagnosticInfo"),

            // --- Find My Device / device metadata ---
            new("policy.findmydevice.allow",
                "Device",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\FindMyDevice",
                "AllowFindMyDevice"),
            new("policy.device.metadataretrieval",
                "Device",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata",
                "PreventDeviceMetadataFromNetwork"),

            // --- Feedback ---
            new("policy.feedback.siufperiod",
                "Feedback",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "DoNotShowFeedbackNotifications"),
            new("policy.feedback.numberoffeedbacksiuf",
                "Feedback",
                RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Siuf\Rules",
                "NumberOfSIUFInPeriod"),
            new("policy.feedback.periodinsiuf",
                "Feedback",
                RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Siuf\Rules",
                "PeriodInNanoSeconds"),

            // --- OneDrive / sync hints (policy) ---
            new("policy.onedrive.disablefilesonDemand",
                "CloudContent",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\OneDrive",
                "DisableFileSyncNGSC"),

            // --- Explorer / personalization related privacy ---
            new("policy.explorer.allowonlinecontent",
                "Explorer",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "AllowOnlineTips"),
            new("policy.explorer.norecentserverdocs",
                "Explorer",
                RegistryHive.CurrentUser,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                "NoRecentDocsHistory"),

            // --- Biometrics ---
            new("policy.biometrics.enabled",
                "Biometrics",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Biometrics",
                "Enabled"),
            new("policy.biometrics.facialfeatures",
                "Biometrics",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Biometrics\FacialFeatures",
                "EnhancedAntiSpoofing"),

            // --- Microsoft Edge (policy) high-value privacy ---
            new("policy.edge.autofilladdress",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "AutofillAddressEnabled"),
            new("policy.edge.autofillcreditcard",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "AutofillCreditCardEnabled"),
            new("policy.edge.passwordmanager",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "PasswordManagerEnabled"),
            new("policy.edge.searchsuggest",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "SearchSuggestEnabled"),
            new("policy.edge.alternateerrorpages",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "AlternateErrorPagesEnabled"),
            new("policy.edge.paymentmethods",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "PaymentMethodQueryEnabled"),
            new("policy.edge.personalizationreporting",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "PersonalizationReportingEnabled"),
            new("policy.edge.metricsreporting",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "MetricsReportingEnabled"),
            new("policy.edge.sendsitinfo",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "SendSiteInfoToImproveServices"),
            new("policy.edge.trackingprevention",
                "Edge",
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Edge",
                "TrackingPrevention")
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
                catch
                {
                    // Individual probe failure must not abort the scan.
                    snapshot.PolicySettings.Add(new PolicySettingInfo
                    {
                        Name = probe.Id,
                        Category = probe.Category,
                        Hive = probe.Hive.ToString(),
                        Path = probe.SubKey,
                        ValueName = probe.ValueName,
                        Value = "Error reading"
                    });
                }
            }
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

            RegistryKey? baseKey = null;
            try
            {
                baseKey = probe.Hive == RegistryHive.LocalMachine
                    ? Registry.LocalMachine
                    : Registry.CurrentUser;

                using var key = baseKey.OpenSubKey(probe.SubKey, writable: false);
                if (key is null)
                    return info;

                var raw = key.GetValue(probe.ValueName);
                if (raw is null)
                    return info;

                info.Value = FormatValue(raw);
                return info;
            }
            finally
            {
                // Do not dispose Registry.LocalMachine / CurrentUser static roots.
            }
        }

        private static string FormatValue(object raw)
        {
            return raw switch
            {
                byte[] bytes => BitConverter.ToString(bytes),
                string[] arr => string.Join(";", arr),
                _ => raw.ToString() ?? "Not configured"
            };
        }
    }
}
