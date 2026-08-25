using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner.Binding;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class PolicyPrecedenceResolverTests
{
    private static readonly ManagedObject SameMeaningDefinition = new()
    {
        ObjectId = "test.same-meaning",
        ObjectName = "Same meaning test",
        ValueSemantics =
        [
            new ValueMeaning { RawValue = "0", Canonical = "Off", DisplayLabel = "Disabled" },
            new ValueMeaning { RawValue = "1", Canonical = "Off", DisplayLabel = "Disabled (alternate encoding)" }
        ]
    };

    [Fact]
    public void ResolveAlternateMachinePolicyPaths_DifferentEncodingSameMeaning_NoConflict()
    {
        var result = PolicyPrecedenceResolver.ResolveAlternateMachinePolicyPaths(
            Observation(ConfigurationLayer.MachinePolicy, "0"),
            Observation(ConfigurationLayer.AlternatePolicyStore, "1"),
            SameMeaningDefinition,
            "Example policy");

        Assert.False(result.HasConflict);
        Assert.Equal(EffectiveConfidence.High, result.Confidence);
        Assert.Equal("Off", result.SemanticValue);
    }

    [Fact]
    public void ResolveByLayerRank_DifferentEncodingSameMeaning_NoConflict()
    {
        var result = PolicyPrecedenceResolver.ResolveByLayerRank(
        [
            Observation(ConfigurationLayer.MachinePolicy, "0"),
            Observation(ConfigurationLayer.UserPreference, "1")
        ], SameMeaningDefinition, "Example policy");

        Assert.False(result.HasConflict);
        Assert.Equal(EffectiveConfidence.High, result.Confidence);
        Assert.Equal("Off", result.SemanticValue);
    }

    [Fact]
    public void ResolveByLayerRank_SameRankDifferentEncodingSameMeaning_NoConflict()
    {
        var result = PolicyPrecedenceResolver.ResolveByLayerRank(
        [
            Observation(ConfigurationLayer.MachinePolicy, "0"),
            Observation(ConfigurationLayer.MachinePolicy, "1")
        ], SameMeaningDefinition, "Example policy");

        Assert.False(result.HasConflict);
        Assert.Equal("0", result.EffectiveValue);
    }

    private static ConfigurationObservation Observation(ConfigurationLayer layer, string value) =>
        new() { Layer = layer, RawValue = value };
}
