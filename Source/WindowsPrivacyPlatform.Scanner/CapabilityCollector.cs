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
    /// Query only. Never elevates. Never writes.
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
                if (TryCollectViaPowerShell(snapshot))
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
        private static bool TryCollectViaPowerShell(InventorySnapshot snapshot)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments =
                        "-NoProfile -NonInteractive -Command " +
                        "\"Get-WindowsCapability -Online | Select-Object -ExpandProperty Name\"",
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
                process.WaitForExit(30000);

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
                        name.StartsWith("At line", StringComparison.OrdinalIgnoreCase))
                        continue;

                    snapshot.InstalledCapabilities.Add(name);
                    added++;
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
                    // DISM frequently emits OEM code page; UTF-8 can drop characters.
                    // Default encoding is safer for label matching when /English is used.
                    StandardOutputEncoding = Encoding.Default
                };

                using var process = Process.Start(psi);
                if (process is null)
                    return;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(30000);

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
                    if (!string.IsNullOrWhiteSpace(identity))
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
