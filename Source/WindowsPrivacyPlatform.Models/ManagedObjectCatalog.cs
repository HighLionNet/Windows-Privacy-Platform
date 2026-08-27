// Source/WindowsPrivacyPlatform.Models/ManagedObjectCatalog.cs
// WritableTarget is explicit catalog-backed authorization only.
// DiscoveryMethod is observation metadata and NEVER creates write permission.
using System.Collections.Generic;
using System.Linq;

namespace WindowsPrivacyPlatform.Models;

public static class ManagedObjectCatalog
{
    public const string CatalogVersion = "2.4";
    public static IReadOnlyList<ManagedObject> PrivacySettings { get; } = Finalize(CreatePrivacyBatch());
    public static IReadOnlyList<ManagedObject> PolicySettings { get; } = Finalize(
        CreatePolicyBatch()
            .Concat(CreateExtendedPolicyBatch())
            .Concat(CatalogExpansion.CreateCoverageBatch())
            .Concat(CatalogV22Expansion.CreateCoverageBatch())
            .ToList());
    public static IReadOnlyList<ManagedObject> FirewallSettings { get; } = Finalize(CreateFirewallBatch());
    public static IReadOnlyList<ManagedObject> All { get; } =
        PrivacySettings.Concat(PolicySettings).Concat(FirewallSettings).ToList().AsReadOnly();

    /// <summary>Revalidates a runtime object against the immutable catalog authorization.</summary>
    public static bool IsAuthorizedWriteTarget(ManagedObject candidate)
    {
        if (candidate?.WritableTarget is not { IsComplete: true } requested)
            return false;
        var definition = All.FirstOrDefault(item =>
            item.ObjectId.Equals(candidate.ObjectId, StringComparison.OrdinalIgnoreCase));
        if (definition?.WritableTarget is not { IsComplete: true } authorized)
            return false;
        return requested.Kind == authorized.Kind &&
               requested.View == authorized.View && requested.ValueKind == authorized.ValueKind &&
               requested.SupportsDeletion == authorized.SupportsDeletion &&
               requested.RequiresElevation == authorized.RequiresElevation &&
               requested.Hive.Equals(authorized.Hive, StringComparison.OrdinalIgnoreCase) &&
               requested.SubKey.Equals(authorized.SubKey, StringComparison.OrdinalIgnoreCase) &&
               requested.ValueName.Equals(authorized.ValueName, StringComparison.OrdinalIgnoreCase) &&
               requested.SupportedRawValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                   .SequenceEqual(authorized.SupportedRawValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase),
                       StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ManagedObject> Finalize(IReadOnlyList<ManagedObject> batch)
    {
        foreach (var mo in batch)
        {
            if (mo is null) continue;
            mo.SchemaVersion = CatalogVersion;
            mo.ConfidenceSource = "Curated catalog";
            mo.MinimumBuild = mo.MinimumBuild <= 0 ? 10240 : mo.MinimumBuild;
            mo.SupportedWindowsVersions ??= ["Windows 10", "Windows 11"];
            ApplyKnownSemantics(mo);
            AttachWritableTarget(mo);
            mo.TechnicalLocation = TechnicalLocationFormatter.FromDefinition(mo);
            ApplyExclusionDecision(mo);
            ApplyNativeToolLink(mo);
            mo.Bucket = CatalogPolicy.ResolveBucket(mo);
            HubTaxonomy.Apply(mo);
            CatalogNarrativeAuthoring.Apply(mo);
        }
        return batch;
    }

    private static void AttachWritableTarget(ManagedObject mo)
    {
        if (CuratedWriteAuthorizations.TryCreateTarget(mo.ObjectId, out var curated))
        {
            mo.WritableTarget = curated;
            return;
        }

        if (!IsExplicitlyAuthorizedForWrite(mo.ObjectId))
            return;

        if (!TryParseRegistryPath(mo.DiscoveryMethod, out var hive, out var subKey, out var valueName))
            return;

        var kind = ResolveValueKind(mo.ObjectId);
        var supported = mo.ValueSemantics?
            .Where(v => v is not null && !string.IsNullOrWhiteSpace(v.RawValue))
            .Select(v => v.RawValue!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        // A typed target without an explicit value set is still too broad for this product.
        if (supported.Count == 0)
            return;

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
            Kind = WritableTargetKind.Registry,
            Notes = "Explicit catalog authorization. Discovery metadata never grants write permission."
        };
    }

    private static void ApplyExclusionDecision(ManagedObject mo)
    {
        if (mo.IsWritable)
        {
            mo.ExclusionReason = ExclusionReason.None;
            return;
        }

        if (mo.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase) ||
            mo.ObjectId.StartsWith("policy.uac.", StringComparison.OrdinalIgnoreCase))
        {
            // BitLocker can require recovery material; UAC master behavior affects every later elevation.
            mo.ExclusionReason = ExclusionReason.HighRiskIrreversible;
            if (mo.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase) &&
                !mo.ObjectId.Equals("policy.bitlocker.requiredeviceencryption", StringComparison.OrdinalIgnoreCase))
            {
                mo.SupportedEditions = ["Pro", "Enterprise", "Education", "Pro for Workstations"];
            }
            return;
        }

