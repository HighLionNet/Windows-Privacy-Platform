using System.Text;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>
/// Read-only export of the local security-policy database through secedit.exe. The export file is
/// created under the current user's temp directory, parsed, and deleted. No policy is configured.
/// </summary>
public sealed class LocalSecurityPolicyCollector : IInventoryCollector
{
    public string Name => nameof(LocalSecurityPolicyCollector);

    private static readonly IReadOnlyDictionary<string, (string Id, string Section)> Fields =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["PasswordComplexity"] = ("policy.security.passwordcomplexity", "System Access"),
            ["MinimumPasswordLength"] = ("policy.security.minpasswordlength", "System Access"),
            ["PasswordHistorySize"] = ("policy.security.passwordhistory", "System Access"),
            ["MaximumPasswordAge"] = ("policy.security.maxpasswordage", "System Access"),
            ["LockoutBadCount"] = ("policy.security.lockoutthreshold", "System Access"),
            ["LockoutDuration"] = ("policy.security.lockoutduration", "System Access"),
            ["AuditLogonEvents"] = ("policy.security.auditlogons", "Event Audit"),
            ["AuditPolicyChange"] = ("policy.security.auditpolicychange", "Event Audit"),
            ["SeDebugPrivilege"] = ("policy.security.debugprograms", "Privilege Rights"),
            ["SeImpersonatePrivilege"] = ("policy.security.impersonate", "Privilege Rights"),
            ["SeRemoteInteractiveLogonRight"] = ("policy.security.remotedesktoplogon", "Privilege Rights"),
            ["SeDenyNetworkLogonRight"] = ("policy.security.denynetworklogon", "Privilege Rights")
        };

    public void Collect(InventorySnapshot snapshot)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        var exportPath = Path.Combine(Path.GetTempPath(), $"wpp-security-{Guid.NewGuid():N}.inf");
        try
        {
            var secedit = Path.Combine(Environment.SystemDirectory, "secedit.exe");
            if (!File.Exists(secedit))
                secedit = "secedit.exe";

            var result = SafeProcessRunner.Run(
                secedit,
                $"/export /cfg \"{exportPath}\" /areas SECURITYPOLICY USER_RIGHTS /quiet",
                TimeSpan.FromSeconds(30),
                outputEncoding: Encoding.Unicode);

            if (!result.Started || result.TimedOut || result.Canceled || result.ExitCode != 0 || !File.Exists(exportPath))
                return;

            ParseExport(snapshot, File.ReadAllLines(exportPath, Encoding.Unicode));
        }
        catch
        {
            // Local policy export can be restricted. Absence of results remains Not observed.
        }
        finally
        {
            try { if (File.Exists(exportPath)) File.Delete(exportPath); } catch { /* best effort */ }
        }
    }

    private static void ParseExport(InventorySnapshot snapshot, IEnumerable<string> lines)
    {
        var section = string.Empty;
        foreach (var source in lines)
        {
            var line = source.Trim();
            if (line.Length == 0 || line.StartsWith(';'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1];
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;
            var field = line[..separator].Trim();
            if (!Fields.TryGetValue(field, out var definition) ||
                !section.Equals(definition.Section, StringComparison.OrdinalIgnoreCase))
                continue;

            snapshot.PolicySettings.Add(new PolicySettingInfo
            {
                Name = definition.Id,
                Category = "LocalSecurityPolicy",
                Hive = "SECEDIT",
                Path = section,
                ValueName = field,
                Value = line[(separator + 1)..].Trim()
            });
        }
    }
}
