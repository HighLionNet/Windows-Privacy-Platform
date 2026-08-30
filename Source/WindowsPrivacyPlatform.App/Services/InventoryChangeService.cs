using System.Text;
using System.IO;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;

namespace WindowsPrivacyPlatform.App.Services;

public enum ScheduledTaskAction
{
    Enable,
    Disable
}

/// <summary>
/// Bounded live-inventory mutations. Identifiers must come from the current, fresh snapshot;
/// no dynamic inventory row receives a WritableTarget and no action passes through PolicyChangeService.
/// </summary>
public sealed class InventoryChangeService
{
    private static readonly TimeSpan MaximumSnapshotAge = TimeSpan.FromMinutes(30);
    private readonly ScanService _scan;
    private readonly ElevationService _elevation;
    private readonly IAuditLogger _log;
    private readonly ServiceControlService _services = new();

    public InventoryChangeService(ScanService scan, ElevationService elevation, IAuditLogger log)
    {
        _scan = scan;
        _elevation = elevation;
        _log = log;
    }

    public bool TryChangeService(ServiceInfo requested, ServiceControlAction action, bool confirmed, out string error)
    {
        if (!TryGetFreshSnapshot(out var snapshot, out error)) return false;
        var current = snapshot.Services.FirstOrDefault(item =>
            item.Name.Equals(requested.Name, StringComparison.OrdinalIgnoreCase));
        if (current is null)
        {
            error = "The service is not present in the current scan snapshot.";
            return false;
        }

        _log.Change("InventoryChange", $"kind=Service id={current.Name} current={current.State} intended={action} result=Attempt");
        var success = _services.TryChange(current, action, _elevation.IsAdminAuthorized, confirmed, out error);
        _log.Change("InventoryChange", $"kind=Service id={current.Name} intended={action} result={(success ? "Verified" : "Denied")} detail={error}");
        return success;
    }

    public bool TryChangeTask(TaskInfo requested, ScheduledTaskAction action, bool confirmed, out string error)
    {
        error = string.Empty;
        if (!_elevation.IsAdminAuthorized) { error = "Administrator mode is required."; return false; }
        if (!confirmed) { error = "The scheduled-task action was not confirmed."; return false; }
        if (!TryGetFreshSnapshot(out var snapshot, out error)) return false;
        if (!TaskMutationPolicy.CanMutate(requested, snapshot.ScheduledTasks, out error)) return false;

        var current = QueryTaskState(requested.Path, out var queryError);
        if (current is null)
        {
            error = "The live scheduled-task state could not be read before the change. " + queryError;
            return false;
        }

        var intended = action == ScheduledTaskAction.Enable ? "Ready" : "Disabled";
        _log.Change("InventoryChange", $"kind=ScheduledTask id={requested.Path} current={current} intended={intended} result=Attempt");
        var executable = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
        var verb = action == ScheduledTaskAction.Enable ? "/ENABLE" : "/DISABLE";
        var result = SafeProcessRunner.Run(executable, ["/Change", "/TN", requested.Path, verb], TimeSpan.FromSeconds(20),
            outputEncoding: Encoding.UTF8);
        if (!result.Started || result.TimedOut || result.Canceled || result.ExitCode != 0)
        {
            error = "Windows rejected the scheduled-task change (" + (result.FailureCategory ?? "exit " + result.ExitCode) + ").";
            _log.Change("InventoryChange", $"kind=ScheduledTask id={requested.Path} intended={intended} result=NotVerified detail={error}");
            return false;
        }

        var observed = QueryTaskState(requested.Path, out queryError);
        var matches = action == ScheduledTaskAction.Disable
            ? observed?.Contains("Disabled", StringComparison.OrdinalIgnoreCase) == true
            : observed is not null && !observed.Contains("Disabled", StringComparison.OrdinalIgnoreCase);
        if (!matches)
        {
            error = "The task change did not pass independent live-state read-back. " + queryError;
            _log.Change("InventoryChange", $"kind=ScheduledTask id={requested.Path} intended={intended} observed={observed ?? "Unknown"} result=NotVerified");
            return false;
        }

        _log.Change("InventoryChange", $"kind=ScheduledTask id={requested.Path} intended={intended} observed={observed} result=Verified");
        return true;
    }

    private bool TryGetFreshSnapshot(out InventorySnapshot snapshot, out string error)
    {
        snapshot = _scan.LastScanResult?.Snapshot ?? new InventorySnapshot();
        if (_scan.LastScanResult?.Status is not (ScanStatus.Completed or ScanStatus.CompletedWithWarnings) ||
            _scan.LastScanResult.Snapshot is null)
        {
            error = "A completed scan snapshot is required before inventory actions.";
            return false;
        }
        if (snapshot.CaptureTimestamp == default || DateTime.UtcNow - snapshot.CaptureTimestamp > MaximumSnapshotAge)
        {
            error = "The scan snapshot is stale. Scan again before an inventory action.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static string? QueryTaskState(string path, out string error)
    {
        var executable = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
        var result = SafeProcessRunner.Run(executable, ["/Query", "/TN", path, "/FO", "CSV", "/NH"],
            TimeSpan.FromSeconds(15), outputEncoding: Encoding.UTF8);
        if (!result.Started || result.TimedOut || result.Canceled || result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StdOut))
        {
            error = result.FailureCategory ?? "query returned no verified row";
            return null;
        }
        var first = result.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.TrimStart().StartsWith('"'));
        var fields = first is null ? [] : ParseCsvLine(first);
        if (fields.Count < 3)
        {
            error = "query returned an unexpected format";
            return null;
        }
        error = string.Empty;
        return fields[2].Trim();
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        foreach (var character in line)
        {
            if (character == '"') quoted = !quoted;
            else if (character == ',' && !quoted) { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(character);
        }
        fields.Add(current.ToString());
        return fields;
    }
}