        if (mo.FeatureCategory is FeatureCategory.WindowsService or FeatureCategory.ScheduledTask or
            FeatureCategory.AppxPackage or FeatureCategory.ProvisionedPackage or
            FeatureCategory.OptionalFeature or FeatureCategory.WindowsCapability or
            FeatureCategory.FirewallRule)
        {
            mo.ExclusionReason = ExclusionReason.ReadOnlyByDesign;
            return;
        }

        if (mo.ObjectId.Contains("asr.", StringComparison.OrdinalIgnoreCase) ||
            mo.SubCategory?.Contains("Exploit", StringComparison.OrdinalIgnoreCase) == true ||
            mo.ProductDomain == ProductDomain.LocalSecurity)
        {
            mo.ExclusionReason = ExclusionReason.RequiresMultiKeyCoordination;
            return;
        }

        mo.ExclusionReason = ExclusionReason.NotYetCatalogued;
    }

    private static void ApplyNativeToolLink(ManagedObject mo)
    {
        if (mo.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase))
        {
            mo.NativeTool = new NativeToolLink
            {
                Label = "Open BitLocker Drive Encryption",
                Executable = "control.exe",
                Arguments = "/name Microsoft.BitLockerDriveEncryption"
            };
        }
        else if (mo.ObjectId.StartsWith("policy.uac.", StringComparison.OrdinalIgnoreCase))
        {
            mo.NativeTool = new NativeToolLink
            {
                Label = "Open User Account Control settings",
                Executable = "UserAccountControlSettings.exe"
            };
        }
        else if (mo.FeatureCategory == FeatureCategory.FirewallRule)
        {
            mo.NativeTool = new NativeToolLink
            {
                Label = "Open Firewall with Advanced Security",
                Executable = "wf.msc"
            };
        }
    }

    private static bool IsExplicitlyAuthorizedForWrite(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
            return false;

        if (objectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase))
            return true;

        // AppPrivacy machine policies (0/1/2) — explicit force allow/deny/user-controlled.
        if (objectId.StartsWith("policy.appprivacy.", StringComparison.OrdinalIgnoreCase))
            return true;

        if (objectId is
            "privacy.advertisingid.enabled" or
            "privacy.tailoredexperiences" or
            "privacy.contentdelivery.systempanesuggestions" or
            "privacy.speech.onlinespeech")
            return true;

        // Core + expanded policy settings with known kinds and semantics.
        // BitLocker, UAC master switches, and service objects are intentionally NOT listed.
        if (objectId is
            "policy.telemetry.allowtelemetry" or
            "policy.telemetry.allowtelemetry.currentversion" or
            "policy.telemetry.donotshowfeedback" or
            "policy.update.noautoupdate" or
            "policy.update.auoptions" or
            "policy.deliveryopt.downloadmode" or
            "policy.defender.disableantispyware" or
            "policy.defender.disablerealtime" or
            "policy.defender.disablebehaviormonitor" or
            "policy.defender.disableioav" or
            "policy.defender.spynetreporting" or
            "policy.defender.submitsamples" or
            "policy.defender.puaprotection" or
            "policy.defender.enablenetworkprotection" or
            "policy.defender.enablecontrolledfolderaccess" or
            "policy.defender.cloudblocklevel" or
            "policy.defender.disableblockatfirstseen" or
            "policy.defender.disablescriptscanning" or
            "policy.defender.disablecatchupfullscan" or
            "policy.defender.disablecatchupquickscan" or
            "policy.search.allowcortana" or
            "policy.search.disablewebsearch" or
            "policy.search.connectedsearchuseweb" or
            "policy.search.allowsearchlocation" or
            "policy.search.allowcloudsearch" or
            "policy.activity.enableactivityfeed" or
            "policy.activity.uploaduseractivities" or
            "policy.activity.publishuseractivities" or
            "policy.cloud.disableconsumerfeatures" or
            "policy.cloud.disablesoftlanding" or
            "policy.cloud.disablecloudoptimized" or
            "policy.cloud.disablewindowsspotlight.hkcu" or
            "policy.cloud.disabletailored.hkcu" or
            "policy.advertising.disabledbygpo" or
            "policy.location.disablelocation" or
            "policy.location.disablelocationscripting" or
            "policy.location.disablewindowslocationsupplier" or
            "policy.smartscreen.enable" or
            "policy.smartscreen.shelllevel" or
            "policy.edge.trackingprevention" or
            "policy.edge.metricsreporting" or
            "policy.edge.personalizationreporting" or
            "policy.edge.searchsuggest" or
            "policy.edge.passwordmanager" or
            "policy.edge.autofilladdress" or
            "policy.edge.autofillcreditcard" or
            "policy.edge.sendsitinfo" or
            "policy.clipboard.allowhistory" or
            "policy.clipboard.allowcrossdevice" or
            "policy.copilot.turnoff" or
            "policy.recall.disableaidataanalysis" or
            "policy.recall.disableclicktodo" or
            "policy.widgets.allow" or
            "policy.widgets.disableboard" or
            "policy.onedrive.disablefilesync" or
            "policy.onedrive.disablepersonal" or
            "policy.onedrive.filesondemand" or
            "policy.network.llmnr" or
            "policy.network.bridge" or
            "policy.remote.rdp" or
            "policy.remote.assistance" or
            "policy.remote.unsolicited" or
            "policy.update.targetreleaseversion" or
            "policy.update.disabledualscan" or
            "policy.update.managepreviewbuilds" or
            "policy.update.allowmuupdateservice" or
            "policy.update.elevatednonadmins" or
            "policy.update.disablewuaccess" or
            "policy.update.donotconnectinternet" or
            "policy.update.excludewudrivers" or
            "policy.update.disableuxwuaccess" or
            "policy.findmydevice.allow" or
            "policy.device.metadataretrieval" or
            "policy.biometrics.enabled" or
            "policy.biometrics.facialfeatures")
            return true;

        return false;
    }

    private static RegistryValueKindExpected ResolveValueKind(string objectId)
    {
        if (objectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase))
            return RegistryValueKindExpected.String;

        if (objectId.Equals("policy.smartscreen.shelllevel", StringComparison.OrdinalIgnoreCase))
            return RegistryValueKindExpected.String;

        if (objectId.Equals("policy.update.targetreleaseversioninfo", StringComparison.OrdinalIgnoreCase))
            return RegistryValueKindExpected.String;

        return RegistryValueKindExpected.DWord;
    }

    private static bool TryParseRegistryPath(string path, out string hive, out string subKey, out string valueName)
    {
        hive = string.Empty;
        subKey = string.Empty;
        valueName = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (path.Contains("...") || path.Contains('*') ||
            path.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("FirewallPolicy-", StringComparison.OrdinalIgnoreCase))
            return false;

        path = path.Replace('/', '\\').Trim();

        string rest;
        if (path.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = "HKLM";
            rest = path[5..];
        }
        else if (path.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = "HKCU";
            rest = path[5..];
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
            return false;

        var lastSlash = rest.LastIndexOf('\\');
        if (lastSlash <= 0 || lastSlash >= rest.Length - 1)
            return false;

        subKey = rest[..lastSlash];
        valueName = rest[(lastSlash + 1)..];
        return !string.IsNullOrWhiteSpace(subKey) && !string.IsNullOrWhiteSpace(valueName);
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
        { mo.ValueSemantics = [V("0", "Disabled", "Disabled", "Windows does not provide an Advertising ID to applications for this user."), V("1", "Enabled", "Enabled", "Windows may provide an Advertising ID to applications for cross-app advertising correlation.")]; return; }
        if (mo.ObjectId is "privacy.tailoredexperiences" or
            "privacy.contentdelivery.systempanesuggestions" or
            "privacy.speech.onlinespeech")
        { mo.ValueSemantics = [V("0", "Disabled", "Disabled", "The user preference is disabled."), V("1", "Enabled", "Enabled", "The user preference is enabled.")]; return; }
        if (mo.ObjectId.Contains("allowtelemetry", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [new ValueMeaning { RawValue = "0", Canonical = "Security", DisplayLabel = "Security", Description = "Minimum supported diagnostic data level (Security).", SupportedEditions = ["Enterprise", "Education"], SupportedVersions = ["Windows 10", "Windows 11"], Confidence = EffectiveConfidence.High }, V("1", "Basic", "Basic", "Basic diagnostic data level."), V("2", "Enhanced", "Enhanced", "Enhanced diagnostic data level."), V("3", "Full", "Full", "Full diagnostic data level.")]; return; }
        if (mo.ObjectId.StartsWith("policy.appprivacy.", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("0", "UserControlled", "User controlled", "Machine policy leaves capability control to the per-user ConsentStore value."), V("1", "ForceAllow", "Force allow", "Machine policy forces the capability allowed for apps."), V("2", "ForceDeny", "Force deny", "Machine policy forces the capability denied for apps.")]; return; }
        if (mo.ObjectId.Contains(".enabled", StringComparison.OrdinalIgnoreCase) && mo.ProductDomain == ProductDomain.Firewall)
        { mo.ValueSemantics = [V("0", "Disabled", "Disabled", "This firewall profile is disabled."), V("1", "Enabled", "Enabled", "This firewall profile is enabled.")]; return; }
        if ((mo.ObjectId.Contains(".inbound", StringComparison.OrdinalIgnoreCase) ||
             mo.ObjectId.Contains(".outbound", StringComparison.OrdinalIgnoreCase)) && mo.ProductDomain == ProductDomain.Firewall)
        { mo.ValueSemantics = [V("0", "Block", "Block", "Default inbound action is Block."), V("1", "Allow", "Allow", "Default inbound action is Allow.")]; return; }
        if (mo.ObjectId.Contains(".notifications", StringComparison.OrdinalIgnoreCase) && mo.ProductDomain == ProductDomain.Firewall)
        { mo.ValueSemantics = [V("0", "Enabled", "Notifications enabled", "Windows notifies the user when a new app is blocked."), V("1", "Disabled", "Notifications disabled", "Blocked-app notifications are suppressed for this profile.")]; return; }
        if (mo.FeatureCategory == FeatureCategory.WindowsService)
        { mo.ValueSemantics = [V("Startup:Automatic", "Automatic", "Automatic startup", "Start with Windows."), V("Startup:Manual", "Manual", "Manual startup", "Start only when requested."), V("Startup:Disabled", "Disabled", "Disabled startup", "Prevent service startup."), V("State:Running", "Running", "Start service", "Start the service now."), V("State:Stopped", "Stopped", "Stop service", "Stop the service now.")]; return; }
        if (mo.FeatureCategory == FeatureCategory.ScheduledTask)
        { mo.ValueSemantics = [V("Enabled", "Enabled", "Enabled", "Allow the task to run on its configured schedule."), V("Disabled", "Disabled", "Disabled", "Prevent scheduled execution until re-enabled.")]; return; }
        if (mo.FeatureCategory == FeatureCategory.AppxPackage)
        { mo.ValueSemantics = [V("Remove", "Removed", "Remove for current user", "Remove the package for the signed-in user.")]; return; }
        if (mo.FeatureCategory == FeatureCategory.OptionalFeature)
        { mo.ValueSemantics = [V("Enabled", "Enabled", "Enable feature", "Enable the optional Windows feature."), V("Disabled", "Disabled", "Disable feature", "Disable the optional Windows feature.")]; return; }
        if (mo.ObjectId.Equals("policy.update.auoptions", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("2", "NotifyBeforeDownload", "Notify before download", "Notify before downloading updates."), V("3", "AutoDownloadNotifyInstall", "Auto download, notify install", "Download automatically and notify before installing."), V("4", "AutoDownloadScheduledInstall", "Auto download and scheduled install", "Download and install on a schedule."), V("5", "LocalAdminCanChoose", "Local admin chooses", "Allow local administrators to choose.")]; return; }
        if (mo.ObjectId.Equals("policy.deliveryopt.downloadmode", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("0", "HttpOnly", "HTTP only", "HTTP only."), V("1", "HttpAndLan", "HTTP + LAN", "HTTP + LAN."), V("2", "HttpLanInternet", "HTTP + LAN + Internet", "HTTP + LAN + Internet."), V("3", "LanOnly", "LAN only", "LAN only."), V("99", "Simple", "Simple mode", "Simple."), V("100", "Bypass", "Bypass", "Bypass.")]; return; }
        if (mo.ObjectId.Equals("policy.defender.spynetreporting", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("0", "Disabled", "Disabled", "MAPS disabled."), V("1", "Basic", "Basic", "Basic."), V("2", "Advanced", "Advanced", "Advanced.")]; return; }
        if (mo.ObjectId.Equals("policy.defender.submitsamples", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("0", "AlwaysPrompt", "Always prompt", "Always prompt."), V("1", "SendSafeSamples", "Send safe samples automatically", "Send safe samples."), V("2", "NeverSend", "Never send", "Never send."), V("3", "SendAllSamples", "Send all samples automatically", "Send all.")]; return; }
        if (mo.ObjectId.Equals("policy.edge.trackingprevention", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("0", "Off", "Off", "Off."), V("1", "Basic", "Basic", "Basic."), V("2", "Balanced", "Balanced", "Balanced."), V("3", "Strict", "Strict", "Strict.")]; return; }
        if (mo.ObjectId.Equals("policy.defender.cloudblocklevel", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("0", "Default", "Default", "Default."), V("2", "High", "High", "High."), V("4", "HighPlus", "High+", "High+."), V("6", "ZeroTolerance", "Zero tolerance", "Zero tolerance.")]; return; }
        if (mo.ObjectId.Equals("policy.smartscreen.shelllevel", StringComparison.OrdinalIgnoreCase))
        { mo.ValueSemantics = [V("Warn", "Warn", "Warn", "Warn."), V("Block", "Block", "Block", "Block.")]; return; }
        if (mo.ObjectId.Equals("policy.uac.consentpromptbehavioradmin", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics =
            [
                V("0", "ElevateWithoutPrompt", "Elevate without prompting", "Admin elevation without prompt (least secure)."),
                V("1", "PromptCredentials", "Prompt for credentials", "Prompt for credentials on the secure desktop."),
                V("2", "PromptConsent", "Prompt for consent", "Prompt for consent on the secure desktop."),
                V("3", "PromptCredentialsNotSecureDesktop", "Prompt credentials (not secure desktop)", "Prompt for credentials without secure desktop."),
                V("4", "PromptConsentNotSecureDesktop", "Prompt consent (not secure desktop)", "Prompt for consent without secure desktop."),
                V("5", "PromptConsentForNonWindows", "Prompt consent for non-Windows binaries", "Default modern admin behavior.")
            ];
            return;
        }
        // Binary 0/1 policies
        if (mo.ObjectId.StartsWith("policy.", StringComparison.OrdinalIgnoreCase)
            && mo.ValueSemantics.Count == 0
            && !mo.ObjectId.Contains("defer", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("targetreleaseversioninfo", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("auoptions", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("downloadmode", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("cloudblock", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("spynet", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("submit", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("tracking", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("shelllevel", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.Contains("encryptionmethod", StringComparison.OrdinalIgnoreCase)
            && !mo.ObjectId.StartsWith("service.", StringComparison.OrdinalIgnoreCase))
        {
            mo.ValueSemantics = [V("0", "Disabled", "Off / Not forced", "Policy value 0."), V("1", "Enabled", "On / Forced", "Policy value 1.")];
        }
    }

    private static ValueMeaning V(string raw, string canonical, string label, string description) => new()
    { RawValue = raw, Canonical = canonical, DisplayLabel = label, Description = description, Confidence = EffectiveConfidence.High };

    private static IReadOnlyList<ManagedObject> CreatePrivacyBatch()
    {
        const string Cs = @"HKCU\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";
        var list = new List<ManagedObject>
        {
            P("privacy.consentstore.location", "Location", "Controls whether apps can access the device location.", "Location data reveals physical movement.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\location\Value"),
            P("privacy.consentstore.webcam", "Camera (Webcam)", "Controls whether apps can access the camera.", "Unauthorized camera access is a privacy risk.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\webcam\Value"),
            P("privacy.consentstore.microphone", "Microphone", "Controls whether apps can access the microphone.", "Microphone access enables continuous audio capture.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\microphone\Value"),
            P("privacy.consentstore.userAccountInformation", "Account Information", "Controls whether apps can access account info.", "Account information is used for personalization.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\userAccountInformation\Value"),
            P("privacy.consentstore.contacts", "Contacts", "Controls whether apps can access contacts.", "Contacts include personal relationships.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\contacts\Value"),
            P("privacy.consentstore.appointments", "Calendar", "Controls whether apps can access calendar.", "Calendar reveals schedule.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\appointments\Value"),
            P("privacy.consentstore.email", "Email", "Controls whether apps can access email.", "Email is highly sensitive.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\email\Value"),
            P("privacy.consentstore.phoneCallHistory", "Call History", "Controls whether apps can access call history.", "Call history exposes communication patterns.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\phoneCallHistory\Value"),
            P("privacy.consentstore.phoneCall", "Phone Call", "Controls whether apps can make phone calls.", "Phone-call capability.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\phoneCall\Value"),
            P("privacy.consentstore.chat", "Chat / Messaging", "Controls whether apps can access messaging.", "Messaging can expose conversation content.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\chat\Value"),
            P("privacy.consentstore.appDiagnostics", "App Diagnostics", "Controls whether apps can access diagnostic info about other apps.", "Observe other apps' runtime behavior.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\appDiagnostics\Value"),
            P("privacy.consentstore.documentsLibrary", "Documents Library", "Controls Documents library access.", "Documents often contain personal files.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\documentsLibrary\Value"),
            P("privacy.consentstore.picturesLibrary", "Pictures Library", "Controls Pictures library access.", "Photos can contain location EXIF.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\picturesLibrary\Value"),
            P("privacy.consentstore.videosLibrary", "Videos Library", "Controls Videos library access.", "Videos may hold personal recordings.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\videosLibrary\Value"),
            P("privacy.consentstore.broadFileSystemAccess", "Broad File System Access", "Controls broad filesystem access.", "Highest-impact AppX capability.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\broadFileSystemAccess\Value"),
            P("privacy.consentstore.radios", "Radios", "Controls radio control by apps.", "Radio control can enable tracking.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\radios\Value"),
            P("privacy.consentstore.bluetoothSync", "Bluetooth Sync", "Controls Bluetooth sync.", "Can exchange personal data.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\bluetoothSync\Value"),
            P("privacy.consentstore.musicLibrary", "Music Library", "Controls Music library access.", "Personal media.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\musicLibrary\Value"),
            P("privacy.consentstore.downloadsFolder", "Downloads Folder", "Controls Downloads folder access.", "Downloads often contain personal files.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\downloadsFolder\Value"),
            P("privacy.consentstore.gazeInput", "Gaze Input", "Controls eye-tracking access.", "Biometric-adjacent.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\gazeInput\Value"),
            P("privacy.consentstore.activity", "Activity", "Controls activity capability.", "Activity history surfaces.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\activity\Value"),
            P("privacy.consentstore.activityData", "Activity Data", "Controls activity data capability.", "Usage patterns.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\activityData\Value"),
            P("privacy.consentstore.humanPresence", "Human Presence", "Controls presence sensors.", "Presence near device.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\humanPresence\Value"),
            P("privacy.consentstore.graphicsCaptureProgrammatic", "Graphics Capture (Programmatic)", "Controls programmatic capture.", "Can expose private content.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\graphicsCaptureProgrammatic\Value"),
            P("privacy.consentstore.graphicsCaptureWithoutBorder", "Graphics Capture Without Border", "Controls capture without border.", "Reduces awareness of recording.", RiskLevel.High, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\graphicsCaptureWithoutBorder\Value"),
            P("privacy.consentstore.cellularData", "Cellular Data", "Controls cellular data use by apps.", "Cellular devices.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\cellularData\Value"),
            P("privacy.consentstore.wifiData", "Wi-Fi Data", "Controls Wi-Fi data use.", "Restricted scenarios.", RiskLevel.Low, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\wifiData\Value"),
            P("privacy.consentstore.userDataSystem", "User Data System", "Controls system user-data surfaces.", "Lower visibility.", RiskLevel.Medium, ProductDomain.ConsentStore, "ConsentStore", Cs + @"\userDataSystem\Value"),
            P("privacy.advertisingid.enabled", "Advertising ID", "Controls Advertising ID.", "Cross-app advertising correlation.", RiskLevel.Medium, ProductDomain.Advertising, "Advertising", @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo\Enabled"),
            P("privacy.tailoredexperiences", "Tailored Experiences", "Controls tailored tips from diagnostic data.", "Personalization from diagnostics.", RiskLevel.Medium, ProductDomain.Telemetry, "DiagnosticPersonalization", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy\TailoredExperiencesWithDiagnosticDataEnabled"),
            P("privacy.contentdelivery.systempanesuggestions", "System Pane Suggestions", "Controls suggested content in system UI.", "Low severity.", RiskLevel.Low, ProductDomain.CloudContent, "ContentDelivery", @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager\SystemPaneSuggestionsEnabled"),
            P("privacy.speech.onlinespeech", "Online Speech Recognition", "Controls online speech processing.", "Audio to cloud.", RiskLevel.High, ProductDomain.Speech, "Speech", @"HKCU\Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy\HasAccepted")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreatePolicyBatch()
    {
        var list = new List<ManagedObject>
        {
            Pol("policy.telemetry.allowtelemetry", "Allow Telemetry (GPO)", "Diagnostic data level via GPO.", "Primary enterprise control.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\AllowTelemetry"),
            Pol("policy.telemetry.allowtelemetry.currentversion", "Allow Telemetry (CurrentVersion)", "Alternate diagnostic data path.", "Same semantic as GPO.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection\AllowTelemetry"),
            Pol("policy.telemetry.donotshowfeedback", "Do Not Show Feedback Notifications", "Suppresses feedback reminders.", "Reduces interruption.", RiskLevel.Low, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.Telemetry, "Telemetry", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection\DoNotShowFeedbackNotifications"),
            Pol("policy.update.noautoupdate", "No Auto Update", "Disables automatic Windows Update.", "Increases exposure unless another channel exists.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\NoAutoUpdate"),
            Pol("policy.update.auoptions", "AU Options", "Automatic update mode.", "Download/install aggressiveness.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU\AUOptions"),
            Pol("policy.deliveryopt.downloadmode", "Delivery Optimization Download Mode", "Peer-to-peer update delivery.", "HTTP-only reduces sharing.", RiskLevel.Medium, FeatureCategory.NetworkSetting, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization\DODownloadMode"),
            Pol("policy.defender.disableantispyware", "Disable AntiSpyware (legacy)", "Legacy policy that can disable Defender.", "Severe security impact.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\DisableAntiSpyware"),
            Pol("policy.defender.disablerealtime", "Disable Real-Time Monitoring", "Disables Defender real-time protection.", "Core host security.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection\DisableRealtimeMonitoring"),
            Pol("policy.defender.spynetreporting", "MAPS / Spynet Reporting", "Cloud-delivered protection reporting.", "Sends metadata.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SpynetReporting"),
            Pol("policy.defender.submitsamples", "Submit Samples Consent", "Automatic sample submission.", "Can include suspicious files.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SubmitSamplesConsent"),
            Pol("policy.search.allowcortana", "Allow Cortana", "Enables/disables Cortana.", "Cloud assistant.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\AllowCortana"),
            Pol("policy.search.disablewebsearch", "Disable Web Search", "Prevents web search queries.", "Keeps search local.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsSearch, ControlLevel.AdministratorControlled, ProductDomain.Search, "Search", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search\DisableWebSearch"),
            Pol("policy.activity.enableactivityfeed", "Enable Activity Feed", "Timeline / activity feed.", "Stores recent activity.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.ActivityHistory, "ActivityHistory", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableActivityFeed"),
            Pol("policy.activity.uploaduseractivities", "Upload User Activities", "Upload activities to cloud.", "Higher privacy impact.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Telemetry, ControlLevel.AdministratorControlled, ProductDomain.ActivityHistory, "ActivityHistory", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\UploadUserActivities"),
            Pol("policy.cloud.disableconsumerfeatures", "Disable Windows Consumer Features", "Turns off consumer experiences.", "Reduces upsell.", RiskLevel.Low, FeatureCategory.CloudComponent, ComponentOwner.Store, ControlLevel.AdministratorControlled, ProductDomain.CloudContent, "CloudContent", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent\DisableWindowsConsumerFeatures"),
            Pol("policy.advertising.disabledbygpo", "Advertising ID Disabled by GPO", "Forces advertising ID off.", "Stronger than user toggle.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Advertising, "Advertising", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo\DisabledByGroupPolicy"),
            Pol("policy.location.disablelocation", "Disable Location", "Disables Windows location.", "Machine-wide kill switch.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Location, "Location", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors\DisableLocation"),
            Pol("policy.appprivacy.location", "Let Apps Access Location (GPO)", "Force location access policy.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessLocation"),
            Pol("policy.appprivacy.camera", "Let Apps Access Camera (GPO)", "Force camera access policy.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessCamera"),
            Pol("policy.appprivacy.microphone", "Let Apps Access Microphone (GPO)", "Force microphone access policy.", "Overrides ConsentStore.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.AppPrivacy, "AppPrivacy", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy\LetAppsAccessMicrophone"),
            Pol("policy.smartscreen.enable", "Enable SmartScreen", "Enables Windows SmartScreen.", "Reputation checks.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Defender, "SmartScreen", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\EnableSmartScreen"),
            Pol("policy.smartscreen.shelllevel", "Shell SmartScreen Level", "Warn or Block for shell.", "Block more restrictive.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Defender, "SmartScreen", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\ShellSmartScreenLevel"),
            Pol("policy.edge.trackingprevention", "Edge Tracking Prevention", "Tracking prevention level.", "Higher reduces tracking.", RiskLevel.Medium, FeatureCategory.EdgePolicy, ComponentOwner.MicrosoftEdge, ControlLevel.AdministratorControlled, ProductDomain.Edge, "Edge", @"HKLM\SOFTWARE\Policies\Microsoft\Edge\TrackingPrevention"),
            Pol("policy.clipboard.allowhistory", "Allow Clipboard History", "Clipboard history on/off.", "Stores multiple items.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Other, "Clipboard", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\AllowClipboardHistory"),
            Pol("policy.clipboard.allowcrossdevice", "Allow Cross-Device Clipboard", "Cross-device clipboard sync.", "Cloud sync of clipboard.", RiskLevel.Medium, FeatureCategory.RegistryPolicy, ComponentOwner.Other, ControlLevel.AdministratorControlled, ProductDomain.Other, "Clipboard", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System\AllowCrossDeviceClipboard")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreateExtendedPolicyBatch()
    {
        var list = new List<ManagedObject>
        {
            Pol("policy.defender.enablenetworkprotection", "Enable Network Protection", "Windows Defender Network Protection.", "Blocks malicious hosts.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection\EnableNetworkProtection"),
            Pol("policy.defender.enablecontrolledfolderaccess", "Enable Controlled Folder Access", "Ransomware protection.", "Protects folders.", RiskLevel.High, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access\EnableControlledFolderAccess"),
            Pol("policy.defender.cloudblocklevel", "Cloud Block Level", "Cloud-delivered block level.", "Higher blocks more aggressively.", RiskLevel.Medium, FeatureCategory.DefenderSetting, ComponentOwner.Defender, ControlLevel.AdministratorControlled, ProductDomain.Defender, "Defender", @"HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine\MpCloudBlockLevel"),
            Pol("policy.update.targetreleaseversion", "Target Release Version", "Pin feature release.", "With TargetReleaseVersionInfo.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\TargetReleaseVersion"),
            Pol("policy.update.disabledualscan", "Disable Dual Scan", "Block public MU when WSUS configured.", "Intranet path only.", RiskLevel.High, FeatureCategory.RegistryPolicy, ComponentOwner.WindowsUpdate, ControlLevel.AdministratorControlled, ProductDomain.WindowsUpdate, "WindowsUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\DisableDualScan")
        };
        return list.AsReadOnly();
    }

    private static IReadOnlyList<ManagedObject> CreateFirewallBatch()
    {
        var list = new List<ManagedObject>
        {
            Fw("firewall.profile.domain.enabled", "Domain Profile Enabled", "Domain firewall profile enabled.", "Domain networks.", RiskLevel.High, "Profiles", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile\EnableFirewall"),
            Fw("firewall.profile.private.enabled", "Private Profile Enabled", "Private firewall profile enabled.", "Private networks.", RiskLevel.High, "Profiles", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\EnableFirewall"),
            Fw("firewall.profile.public.enabled", "Public Profile Enabled", "Public firewall profile enabled.", "Untrusted networks.", RiskLevel.High, "Profiles", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile\EnableFirewall"),
            Fw("firewall.profile.domain.inbound", "Domain Profile Default Inbound Action", "Default inbound on Domain.", "Block unsolicited inbound.", RiskLevel.High, "Defaults", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile\DefaultInboundAction"),
            Fw("firewall.profile.private.inbound", "Private Profile Default Inbound Action", "Default inbound on Private.", "Private network posture.", RiskLevel.High, "Defaults", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\DefaultInboundAction"),
            Fw("firewall.profile.public.inbound", "Public Profile Default Inbound Action", "Default inbound on Public.", "Untrusted; Block expected.", RiskLevel.High, "Defaults", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile\DefaultInboundAction"),
            Fw("firewall.profile.domain.outbound", "Domain Profile Default Outbound Action", "Controls the default action for outbound traffic on domain networks.", "Unexpected outbound blocking can interrupt managed services.", RiskLevel.High, "Domain profile", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile\DefaultOutboundAction"),
            Fw("firewall.profile.private.outbound", "Private Profile Default Outbound Action", "Controls the default action for outbound traffic on private networks.", "Private-network applications often depend on outbound access.", RiskLevel.High, "Private profile", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\DefaultOutboundAction"),
            Fw("firewall.profile.public.outbound", "Public Profile Default Outbound Action", "Controls the default action for outbound traffic on public networks.", "Public-network restrictions can reduce exposure but may interrupt connectivity.", RiskLevel.High, "Public profile", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile\DefaultOutboundAction"),
            Fw("firewall.profile.domain.notifications", "Domain Profile Inbound Notifications", "Controls notifications when a new app is blocked on domain networks.", "Notifications help explain blocked inbound access.", RiskLevel.Medium, "Domain profile", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile\DisableNotifications"),
            Fw("firewall.profile.private.notifications", "Private Profile Inbound Notifications", "Controls notifications when a new app is blocked on private networks.", "Notifications help users distinguish policy blocks from application failures.", RiskLevel.Medium, "Private profile", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile\DisableNotifications"),
            Fw("firewall.profile.public.notifications", "Public Profile Inbound Notifications", "Controls notifications when a new app is blocked on public networks.", "Notifications can surface unexpected inbound requests on untrusted networks.", RiskLevel.Medium, "Public profile", @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile\DisableNotifications"),
            Fw("firewall.service.mpssvc", "Windows Firewall Service (MpsSvc)", "Reports whether the Windows Defender Firewall service is running.", "The service is core network protection and stays view-only.", RiskLevel.High, "Service", "ServiceController:MpsSvc"),
            Fw("firewall.logging.summary", "Firewall Logging Configuration", "Logging summary.", "Observation-only.", RiskLevel.Medium, "Logging", "FirewallPolicy-LoggingSummary")
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
        => Create(id, name, "FirewallSetting", description, rationale, risk,
            id.StartsWith("firewall.profile.", StringComparison.OrdinalIgnoreCase) ? FeatureCategory.FirewallProfile : FeatureCategory.FirewallRule,
            ComponentOwner.Networking, ControlLevel.AdministratorControlled, ProductDomain.Firewall, subCategory, discovery,
            InterfaceName.Firewall, ConfigurationType.FirewallRuleState);

    private static ManagedObject Create(string id, string name, string objectType, string description, string rationale, RiskLevel risk, FeatureCategory category, ComponentOwner owner, ControlLevel control, ProductDomain domain, string subCategory, string discovery, InterfaceName iface, ConfigurationType cfg)
    {
        return new ManagedObject
        {
            ObjectId = id, ObjectName = name, ObjectType = objectType, Description = description, Rationale = rationale,
            FeatureCategory = category, ProductDomain = domain, SubCategory = subCategory, RiskLevel = risk,
            ImpactLevel = ImpactLevel.Security, LifecycleState = LifecycleState.Active, InterfaceName = iface,
            ConfigurationType = cfg, DiscoveryMethod = discovery, CanonicalPath = id, ControlLevel = control,
            ComponentOwner = owner, PriorityLevel = PriorityLevel.Recommended, Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None, SchemaVersion = CatalogVersion, CreatedBy = "ManagedObjectCatalog",
            CreatedTimestamp = DateTime.UnixEpoch, ConfidenceScore = 80, ConfidenceSource = "Curated catalog"
        };
    }
}
