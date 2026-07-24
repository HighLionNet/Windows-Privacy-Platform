// Source/WindowsPrivacyPlatform.Models/ManagedObjectCatalog.cs
using System.Collections.Generic;

namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Static catalog of predefined ManagedObjects for high-value privacy settings.
/// Pure data only — no business logic. Used by later model/report layers
/// to explain discovered inventory, not merely list it.
/// </summary>
public static class ManagedObjectCatalog
{
    /// <summary>
    /// First batch: ConsentStore capabilities and related current-user privacy preferences.
    /// </summary>
    public static IReadOnlyList<ManagedObject> PrivacySettings { get; } = CreatePrivacyBatch();

    private static IReadOnlyList<ManagedObject> CreatePrivacyBatch()
    {
        var list = new List<ManagedObject>
        {
            Create(
                id: "privacy.consentstore.location",
                name: "Location",
                description: "Controls whether apps can access the device location.",
                rationale: "Location data reveals physical movement and habitual places. Denying access reduces tracking risk while breaking navigation and weather apps that need it.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\location\\Value"),

            Create(
                id: "privacy.consentstore.webcam",
                name: "Camera (Webcam)",
                description: "Controls whether apps can access the camera.",
                rationale: "Unauthorized camera access is a direct privacy and safety risk. Prefer Deny or Prompt unless a trusted app requires it.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\webcam\\Value"),

            Create(
                id: "privacy.consentstore.microphone",
                name: "Microphone",
                description: "Controls whether apps can access the microphone.",
                rationale: "Microphone access enables continuous audio capture. High risk if granted broadly; keep at Prompt or Deny for untrusted apps.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\microphone\\Value"),

            Create(
                id: "privacy.consentstore.userAccountInformation",
                name: "Account Information",
                description: "Controls whether apps can access your name, picture, and account info.",
                rationale: "Account information is used for personalization and can aid profiling. Medium risk; review which apps truly need it.",
                risk: RiskLevel.Medium,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\userAccountInformation\\Value"),

            Create(
                id: "privacy.consentstore.contacts",
                name: "Contacts",
                description: "Controls whether apps can access your contacts.",
                rationale: "Contacts often include personal and professional relationships. Exposing them increases social-graph leakage risk.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\contacts\\Value"),

            Create(
                id: "privacy.consentstore.appointments",
                name: "Calendar",
                description: "Controls whether apps can access your calendar appointments.",
                rationale: "Calendar data reveals schedule, meetings, and often location. High sensitivity for professional and personal privacy.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\appointments\\Value"),

            Create(
                id: "privacy.consentstore.email",
                name: "Email",
                description: "Controls whether apps can access email.",
                rationale: "Email content and metadata are highly sensitive. Restrict to apps that explicitly require mail access.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\email\\Value"),

            Create(
                id: "privacy.consentstore.phoneCallHistory",
                name: "Call History",
                description: "Controls whether apps can access phone call history.",
                rationale: "Call history exposes communication patterns and contacts. Rarely needed by desktop apps; prefer Deny.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\phoneCallHistory\\Value"),

            Create(
                id: "privacy.consentstore.appDiagnostics",
                name: "App Diagnostics",
                description: "Controls whether apps can access diagnostic information about other apps.",
                rationale: "Allows one app to observe others' runtime behavior. Useful for system tools; unnecessary for most apps.",
                risk: RiskLevel.Medium,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\appDiagnostics\\Value"),

            Create(
                id: "privacy.consentstore.documentsLibrary",
                name: "Documents Library",
                description: "Controls whether apps can access the Documents library.",
                rationale: "Documents often contain personal and work files. Prefer Prompt so the user approves each access pattern.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\documentsLibrary\\Value"),

            Create(
                id: "privacy.consentstore.picturesLibrary",
                name: "Pictures Library",
                description: "Controls whether apps can access the Pictures library.",
                rationale: "Photos can contain location EXIF data and private imagery. High privacy impact if broadly allowed.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\picturesLibrary\\Value"),

            Create(
                id: "privacy.consentstore.videosLibrary",
                name: "Videos Library",
                description: "Controls whether apps can access the Videos library.",
                rationale: "Video libraries may hold personal recordings. Same risk profile as pictures; restrict unless needed.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\videosLibrary\\Value"),

            Create(
                id: "privacy.consentstore.broadFileSystemAccess",
                name: "Broad File System Access",
                description: "Controls whether apps can access the file system broadly beyond known folders.",
                rationale: "Broad filesystem access is one of the highest-impact AppX capabilities. Prefer Deny unless a trusted tool requires full access.",
                risk: RiskLevel.High,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\broadFileSystemAccess\\Value"),

            Create(
                id: "privacy.consentstore.radios",
                name: "Radios",
                description: "Controls whether apps can control device radios (Bluetooth, Wi-Fi, etc.).",
                rationale: "Radio control can enable tracking or unexpected connectivity changes. Medium risk; limit to system-like apps.",
                risk: RiskLevel.Medium,
                subCategory: "ConsentStore",
                discovery: "HKCU\\...\\ConsentStore\\radios\\Value"),

            Create(
                id: "privacy.advertisingid.enabled",
                name: "Advertising ID",
                description: "Controls whether Windows provides an advertising ID to apps for cross-app tracking.",
                rationale: "Disabling the advertising ID reduces cross-app advertising correlation. Low functional impact for most users.",
                risk: RiskLevel.Medium,
                subCategory: "Advertising",
                discovery: "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\\Enabled"),

            Create(
                id: "privacy.tailoredexperiences",
                name: "Tailored Experiences",
                description: "Controls whether diagnostic data is used to offer tailored tips and recommendations.",
                rationale: "Uses diagnostic data for personalization. Disabling reduces data reuse for recommendations with minimal feature loss.",
                risk: RiskLevel.Medium,
                subCategory: "DiagnosticPersonalization",
                discovery: "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\\TailoredExperiencesWithDiagnosticDataEnabled"),

            Create(
                id: "privacy.contentdelivery.systempanesuggestions",
                name: "System Pane Suggestions",
                description: "Controls suggested content in system UI panes (Settings tips, etc.).",
                rationale: "Suggested content is low severity but contributes to attention and soft telemetry. Optional to disable for a quieter UI.",
                risk: RiskLevel.Low,
                subCategory: "ContentDelivery",
                discovery: "HKCU\\...\\ContentDeliveryManager\\SystemPaneSuggestionsEnabled"),

            Create(
                id: "privacy.speech.onlinespeech",
                name: "Online Speech Recognition",
                description: "Controls whether speech input may be processed by online (cloud) speech services.",
                rationale: "Online speech sends audio to Microsoft cloud services. Prefer local-only recognition when available if cloud processing is undesirable.",
                risk: RiskLevel.High,
                subCategory: "Speech",
                discovery: "HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy\\HasAccepted")
        };

        return list.AsReadOnly();
    }

    private static ManagedObject Create(
        string id,
        string name,
        string description,
        string rationale,
        RiskLevel risk,
        string subCategory,
        string discovery)
    {
        return new ManagedObject
        {
            ObjectId = id,
            ObjectName = name,
            ObjectType = "PrivacySetting",
            Description = description,
            Rationale = rationale,
            FeatureCategory = FeatureCategory.PrivacyPermission,
            SubCategory = subCategory,
            RiskLevel = risk,
            ImpactLevel = ImpactLevel.User,
            LifecycleState = LifecycleState.Active,
            InterfaceName = InterfaceName.Registry,
            ConfigurationType = ConfigurationType.RegistryValue,
            DiscoveryMethod = discovery,
            ControlLevel = ControlLevel.UserControlled,
            ComponentOwner = ComponentOwner.Other,
            PriorityLevel = PriorityLevel.Recommended,
            Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None,
            SchemaVersion = "0.4",
            CreatedBy = "ManagedObjectCatalog",
            CreatedTimestamp = DateTime.UtcNow,
            ConfidenceScore = 80,
            ConfidenceSource = "Catalog-v0.4-batch1"
        };
    }
}
