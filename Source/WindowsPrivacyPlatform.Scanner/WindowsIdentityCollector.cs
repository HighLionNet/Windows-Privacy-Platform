// Source/WindowsPrivacyPlatform.Scanner/WindowsIdentityCollector.cs
using System;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for basic Windows identity information.
    /// Uses only non-elevated Registry.LocalMachine reads and Environment APIs.
    /// Never writes. Never requests elevation.
    /// </summary>
    public sealed class WindowsIdentityCollector : IInventoryCollector
    {
        public string Name => "WindowsIdentityCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            try
            {
                // Primary source: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion
                // Readable without elevation on standard Windows installations.
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", writable: false);

                if (key is not null)
                {
                    // Product name (e.g. "Windows 11 Pro")
                    snapshot.WindowsVersion = key.GetValue("ProductName") as string
                        ?? key.GetValue("DisplayVersion") as string
                        ?? "Unknown";

                    // Edition / display version
                    var displayVersion = key.GetValue("DisplayVersion") as string;
                    var editionId = key.GetValue("EditionID") as string;
                    snapshot.Edition = !string.IsNullOrWhiteSpace(displayVersion)
                        ? displayVersion
                        : (editionId ?? "Unknown");

                    // Build number
                    if (int.TryParse(key.GetValue("CurrentBuild") as string, out var build))
                    {
                        snapshot.BuildNumber = build;
                    }
                    else if (int.TryParse(key.GetValue("CurrentBuildNumber") as string, out var buildAlt))
                    {
                        snapshot.BuildNumber = buildAlt;
                    }
                }
                else
                {
                    // Fallback when registry key is unavailable
                    ApplyEnvironmentFallback(snapshot);
                }
            }
            catch (Exception)
            {
                // Any failure (permissions, missing key, etc.) falls back safely.
                // Never throws out of Collect – scanner orchestration continues.
                ApplyEnvironmentFallback(snapshot);
            }

            snapshot.CaptureTimestamp = DateTime.UtcNow;
        }

        private static void ApplyEnvironmentFallback(InventorySnapshot snapshot)
        {
            snapshot.WindowsVersion = Environment.OSVersion.VersionString;
            snapshot.Edition = "Unknown (fallback)";
            snapshot.BuildNumber = Environment.OSVersion.Version.Build;
        }
    }
}
