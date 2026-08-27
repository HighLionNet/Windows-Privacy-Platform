// Source/WindowsPrivacyPlatform.Scanner/FirewallCollector.cs
using System;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for Windows Firewall profile state and service availability.
    /// Sources: FirewallPolicy registry paths + ServiceController (MpsSvc).
    /// Never enables/disables rules or profiles. Never elevates. Fail-soft.
    /// </summary>
    public sealed class FirewallCollector : IInventoryCollector
    {
        public string Name => "FirewallCollector";

        private static readonly (string Profile, string SubKey)[] Profiles =
        {
            ("Domain", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile"),
            ("Private", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile"),
            ("Public", @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile")
        };

        public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            var notes = new List<string>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                CollectServiceState(snapshot, notes);
                CollectProfiles(snapshot, notes, cancellationToken);
                CollectRules(snapshot, notes, cancellationToken);
                CollectDefenderServiceHint(snapshot, notes);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                notes.Add($"Firewall collection error: {ex.GetType().Name}. Partial or empty results.");
            }

            snapshot.Networking.FirewallCollectionNotes = string.Join(" ", notes);
        }

        private static void CollectServiceState(InventorySnapshot snapshot, List<string> notes)
        {
            try
            {
                using var sc = new ServiceController("MpsSvc");
                snapshot.Networking.FirewallServiceState = sc.Status.ToString();
                notes.Add($"Windows Firewall service (MpsSvc) state via ServiceController: {sc.Status}.");
            }
            catch (Exception)
            {
                snapshot.Networking.FirewallServiceState = "Unknown";
                notes.Add("MpsSvc service state could not be read; reported as Unknown.");
            }
        }

        private static void CollectDefenderServiceHint(InventorySnapshot snapshot, List<string> notes)
        {
            try
            {
                using var sc = new ServiceController("WinDefend");
                snapshot.Security.DefenderServiceState = sc.Status.ToString();
            }
            catch
            {
                snapshot.Security.DefenderServiceState = "Unknown";
            }
        }

        private static void CollectProfiles(InventorySnapshot snapshot, List<string> notes, CancellationToken cancellationToken)
        {
            foreach (var (profile, subKey) in Profiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = new FirewallProfileInfo
                {
                    ProfileName = profile,
                    SourcePath = $@"HKLM\{subKey}"
                };

                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(subKey, writable: false);
                    if (key is null)
                    {
                        info.Enabled = "Unknown";
                        info.DefaultInboundAction = "Unknown";
                        info.DefaultOutboundAction = "Unknown";
                        info.LoggingEnabled = "Unknown";
                        info.InboundNotifications = "Unknown";
                        info.CollectionNotes = "Registry profile key not present or inaccessible (read-only).";
                        notes.Add($"{profile} profile key missing or inaccessible.");
                    }
                    else
                    {
                        info.Enabled = ReadEnableFirewall(key);
                        info.DefaultInboundAction = ReadAction(key, "DefaultInboundAction");
                        info.DefaultOutboundAction = ReadAction(key, "DefaultOutboundAction");
                        info.LoggingEnabled = ReadLogging(key);
                        info.InboundNotifications = ReadNotifications(key);
                        info.CollectionNotes = "Observed from FirewallPolicy registry (read-only).";
                    }
                }
                catch (Exception)
                {
                    info.Enabled = "Unknown";
                    info.DefaultInboundAction = "Unknown";
                    info.DefaultOutboundAction = "Unknown";
                    info.LoggingEnabled = "Unknown";
                    info.InboundNotifications = "Unknown";
                    info.CollectionNotes = "Error reading profile; treated as Unknown.";
                    notes.Add($"{profile} profile read failed.");
                }

                snapshot.Networking.FirewallProfiles.Add(info);
            }

            if (snapshot.Networking.FirewallProfiles.Count > 0)
                notes.Add("Firewall profiles collected from SharedAccess FirewallPolicy registry paths.");
        }

        private static string ReadNotifications(RegistryKey key)
        {
            var raw = key.GetValue("DisableNotifications");
            if (raw is null)
                return "Not configured";
            if (!int.TryParse(raw.ToString(), out var value))
                return "Unknown";
            return value == 0 ? "Enabled" : "Disabled";
        }

        private static void CollectRules(InventorySnapshot snapshot, List<string> notes, CancellationToken cancellationToken)
        {
            const string command = "Get-NetFirewallRule -PolicyStore ActiveStore -ErrorAction SilentlyContinue | Select-Object Name,DisplayName,Enabled,Direction,Action,Profile | ConvertTo-Csv -NoTypeInformation";
            var result = SafeProcessRunner.Run(
                Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                ["-NoProfile", "-NonInteractive", "-Command", command],
                TimeSpan.FromSeconds(35),
                cancellationToken,
                Encoding.UTF8);

            if (!result.Started || result.TimedOut || result.Canceled || string.IsNullOrWhiteSpace(result.StdOut))
            {
                notes.Add("Firewall rule inventory was unavailable.");
                return;
            }

            foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Skip(1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fields = ParseCsvLine(line);
                if (fields.Count < 6 || string.IsNullOrWhiteSpace(fields[0]))
                    continue;

                snapshot.FirewallRules.Add(new FirewallRuleInfo
                {
                    Name = fields[0],
                    DisplayName = string.IsNullOrWhiteSpace(fields[1]) ? fields[0] : fields[1],
                    Enabled = fields[2],
                    Direction = fields[3],
                    Action = fields[4],
                    Profile = fields[5]
                });
            }

            notes.Add($"Collected {snapshot.FirewallRules.Count} active firewall rule definitions for read-only inventory.");
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else quoted = !quoted;
                }
                else if (c == ',' && !quoted)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }
            fields.Add(current.ToString());
            return fields;
        }

        private static string ReadEnableFirewall(RegistryKey key)
        {
            var raw = key.GetValue("EnableFirewall");
            if (raw is null)
                return "Not configured";
            if (raw is int i)
                return i != 0 ? "Enabled" : "Disabled";
            if (int.TryParse(raw.ToString(), out var parsed))
                return parsed != 0 ? "Enabled" : "Disabled";
            return "Unknown";
        }

        private static string ReadAction(RegistryKey key, string valueName)
        {
            // 0 = Block, 1 = Allow (Windows Firewall policy encoding)
            var raw = key.GetValue(valueName);
            if (raw is null)
                return "Not configured";
            int code;
            if (raw is int i)
                code = i;
            else if (!int.TryParse(raw.ToString(), out code))
                return "Unknown";

            return code switch
            {
                0 => "Block",
                1 => "Allow",
                _ => $"Unknown ({code})"
            };
        }

        private static string ReadLogging(RegistryKey key)
        {
            try
            {
                using var logKey = key.OpenSubKey("Logging", writable: false);
                if (logKey is null)
                    return "Unknown";

                var logFile = logKey.GetValue("LogFilePath") as string;
                var logDropped = logKey.GetValue("LogDroppedPackets");
                var logAllowed = logKey.GetValue("LogSuccessfulConnections");

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(logFile))
                    parts.Add($"path={logFile}");
                if (logDropped is int d)
                    parts.Add(d != 0 ? "dropped=on" : "dropped=off");
                if (logAllowed is int a)
                    parts.Add(a != 0 ? "success=on" : "success=off");

                return parts.Count > 0 ? string.Join("; ", parts) : "Present (details limited)";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
