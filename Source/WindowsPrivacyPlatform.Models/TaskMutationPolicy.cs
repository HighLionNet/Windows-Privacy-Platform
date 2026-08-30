namespace WindowsPrivacyPlatform.Models;

/// <summary>Source-controlled allow/deny policy for scheduled-task runtime enablement.</summary>
public static class TaskMutationPolicy
{
    public const int MaximumPathLength = 512;

    public static bool IsMicrosoftPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase);

    public static bool CanMutate(TaskInfo task, IReadOnlyCollection<TaskInfo> currentSnapshot, out string reason)
    {
        reason = string.Empty;
        if (task is null || currentSnapshot is null || string.IsNullOrWhiteSpace(task.Path) ||
            task.Path == @"\" || task.Path.Length > MaximumPathLength ||
            !task.Path.StartsWith('\\') || task.Path.Contains("..", StringComparison.Ordinal))
        {
            reason = "The scheduled-task identity is invalid.";
            return false;
        }

        if (!currentSnapshot.Any(candidate => candidate.Path.Equals(task.Path, StringComparison.OrdinalIgnoreCase)))
        {
            reason = "The task is not present in the current scan snapshot.";
            return false;
        }

        if (IsMicrosoftPath(task.Path) || IsProtectedSubsystem(task.Path))
        {
            reason = "Microsoft and protected Windows tasks are diagnose-only.";
            return false;
        }

        return true;
    }

    private static bool IsProtectedSubsystem(string path) =>
        path.Contains("Defender", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("BitLocker", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("WindowsUpdate", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("UpdateOrchestrator", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("TaskScheduler", StringComparison.OrdinalIgnoreCase) ||
        path.Contains("Maintenance", StringComparison.OrdinalIgnoreCase);
}
