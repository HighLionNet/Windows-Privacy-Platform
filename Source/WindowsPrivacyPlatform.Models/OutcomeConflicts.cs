namespace WindowsPrivacyPlatform.Models;

public enum ConflictImpact
{
    Low,
    Medium,
    High
}

public sealed record ConflictGroup(
    string GroupId,
    string Family,
    ConflictImpact Impact,
    IReadOnlyList<string> ObjectIds,
    string OutcomeLine);

/// <summary>Pure, named-family outcome comparison. Raw-value inequality alone is never a conflict.</summary>
public static class OutcomeConflictEngine
{
    public static IReadOnlyList<(string UserId, string PolicyId, string Family)> ConsentFamilies { get; } =
    [
        ("privacy.consentstore.location", "policy.appprivacy.location", "Location access"),
        ("privacy.consentstore.webcam", "policy.appprivacy.camera", "Camera access"),
        ("privacy.consentstore.microphone", "policy.appprivacy.microphone", "Microphone access"),
        ("privacy.consentstore.userAccountInformation", "policy.appprivacy.accountinfo", "Account information access"),
        ("privacy.consentstore.contacts", "policy.appprivacy.contacts", "Contacts access"),
        ("privacy.consentstore.appointments", "policy.appprivacy.calendar", "Calendar access"),
        ("privacy.consentstore.email", "policy.appprivacy.email", "Email access"),
        ("privacy.consentstore.phoneCallHistory", "policy.appprivacy.callhistory", "Call history access"),
        ("privacy.consentstore.radios", "policy.appprivacy.radios", "Radio access"),
        ("privacy.consentstore.documentsLibrary", "policy.appprivacy.documents", "Documents access"),
        ("privacy.consentstore.picturesLibrary", "policy.appprivacy.pictures", "Pictures access"),
        ("privacy.consentstore.videosLibrary", "policy.appprivacy.videos", "Videos access"),
        ("privacy.consentstore.broadFileSystemAccess", "policy.appprivacy.filesystem", "File system access"),
        ("privacy.consentstore.appDiagnostics", "policy.appprivacy.appdiagnostics", "App diagnostics access")
    ];

    public static IReadOnlyList<ConflictGroup> Evaluate(IEnumerable<ManagedObject> items)
    {
        var byId = items.Where(item => item is not null && !string.IsNullOrWhiteSpace(item.ObjectId))
            .GroupBy(item => item.ObjectId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var groups = new List<ConflictGroup>();

        if (byId.TryGetValue("privacy.advertisingid.enabled", out var advertisingUser) &&
            byId.TryGetValue("policy.advertising.disabledbygpo", out var advertisingGpo))
        {
            var user = Raw(advertisingUser);
            var gpo = Raw(advertisingGpo);
            if (user == "1" && gpo == "1")
            {
                groups.Add(new ConflictGroup(
                    "advertising-id",
                    "Advertising ID availability",
                    ConflictImpact.Medium,
                    [advertisingUser.ObjectId, advertisingGpo.ObjectId],
                    "GPO is forcing Advertising ID off; the user toggle cannot turn it on."));
            }
        }

        foreach (var (userId, policyId, family) in ConsentFamilies)
        {
            if (!byId.TryGetValue(userId, out var userItem) || !byId.TryGetValue(policyId, out var policyItem))
                continue;
            var user = Raw(userItem);
            var policy = Raw(policyItem);
            var forceAllowAgainstDeny = policy == "1" && user.Equals("Deny", StringComparison.OrdinalIgnoreCase);
            var forceDenyAgainstAllow = policy == "2" && user.Equals("Allow", StringComparison.OrdinalIgnoreCase);
            if (!forceAllowAgainstDeny && !forceDenyAgainstAllow)
                continue;

            groups.Add(new ConflictGroup(
                "app-access-" + policyId["policy.appprivacy.".Length..],
                family,
                ConflictImpact.High,
                [userItem.ObjectId, policyItem.ObjectId],
                forceDenyAgainstAllow
                    ? $"Machine policy denies {family.ToLowerInvariant()}; the user permission cannot allow it."
                    : $"Machine policy allows {family.ToLowerInvariant()}; the user denial cannot block it."));
        }

        return groups;
    }

    public static bool AdvertisingConflicts(string? userRaw, string? gpoRaw) =>
        Normalize(userRaw) == "1" && Normalize(gpoRaw) == "1";

    public static void ApplyToCatalog(IReadOnlyList<ManagedObject> items)
    {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "privacy.advertisingid.enabled", "policy.advertising.disabledbygpo"
        };
        foreach (var pair in ConsentFamilies)
        {
            known.Add(pair.UserId);
            known.Add(pair.PolicyId);
        }

        foreach (var item in items.Where(item => known.Contains(item.ObjectId)))
        {
            if (item.Observation?.Resolution is not null) item.Observation.Resolution.HasConflict = false;
            if (item.Observation?.Effective is not null) item.Observation.Effective.HasConflict = false;
        }

        var byId = items.ToDictionary(item => item.ObjectId, StringComparer.OrdinalIgnoreCase);
        foreach (var group in Evaluate(items))
        foreach (var id in group.ObjectIds)
        {
            if (!byId.TryGetValue(id, out var item)) continue;
            item.Observation ??= new SettingObservation();
            item.Observation.Resolution ??= new ConfigurationResolution();
            item.Observation.Resolution.HasConflict = true;
            item.Observation.Resolution.ResolutionReason = group.OutcomeLine;
            item.Observation.Effective ??= item.Observation.Resolution.ToEffectiveState();
            item.Observation.Effective.HasConflict = true;
            item.Observation.Effective.Explanation = group.OutcomeLine;
        }
    }

    private static string Raw(ManagedObject item) => Normalize(item.CurrentState);

    private static string Normalize(string? value)
    {
        var token = ValueSemanticsInterpreter.Normalize(value);
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        return token.Split(' ', '(', ')', ';')[0];
    }
}
