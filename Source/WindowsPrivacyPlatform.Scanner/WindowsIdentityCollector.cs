// Source/WindowsPrivacyPlatform.Scanner/WindowsIdentityCollector.cs
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only multi-source collector for Windows identity and machine context.
    /// Primary: HKLM NT\\CurrentVersion.
    /// Cross-checks: RuntimeInformation, Environment, CIM/WMI (fail-soft).
    /// Never writes. Never requests elevation. Unknown is first-class.
    /// </summary>
    public sealed class WindowsIdentityCollector : IInventoryCollector
    {
        public string Name => "WindowsIdentityCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            var notes = new List<string>();
            var sourcesAgreeing = 0;
            var sourcesAttempted = 0;

            try
            {
                sourcesAttempted++;
                var registryOk = TryCollectFromRegistry(snapshot, notes);
                if (registryOk)
                    sourcesAgreeing++;

                sourcesAttempted++;
                TryCollectFromRuntime(snapshot, notes, ref sourcesAgreeing);

                sourcesAttempted++;
                TryCollectFromWmi(snapshot, notes, ref sourcesAgreeing);

                // Always populate .NET / PowerShell best-effort (local process info).
                snapshot.Identity.DotNetRuntimeVersion = RuntimeInformation.FrameworkDescription;
                TryPowerShellVersion(snapshot, notes);

                // Confidence from agreement
                if (sourcesAgreeing >= 2 && registryOk)
                {
                    snapshot.Identity.IdentityConfidence = EffectiveConfidence.High;
                    notes.Add("Windows version and edition confirmed by registry and at least one additional source.");
                }
                else if (registryOk || sourcesAgreeing >= 1)
                {
                    snapshot.Identity.IdentityConfidence = EffectiveConfidence.Medium;
                    notes.Add("Partial identity evidence; some secondary sources were unavailable or incomplete.");
                }
                else
                {
                    snapshot.Identity.IdentityConfidence = EffectiveConfidence.Low;
                    notes.Add("Limited identity evidence; values may be incomplete or from environment fallback only.");
                }
            }
            catch (Exception ex)
            {
                notes.Add($"Identity collection encountered an unexpected error: {ex.GetType().Name}. Pipeline continues with partial data.");
                if (string.IsNullOrWhiteSpace(snapshot.WindowsVersion))
                    ApplyEnvironmentFallback(snapshot);
                snapshot.Identity.IdentityConfidence = EffectiveConfidence.Low;
            }

            snapshot.Identity.IdentityCollectionNotes = string.Join(" ", notes);
            snapshot.CaptureTimestamp = DateTime.UtcNow;
        }

        private static bool TryCollectFromRegistry(InventorySnapshot snapshot, List<string> notes)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", writable: false);

                if (key is null)
                {
                    notes.Add("Registry CurrentVersion key unavailable.");
                    ApplyEnvironmentFallback(snapshot);
                    return false;
                }

                int build = ReadBuildNumber(key);
                snapshot.BuildNumber = build;

                var displayVersion = key.GetValue("DisplayVersion") as string
                                  ?? key.GetValue("ReleaseId") as string
                                  ?? string.Empty;

                var editionId = key.GetValue("EditionID") as string ?? string.Empty;
                string majorName = build >= 22000 ? "Windows 11" : "Windows 10";
                string editionFriendly = MapEditionId(editionId);

                snapshot.WindowsVersion = $"{majorName} {editionFriendly}".Trim();
                snapshot.Edition = string.IsNullOrWhiteSpace(displayVersion)
                    ? editionFriendly
                    : displayVersion;

                // Architecture from registry ProductName is unreliable; Runtime preferred.
                notes.Add("Primary identity from HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion.");
                return true;
            }
            catch (Exception)
            {
                notes.Add("Registry identity read failed; using environment fallback.");
                ApplyEnvironmentFallback(snapshot);
                return false;
            }
        }

        private static void TryCollectFromRuntime(InventorySnapshot snapshot, List<string> notes, ref int sourcesAgreeing)
        {
            try
            {
                var arch = RuntimeInformation.OSArchitecture.ToString();
                if (string.IsNullOrWhiteSpace(snapshot.Identity.Architecture))
                    snapshot.Identity.Architecture = arch;
                else if (!string.Equals(snapshot.Identity.Architecture, arch, StringComparison.OrdinalIgnoreCase))
                    notes.Add($"Architecture conflict: prior={snapshot.Identity.Architecture}, runtime={arch}.");
                else
                    sourcesAgreeing++;

                // OS description cross-check (does not override registry product name).
                var osDesc = RuntimeInformation.OSDescription;
                if (!string.IsNullOrWhiteSpace(osDesc))
                    notes.Add($"Runtime OSDescription: {osDesc}.");
            }
            catch (Exception)
            {
                notes.Add("RuntimeInformation APIs unavailable.");
            }
        }

        private static void TryCollectFromWmi(InventorySnapshot snapshot, List<string> notes, ref int sourcesAgreeing)
        {
            // CIM/WMI is optional and fail-soft. Many locked-down or non-elevated hosts restrict it.
            try
            {
                // Win32_OperatingSystem
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber, OSArchitecture FROM Win32_OperatingSystem"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        using (mo)
                        {
                            var buildStr = mo["BuildNumber"]?.ToString();
                            if (int.TryParse(buildStr, out var wmiBuild) && wmiBuild > 0)
                            {
                                if (snapshot.BuildNumber == 0)
                                    snapshot.BuildNumber = wmiBuild;
                                else if (snapshot.BuildNumber == wmiBuild)
                                    sourcesAgreeing++;
                                else
                                    notes.Add($"Build number conflict: registry={snapshot.BuildNumber}, WMI={wmiBuild}.");
                            }

                            var arch = mo["OSArchitecture"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(arch) && string.IsNullOrWhiteSpace(snapshot.Identity.Architecture))
                                snapshot.Identity.Architecture = arch;

                            notes.Add("WMI Win32_OperatingSystem consulted.");
                        }
                        break;
                    }
                }
            }
            catch (Exception)
            {
                notes.Add("WMI Win32_OperatingSystem unavailable (common without elevation or when WMI is restricted).");
            }

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model, TotalPhysicalMemory FROM Win32_ComputerSystem");
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        snapshot.Identity.DeviceManufacturer = mo["Manufacturer"]?.ToString()?.Trim() ?? string.Empty;
                        snapshot.Identity.DeviceModel = mo["Model"]?.ToString()?.Trim() ?? string.Empty;
                        if (mo["TotalPhysicalMemory"] is ulong bytes)
                            snapshot.Identity.TotalPhysicalMemoryMiB = (long)(bytes / (1024 * 1024));
                        else if (ulong.TryParse(mo["TotalPhysicalMemory"]?.ToString(), out var b))
                            snapshot.Identity.TotalPhysicalMemoryMiB = (long)(b / (1024 * 1024));

                        if (!string.IsNullOrWhiteSpace(snapshot.Identity.DeviceManufacturer))
                            notes.Add("Hardware manufacturer/model from WMI Win32_ComputerSystem.");
                    }
                    break;
                }
            }
            catch (Exception)
            {
                notes.Add("Manufacturer/model unavailable through available read-only providers.");
            }

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor");
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        snapshot.Identity.Processor = mo["Name"]?.ToString()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(snapshot.Identity.Processor))
                            notes.Add("Processor name from WMI Win32_Processor.");
                    }
                    break;
                }
            }
            catch (Exception)
            {
                // Leave processor empty / Unknown.
            }

            // Domain / Azure AD — best-effort via ComputerSystem Domain + optional dsregcmd is avoided (process).
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Domain, PartOfDomain FROM Win32_ComputerSystem");
                using var results = searcher.Get();
                foreach (ManagementObject mo in results)
                {
                    using (mo)
                    {
                        var partOfDomain = mo["PartOfDomain"];
                        var domain = mo["Domain"]?.ToString();
                        if (partOfDomain is bool b && b && !string.IsNullOrWhiteSpace(domain))
                            snapshot.Identity.DomainMembership = domain!;
                        else if (!string.IsNullOrWhiteSpace(domain))
                            snapshot.Identity.DomainMembership = domain!;
                        else
                            snapshot.Identity.DomainMembership = "Workgroup / not domain-joined";
                    }
                    break;
                }
            }
            catch (Exception)
            {
                snapshot.Identity.DomainMembership = "Unknown";
                notes.Add("Domain membership could not be verified with available read-only sources.");
            }

            // Secure Boot / TPM and Entra state still require additional providers.
            snapshot.Identity.SecureBootState = "Unknown";
            snapshot.Identity.TpmPresent = "Unknown";
            snapshot.Identity.TpmVersion = "Unknown";
            snapshot.Identity.AzureAdJoined = "Unknown";
            notes.Add("Secure Boot, TPM, and Entra join state require additional providers on many hosts; reported as Unknown.");

            TryCollectBitLockerStatus(snapshot, notes);
        }

        private static void TryCollectBitLockerStatus(InventorySnapshot snapshot, List<string> notes)
        {
            if (!IsProcessElevated())
            {
                snapshot.Identity.BitLockerProtectionStatus = "Requires Modify mode to observe";
                notes.Add("Live BitLocker volume status was not queried because this Inspect-mode process is not elevated.");
                return;
            }

            try
            {
                var scope = new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption");
                scope.Connect();
                using var searcher = new ManagementObjectSearcher(
                    scope,
                    new ObjectQuery("SELECT DriveLetter, ProtectionStatus, ConversionStatus FROM Win32_EncryptableVolume"));
                using var results = searcher.Get();
                var volumes = new List<string>();

                foreach (ManagementObject volume in results)
                {
                    using (volume)
                    {
                        var drive = volume["DriveLetter"]?.ToString();
                        if (string.IsNullOrWhiteSpace(drive))
                            drive = "Volume";

                        var protection = ToUInt32(volume["ProtectionStatus"]);
                        var conversion = ToUInt32(volume["ConversionStatus"]);
                        volumes.Add($"{drive}: {FormatProtection(protection)}, {FormatConversion(conversion)}");
                    }
                }

                snapshot.Identity.BitLockerProtectionStatus = volumes.Count > 0
                    ? string.Join("; ", volumes)
                    : "No encryptable volumes reported";
                notes.Add("Live BitLocker protection state queried from Win32_EncryptableVolume using the elevated read token.");
            }
            catch (Exception ex)
            {
                snapshot.Identity.BitLockerProtectionStatus = "Unavailable (elevated query failed)";
                notes.Add($"Live BitLocker WMI query failed while elevated ({ex.GetType().Name}); no protection state was inferred.");
            }
        }

        private static bool IsProcessElevated()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static uint? ToUInt32(object? value)
        {
            if (value is uint typed)
                return typed;
            return uint.TryParse(value?.ToString(), out var parsed) ? parsed : null;
        }

        private static string FormatProtection(uint? value) => value switch
        {
            0 => "protection off",
            1 => "protection on",
            2 => "protection unknown",
            _ => "protection unavailable"
        };

        private static string FormatConversion(uint? value) => value switch
        {
            0 => "fully decrypted",
            1 => "fully encrypted",
            2 => "encryption in progress",
            3 => "decryption in progress",
            4 => "encryption paused",
            5 => "decryption paused",
            _ => "conversion state unavailable"
        };

        private static void TryPowerShellVersion(InventorySnapshot snapshot, List<string> notes)
        {
            try
            {
                // Local process environment only — no external shell launch for version.
                var psModule = Environment.GetEnvironmentVariable("PSModulePath");
                if (!string.IsNullOrWhiteSpace(psModule))
                    snapshot.Identity.PowerShellVersion = "Present (module path detected)";
                else
                    snapshot.Identity.PowerShellVersion = "Unknown";
            }
            catch
            {
                snapshot.Identity.PowerShellVersion = "Unknown";
            }
        }

        private static int ReadBuildNumber(RegistryKey key)
        {
            object? raw = key.GetValue("CurrentBuild") ?? key.GetValue("CurrentBuildNumber");
            if (raw is null)
                return 0;

            if (raw is int i)
                return i;

            if (raw is long l)
                return (int)l;

            if (raw is string s && int.TryParse(s.Trim(), out var parsed))
                return parsed;

            if (int.TryParse(raw.ToString(), out var fallback))
                return fallback;

            return 0;
        }

        private static string MapEditionId(string editionId)
        {
            if (string.IsNullOrWhiteSpace(editionId))
                return "Unknown";

            return editionId switch
            {
                "Professional" => "Pro",
                "ProfessionalWorkstation" => "Pro for Workstations",
                "ProfessionalEducation" => "Pro Education",
                "ProfessionalN" => "Pro N",
                "Core" => "Home",
                "CoreN" => "Home N",
                "CoreSingleLanguage" => "Home Single Language",
                "CoreCountrySpecific" => "Home Country Specific",
                "Enterprise" => "Enterprise",
                "EnterpriseN" => "Enterprise N",
                "EnterpriseS" => "Enterprise LTSC",
                "EnterpriseSN" => "Enterprise N LTSC",
                "Education" => "Education",
                "EducationN" => "Education N",
                "IoTUAP" => "IoT",
                "IoTEnterprise" => "IoT Enterprise",
                "ServerRdsh" => "Enterprise multi-session",
                "Cloud" => "S",
                "CloudN" => "S N",
                "CloudEdition" => "SE",
                "CloudEditionN" => "SE N",
                _ => editionId
            };
        }

        private static void ApplyEnvironmentFallback(InventorySnapshot snapshot)
        {
            snapshot.WindowsVersion = Environment.OSVersion.VersionString;
            snapshot.Edition = "Unknown (fallback)";
            snapshot.BuildNumber = Environment.OSVersion.Version.Build;
            try
            {
                snapshot.Identity.Architecture = RuntimeInformation.OSArchitecture.ToString();
            }
            catch
            {
                snapshot.Identity.Architecture = "Unknown";
            }
        }
    }
}
