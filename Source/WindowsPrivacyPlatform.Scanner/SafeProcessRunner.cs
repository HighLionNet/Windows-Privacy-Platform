// Source/WindowsPrivacyPlatform.Scanner/SafeProcessRunner.cs
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>
/// Internal process execution helper for inventory collectors.
/// Fixed executable + argument list only. No shell. Concurrent stream drain.
/// Timeout kills the child. Cancellation is honored.
/// Not exposed to the UI as a general command runner.
/// </summary>
public static class SafeProcessRunner
{
    private const int MaxCapturedCharactersPerStream = 1_000_000;
    public sealed class ProcessRunResult
    {
        public int ExitCode { get; init; }
        public string StdOut { get; init; } = string.Empty;
        public string StdErr { get; init; } = string.Empty;
        public TimeSpan Elapsed { get; init; }
        public bool TimedOut { get; init; }
        public bool Canceled { get; init; }
        public bool Started { get; init; }
        public string? FailureCategory { get; init; }
    }

    public static ProcessRunResult Run(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        Encoding? outputEncoding = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !Path.IsPathFullyQualified(fileName) || !File.Exists(fileName))
            return new ProcessRunResult { Started = false, FailureCategory = "InvalidExecutable" };
        if (arguments is null || arguments.Count > 64 || arguments.Any(a => a is null || a.Length > 32_768))
            return new ProcessRunResult { Started = false, FailureCategory = "InvalidArguments" };

        var sw = Stopwatch.StartNew();
        Process? process = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = outputEncoding ?? Encoding.UTF8,
                StandardErrorEncoding = outputEncoding ?? Encoding.UTF8
            };
            psi.WorkingDirectory = Path.GetDirectoryName(fileName) ?? Environment.SystemDirectory;
            foreach (var argument in arguments)
                psi.ArgumentList.Add(argument);

            process = new Process { StartInfo = psi, EnableRaisingEvents = false };

            if (!process.Start())
            {
                return new ProcessRunResult
                {
                    Started = false,
                    FailureCategory = "StartFailed",
                    Elapsed = sw.Elapsed
                };
            }

            // Concurrent drain to avoid classic stdout/stderr deadlock.
            var stdoutTask = DrainAsync(process.StandardOutput, MaxCapturedCharactersPerStream);
            var stderrTask = DrainAsync(process.StandardError, MaxCapturedCharactersPerStream);

            // Process.WaitForExit only accepts milliseconds (no CancellationToken overload).
            // Register cancellation to kill the child; use timeout on WaitForExit.
            using var cancelReg = cancellationToken.Register(() => TryKill(process));

            var timeoutMs = (int)Math.Clamp(timeout.TotalMilliseconds, 1, int.MaxValue);
            var exited = process.WaitForExit(timeoutMs);

            if (cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                return new ProcessRunResult
                {
                    Started = true,
                    Canceled = true,
                    FailureCategory = "Canceled",
                    Elapsed = sw.Elapsed,
                    StdOut = SafeGet(stdoutTask),
                    StdErr = SafeGet(stderrTask)
                };
            }

            if (!exited)
            {
                TryKill(process);
                return new ProcessRunResult
                {
                    Started = true,
                    TimedOut = true,
                    FailureCategory = "Timeout",
                    Elapsed = sw.Elapsed,
                    StdOut = SafeGet(stdoutTask),
                    StdErr = SafeGet(stderrTask),
                    ExitCode = -1
                };
            }

            // Ensure streams complete after process exit.
            Task.WaitAll(new Task[] { stdoutTask, stderrTask }, TimeSpan.FromSeconds(5));

            return new ProcessRunResult
            {
                Started = true,
                ExitCode = process.ExitCode,
                StdOut = stdoutTask.IsCompletedSuccessfully ? stdoutTask.Result : string.Empty,
                StdErr = stderrTask.IsCompletedSuccessfully ? stderrTask.Result : string.Empty,
                Elapsed = sw.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return new ProcessRunResult
            {
                Started = process is not null,
                Canceled = true,
                FailureCategory = "Canceled",
                Elapsed = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            TryKill(process);
            return new ProcessRunResult
            {
                Started = process is not null,
                FailureCategory = "Exception",
                StdErr = ex.GetType().Name,
                Elapsed = sw.Elapsed
            };
        }
        finally
        {
            try { process?.Dispose(); } catch { /* ignore */ }
        }
    }

    private static void TryKill(Process? process)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // best-effort
        }
    }

    private static string SafeGet(Task<string> task)
    {
        try
        {
            return task.IsCompletedSuccessfully ? task.Result : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<string> DrainAsync(StreamReader reader, int captureLimit)
    {
        var buffer = new char[4096];
        var captured = new StringBuilder(Math.Min(captureLimit, 64 * 1024));
        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
            if (read == 0)
                break;
            var remaining = captureLimit - captured.Length;
            if (remaining > 0)
                captured.Append(buffer, 0, Math.Min(remaining, read));
        }
        return captured.ToString();
    }
}
