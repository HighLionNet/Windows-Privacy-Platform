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
    public void Inventory_split_keeps_only_curated_native_items_in_settings()
    {
        var diagTrack = ManagedObjectCatalog.All.Single(m => m.ObjectId == "service.diagtrack");
        var windowsUpdate = ManagedObjectCatalog.All.Single(m => m.ObjectId == "service.wuaserv");
        Assert.Equal(CatalogBucket.Settings, diagTrack.Bucket);
        Assert.Equal(CatalogBucket.SystemInventory, windowsUpdate.Bucket);

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
