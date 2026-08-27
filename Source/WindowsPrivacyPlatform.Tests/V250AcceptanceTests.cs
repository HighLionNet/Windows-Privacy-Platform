using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class V250AcceptanceTests
{
    [Fact]
    public void Public_mode_terms_are_view_only_and_admin()
    {
        Assert.Equal("View-only", SessionPresentation.ViewOnly);
        Assert.Equal("Admin", SessionPresentation.Admin);
        Assert.DoesNotContain("Inspect", new[] { SessionPresentation.ViewOnly, SessionPresentation.Admin });
        Assert.DoesNotContain("Modify", new[] { SessionPresentation.ViewOnly, SessionPresentation.Admin });
    }

    [Fact]
    public void Apply_keeps_the_current_process_open() =>
        Assert.True(SessionPresentation.KeepProcessOpenAfterApply);

    [Fact]
    public void Credential_failure_never_authorizes_admin()
    {
        var elevation = new ElevationService(new NullLogger(), new RejectingCredentialPrompt());
        Assert.Equal(AdminEntryResult.Denied, elevation.TryEnterAdminMode(null));
        Assert.False(elevation.IsAdminAuthorized);
    }

    [Theory]
    [InlineData("c-137", "", "c-137", ".", "local")]
    [InlineData("CORP\\c-137", "", "c-137", "CORP", "domain")]
    [InlineData("person@example.com", "ignored", "person@example.com", null, "upn")]
    public void Credential_account_forms_are_normalized_for_LogonUser(string inputUser, string inputDomain,
        string expectedUser, string? expectedDomain, string expectedForm)
    {
        var account = CredentialPromptService.NormalizeAccount(inputUser, inputDomain);
        Assert.Equal(expectedUser, account.UserName);
        Assert.Equal(expectedDomain, account.Domain);
        Assert.Equal(expectedForm, account.Form);
    }

    [Fact]
    public void Machine_qualified_credential_uses_the_local_account_database()
    {
        var account = CredentialPromptService.NormalizeAccount($"{Environment.MachineName}\\c-137", string.Empty);
        Assert.Equal("c-137", account.UserName);
        Assert.Equal(".", account.Domain);
        Assert.Equal("local", account.Form);
    }

    [Fact]
    public void Authorization_hash_detects_runtime_table_changes()
    {
        var originals = ManagedObjectCatalog.All.Where(item => item.WritableTarget is not null).ToList();
        var clone = originals.Select(Clone).ToList();
        clone[0].WritableTarget!.ValueName += "Tampered";
        Assert.False(AuthorizationTableIntegrity.Matches(ManagedObjectCatalog.AuthorizationHash, clone));
        Assert.True(ManagedObjectCatalog.HasValidAuthorizationHash());
    }

    [Fact]
    public void Unknown_command_line_arguments_are_rejected()
    {
        Assert.False(CommandLinePolicy.TryParse(["--download-catalog"], out _, out _));
        Assert.False(CommandLinePolicy.TryParse(["--authorize-modify"], out _, out _));
        Assert.True(CommandLinePolicy.TryParse(["--inspect", "--no-shortcut-offer"], out var parsed, out _));
        Assert.True(parsed.ViewOnlyRelaunch);
    }

    [Fact]
    public void Preference_serialization_has_no_secret_or_authorization_state()
    {
        var json = ApplicationPreferencesStore.SerializeForInspection(new ApplicationPreferences
        {
            DefaultMode = DefaultModePreference.Admin,
            AdminSessionMinutes = 30,
            Theme = AppTheme.NavyDark
        });
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorized", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Every_theme_has_a_complete_distinct_surface_palette()
    {
        foreach (var theme in Enum.GetValues<AppTheme>())
        {
            var palette = ThemeManager.PaletteForTesting(theme);
            Assert.True(palette.Count >= 39, theme.ToString());
            Assert.NotEqual(palette["BgWindow"], palette["BgContent"]);
            Assert.NotEqual(palette["BgContent"], palette["BgCard"]);
            Assert.NotEqual(palette["BgCard"], palette["BgHeader"]);
            Assert.DoesNotContain("#FFFFFF", new[] { palette["BgWindow"], palette["BgContent"], palette["BgCard"] });
        }
    }

    [Fact]
    public void Current_Copilot_app_policy_set_and_high_risk_AI_observations_are_covered()
    {
        var expected = new[]
        {
            "policy.copilot.app.browsing", "policy.copilot.app.componentupdates",
            "policy.copilot.app.coworkactions", "policy.copilot.settingsagent",
            "policy.copilot.paint.cocreator", "policy.copilot.paint.generativefill",
            "policy.copilot.paint.imagecreator", "policy.recall.allowenablement",
            "policy.recall.allowexport", "policy.recall.denyapplist", "policy.recall.denyurilist",
            "policy.recall.maxduration", "policy.recall.maxstorage"
        };
        Assert.All(expected, id => Assert.Contains(ManagedObjectCatalog.All, item => item.ObjectId == id));
        Assert.All(ManagedObjectCatalog.All.Where(item => item.ObjectId.StartsWith("policy.copilot.app.")),
            item => Assert.True(item.IsWritable, item.ObjectId));
        Assert.All(ManagedObjectCatalog.All.Where(item => CatalogPolicy.IsMonitoredReadOnlySetting(item.ObjectId)),
            item => Assert.False(item.IsWritable, item.ObjectId));
    }

    private static ManagedObject Clone(ManagedObject item) => new()
    {
        ObjectId = item.ObjectId,
        WritableTarget = item.WritableTarget is null ? null : new WritableTarget
        {
            Kind = item.WritableTarget.Kind, Hive = item.WritableTarget.Hive, View = item.WritableTarget.View,
            SubKey = item.WritableTarget.SubKey, ValueName = item.WritableTarget.ValueName,
            ValueKind = item.WritableTarget.ValueKind,
            SupportedRawValues = item.WritableTarget.SupportedRawValues.ToList(),
            SupportsDeletion = item.WritableTarget.SupportsDeletion,
            RequiresElevation = item.WritableTarget.RequiresElevation
        }
    };

    private sealed class RejectingCredentialPrompt : ICredentialPromptService
    {
        public CredentialAuthorizationResult AuthorizeAdmin(System.Windows.Window? owner, string reason) =>
            new(false, false, "Rejected for test.");
    }

    private sealed class NullLogger : IAuditLogger
    {
        public void Debug(string component, string message) { }
        public void Info(string component, string message) { }
        public void Warning(string component, string message) { }
        public void Error(string component, string message) { }
        public void Log(AuditEventType eventType, string component, string message) { }
        public void Auth(string component, string message) { }
        public void Change(string component, string message) { }
    }
}
