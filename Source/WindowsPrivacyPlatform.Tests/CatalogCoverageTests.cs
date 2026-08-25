using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class CatalogCoverageTests
{
    [Fact]
    public void Catalog_includes_v2_1_policy_and_inventory_expansion()
    {
        var ids = ManagedObjectCatalog.All.Select(m => m.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("policy.appprivacy.contacts", ids);
        Assert.Contains("policy.appprivacy.filesystem", ids);
        Assert.Contains("policy.defender.disablebehaviormonitor", ids);
        Assert.Contains("policy.recall.disableaidataanalysis", ids);
        Assert.Contains("policy.copilot.removemicrosoftcopilotapp", ids);
        Assert.Contains("policy.asr.lsasscredentialtheft", ids);
        Assert.Contains("policy.hello.cloudtrust", ids);
        Assert.Contains("policy.storage.clouddehydration", ids);
        Assert.Contains("policy.network.dohmode", ids);
        Assert.Contains("policy.security.debugprograms", ids);
        Assert.Contains("policy.uac.enablelua", ids);
        Assert.Contains("policy.bitlocker.enablob", ids);
        Assert.Contains("service.diagtrack", ids);
        Assert.Contains("task.ceip.consolidator", ids);
        Assert.Contains("package.widgets.webexperience", ids);
        Assert.Contains("capability.hello.face", ids);
    }

    [Fact]
    public void Sensitive_and_inventory_expansions_are_observation_only()
    {
        foreach (var mo in ManagedObjectCatalog.All.Where(m =>
                     m.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("policy.uac.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("policy.asr.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("policy.security.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("service.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("task.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("package.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("capability.", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.False(mo.IsWritable, mo.ObjectId);
        }
    }

    [Fact]
    public void AppPrivacy_policies_are_writable()
    {
        var appPrivacy = ManagedObjectCatalog.All
            .Where(m => m.ObjectId.StartsWith("policy.appprivacy.", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(appPrivacy);
        foreach (var mo in appPrivacy)
            Assert.True(mo.IsWritable, mo.ObjectId);
    }

    [Fact]
    public void Catalog_meets_v2_1_coverage_floor()
    {
        Assert.True(ManagedObjectCatalog.All.Count >= 250, $"Count={ManagedObjectCatalog.All.Count}");
    }
}
