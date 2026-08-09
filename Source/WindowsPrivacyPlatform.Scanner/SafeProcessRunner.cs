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
internal static class SafeProcessRunner
{
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
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        Encoding? outputEncoding = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return new ProcessRunResult { Started = false, FailureCategory = "InvalidExecutable" };

        var sw = Stopwatch.StartNew();
        Process? process = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = outputEncoding ?? Encoding.UTF8,
                StandardErrorEncoding = outputEncoding ?? Encoding.UTF8
            };

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
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(timeout);

            try
            {
                process.WaitForExit(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);

                if (cancellationToken.IsCancellationRequested)
                {
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
                StdErr = ex.GetType().Name + ": " + ex.Message,
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
}
