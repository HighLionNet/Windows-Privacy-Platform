namespace WindowsPrivacyPlatform.Models;

public enum PostureFindingSeverity { High, Review }

public sealed record PostureFinding(string ObjectId, string Title, string Summary, PostureFindingSeverity Severity);

public sealed class PostureSnapshot
{
    public int HighCount { get; init; }
    public int ReviewCount { get; init; }
    public int ProtectedCount { get; init; }
    public int EvaluatedCount { get; init; }
    public IReadOnlyList<PostureFinding> Findings { get; init; } = [];
}

/// <summary>A small evidence summary, deliberately not a synthetic security score.</summary>
public static class PostureAssessment
{
    public static PostureSnapshot Build(IEnumerable<ManagedObject> settings)
    {
        var findings = new List<PostureFinding>();
        var protectedCount = 0;
        var evaluated = 0;

        foreach (var item in settings)
        {
            var raw = Raw(item.CurrentState);
            if (raw is null) continue;
            evaluated++;
            var id = item.ObjectId.ToLowerInvariant();

            if (id.Contains("firewall.profile") && id.EndsWith(".enabled"))
            {
                if (raw == "0") findings.Add(High(item, "Firewall profile disabled")); else if (raw == "1") protectedCount++;
                continue;
            }
            if (id.Contains("firewall.profile") && id.EndsWith(".inbound"))
            {
                if (raw == "1") findings.Add(High(item, "Unmatched inbound traffic allowed")); else if (raw == "0") protectedCount++;
                continue;
            }
            if (item.ProductDomain == ProductDomain.Defender)
            {
                var positiveProtectionId = id is "policy.smartscreen.enable" or
                    "policy.defender.enablenetworkprotection" or
                    "policy.defender.enablecontrolledfolderaccess" or
                    "policy.defender.puaprotection";
                var protectionDisabled =
                    (id.Contains("disable") && raw == "1") ||
                    (positiveProtectionId && raw == "0");
                if (protectionDisabled) findings.Add(High(item, "Protection is reduced"));
                else if ((id.Contains("disable") && raw == "0") ||
                         (positiveProtectionId && raw != "0"))
                    protectedCount++;
                continue;
            }
            if (item.ProductDomain == ProductDomain.ConsentStore && raw.Equals("Allow", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(Review(item, "App permission allowed"));
                continue;
            }
            if (item.ProductDomain == ProductDomain.AppPrivacy && raw == "1")
            {
                findings.Add(Review(item, "App access is force-allowed"));
                continue;
            }
            if (id == "policy.advertising.disabledbygpo")
            {
                if (raw == "1") protectedCount++; else findings.Add(Review(item, "Advertising ID remains available"));
                continue;
            }
            if ((id.Contains("telemetry") && raw is "2" or "3") ||
                (id == "privacy.advertisingid.enabled" && raw == "1") ||
                (id == "privacy.tailoredexperiences" && raw == "1") ||
                (id.Contains("uploaduseractivities") && raw == "1") ||
                (id.Contains("allowcrossdevice") && raw == "1") ||
                (id.Contains("metricsreporting") && raw == "1"))
            {
                findings.Add(Review(item, "Broader data sharing is enabled"));
            }
        }

        var ordered = findings.OrderBy(f => f.Severity).ThenBy(f => f.Title).ToList();
        return new PostureSnapshot
        {
            HighCount = ordered.Count(f => f.Severity == PostureFindingSeverity.High),
            ReviewCount = ordered.Count(f => f.Severity == PostureFindingSeverity.Review),
            ProtectedCount = protectedCount,
            EvaluatedCount = evaluated,
            Findings = ordered
        };
    }

    private static PostureFinding High(ManagedObject item, string summary) => new(item.ObjectId, item.ObjectName, summary, PostureFindingSeverity.High);
    private static PostureFinding Review(ManagedObject item, string summary) => new(item.ObjectId, item.ObjectName, summary, PostureFindingSeverity.Review);

    private static string? Raw(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        if (text.Equals("Not configured", StringComparison.OrdinalIgnoreCase) || text.Equals("Not observed", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("Unknown", StringComparison.OrdinalIgnoreCase) || text.StartsWith("Error", StringComparison.OrdinalIgnoreCase)) return null;
        return text.Split(' ', '(', ')', ';')[0].Trim();
    }
}
