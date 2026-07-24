// Source/WindowsPrivacyPlatform.Scanner/ScheduledTaskCollector.cs
using System;
using System.Diagnostics;
using System.Text;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for scheduled tasks.
    /// Uses schtasks /query /fo CSV /nh (query only, no elevation required for listing).
    /// </summary>
    public sealed class ScheduledTaskCollector : IInventoryCollector
    {
        public string Name => "ScheduledTaskCollector";

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/query /fo CSV /nh",
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

                // CSV format: "TaskName","Next Run Time","Status"
                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.StartsWith('"'))
                        continue;

                    var fields = ParseCsvLine(trimmed);
                    if (fields.Count >= 3)
                    {
                        snapshot.ScheduledTasks.Add(new TaskInfo
                        {
                            Path = fields[0],
                            State = fields[2]
                        });
                    }
                }
            }
            catch
            {
                // schtasks may be unavailable or restricted; leave list empty.
            }
        }

        private static System.Collections.Generic.List<string> ParseCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }
    }
}
