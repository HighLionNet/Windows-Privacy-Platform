// Source/WindowsPrivacyPlatform.Scanner/PackageCollector.cs
using System;
using System.Text;
using System.Threading;
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
                var result = SafeProcessRunner.Run(
                    "powershell.exe",
                    "-NoProfile -NonInteractive -Command \"Get-AppxPackage | Select-Object -ExpandProperty Name\"",
                    TimeSpan.FromSeconds(20),
                    CancellationToken.None,
                    Encoding.UTF8);

                if (!result.Started || result.TimedOut || result.Canceled ||
                    (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.StdOut)))
                    return;

                foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
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
