using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using Microsoft.Win32.SafeHandles;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

public enum AdminEntryResult
{
    Authorized,
    RelaunchStarted,
    Denied
}

/// <summary>Windows UAC authorization and operating-system token authority for Administrator mode.</summary>
public sealed class ElevationService
{
    private readonly IAuditLogger _log;
    private readonly Func<bool> _isElevated;
    private readonly Func<bool> _launchElevated;
    private bool _adminAuthorized;
    private DateTime _authorizedUtc;
    private int _sessionMinutes;
    private string _initiatingSid;

    public ElevationService(IAuditLogger log)
        : this(log, IsProcessElevated, null)
    {
    }

    internal ElevationService(IAuditLogger log, Func<bool> isElevated, Func<bool>? launchElevated)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _isElevated = isElevated ?? throw new ArgumentNullException(nameof(isElevated));
        _launchElevated = launchElevated ?? TryRelaunchElevated;
        _initiatingSid = CurrentSid();
    }

    public bool IsAdminAuthorized
    {
        get
        {
            if (!_adminAuthorized || !_isElevated()) return false;
            if (_sessionMinutes == 0 || DateTime.UtcNow - _authorizedUtc < TimeSpan.FromMinutes(_sessionMinutes))
                return true;
            _adminAuthorized = false;
            _log.Auth("ElevationService", "Administrator authorization expired by the configured session lifetime.");
            return false;
        }
    }

    public string LastError { get; private set; } = string.Empty;

    public void SetSessionLifetime(int minutes) =>
        _sessionMinutes = minutes is 0 or 15 or 30 or 60 or 240 ? minutes : 0;

    public static bool IsProcessElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (!GetTokenInformation(identity.AccessToken, TokenElevationInformationClass, out var elevation,
                    Marshal.SizeOf<TokenElevationInfo>(), out _))
                return false;
            return elevation.TokenIsElevated != 0;
        }
        catch
        {
            return false;
        }
    }

    public AdminEntryResult TryEnterAdminMode(Window? owner)
    {
        _ = owner;
        LastError = string.Empty;
        if (IsAdminAuthorized) return AdminEntryResult.Authorized;

        var elevated = _isElevated();
        _log.Auth("ElevationService", $"Administrator mode request. Elevated={elevated}.");
        if (!elevated)
        {
            if (_launchElevated()) return AdminEntryResult.RelaunchStarted;
            LastError = "Windows UAC elevation was cancelled or could not start.";
            return AdminEntryResult.Denied;
        }

        _initiatingSid = CurrentSid();
        AuthorizeInMemory("Administrator mode authorized from the Windows-elevated process token.");
        return AdminEntryResult.Authorized;
    }

    public bool ConfirmAdminModeExit(Window? owner)
    {
        return MessageBox.Show(owner,
            "Leave Administrator mode? The app will restart in View-only and drop its elevated token.",
            "Leave Administrator mode", MessageBoxButton.YesNo, MessageBoxImage.Question,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    public void ExitAdminMode()
    {
        _adminAuthorized = false;
        _authorizedUtc = default;
        _log.Auth("ElevationService", "Administrator authorization cleared from memory.");
    }

    public bool AuthorizeRelaunchedSession(string? initiatingSid)
    {
        if (!_isElevated() || string.IsNullOrWhiteSpace(initiatingSid) || initiatingSid.Length > 184 ||
            !initiatingSid.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
        {
            _log.Auth("ElevationService", "Administrator relaunch marker rejected.");
            return false;
        }

        _initiatingSid = initiatingSid;
        AuthorizeInMemory("Administrator mode inherited after Windows UAC completed.");
        return true;
    }

    public bool CanModifyHive(string hive, out string reason)
    {
        reason = string.Empty;
        if (!hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase) &&
            !hive.Equals("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(_initiatingSid, CurrentSid(), StringComparison.OrdinalIgnoreCase)) return true;
        reason = "Per-user settings cannot be changed when Windows elevation used a different administrator account. Sign in as that user and run with that same account.";
        _log.Auth("ElevationService", "HKCU write denied because the elevated identity differs from the initiating identity.");
        return false;
    }

    public bool TryRelaunchElevated()
    {
        try
        {
            var executable = ResolveExecutablePath();
            if (!File.Exists(executable)) return false;
            var start = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
            };
            start.ArgumentList.Add("--authorize-modify");
            start.ArgumentList.Add("--initiating-sid");
            start.ArgumentList.Add(_initiatingSid);
            start.ArgumentList.Add("--no-shortcut-offer");
            var process = Process.Start(start);
            if (process is null) return false;
            _log.Auth("ElevationService", "Elevated self-relaunch started.");
            return true;
        }
        catch (Exception ex)
        {
            _log.Auth("ElevationService", "Elevated self-relaunch failed: category=" + ex.GetType().Name);
            return false;
        }
    }

    public bool TryRelaunchViewOnly()
    {
        try
        {
            var executable = ResolveExecutablePath();
            if (!File.Exists(executable)) return false;
            var shellType = Type.GetTypeFromProgID("Shell.Application", throwOnError: false);
            var shell = shellType is null ? null : Activator.CreateInstance(shellType);
            if (shell is not null)
            {
                try
                {
                    shellType!.InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, shell,
                    [
                        executable,
                        "--inspect --no-shortcut-offer",
                        Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                        "open",
                        1
                    ]);
                }
                finally { if (Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell); }
            }
            else
            {
                var explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                if (!File.Exists(explorer)) return false;
                var start = new ProcessStartInfo { FileName = explorer, UseShellExecute = false };
                start.ArgumentList.Add(executable);
                start.ArgumentList.Add("--inspect");
                start.ArgumentList.Add("--no-shortcut-offer");
                if (Process.Start(start) is null) return false;
            }
            _log.Auth("ElevationService", "Unelevated View-only self-relaunch requested through the Windows shell.");
            return true;
        }
        catch (Exception ex)
        {
            _log.Auth("ElevationService", "View-only self-relaunch failed: category=" + ex.GetType().Name);
            return false;
        }
    }

    private void AuthorizeInMemory(string logMessage)
    {
        _adminAuthorized = true;
        _authorizedUtc = DateTime.UtcNow;
        _log.Auth("ElevationService", logMessage);
    }

    private static string ResolveExecutablePath()
    {
        try
        {
            var main = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(main) && File.Exists(main)) return main;
        }
        catch { }

        var location = Assembly.GetEntryAssembly()?.Location ?? Assembly.GetExecutingAssembly().Location;
        if (location.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            var executable = Path.ChangeExtension(location, ".exe");
            if (File.Exists(executable)) return executable;
        }
        return File.Exists(location) ? location : Environment.ProcessPath ?? string.Empty;
    }

    private static string CurrentSid()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private const int TokenElevationInformationClass = 20;

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenElevationInfo
    {
        public int TokenIsElevated;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(SafeAccessTokenHandle tokenHandle, int tokenInformationClass,
        out TokenElevationInfo tokenInformation, int tokenInformationLength, out int returnLength);
}
