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
