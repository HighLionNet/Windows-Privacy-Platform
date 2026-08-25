using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class CatalogNarrativeTests
{
    [Fact]
    public void Every_catalog_entry_has_complete_authored_narrative()
    {
        Assert.NotEmpty(ManagedObjectCatalog.All);

        foreach (var setting in ManagedObjectCatalog.All)
        {
            Assert.True(setting.Narrative.IsComplete, setting.ObjectId);
            Assert.False(setting.Narrative.FallbackUsed, setting.ObjectId);
            Assert.False(string.IsNullOrWhiteSpace(setting.ConsumerImpact), setting.ObjectId);
            Assert.False(string.IsNullOrWhiteSpace(setting.TypicalEnterpriseUse), setting.ObjectId);
            Assert.False(string.IsNullOrWhiteSpace(setting.WhenIgnored), setting.ObjectId);
            Assert.False(string.IsNullOrWhiteSpace(setting.KnownSideEffects), setting.ObjectId);
            Assert.False(string.IsNullOrWhiteSpace(setting.CommonMisconception), setting.ObjectId);
        }
    }

    [Fact]
    public void Mechanics_are_setting_specific_not_shared_templates()
    {
        var mechanics = ManagedObjectCatalog.All.Select(setting => setting.Narrative.Mechanics).ToList();
        Assert.Equal(mechanics.Count, mechanics.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Explanation_factory_uses_authored_catalog_fields()
    {
        foreach (var setting in ManagedObjectCatalog.All)
        {
            var explanation = SettingExplanationFactory.FromDefinition(setting);
            Assert.Equal(setting.Narrative.Mechanics, explanation.WhatIsIt);
            Assert.Equal(setting.Narrative.KnownSideEffects, explanation.SideEffects);
            Assert.Equal(setting.Narrative.CommonMisconception, explanation.CommonMisconceptions);
        }
    }
}
