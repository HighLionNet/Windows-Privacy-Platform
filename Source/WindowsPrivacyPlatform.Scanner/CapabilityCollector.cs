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
    /// Primary: PowerShell Get-WindowsCapability -Online (structured, locale-independent).
    /// Fallback: DISM /online /get-capabilities /English via System32 path.
    /// Query only. Never elevates. Never writes. Fail-soft.
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
                // Prefer installed capabilities first (most useful, often works without elevation).
                if (TryCollectViaPowerShell(snapshot, installedOnly: true))
                    return;

                // Broader query if installed-only returned nothing.
                if (TryCollectViaPowerShell(snapshot, installedOnly: false))
                    return;

                TryCollectViaDism(snapshot);
            }
            catch
            {
                // Capability enumeration may be unavailable; leave list empty.
            }
        }

        /// <summary>
        /// Preferred path. Get-WindowsCapability returns structured Name values
        /// without depending on localized DISM labels.
        /// </summary>
        private static bool TryCollectViaPowerShell(InventorySnapshot snapshot, bool installedOnly)
        {
            try
            {
                // Fixed argument strings only — no user-controlled injection.
                // -ErrorAction SilentlyContinue avoids terminating errors on restricted hosts.
                var filter = installedOnly
                    ? "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Where-Object { $_.State -eq 'Installed' } | Select-Object -ExpandProperty Name"
                    : "Get-WindowsCapability -Online -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
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
                // Drain stderr so the process can exit cleanly; do not surface as data.
                _ = process.StandardError.ReadToEnd();
                process.WaitForExit(45000);

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
                    return false;

                var added = 0;
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = line.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    // Skip PowerShell error/noise lines.
                    if (name.StartsWith("Get-WindowsCapability", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("At line", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("+", StringComparison.Ordinal) ||
                        name.StartsWith("CategoryInfo", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("FullyQualifiedErrorId", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith("Where-Object", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Capability names are typically Identity~~~~version
                    if (name.Length < 3)
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

        /// <summary>
        /// Fallback when PowerShell path yields nothing.
        /// Forces English output and uses the System32 DISM binary.
        /// </summary>
        private static void TryCollectViaDism(InventorySnapshot snapshot)
        {
            try
            {
                var dismPath = Path.Combine(Environment.SystemDirectory, "dism.exe");
                if (!File.Exists(dismPath))
                    dismPath = "dism.exe";

                var psi = new ProcessStartInfo
                {
                    FileName = dismPath,
                    Arguments = "/online /get-capabilities /English",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // DISM frequently emits OEM code page; default encoding is safer with /English.
                    StandardOutputEncoding = Encoding.Default
                };

                using var process = Process.Start(psi);
                if (process is null)
                    return;

                var output = process.StandardOutput.ReadToEnd();
                _ = process.StandardError.ReadToEnd();
                process.WaitForExit(45000);

                // Parse lines of the form:  Capability Identity : Name~~~~0.0.1.0
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("Capability Identity", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = trimmed.Split(':', 2);
                    if (parts.Length != 2)
                        continue;

                    var identity = parts[1].Trim();
                    if (string.IsNullOrWhiteSpace(identity))
                        continue;

                    if (!snapshot.InstalledCapabilities.Contains(identity, StringComparer.OrdinalIgnoreCase))
                        snapshot.InstalledCapabilities.Add(identity);
                }
            }
            catch
            {
                // Leave list empty on DISM failure.
            }
        }
    }
}
