namespace WindowsPrivacyPlatform.Models;

public static class CatalogV251Expansion
{
    public static IReadOnlyList<ManagedObject> CreateCoverageBatch() =>
    [
        new ManagedObject
        {
            ObjectId = "policy.bitlocker.preventdeviceencryption",
            ObjectName = "Prevent Automatic Device Encryption",
            ObjectType = "PolicySetting",
            CanonicalPath = "policy.bitlocker.preventdeviceencryption",
            Description = "Controls whether Windows may turn on automatic device encryption.",
            Rationale = "Preventing automatic encryption can leave device data unprotected.",
            FeatureCategory = FeatureCategory.RegistryPolicy,
            ProductDomain = ProductDomain.BitLocker,
            SubCategory = "BitLocker",
            RiskLevel = RiskLevel.High,
            ImpactLevel = ImpactLevel.Security,
            InterfaceName = InterfaceName.Registry,
            ConfigurationType = ConfigurationType.PolicyState,
            DiscoveryMethod = @"HKLM\SYSTEM\CurrentControlSet\Control\BitLocker\PreventDeviceEncryption",
            ControlLevel = ControlLevel.AdministratorControlled,
            ComponentOwner = ComponentOwner.Other,
            PriorityLevel = PriorityLevel.Recommended,
            Reversibility = Reversibility.Reversible,
            RebootRequirement = RebootRequirement.None,
            LifecycleState = LifecycleState.Active,
            CreatedBy = nameof(CatalogV251Expansion),
            CreatedTimestamp = DateTime.UnixEpoch,
            ConfidenceScore = 95,
            SupportedWindowsVersions = ["Windows 10", "Windows 11"]
        }
    ];
}

public static class CatalogImpact
{
    public static bool IsHighImpact(string objectId) =>
        objectId.StartsWith("policy.uac.", StringComparison.OrdinalIgnoreCase) ||
        objectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase) ||
        objectId is
            "policy.defender.disablerealtime" or
            "policy.defender.disableantispyware" or
            "policy.update.disablewuaccess" or
            "policy.update.noautoupdate" or
            "policy.update.donotconnectinternet" or
            "policy.security.runasppl" or
            "policy.copilot.removemicrosoftcopilotapp" or
            "policy.recall.disableaidataanalysis";

    public static bool RequiresStepUp(ManagedObject item, string? rawValue) =>
        item.HighImpact ||
        item.ObjectId.Equals("policy.remote.rdp", StringComparison.OrdinalIgnoreCase) && rawValue == "0";
}
