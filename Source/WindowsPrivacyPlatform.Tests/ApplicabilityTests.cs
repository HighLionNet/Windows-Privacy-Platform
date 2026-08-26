using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public class ApplicabilityTests
{
    [Fact]
    public void Edition_restriction_marks_Home_as_unavailable()
    {
        var item = new ManagedObject { SupportedEditions = ["Pro", "Enterprise"] };
        var result = ApplicabilityEvaluator.Evaluate(item, "Windows 11 24H2", "Home", 26100);
        Assert.Equal(ApplicabilityState.NotAvailableOnEdition, result.State);
    }

    [Fact]
    public void Build_restriction_is_reported_without_hiding_the_entry()
    {
        var item = new ManagedObject { MinimumBuild = 26100, SupportedWindowsVersions = ["Windows 11"] };
        var result = ApplicabilityEvaluator.Evaluate(item, "Windows 11 23H2", "Pro", 22631);
        Assert.Equal(ApplicabilityState.NotAvailableOnBuild, result.State);
        Assert.Contains("26100", result.Reason);
    }

    [Fact]
    public void Matching_version_edition_and_build_are_applicable()
    {
        var item = new ManagedObject
        {
            MinimumBuild = 22000,
            SupportedWindowsVersions = ["Windows 11"],
            SupportedEditions = ["Pro", "Enterprise"]
        };
        var result = ApplicabilityEvaluator.Evaluate(item, "Windows 11 24H2", "Windows 11 Pro", 26100);
        Assert.Equal(ApplicabilityState.Applicable, result.State);
    }

    [Fact]
    public void Edition_sensitive_value_options_are_disabled_independently()
    {
        var securityLevel = new ValueMeaning
        {
            RawValue = "0",
            SupportedEditions = ["Enterprise", "Education"],
            SupportedVersions = ["Windows 10", "Windows 11"]
        };
        Assert.False(ApplicabilityEvaluator.IsValueApplicable(securityLevel, "Windows 11 24H2", "Pro"));
        Assert.True(ApplicabilityEvaluator.IsValueApplicable(securityLevel, "Windows 11 24H2", "Enterprise"));
    }
}
