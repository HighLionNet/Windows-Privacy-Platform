// Source/WindowsPrivacyPlatform.Scanner/ScheduledTaskCollector.cs
using System;
using System.IO;
using System.Text;
using System.Threading;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>
/// Read-only collector for scheduled tasks via schtasks /query.
/// Query only. No modification surface.
/// Empty output is not claimed as "no tasks exist" — it may mean query failure.
/// </summary>
public sealed class ScheduledTaskCollector : IInventoryCollector
{
    public string Name => "ScheduledTaskCollector";

    public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        try
        {
            var result = SafeProcessRunner.Run(
                Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                ["/query", "/fo", "CSV", "/nh"],
                TimeSpan.FromSeconds(25),
                cancellationToken,
                Encoding.UTF8);

            if (!result.Started || result.TimedOut || result.Canceled)
                return;

            if (string.IsNullOrWhiteSpace(result.StdOut))
                return;

            foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                cancellationToken.ThrowIfCancellationRequested();
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // schtasks may be unavailable or restricted; leave list empty (unknown, not proven absence).
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
