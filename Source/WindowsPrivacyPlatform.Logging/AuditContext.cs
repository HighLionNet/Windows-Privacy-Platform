using System.Text.RegularExpressions;

namespace WindowsPrivacyPlatform.Logging;

public static class AuditContext
{
    public static string SessionId { get; } = Guid.NewGuid().ToString("N");
    public static string User { get; } = Environment.UserDomainName + "\\" + Environment.UserName;
    public static string Mode { get; set; } = "View-only";

    public static string Fields => $"session={SessionId} user={User} mode={Mode} object=- name=\"-\" old=\"-\" new=\"-\" result=Recorded";

    public static string Change(string objectId, string humanName, string oldValue, string newValue,
        string result, string? detail = null) =>
        $"object={objectId} name=\"{humanName}\" old=\"{oldValue}\" new=\"{newValue}\" result={result}" +
        (string.IsNullOrWhiteSpace(detail) ? string.Empty : " detail=\"" + detail + "\"");
}

public sealed record AuditSessionSummary(string SessionId, string User, string Mode,
    DateTime StartUtc, DateTime EndUtc, int ChangesApplied);

public static class AuditSessionHistory
{
    private static readonly Regex Field = new(@"\b(?<key>session|user|mode)=(?<value>[^\s]+)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

    public static IReadOnlyList<AuditSessionSummary> ReadRecent(string logRoot, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(logRoot) || limit is < 1 or > 100) return [];
        var paths = new[] { "auth.log", "auth.log.previous", "changes.log", "changes.log.previous" }
            .Select(name => Path.Combine(logRoot, name)).Where(File.Exists).ToList();
        var rows = new List<Row>();
        foreach (var path in paths)
        {
            try
            {
                foreach (var line in File.ReadLines(path).TakeLast(20_000))
                {
                    if (!TryRead(line, out var row)) continue;
                    rows.Add(row);
                }
            }
            catch { }
        }

        return rows.GroupBy(row => row.SessionId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AuditSessionSummary(
                group.Key,
                group.Last().User,
                group.Last().Mode,
                group.Min(row => row.Timestamp),
                group.Max(row => row.Timestamp),
                group.Count(row => row.Applied)))
            .OrderByDescending(summary => summary.StartUtc)
            .Take(limit)
            .ToList();
    }

    private static bool TryRead(string line, out Row row)
    {
        row = default;
        var close = line.IndexOf(']');
        if (close <= 1 || !DateTime.TryParse(line[1..close], null,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var timestamp)) return false;
        var values = Field.Matches(line).ToDictionary(match => match.Groups["key"].Value,
            match => match.Groups["value"].Value, StringComparer.OrdinalIgnoreCase);
        if (!values.TryGetValue("session", out var session)) return false;
        row = new Row(timestamp, session, values.GetValueOrDefault("user", "Unknown"),
            values.GetValueOrDefault("mode", "Unknown"), line.Contains("result=Verified", StringComparison.OrdinalIgnoreCase));
        return true;
    }

    private readonly record struct Row(DateTime Timestamp, string SessionId, string User, string Mode, bool Applied);
}
