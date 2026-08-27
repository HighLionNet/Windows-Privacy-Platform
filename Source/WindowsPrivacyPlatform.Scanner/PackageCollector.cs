// Source/WindowsPrivacyPlatform.Scanner/PackageCollector.cs
using System;
using System.IO;
using System.Text;
using System.Threading;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for installed AppX / MSIX packages.
    /// Uses fixed PowerShell inventory commands for current-user and provisioned packages.
    /// </summary>
    public sealed class PackageCollector : IInventoryCollector
    {
        public string Name => "PackageCollector";

        public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            CollectNames(
                "Get-AppxPackage -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name",
                snapshot.InstalledPackages,
                cancellationToken);

            CollectNames(
                "Get-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue | Select-Object -ExpandProperty DisplayName",
                snapshot.ProvisionedPackages,
                cancellationToken);
        }

        private static void CollectNames(string command, List<string> destination, CancellationToken cancellationToken)
        {
            var result = SafeProcessRunner.Run(
                Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                ["-NoProfile", "-NonInteractive", "-Command", command],
                TimeSpan.FromSeconds(25),
                cancellationToken,
                Encoding.UTF8);

            if (!result.Started || result.TimedOut || result.Canceled ||
                (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.StdOut)))
                return;

            foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = line.Trim();
                if (string.IsNullOrWhiteSpace(name) || name.StartsWith("Get-Appx", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!destination.Contains(name, StringComparer.OrdinalIgnoreCase))
                    destination.Add(name);
            }
        }
    }
}
