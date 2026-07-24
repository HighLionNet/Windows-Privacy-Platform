// Source/WindowsPrivacyPlatform.Scanner/CapabilityCollector.cs
using System;
using System.Diagnostics;
using System.Text;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for Windows capabilities.
    /// Uses DISM /online /get-capabilities (query only, no elevation required for listing).
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
                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = "/online /get-capabilities",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using var process = Process.Start(psi);
                if (process is null)
                    return;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(15000);

                // Parse lines of the form:  Capability Identity : Name~~~~0.0.1.0
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("Capability Identity", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            var identity = parts[1].Trim();
                            if (!string.IsNullOrWhiteSpace(identity))
                                snapshot.InstalledCapabilities.Add(identity);
                        }
                    }
                }
            }
            catch
            {
                // DISM may be unavailable or blocked; leave list empty.
            }
        }
    }
}
