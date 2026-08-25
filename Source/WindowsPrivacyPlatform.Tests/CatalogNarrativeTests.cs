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
    public void Decision_support_paragraphs_are_not_reused_between_settings()
    {
        var narratives = ManagedObjectCatalog.All.Select(setting => setting.Narrative).ToList();

        AssertUnique(narratives.Select(item => item.WhyItMatters), nameof(SettingNarrative.WhyItMatters));
        AssertUnique(narratives.Select(item => item.ConsumerImpact), nameof(SettingNarrative.ConsumerImpact));
        AssertUnique(narratives.Select(item => item.TypicalEnterpriseUse), nameof(SettingNarrative.TypicalEnterpriseUse));
        AssertUnique(narratives.Select(item => item.WhenIgnored), nameof(SettingNarrative.WhenIgnored));
        AssertUnique(narratives.Select(item => item.KnownSideEffects), nameof(SettingNarrative.KnownSideEffects));
        AssertUnique(narratives.Select(item => item.CommonMisconception), nameof(SettingNarrative.CommonMisconception));
        AssertUnique(narratives.Select(item => item.PrivacyImpact), nameof(SettingNarrative.PrivacyImpact));
        AssertUnique(narratives.Select(item => item.SecurityImpact), nameof(SettingNarrative.SecurityImpact));
        AssertUnique(narratives.Select(item => item.DecisionGuidance), nameof(SettingNarrative.DecisionGuidance));
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

    private static void AssertUnique(IEnumerable<string> paragraphs, string field)
    {
        var values = paragraphs.ToList();
        Assert.True(
            values.Count == values.Distinct(StringComparer.Ordinal).Count(),
            $"{field} contains a paragraph reused by multiple settings.");
    }
}
