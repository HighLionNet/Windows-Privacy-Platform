using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class WritableTargetAuthorizationTests
{
    public static IEnumerable<object[]> CuratedNativeCases() =>
        CuratedWriteAuthorizations.Targets
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new object[] { pair.Key, pair.Value.SupportedRawValues[0] });

    [Theory]
    [MemberData(nameof(CuratedNativeCases))]
    public void Every_curated_native_authorization_is_present_complete_and_round_trips(
        string objectId,
        string requestedValue)
    {
        var item = ManagedObjectCatalog.All.Single(m => m.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(item.WritableTarget);
        Assert.True(item.WritableTarget!.IsComplete, objectId);
        Assert.Equal(ExclusionReason.None, item.ExclusionReason);
        Assert.Equal(CatalogBucket.Settings, item.Bucket);

        var backend = new MemoryBackend();
        var result = VerifiedWriteContract.Execute(item.WritableTarget, requestedValue, backend);

        Assert.True(result.Success, $"{objectId}: {result.Message}");
        Assert.True(result.After.Readable);
        Assert.True(VerifiedWriteContract.Matches(item.WritableTarget.Kind, result.After.Value, requestedValue));
        Assert.Equal(1, backend.WriteCount);
    }

    [Fact]
    public void All_writable_catalog_entries_have_complete_explicit_targets()
    {
        foreach (var item in ManagedObjectCatalog.All.Where(m => m.IsWritable))
        {
            Assert.NotNull(item.WritableTarget);
            Assert.True(item.WritableTarget!.IsComplete, item.ObjectId);
            Assert.NotEmpty(item.WritableTarget.SupportedRawValues);
            Assert.Equal(ExclusionReason.None, item.ExclusionReason);
        }
    }

    [Fact]
    public void ConsentStore_entries_are_explicit_string_targets_for_current_user()
    {
        var entries = ManagedObjectCatalog.PrivacySettings
            .Where(m => m.ObjectId.StartsWith("privacy.consentstore.", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(entries);
        foreach (var item in entries)
        {
            Assert.Equal(WritableTargetKind.Registry, item.WritableTarget?.Kind);
            Assert.Equal("HKCU", item.WritableTarget?.Hive, ignoreCase: true);
            Assert.Equal(RegistryValueKindExpected.String, item.WritableTarget?.ValueKind);
            Assert.False(item.WritableTarget!.RequiresElevation);
        }
    }

    [Fact]
    public void Only_profile_level_firewall_entries_are_writable()
    {
        var profiles = ManagedObjectCatalog.FirewallSettings.Where(m => m.FeatureCategory == FeatureCategory.FirewallProfile).ToList();
        Assert.Equal(12, profiles.Count);
        Assert.All(profiles, item => Assert.True(item.IsWritable, item.ObjectId));

        var rulesAndService = ManagedObjectCatalog.FirewallSettings.Where(m => m.FeatureCategory != FeatureCategory.FirewallProfile);
        Assert.All(rulesAndService, item =>
        {
            Assert.False(item.IsWritable, item.ObjectId);
            Assert.Equal(ExclusionReason.ReadOnlyByDesign, item.ExclusionReason);
        });
    }

    [Fact]
    public void BitLocker_and_Uac_remain_high_risk_native_handoffs()
    {
        var entries = ManagedObjectCatalog.All.Where(m =>
            m.ObjectId.StartsWith("policy.bitlocker.", StringComparison.OrdinalIgnoreCase) ||
            m.ObjectId.StartsWith("policy.uac.", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, item =>
        {
            Assert.False(item.IsWritable, item.ObjectId);
            Assert.Equal(ExclusionReason.HighRiskIrreversible, item.ExclusionReason);
            Assert.True(item.NativeTool is { IsComplete: true }, item.ObjectId);
            Assert.Equal(CatalogBucket.Settings, item.Bucket);
        });
    }

    [Fact]
    public void Discovery_metadata_cannot_authorize_an_unknown_target()
    {
        var discovered = new ManagedObject
        {
            ObjectId = "unapproved.example",
            DiscoveryMethod = @"HKLM\SOFTWARE\Example\Value"
        };
        Assert.Null(discovered.WritableTarget);
        Assert.False(discovered.IsWritable);
    }

    private sealed class MemoryBackend : IManagedWriteBackend
    {
        private string _state = "Before";
        public int WriteCount { get; private set; }

        public ManagedWriteState Read(WritableTarget target) => new(true, _state);

        public bool Write(WritableTarget target, string requestedValue, out string error)
        {
            WriteCount++;
            _state = target.Kind == WritableTargetKind.AppxPackage && requestedValue == "Remove"
                ? "Removed"
                : requestedValue;
            error = string.Empty;
            return true;
        }
    }
}
