// Source/WindowsPrivacyPlatform.Scanner/CapabilityCollector.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for Windows capabilities.
    /// Primary: PowerShell Get-WindowsCapability -Online.
    /// Fallback: pwsh, then DISM /online /get-capabilities /English.
    /// Query only. Never elevates. Never writes. Fail-soft.
    /// On many non-elevated Win11 hosts the APIs return empty; callers should
    /// treat zero results as Unknown availability, not proven absence.
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
                if (TryCollectViaShell("powershell.exe", snapshot, installedOnly: true))
                    return;
                if (TryCollectViaShell("powershell.exe", snapshot, installedOnly: false))
                    return;
                if (TryCollectViaShell("pwsh.exe", snapshot, installedOnly: true))
                    return;
                if (TryCollectViaShell("pwsh.exe", snapshot, installedOnly: false))
                    return;

                TryCollectViaDism(snapshot);
            }
            catch
            {
                // Capability enumeration may be unavailable; leave list empty.
            }
        }

        private static bool TryCollectViaShell(string shell, InventorySnapshot snapshot, bool installedOnly)
        {
            try
            {
                // Fixed argument strings only — no user-controlled injection.
                var filter = installedOnly
                    ? "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Where-Object { $_.State -eq 'Installed' } | Select-Object -ExpandProperty Name"
                    : "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name";

                var psi = new ProcessStartInfo
                {
                    FileName = shell,
                    Arguments =
                        "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command " +
                        $"\"{filter}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using var process = Process.Start(psi);
                if (process is null)
                    return false;

                var output = process.StandardOutput.ReadToEnd();
                _ = process.StandardError.ReadToEnd();
                process.WaitForExit(45000);

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
                    return false;

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

                return added > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPowerShellNoise(string name) =>
            name.StartsWith("Get-WindowsCapability", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("At line", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("+", StringComparison.Ordinal) ||
            name.StartsWith("CategoryInfo", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("FullyQualifiedErrorId", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Where-Object", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Select-Object", StringComparison.OrdinalIgnoreCase);

        private static void TryCollectViaDism(InventorySnapshot snapshot)
        {
            try
            {
                var dismPath = Path.Combine(Environment.SystemDirectory, "dism.exe");
                if (!File.Exists(dismPath))
                    dismPath = "dism.exe";

                // /English keeps labels stable; no elevation requested.
                foreach (var args in new[]
                         {
                             "/online /get-capabilities /English",
                             "/online /get-capabilities /format:table /English"
                         })
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = dismPath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.Default
                    };

                    using var process = Process.Start(psi);
                    if (process is null)
                        continue;

                    var output = process.StandardOutput.ReadToEnd();
                    _ = process.StandardError.ReadToEnd();
                    process.WaitForExit(45000);

                    var before = snapshot.InstalledCapabilities.Count;
                    ParseDismOutput(snapshot, output);
                    if (snapshot.InstalledCapabilities.Count > before)
                        return;
                }
            }
            catch
            {
                // Leave list empty on DISM failure.
            }
        }

        private static void ParseDismOutput(InventorySnapshot snapshot, string output)
        {
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();

                // Classic: Capability Identity : Name~~~~0.0.1.0
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

                // Table format: capability name often contains ~~~~
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
}
