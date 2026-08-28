using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.App.Services;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class V251AcceptanceTests
{
    public static readonly string[] FreezeListIds =
    [
        "policy.copilot.removemicrosoftcopilotapp",
        "policy.copilot.disablesettingsagent",
        "policy.edge.m365copiloticon",
        "policy.copilot.turnoffwindowscopilot",
        "policy.bitlocker.preventdeviceencryption",
        "policy.bitlocker.activeDirectoryBackup",
        "policy.bitlocker.enablob",
        "policy.bitlocker.encryptionmethod",
        "policy.bitlocker.recoverypassword",
        "policy.bitlocker.requiredeviceencryption",
        "policy.uac.consentpromptbehavioradmin",
        "policy.uac.enablelua",
        "policy.uac.filteradministratortoken",
        "policy.uac.promptonsecuredesktop",
        "policy.findmydevice.allow",
        "policy.update.noautoupdate",
        "policy.update.auoptions",
        "policy.update.deferfeatureupdates",
        "policy.update.deferqualityupdates",
        "policy.update.disabledualscan",
        "policy.update.disablewuaccess",
        "policy.update.donotconnectinternet",
        "policy.update.elevatednonadmins",
        "policy.update.excludewudrivers",
        "policy.update.managepreviewbuilds",
        "policy.update.targetreleaseversion",
        "policy.update.targetreleaseversioninfo",
        "policy.update.allowmuupdateservice",
        "policy.deliveryopt.downloadmode",
        "policy.storage.allow",
        "policy.storage.cadence",
        "policy.storage.onedriveage"
    ];

    [Fact]
    public void Freeze_list_object_ids_are_present()
    {
        var ids = ManagedObjectCatalog.All.Select(item => item.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(FreezeListIds, id => Assert.Contains(id, ids));
    }

    [Fact]
    public void Copilot_remove_path_is_exact()
    {
        var item = ManagedObjectCatalog.All.Single(item => item.ObjectId == "policy.copilot.removemicrosoftcopilotapp");
        Assert.Equal("HKLM", item.WritableTarget?.Hive, ignoreCase: true);
        Assert.Equal(@"SOFTWARE\Policies\Microsoft\Windows\WindowsAI", item.WritableTarget?.SubKey, ignoreCase: true);
        Assert.Equal("RemoveMicrosoftCopilotApp", item.WritableTarget?.ValueName, ignoreCase: true);
    }

    [Theory]
    [InlineData("0", "1", false)]
    [InlineData("1", "1", true)]
    public void Advertising_conflicts_compare_outcomes(string userRaw, string gpoRaw, bool expected) =>
        Assert.Equal(expected, OutcomeConflictEngine.AdvertisingConflicts(userRaw, gpoRaw));

    [Fact]
    public void Required_windows_domains_are_typed_settings_not_internal_reference()
    {
        var domains = new[] { ProductDomain.WindowsUpdate, ProductDomain.Storage, ProductDomain.BitLocker,
            ProductDomain.Uac, ProductDomain.FindMyDevice };
        foreach (var domain in domains)
        {
            var items = ManagedObjectCatalog.All.Where(item => item.ProductDomain == domain &&
                item.Bucket == CatalogBucket.Settings).ToList();
            Assert.NotEmpty(items);
            Assert.All(items, item =>
            {
                Assert.Equal(CatalogBucket.Settings, item.Bucket);
                Assert.True(item.IsWritable, item.ObjectId);
                Assert.Equal(WritableTargetKind.Registry, item.WritableTarget?.Kind);
            });
        }
        var required = FreezeListIds.Where(id => id.StartsWith("policy.update.") || id.StartsWith("policy.storage.") ||
            id.StartsWith("policy.bitlocker.") || id.StartsWith("policy.uac.") || id.StartsWith("policy.findmydevice."));
        Assert.All(required, id => Assert.Equal(CatalogBucket.Settings,
            ManagedObjectCatalog.All.Single(item => item.ObjectId.Equals(id, StringComparison.OrdinalIgnoreCase)).Bucket));
        Assert.True(ManagedObjectCatalog.All.Single(item => item.ObjectId == "policy.copilot.disablesettingsagent").IsWritable);
    }

    [Fact]
    public void Uac_and_BitLocker_lockout_controls_are_high_impact()
    {
        foreach (var id in new[] { "policy.uac.enablelua", "policy.bitlocker.preventdeviceencryption",
                     "policy.bitlocker.requiredeviceencryption" })
            Assert.True(ManagedObjectCatalog.All.Single(item => item.ObjectId == id).HighImpact, id);
    }

    [Fact]
    public void Permanent_service_deny_list_is_rejected_by_public_api_before_mutation()
    {
        var names = new[] { "RpcSs", "DcomLaunch", "Lsa", "SamSs", "EventLog", "PlugPlay", "Power",
            "WinDefend", "Sense", "wdfilter", "BFE", "mpssvc", "CryptSvc", "Schedule", "ProfSvc",
            "UserManager", "Dhcp", "Dnscache", "NlaSvc", "nsi", "Winmgmt", "CSM", "CoreMessaging" };
        var serviceControl = new ServiceControlService();
        foreach (var name in names)
        {
            Assert.True(ServiceMutationPolicy.IsDeniedName(name), name);
            var service = new ServiceInfo { Name = name, IsMicrosoft = false, StartMode = "Manual", State = "Stopped" };
            foreach (var action in Enum.GetValues<ServiceControlAction>())
            foreach (var administrator in new[] { false, true })
            foreach (var confirmed in new[] { false, true })
                Assert.False(serviceControl.TryChange(service, action, administrator, confirmed, out _), $"{name}:{action}");
        }
    }

    [Fact]
    public void Inverted_disable_policy_raw_one_always_describes_feature_off()
    {
        var item = ManagedObjectCatalog.All.First(setting => setting.ObjectId.Contains("disable", StringComparison.OrdinalIgnoreCase) &&
            setting.ValueSemantics.Any(value => value.RawValue == "1"));
        var copy = SettingOptionLanguage.For(item, item.ValueSemantics.First(value => value.RawValue == "1"));
        Assert.Contains(new[] { "Block the feature", "Force advertising ID off" }, action => copy.Action == action);
        Assert.DoesNotContain("Set to On", copy.Action, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("On", copy.Effect, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Default_and_not_on_this_pc_filters_are_disjoint()
    {
        var target = ManagedObjectCatalog.All.First(item => item.IsWritable);
        var applicable = CloneForFilter(target, ApplicabilityState.Applicable);
        var unavailable = CloneForFilter(target, ApplicabilityState.NotAvailableOnBuild);
        Assert.Equal([applicable], CatalogFilter.DefaultSettings([applicable, unavailable]));
        Assert.Equal([unavailable], CatalogFilter.NotOnThisPc([applicable, unavailable]));
    }

    [Fact]
    public void Catalog_write_targets_are_typed_registry_contracts()
    {
        var writable = ManagedObjectCatalog.All.Where(item => item.IsWritable).ToList();
        Assert.NotEmpty(writable);
        Assert.All(writable, item =>
        {
            Assert.Equal(WritableTargetKind.Registry, item.WritableTarget?.Kind);
            Assert.True(item.WritableTarget is { IsComplete: true }, item.ObjectId);
            Assert.DoesNotContain("cmd.exe", item.TechnicalLocation, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("powershell", item.TechnicalLocation, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static ManagedObject CloneForFilter(ManagedObject source, ApplicabilityState applicability) => new()
    {
        ObjectId = source.ObjectId + "." + applicability,
        ObjectName = source.ObjectName,
        ProductDomain = source.ProductDomain,
        FeatureCategory = source.FeatureCategory,
        Applicability = applicability,
        Bucket = CatalogBucket.Settings,
        WritableTarget = source.WritableTarget,
        ExclusionReason = ExclusionReason.None
    };
}
