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
    private string _initiatingSid;

    public ElevationService(IAuditLogger log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _initiatingSid = CurrentSid();
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
        catch (Exception)
        {
            Debug.WriteLine("Elevation check failed.");
            return false;
        }
    }

    /// <summary>
    /// Enter Modify mode. If not elevated, offers UAC relaunch.
    /// If elevated, requires explicit confirmation for this session.
    /// </summary>
    public bool TryEnterModifyMode(Window owner)
    {
        var elevated = IsProcessElevated();

        _log.Auth("ElevationService", $"Modify mode request. Elevated={elevated}. Authorized={_modifyAuthorized}.");

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

    /// <summary>Consumes the explicit relaunch marker after Windows has completed the single UAC transition.</summary>
    public bool AuthorizeRelaunchedSession(string? initiatingSid)
    {
        if (!IsProcessElevated())
        {
            _log.Auth("ElevationService", "Relaunch marker rejected because the process is not elevated.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(initiatingSid) || initiatingSid.Length > 184 ||
            !initiatingSid.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
        {
            _log.Auth("ElevationService", "Relaunch marker rejected because the initiating identity marker is malformed.");
            return false;
        }
        _initiatingSid = initiatingSid;
        _modifyAuthorized = true;
        _log.Auth("ElevationService", "Modify mode authorized from the completed UAC relaunch.");
        return true;
    }

    /// <summary>Prevents an over-the-shoulder administrator token from silently changing that administrator's HKCU.</summary>
    public bool CanModifyHive(string hive, out string reason)
    {
        reason = string.Empty;
        if (!hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase) &&
            !hive.Equals("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(_initiatingSid, CurrentSid(), StringComparison.OrdinalIgnoreCase))
            return true;
        reason = "Per-user settings cannot be changed when Windows elevation used a different administrator account. Sign in as that user and run with that same account.";
        _log.Auth("ElevationService", "HKCU write denied because the elevated identity differs from the initiating identity.");
        return false;
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
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
            };
            psi.ArgumentList.Add("--authorize-modify");
            psi.ArgumentList.Add("--initiating-sid");
            psi.ArgumentList.Add(_initiatingSid);

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
            _log.Auth("ElevationService", "Relaunch elevated failed: category=" + ex.GetType().Name);
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

    private static string CurrentSid()
    {
        try { using var identity = WindowsIdentity.GetCurrent(); return identity.User?.Value ?? string.Empty; }
        catch { return string.Empty; }
    }
}
