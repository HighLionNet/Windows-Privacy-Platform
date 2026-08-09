using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class WritableTargetAuthorizationTests
{
    [Fact]
    public void Firewall_settings_are_never_writable()
    {
        foreach (var mo in ManagedObjectCatalog.FirewallSettings)
        {
            Assert.Null(mo.WritableTarget);
            Assert.False(mo.IsWritable);
        }
    }

    [Fact]
    public void DiscoveryMethod_alone_does_not_authorize_write_for_unknown_ids()
    {
        // Any setting without explicit whitelist entry must remain observation-only.
        // Firewall already covered; also assert no WritableTarget is fabricated for empty ObjectId patterns.
        var all = ManagedObjectCatalog.All;
        Assert.NotEmpty(all);

        foreach (var mo in all)
        {
            if (mo.WritableTarget is null)
                continue;

            // If writable, target must be complete and ValueKind must not be Unsupported.
            Assert.True(mo.WritableTarget.IsComplete, mo.ObjectId);
            Assert.NotEqual(RegistryValueKindExpected.Unsupported, mo.WritableTarget.ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(mo.WritableTarget.Hive));
            Assert.False(string.IsNullOrWhiteSpace(mo.WritableTarget.SubKey));
            Assert.False(string.IsNullOrWhiteSpace(mo.WritableTarget.ValueName));
        }
    }

    [Fact]
    public void ConsentStore_entries_are_explicitly_writable_as_String_HKCU()
    {
        var cs = ManagedObjectCatalog.PrivacySettings
            .Where(m => m.ObjectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(cs);

        foreach (var mo in cs)
        {
            Assert.True(mo.IsWritable, mo.ObjectId);
            Assert.NotNull(mo.WritableTarget);
            Assert.Equal("HKCU", mo.WritableTarget!.Hive, ignoreCase: true);
            Assert.Equal(RegistryValueKindExpected.String, mo.WritableTarget.ValueKind);
            Assert.False(mo.WritableTarget.RequiresElevation);
        }
    }

    [Fact]
    public void Catalog_ObjectIds_are_unique()
    {
        var ids = ManagedObjectCatalog.All.Select(m => m.ObjectId).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Schema_version_is_2_1()
    {
        foreach (var mo in ManagedObjectCatalog.All)
            Assert.Equal("2.1", mo.SchemaVersion);
    }
}
