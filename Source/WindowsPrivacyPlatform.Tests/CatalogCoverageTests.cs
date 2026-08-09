using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class CatalogCoverageTests
{
    [Fact]
    public void Catalog_includes_expanded_appprivacy_and_defender()
    {
        var ids = ManagedObjectCatalog.All.Select(m => m.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("policy.appprivacy.contacts", ids);
        Assert.Contains("policy.appprivacy.filesystem", ids);
        Assert.Contains("policy.defender.disablebehaviormonitor", ids);
        Assert.Contains("policy.uac.enablelua", ids);
        Assert.Contains("policy.bitlocker.enablob", ids);
        Assert.Contains("service.diagtrack", ids);
    }

    [Fact]
    public void BitLocker_and_services_are_observation_only()
    {
        foreach (var mo in ManagedObjectCatalog.All.Where(m =>
                     m.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.StartsWith("service.", StringComparison.OrdinalIgnoreCase)
                     || m.ObjectId.Equals("policy.uac.enablelua", StringComparison.OrdinalIgnoreCase)))
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
    public void Catalog_has_substantially_more_than_core_batch()
    {
        // Core privacy alone is ~32; expansion should push total well above 80.
        Assert.True(ManagedObjectCatalog.All.Count >= 80, $"Count={ManagedObjectCatalog.All.Count}");
    }
}
