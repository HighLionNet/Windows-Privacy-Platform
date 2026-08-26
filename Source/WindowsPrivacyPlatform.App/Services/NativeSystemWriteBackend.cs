using System.Globalization;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Xml.Linq;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;

namespace WindowsPrivacyPlatform.App.Services;

/// <summary>Typed operating-system backend for the curated non-registry write allowlist.</summary>
public sealed class NativeSystemWriteBackend : IManagedWriteBackend
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(60);

    public ManagedWriteState Read(WritableTarget target) => target.Kind switch
    {
        WritableTargetKind.Service => ReadService(target.Identifier),
        WritableTargetKind.ScheduledTask => ReadTask(target.Identifier),
        WritableTargetKind.AppxPackage => ReadAppx(target.Identifier),
        WritableTargetKind.OptionalFeature => ReadFeature(target.Identifier),
        _ => ManagedWriteState.Unreadable("This backend does not read registry targets.")
    };

    public bool Write(WritableTarget target, string requestedValue, out string error)
    {
        error = string.Empty;
        if (!target.SupportedRawValues.Contains(requestedValue, StringComparer.OrdinalIgnoreCase))
        {
            error = "Requested value is outside the target allowlist.";
            return false;
        }

        return target.Kind switch
        {
            WritableTargetKind.Service => WriteService(target.Identifier, requestedValue, out error),
            WritableTargetKind.ScheduledTask => WriteTask(target.Identifier, requestedValue, out error),
            WritableTargetKind.AppxPackage => WriteAppx(target.Identifier, requestedValue, out error),
            WritableTargetKind.OptionalFeature => WriteFeature(target.Identifier, requestedValue, out error),
            _ => Fail("This backend does not write registry targets.", out error)
        };
    }

    private static ManagedWriteState ReadService(string name)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{name}", writable: false);
            var rawStart = key?.GetValue("Start");
            if (rawStart is null || !int.TryParse(rawStart.ToString(), out var startCode))
                return ManagedWriteState.Unreadable("The service startup value could not be read.");

            var startup = startCode switch
            {
                2 => "Automatic",
                3 => "Manual",
                4 => "Disabled",
                _ => "Unknown"
            };
            if (startup == "Unknown")
                return ManagedWriteState.Unreadable($"Unsupported service startup code {startCode}.");

            using var controller = new ServiceController(name);
            var state = controller.Status switch
            {
                ServiceControllerStatus.Running => "Running",
                ServiceControllerStatus.Stopped => "Stopped",
                _ => controller.Status.ToString()
            };
            return new ManagedWriteState(true, $"Startup:{startup}; State:{state}",
                "Verified through both the service registry startup value and Service Control Manager state.");
        }
        catch (Exception ex)
        {
            return ManagedWriteState.Unreadable(ex.Message);
        }
    }

    private static bool WriteService(string name, string requested, out string error)
    {
        error = string.Empty;
        if (requested.StartsWith("Startup:", StringComparison.OrdinalIgnoreCase))
        {
            var mode = requested["Startup:".Length..].ToLowerInvariant() switch
            {
                "automatic" => "auto",
                "manual" => "demand",
                "disabled" => "disabled",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(mode))
                return Fail("Unsupported service startup mode.", out error);

            var result = SafeProcessRunner.Run("sc.exe", $"config \"{name}\" start= {mode}", ProcessTimeout);
            return ProcessSucceeded(result, out error);
        }

        try
        {
            using var controller = new ServiceController(name);
            if (requested.Equals("State:Running", StringComparison.OrdinalIgnoreCase))
            {
                if (controller.Status != ServiceControllerStatus.Running)
                {
                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                }
                return true;
            }
            if (requested.Equals("State:Stopped", StringComparison.OrdinalIgnoreCase))
            {
                if (controller.Status != ServiceControllerStatus.Stopped)
                {
                    if (!controller.CanStop)
                        return Fail("Windows reports that this service cannot be stopped in its current state.", out error);
                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                return true;
            }
            return Fail("Unsupported service state request.", out error);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message, out error);
        }
    }

    private static ManagedWriteState ReadTask(string path)
    {
        var result = SafeProcessRunner.Run("schtasks.exe", $"/query /tn \"{path}\" /xml", ProcessTimeout, outputEncoding: Encoding.Unicode);
        if (!result.Started || result.TimedOut || result.Canceled || result.ExitCode != 0)
            return ManagedWriteState.Unreadable(ProcessError(result));

        try
        {
            var document = XDocument.Parse(result.StdOut);
            var enabled = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Enabled")?.Value;
            return enabled?.Equals("false", StringComparison.OrdinalIgnoreCase) == true
                ? new ManagedWriteState(true, "Disabled")
                : new ManagedWriteState(true, "Enabled");
        }
        catch (Exception ex)
        {
            return ManagedWriteState.Unreadable("Task XML could not be parsed: " + ex.Message);
        }
    }

    private static bool WriteTask(string path, string requested, out string error)
    {
        var action = requested.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? "/enable"
            : requested.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ? "/disable"
            : string.Empty;
        if (string.IsNullOrEmpty(action))
            return Fail("Unsupported scheduled-task request.", out error);

        var result = SafeProcessRunner.Run("schtasks.exe", $"/change /tn \"{path}\" {action}", ProcessTimeout);
        return ProcessSucceeded(result, out error);
    }

    private static ManagedWriteState ReadAppx(string packageName)
    {
        var escaped = packageName.Replace("'", "''", StringComparison.Ordinal);
        var command = $"@(Get-AppxPackage -Name '{escaped}' -ErrorAction SilentlyContinue).Count";
        var result = RunPowerShell(command);
        if (!result.Started || result.TimedOut || result.Canceled || result.ExitCode != 0)
            return ManagedWriteState.Unreadable(ProcessError(result));
        return int.TryParse(result.StdOut.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
            ? new ManagedWriteState(true, count > 0 ? "Installed" : "Removed")
            : ManagedWriteState.Unreadable("Package query returned an unrecognized value.");
    }

    private static bool WriteAppx(string packageName, string requested, out string error)
    {
        if (!requested.Equals("Remove", StringComparison.OrdinalIgnoreCase))
            return Fail("Only per-user removal is authorized for this package.", out error);

        var escaped = packageName.Replace("'", "''", StringComparison.Ordinal);
        var result = RunPowerShell($"Get-AppxPackage -Name '{escaped}' -ErrorAction Stop | Remove-AppxPackage -ErrorAction Stop");
        return ProcessSucceeded(result, out error);
    }

    private static ManagedWriteState ReadFeature(string featureName)
    {
        var result = SafeProcessRunner.Run(
            ResolveDism(),
            $"/Online /Get-FeatureInfo /FeatureName:\"{featureName}\" /English",
            ProcessTimeout,
            outputEncoding: Encoding.Default);
        if (!result.Started || result.TimedOut || result.Canceled || result.ExitCode != 0)
            return ManagedWriteState.Unreadable(ProcessError(result));

        var stateLine = result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.TrimStart().StartsWith("State :", StringComparison.OrdinalIgnoreCase));
        if (stateLine is null)
            return ManagedWriteState.Unreadable("DISM did not return a feature state.");
        var state = stateLine[(stateLine.IndexOf(':') + 1)..].Trim();
        return state.StartsWith("Enabled", StringComparison.OrdinalIgnoreCase)
            ? new ManagedWriteState(true, "Enabled", state)
            : state.StartsWith("Disabled", StringComparison.OrdinalIgnoreCase)
                ? new ManagedWriteState(true, "Disabled", state)
                : ManagedWriteState.Unreadable("DISM returned an unsupported feature state: " + state);
    }

    private static bool WriteFeature(string featureName, string requested, out string error)
    {
        var action = requested.Equals("Enabled", StringComparison.OrdinalIgnoreCase)
            ? $"/Online /Enable-Feature /FeatureName:\"{featureName}\" /NoRestart /English"
            : requested.Equals("Disabled", StringComparison.OrdinalIgnoreCase)
                ? $"/Online /Disable-Feature /FeatureName:\"{featureName}\" /NoRestart /English"
                : string.Empty;
        if (string.IsNullOrEmpty(action))
            return Fail("Unsupported optional-feature request.", out error);

        var result = SafeProcessRunner.Run(ResolveDism(), action, TimeSpan.FromMinutes(3), outputEncoding: Encoding.Default);
        if (result.Started && !result.TimedOut && !result.Canceled && result.ExitCode is 0 or 3010)
        {
            error = string.Empty;
            return true;
        }
        error = ProcessError(result);
        return false;
    }

    private static SafeProcessRunner.ProcessRunResult RunPowerShell(string command) =>
        SafeProcessRunner.Run(
            "powershell.exe",
            "-NoProfile -NonInteractive -Command \"" + command.Replace("\"", "`\"", StringComparison.Ordinal) + "\"",
            ProcessTimeout,
            outputEncoding: Encoding.UTF8);

    private static string ResolveDism()
    {
        var path = Path.Combine(Environment.SystemDirectory, "dism.exe");
        return File.Exists(path) ? path : "dism.exe";
    }

    private static bool ProcessSucceeded(SafeProcessRunner.ProcessRunResult result, out string error)
    {
        if (result.Started && !result.TimedOut && !result.Canceled && result.ExitCode == 0)
        {
            error = string.Empty;
            return true;
        }
        error = ProcessError(result);
        return false;
    }

    private static string ProcessError(SafeProcessRunner.ProcessRunResult result)
    {
        if (!result.Started) return result.StdErr.Length > 0 ? result.StdErr : "The native process did not start.";
        if (result.TimedOut) return "The native process timed out.";
        if (result.Canceled) return "The native process was cancelled.";
        return string.IsNullOrWhiteSpace(result.StdErr)
            ? $"The native process exited with code {result.ExitCode}."
            : result.StdErr.Trim();
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
