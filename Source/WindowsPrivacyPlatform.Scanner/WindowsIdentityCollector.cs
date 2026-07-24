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
                    // Build number is the reliable discriminator between Windows 10 and 11.
                    // Microsoft intentionally left ProductName as "Windows 10 ..." on Windows 11
                    // for application compatibility.
                    int build = 0;
                    if (!int.TryParse(key.GetValue("CurrentBuild") as string, out build))
                    {
                        int.TryParse(key.GetValue("CurrentBuildNumber") as string, out build);
                    }
                    snapshot.BuildNumber = build;

                    // DisplayVersion is the marketing version (e.g. "25H2", "24H2", "23H2")
                    var displayVersion = key.GetValue("DisplayVersion") as string
                                      ?? key.GetValue("ReleaseId") as string
                                      ?? string.Empty;

                    // EditionID is more precise than the old ProductName string
                    // (e.g. "Professional", "Core", "Enterprise")
                    var editionId = key.GetValue("EditionID") as string ?? string.Empty;

                    // Construct a correct product name.
                    // Build >= 22000 = Windows 11 (Microsoft's official cutoff).
                    string majorName = build >= 22000 ? "Windows 11" : "Windows 10";

                    // Map common EditionID values to friendly names
                    string editionFriendly = editionId switch
                    {
                        "Professional" => "Pro",
                        "ProfessionalWorkstation" => "Pro for Workstations",
                        "ProfessionalEducation" => "Pro Education",
                        "Core" => "Home",
                        "CoreN" => "Home N",
                        "CoreSingleLanguage" => "Home Single Language",
                        "Enterprise" => "Enterprise",
                        "EnterpriseN" => "Enterprise N",
                        "Education" => "Education",
                        "IoTUAP" => "IoT",
                        "ServerRdsh" => "Enterprise multi-session",
                        _ => string.IsNullOrWhiteSpace(editionId) ? "Unknown" : editionId
                    };

                    snapshot.WindowsVersion = $"{majorName} {editionFriendly}".Trim();
                    snapshot.Edition = string.IsNullOrWhiteSpace(displayVersion)
                        ? editionFriendly
                        : displayVersion;
                }
                else
                {
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
            // Environment.OSVersion still reports major version 10 even on Windows 11,
            // so we can only give a generic string here.
            snapshot.WindowsVersion = Environment.OSVersion.VersionString;
            snapshot.Edition = "Unknown (fallback)";
            snapshot.BuildNumber = Environment.OSVersion.Version.Build;
        }
    }
}
