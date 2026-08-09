// Source/WindowsPrivacyPlatform.Scanner/CapabilityCollector.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>
/// Read-only collector for Windows capabilities.
/// Primary: PowerShell Get-WindowsCapability -Online (minimal invocation).
/// Fallback: DISM /online /get-capabilities /English.
/// Query only. Never elevates. Never writes. Fail-soft.
/// Empty results mean Unknown availability, not proven absence.
/// </summary>
public sealed class CapabilityCollector : IInventoryCollector
{
    public string Name => "CapabilityCollector";

    public void Collect(InventorySnapshot snapshot)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        try
        {
            // Prefer a single reasonable PowerShell attempt first (installed only is most useful).
            if (TryCollectViaPowerShell(snapshot, installedOnly: true))
                return;

            // One more attempt for full list if installed-only returned nothing useful.
            if (TryCollectViaPowerShell(snapshot, installedOnly: false))
                return;

            TryCollectViaDism(snapshot);
        }
        catch
        {
            // Capability enumeration may be unavailable; leave list empty (Unknown, not absence).
        }
    }

    private static bool TryCollectViaPowerShell(InventorySnapshot snapshot, bool installedOnly)
    {
        // Fixed command strings only. No user-controlled injection.
        // Avoid -ExecutionPolicy Bypass; use the minimum required for a read-only query.
        var filter = installedOnly
            ? "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Where-Object { $_.State -eq 'Installed' } | Select-Object -ExpandProperty Name"
            : "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name";

        var args = "-NoProfile -NonInteractive -Command \"" + filter + "\"";

        // Try powershell.exe then pwsh.exe once each for this mode.
        foreach (var shell in new[] { "powershell.exe", "pwsh.exe" })
        {
            var result = SafeProcessRunner.Run(
                shell,
                args,
                TimeSpan.FromSeconds(25),
                CancellationToken.None,
                Encoding.UTF8);

            if (!result.Started || result.TimedOut || result.Canceled)
                continue;

            if (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.StdOut))
                continue;

            var added = ParseCapabilityNames(snapshot, result.StdOut);
            if (added > 0)
                return true;
        }

        return false;
    }

    private static void TryCollectViaDism(InventorySnapshot snapshot)
    {
        var dismPath = Path.Combine(Environment.SystemDirectory, "dism.exe");
        if (!File.Exists(dismPath))
            dismPath = "dism.exe";

        var result = SafeProcessRunner.Run(
            dismPath,
            "/online /get-capabilities /English",
            TimeSpan.FromSeconds(30),
            CancellationToken.None,
            Encoding.Default);

        if (!result.Started || result.TimedOut || result.Canceled)
            return;

        ParseDismOutput(snapshot, result.StdOut);
    }

    private static int ParseCapabilityNames(InventorySnapshot snapshot, string output)
    {
        var added = 0;
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var name = line.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
                continue;

            if (IsPowerShellNoise(name))
                continue;

            if (!snapshot.InstalledCapabilities.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                snapshot.InstalledCapabilities.Add(name);
                added++;
            }
        }
        return added;
    }

    private static bool IsPowerShellNoise(string name) =>
        name.StartsWith("Get-WindowsCapability", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("At line", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("+", StringComparison.Ordinal) ||
        name.StartsWith("CategoryInfo", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("FullyQualifiedErrorId", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("Where-Object", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("Select-Object", StringComparison.OrdinalIgnoreCase);

    private static void ParseDismOutput(InventorySnapshot snapshot, string output)
    {
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("Capability Identity", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split(':', 2);
                if (parts.Length == 2)
                {
                    var identity = parts[1].Trim();
                    if (!string.IsNullOrWhiteSpace(identity) &&
                        !snapshot.InstalledCapabilities.Contains(identity, StringComparer.OrdinalIgnoreCase))
                        snapshot.InstalledCapabilities.Add(identity);
                }
                continue;
            }

            if (trimmed.Contains("~~~~", StringComparison.Ordinal) &&
                !trimmed.StartsWith("Capability", StringComparison.OrdinalIgnoreCase))
            {
                var token = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(token) &&
                    token.Contains("~~~~", StringComparison.Ordinal) &&
                    !snapshot.InstalledCapabilities.Contains(token, StringComparer.OrdinalIgnoreCase))
                    snapshot.InstalledCapabilities.Add(token);
            }
        }
    }
}
