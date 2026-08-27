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

    public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        try
        {
            // Prefer a single reasonable PowerShell attempt first (installed only is most useful).
            var collected = TryCollectViaPowerShell(snapshot, installedOnly: true, cancellationToken);
            if (!collected)
                collected = TryCollectViaPowerShell(snapshot, installedOnly: false, cancellationToken);
            if (!collected)
                TryCollectViaDism(snapshot, cancellationToken);

            TryCollectOptionalFeatures(snapshot, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Capability enumeration may be unavailable; leave list empty (Unknown, not absence).
        }
    }

    private static void TryCollectOptionalFeatures(InventorySnapshot snapshot, CancellationToken cancellationToken)
    {
        var dismPath = Path.Combine(Environment.SystemDirectory, "dism.exe");
        if (!File.Exists(dismPath))
            return;

        var result = SafeProcessRunner.Run(
            dismPath,
            ["/online", "/get-features", "/format:table", "/English"],
            TimeSpan.FromSeconds(35),
            cancellationToken,
            Encoding.Default);

        if (!result.Started || result.TimedOut || result.Canceled || string.IsNullOrWhiteSpace(result.StdOut))
            return;

        foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var separator = line.LastIndexOf('|');
            if (separator <= 0 || separator >= line.Length - 1)
                continue;

            var name = line[..separator].Trim();
            var state = line[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith("Feature Name", StringComparison.OrdinalIgnoreCase) ||
                name.All(c => c is '-' or ' '))
                continue;

            snapshot.OptionalFeatures.Add(new OptionalFeatureInfo { Name = name, State = state });
        }
    }

    private static bool TryCollectViaPowerShell(InventorySnapshot snapshot, bool installedOnly, CancellationToken cancellationToken)
    {
        // Fixed command strings only. No user-controlled injection.
        // Avoid -ExecutionPolicy Bypass; use the minimum required for a read-only query.
        var filter = installedOnly
            ? "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Where-Object { $_.State -eq 'Installed' } | Select-Object -ExpandProperty Name"
            : "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name";

        var shell = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        var result = SafeProcessRunner.Run(
            shell,
            ["-NoProfile", "-NonInteractive", "-Command", filter],
            TimeSpan.FromSeconds(25),
            cancellationToken,
            Encoding.UTF8);

        if (!result.Started || result.TimedOut || result.Canceled ||
            (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.StdOut)))
            return false;

        return ParseCapabilityNames(snapshot, result.StdOut) > 0;
    }

    private static void TryCollectViaDism(InventorySnapshot snapshot, CancellationToken cancellationToken)
    {
        var dismPath = Path.Combine(Environment.SystemDirectory, "dism.exe");
        if (!File.Exists(dismPath))
            return;

        var result = SafeProcessRunner.Run(
            dismPath,
            ["/online", "/get-capabilities", "/English"],
            TimeSpan.FromSeconds(30),
            cancellationToken,
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
