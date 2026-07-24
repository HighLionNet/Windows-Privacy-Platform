// Source/WindowsPrivacyPlatform.Scanner/PackageCollector.cs
using System;
using System.Diagnostics;
using System.Text;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for installed AppX / MSIX packages.
    /// Uses PowerShell Get-AppxPackage (query only, current user + system where permitted).
    /// </summary>
    public sealed class PackageCollector : IInventoryCollector
    {
        public string Name => "PackageCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            try
            {
                // -AllUsers requires elevation; omit it to stay non-elevated.
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -Command \"Get-AppxPackage | Select-Object -ExpandProperty Name\"",
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
                process.WaitForExit(20000);

                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = line.Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                        snapshot.InstalledPackages.Add(name);
                }
            }
            catch
            {
                // PowerShell or AppX enumeration may fail; leave list empty.
            }
        }
    }
}
