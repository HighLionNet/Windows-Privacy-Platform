// Source/WindowsPrivacyPlatform.App/Services/ElevationService.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

/// <summary>
/// Elevation gate for Modify mode.
/// Uses WindowsIdentity / WindowsPrincipal. Can relaunch the process elevated via UAC.
/// Session authorization is explicit after elevation. All decisions go to auth.log.
/// </summary>
public sealed class ElevationService
{
    private readonly IAuditLogger _log;
    private bool _modifyAuthorized;

    public ElevationService(IAuditLogger log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public bool IsModifyAuthorized => _modifyAuthorized && IsProcessElevated();

    public static bool IsProcessElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Elevation check failed: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Enter Modify mode. If not elevated, offers UAC relaunch.
    /// If elevated, requires explicit confirmation for this session.
    /// </summary>
    public bool TryEnterModifyMode(Window owner)
    {
        var user = Environment.UserName;
        var elevated = IsProcessElevated();

        _log.Auth("ElevationService", $"Modify mode request by '{user}'. Elevated={elevated}. Authorized={_modifyAuthorized}.");

        if (_modifyAuthorized && elevated)
        {
            _log.Auth("ElevationService", "Modify mode already authorized for this session.");
            return true;
        }

        if (!elevated)
        {
            _log.Auth("ElevationService", "Process not elevated — offering UAC relaunch.");
            var relaunch = MessageBox.Show(
                owner,
                "Modify mode requires Administrator privileges.\n\n" +
                "Windows will prompt for elevation (UAC). The application will restart elevated.\n\n" +
                "Continue?",
                "Elevation required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (relaunch != MessageBoxResult.Yes)
            {
                _log.Auth("ElevationService", "UAC relaunch declined by user.");
                return false;
            }

            if (TryRelaunchElevated())
            {
                _log.Auth("ElevationService", "UAC relaunch started. Current process will exit.");
                Application.Current.Shutdown();
                return false; // current process is going away
            }

            _log.Auth("ElevationService", "UAC relaunch failed or was cancelled.");
            MessageBox.Show(
                owner,
                "Could not restart elevated. UAC may have been cancelled, or the executable path could not be resolved.\n\n" +
                "You can also right-click the app shortcut and choose Run as administrator.",
                "Elevation failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }

        var result = MessageBox.Show(
            owner,
            "This process is running elevated.\n\n" +
            "Authorize Modify mode for this session?\n\n" +
                "• Only explicit catalog-authorized settings can change.\n" +
                "• Every change is pre-read, confirmed, logged, and independently verified.\n" +
            "• You can switch back to Inspect at any time.\n\n" +
            "Continue?",
            "Authorize Modify mode",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            _log.Auth("ElevationService", "Modify mode authorization declined by user.");
            return false;
        }

        _modifyAuthorized = true;
        _log.Auth("ElevationService", "Modify mode authorized for elevated session.");
        return true;
    }

    public void ExitModifyMode()
    {
        if (_modifyAuthorized)
        {
            _modifyAuthorized = false;
            _log.Auth("ElevationService", "Modify mode exited for this session.");
        }
    }

    /// <summary>
    /// Relaunch this process with a full admin token (UAC prompt).
    /// Returns true if the elevated process was started.
    /// </summary>
    public bool TryRelaunchElevated()
    {
        try
        {
            var exePath = ResolveExecutablePath();
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                _log.Auth("ElevationService", $"Cannot relaunch: executable not found ('{exePath}').");
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--authorize-modify",
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
            };

            var process = Process.Start(psi);
            if (process is null)
            {
                _log.Auth("ElevationService", "Process.Start returned null for elevated relaunch.");
                return false;
            }

            _log.Auth("ElevationService", $"Elevated process started. PID={process.Id}. Path={exePath}");
            return true;
        }
        catch (Exception ex)
        {
            // User cancelling UAC throws Win32Exception
            _log.Auth("ElevationService", "Relaunch elevated failed: " + ex.Message);
            return false;
        }
    }

    private static string ResolveExecutablePath()
    {
        // Prefer the process module path (works for published EXE).
        try
        {
            var main = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(main) && File.Exists(main))
                return main;
        }
        catch { /* ignore */ }

        // Fallback: assembly location (may be .dll under dotnet host).
        var loc = Assembly.GetEntryAssembly()?.Location
                  ?? Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(loc))
        {
            if (loc.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = Path.ChangeExtension(loc, ".exe");
                if (File.Exists(candidate))
                    return candidate;
            }
            if (File.Exists(loc))
                return loc;
        }

        return Environment.ProcessPath ?? string.Empty;
    }
}
