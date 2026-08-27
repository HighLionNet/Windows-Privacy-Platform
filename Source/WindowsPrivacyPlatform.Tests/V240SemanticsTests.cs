using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Validator;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class V240SemanticsTests
{
    [Fact]
    public void Search_destination_for_setting_is_category_list_with_highlight()
    {
        var setting = ManagedObjectCatalog.All.First(item => item.Bucket == CatalogBucket.Settings);
        var target = SettingsListTarget.For(setting, "camera");

        Assert.Equal(setting.ProductDomain, target.Domain);
        Assert.Equal(setting.ObjectId, target.HighlightObjectId);
        Assert.Equal("camera", target.Filter);
        Assert.False(string.IsNullOrWhiteSpace(target.Category));
    }

    [Fact]
    public void Inventory_cannot_become_settings_list_destination()
    {
        var inventory = new ManagedObject { Bucket = CatalogBucket.SystemInventory };
        Assert.Throws<InvalidOperationException>(() => SettingsListTarget.For(inventory));
    }

    [Theory]
    [InlineData("Not configured", EvidenceState.NotConfigured)]
    [InlineData("Not observed", EvidenceState.NotObserved)]
    [InlineData("Access denied", EvidenceState.AccessDenied)]
    [InlineData("Unknown", EvidenceState.Unknown)]
    [InlineData("Error reading source", EvidenceState.Error)]
    public void Evidence_states_remain_distinct(string observed, EvidenceState expected)
    {
        var item = new ManagedObject { CurrentState = observed, Applicability = ApplicabilityState.Applicable };
        Assert.Equal(expected, EvidenceStateSemantics.Classify(item));
    }

    [Fact]
    public void Service_filters_are_literal_and_composable()
    {
        var services = new[]
        {
            new ServiceInfo { Name = "alpha", DisplayName = "Alpha", State = "Running", StartMode = "Automatic", IsMicrosoft = true },
            new ServiceInfo { Name = "beta", DisplayName = "Beta", State = "Stopped", StartMode = "Automatic", IsMicrosoft = false }
        };

        var result = ServiceInspection.Apply(services,
            new ServiceFilter("beta", "Stopped", "Automatic", "Non-Microsoft", "Automatic service is stopped"));

        Assert.Single(result);
        Assert.Equal("beta", result[0].Name);
    }

    [Fact]
    public void Malformed_registry_targets_are_denied()
    {
        var target = new WritableTarget
        {
            Hive = "HKLM", SubKey = @"SOFTWARE\..\SAM", ValueName = "Value", ValueKind = RegistryValueKindExpected.DWord,
            SupportedRawValues = ["0", "1"]
        };

        Assert.False(target.IsComplete);
    }

    [Fact]
    public void Runtime_target_tampering_is_denied_by_catalog_revalidation()
    {
        var source = ManagedObjectCatalog.All.First(item => item.WritableTarget is not null);
        var tampered = new ManagedObject { ObjectId = source.ObjectId, WritableTarget = new WritableTarget
        {
            Hive = source.WritableTarget!.Hive, View = source.WritableTarget.View,
            SubKey = source.WritableTarget.SubKey, ValueName = source.WritableTarget.ValueName + "Changed",
            ValueKind = source.WritableTarget.ValueKind, SupportedRawValues = source.WritableTarget.SupportedRawValues.ToList(),
            SupportsDeletion = source.WritableTarget.SupportsDeletion, RequiresElevation = source.WritableTarget.RequiresElevation
        }};

        Assert.False(ManagedObjectCatalog.IsAuthorizedWriteTarget(tampered));
    }

    [Fact]
    public void Editable_content_rule_rejects_title_as_explanation()
    {
        var item = new ManagedObject
        {
            Bucket = CatalogBucket.Settings, ObjectName = "Repeated", Description = "Repeated", MinimumBuild = 10240,
            Narrative = new SettingNarrative { Summary = "Repeated" },
            ValueSemantics = [new ValueMeaning { RawValue = "1", DisplayLabel = "Turn on", Description = "Enables the policy." }],
            WritableTarget = new WritableTarget { Hive = "HKLM", SubKey = @"SOFTWARE\Policies\Example", ValueName = "Value",
                ValueKind = RegistryValueKindExpected.DWord, SupportedRawValues = ["1"] }
        };

        Assert.False(new SettingContentRule().Evaluate(item, out var error));
        Assert.Contains("title", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Atomic_local_state_refuses_parent_escape()
    {
        var root = Path.Combine(Path.GetTempPath(), "wpp-atomic-test", Guid.NewGuid().ToString("N"));
        var escaped = Path.Combine(root, "..", "outside.txt");
        Assert.Throws<UnauthorizedAccessException>(() => AtomicLocalFile.WriteText(root, escaped, "data"));
    }

    [Fact]
    public void Partial_batch_is_not_reported_as_all_verified()
    {
        var summary = PolicyBatchSummary.From(new[]
        {
            new PolicyChangeOutcome("one", true, "Verified"),
            new PolicyChangeOutcome("two", false, "Read-back mismatch")
        });
        Assert.Equal(1, summary.Verified);
        Assert.Equal(1, summary.NotVerified);
        Assert.False(summary.AllVerified);
    }

    [Fact]
    public void Audit_sanitizer_redacts_common_secret_assignments()
    {
        var sanitized = AuditLogger.SanitizeField("operation token=abc123 password:letmein authorization=BearerValue");
        Assert.DoesNotContain("abc123", sanitized);
        Assert.DoesNotContain("letmein", sanitized);
        Assert.DoesNotContain("BearerValue", sanitized);
        Assert.Contains("[redacted]", sanitized);
    }
}
