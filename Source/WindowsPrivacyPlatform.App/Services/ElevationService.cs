// Source/WindowsPrivacyPlatform.App/Services/ElevationService.cs
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App.Services;

/// <summary>
/// Minimal elevation skeleton for Modify mode.
/// Uses WindowsIdentity / WindowsPrincipal (no custom password store).
/// Does not perform any registry or system writes.
/// Logs all auth decisions to auth.log via AuditLogger.
/// </summary>
public sealed class ElevationService
{
    private readonly IAuditLogger _log;
    private bool _modifyAuthorized;

    public ElevationService(IAuditLogger log)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>True when the current process token is elevated and Modify has been authorized this session.</summary>
    public bool IsModifyAuthorized => _modifyAuthorized && IsProcessElevated();

    /// <summary>True when the process is running with an elevated (admin) token.</summary>
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

    /// <summary>True when the current user belongs to the Administrators group (token may still be filtered).</summary>
    public static bool IsUserAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempt to enter Modify mode.
    /// - If already elevated and previously authorized this session → succeed.
    /// - If elevated but not yet authorized → confirm via dialog, then authorize.
    /// - If not elevated → inform user that elevation is required; do not auto-relaunch in this skeleton.
    /// Never writes configuration. Logs outcome to auth.log.
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
            _log.Auth("ElevationService", "Modify mode denied: process is not elevated.");
            MessageBox.Show(
                owner,
                "Modify mode requires an elevated (Administrator) process.\n\n" +
                "Close the application and relaunch via 'Run as administrator', then select Modify again.\n\n" +
                "No configuration changes are performed by this version.",
                "Elevation required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        // Elevated but not yet session-authorized: explicit confirmation.
        var result = MessageBox.Show(
            owner,
            "You are about to enter Modify mode.\n\n" +
            "This session is running with an elevated Administrator token.\n" +
            "In future releases this mode will allow controlled, reversible changes.\n\n" +
            "This build still performs NO writes. Continue?",
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
        _log.Auth("ElevationService", "Modify mode authorized for elevated session. User confirmed.");
        return true;
    }

    /// <summary>Exit Modify mode for this session (does not drop elevation).</summary>
    public void ExitModifyMode()
    {
        if (_modifyAuthorized)
        {
            _modifyAuthorized = false;
            _log.Auth("ElevationService", "Modify mode exited for this session.");
        }
    }
}
