using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Validator;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class CatalogIntegrityTests
{
    [Fact]
    public void Catalog_has_unique_ids_complete_narratives_and_explicit_write_decisions()
    {
        var catalog = ManagedObjectCatalog.All;
        Assert.True(catalog.Count >= 140, $"Catalog count was {catalog.Count}.");
        Assert.Equal(catalog.Count, catalog.Select(m => m.ObjectId).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var narrativeRule = new NarrativeContentRule();
        var writeRule = new WriteAuthorizationDecisionRule();
        foreach (var item in catalog)
        {
            Assert.Equal(ManagedObjectCatalog.CatalogVersion, item.SchemaVersion);
            Assert.False(string.IsNullOrWhiteSpace(item.TechnicalLocation), item.ObjectId);
            Assert.True(narrativeRule.Evaluate(item, out var narrativeError), $"{item.ObjectId}: {narrativeError}");
            Assert.True(writeRule.Evaluate(item, out var writeError), $"{item.ObjectId}: {writeError}");
        }
    }

    [Fact]
    public void Narrative_rule_rejects_registry_paths_discovery_tokens_and_object_ids()
    {
        foreach (var leak in new[]
                 {
                     @"The value is at HKLM\SOFTWARE\Example.",
                     "ServiceController:Example reports the state.",
                     "ScheduledTask:Example runs daily.",
                     "Windows Privacy Platform observes the value.",
                     "The internal id is test.object.id."
                 })
        {
            var item = CompleteItem();
            item.Narrative.Summary = leak;
            Assert.False(new NarrativeContentRule().Evaluate(item, out _));
        }
    }

    [Fact]
    public void Write_decision_rule_rejects_default_exclusion_on_view_only_entry()
    {
        var item = CompleteItem();
        item.WritableTarget = null;
        item.ExclusionReason = ExclusionReason.None;
        Assert.False(new WriteAuthorizationDecisionRule().Evaluate(item, out var error));
        Assert.Contains("explicit", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(20, true)]
    public void Category_flattening_threshold_is_stable(int count, bool expected) =>
        Assert.Equal(expected, CatalogPolicy.RequiresDrillDown(count));

    [Fact]
    public void Inventory_split_keeps_all_native_components_read_only()
    {
        var diagTrack = ManagedObjectCatalog.All.Single(m => m.ObjectId == "service.diagtrack");
        var windowsUpdate = ManagedObjectCatalog.All.Single(m => m.ObjectId == "service.wuaserv");
        Assert.Equal(CatalogBucket.SystemInventory, diagTrack.Bucket);
        Assert.Equal(CatalogBucket.SystemInventory, windowsUpdate.Bucket);
        Assert.False(diagTrack.IsWritable);

        var snapshot = new InventorySnapshot
        {
            Services = [new ServiceInfo { Name = "ExampleService", StartMode = "Manual", State = "Stopped" }]
        };
        var dynamicItem = Assert.Single(DynamicInventoryCatalog.Create(snapshot, ManagedObjectCatalog.All));
        Assert.True(dynamicItem.IsDynamicInventory);
        Assert.Equal(CatalogBucket.SystemInventory, dynamicItem.Bucket);
        Assert.Equal(ExclusionReason.ReadOnlyByDesign, dynamicItem.ExclusionReason);
        Assert.False(dynamicItem.IsWritable);
    }

    [Fact]
    public void Public_settings_are_all_editable_registry_policies()
    {
        var settings = ManagedObjectCatalog.All.Where(m => m.Bucket == CatalogBucket.Settings).ToList();
        Assert.NotEmpty(settings);
        Assert.All(settings, item =>
        {
            Assert.True(item.IsWritable, item.ObjectId);
            Assert.Equal(WritableTargetKind.Registry, item.WritableTarget!.Kind);
            Assert.DoesNotContain(item.ProductDomain, new[] { ProductDomain.WindowsUpdate, ProductDomain.Storage });
        });
    }

    [Fact]
    public void Public_option_copy_is_actionable_and_never_raw_value_boilerplate()
    {
        foreach (var item in ManagedObjectCatalog.All.Where(m => m.Bucket == CatalogBucket.Settings))
        foreach (var meaning in item.ValueSemantics ?? [])
        {
            var copy = SettingOptionLanguage.For(item, meaning);
            Assert.False(string.IsNullOrWhiteSpace(copy.Action), item.ObjectId);
            Assert.False(string.IsNullOrWhiteSpace(copy.Effect), item.ObjectId);
            Assert.DoesNotContain("Policy value", copy.Effect, StringComparison.OrdinalIgnoreCase);
            Assert.False(copy.Action.Equals(copy.Effect, StringComparison.OrdinalIgnoreCase), item.ObjectId);
        }
    }

    [Fact]
    public void Posture_summary_does_not_count_unknown_as_safe()
    {
        var item = new ManagedObject
        {
            ObjectId = "privacy.advertisingid.enabled",
            ObjectName = "Advertising ID",
            ProductDomain = ProductDomain.Advertising,
            CurrentState = "Unknown"
        };
        var posture = PostureAssessment.Build([item]);
        Assert.Equal(0, posture.EvaluatedCount);
        Assert.Equal(0, posture.ProtectedCount);
        Assert.Empty(posture.Findings);
    }

    private static ManagedObject CompleteItem()
    {
        var item = new ManagedObject
        {
            ObjectId = "test.object.id",
            ObjectName = "Test object",
            ObjectType = "Test",
            Description = "Controls a test behavior.",
            Rationale = "The behavior matters during validation.",
            TechnicalLocation = "Registry source",
            ExclusionReason = ExclusionReason.ReadOnlyByDesign
        };
        CatalogNarrativeAuthoring.Apply(item);
        return item;
    }
}
