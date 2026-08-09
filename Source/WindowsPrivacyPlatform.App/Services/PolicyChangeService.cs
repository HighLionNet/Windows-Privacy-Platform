// Source/WindowsPrivacyPlatform.App/Services/PolicyChangeService.cs
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Services;

/// <summary>
/// Applies controlled registry changes for catalog-backed settings.
/// Requires ElevationService.IsModifyAuthorized. Every write is confirmed and audited.
/// Supports DWORD numeric values, string ConsentStore values, and value deletion (Not configured).
/// Does not touch firewall service state or non-registry interfaces.
/// </summary>
public sealed class PolicyChangeService
{
    private readonly ElevationService _elevation;
    private readonly IAuditLogger _log;

    public PolicyChangeService(ElevationService elevation, IAuditLogger log)
    {
        _elevation = elevation ?? throw new ArgumentNullException(nameof(elevation));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    /// <summary>
    /// Apply a catalog value. rawValue null or empty means delete the value (Not configured).
    /// Returns true if the write succeeded.
    /// </summary>
    public bool TryApply(ManagedObject mo, string? rawValue, Window? owner, out string message)
    {
        message = string.Empty;

        if (mo is null)
        {
            message = "Setting is null.";
            return false;
        }

        if (!_elevation.IsModifyAuthorized)
        {
            message = "Modify mode is not authorized. Switch to Modify and elevate first.";
            _log.Change("PolicyChangeService", $"Denied write for {mo.ObjectId}: not authorized.");
            return false;
        }

        if (!TryResolveTarget(mo, out var hive, out var subKey, out var valueName, out var resolveError))
        {
            message = resolveError;
            _log.Change("PolicyChangeService", $"Cannot resolve path for {mo.ObjectId}: {resolveError}");
            return false;
        }

        var displayValue = string.IsNullOrWhiteSpace(rawValue) ? "(delete / Not configured)" : rawValue;
        var confirm = MessageBox.Show(
            owner,
            $"Change setting:\n\n" +
            $"{mo.ObjectName}\n" +
            $"ObjectId: {mo.ObjectId}\n\n" +
            $"Target: {FormatHive(hive)}\\{subKey}\\{valueName}\n" +
            $"New value: {displayValue}\n\n" +
            "This writes to the Windows registry under an elevated token.\n" +
            "Continue?",
            "Confirm change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
        {
            message = "Change cancelled.";
            _log.Change("PolicyChangeService", $"User cancelled write for {mo.ObjectId} → {displayValue}");
            return false;
        }

        try
        {
            using var baseKey = hive == RegistryHive.LocalMachine
                ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                : RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                using var key = baseKey.OpenSubKey(subKey, writable: true);
                if (key is null)
                {
                    message = "Key does not exist; nothing to clear.";
                    _log.Change("PolicyChangeService", $"{mo.ObjectId}: clear skipped — key missing.");
                    return true; // already not configured
                }

                key.DeleteValue(valueName, throwOnMissingValue: false);
                message = "Value cleared (Not configured).";
                _log.Change("PolicyChangeService",
                    $"CLEARED {mo.ObjectId} | {FormatHive(hive)}\\{subKey}\\{valueName} | by {Environment.UserName}");
                return true;
            }

            using (var key = baseKey.CreateSubKey(subKey, writable: true))
            {
                if (key is null)
                {
                    message = "Could not open or create registry key.";
                    return false;
                }

                // Prefer DWORD for pure numeric policy values; string for ConsentStore-style.
                if (IsAllDigits(rawValue))
                {
                    if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dword))
                    {
                        message = "Invalid numeric value.";
                        return false;
                    }
                    key.SetValue(valueName, dword, RegistryValueKind.DWord);
                }
                else
                {
                    key.SetValue(valueName, rawValue, RegistryValueKind.String);
                }
            }

            message = $"Set to {rawValue}.";
            _log.Change("PolicyChangeService",
                $"SET {mo.ObjectId} | {FormatHive(hive)}\\{subKey}\\{valueName} = {rawValue} | by {Environment.UserName}");
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            message = "Access denied. Process may not be fully elevated.";
            _log.Change("PolicyChangeService", $"Access denied writing {mo.ObjectId}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            message = "Write failed: " + ex.Message;
            _log.Change("PolicyChangeService", $"Write failed for {mo.ObjectId}: {ex.Message}");
            return false;
        }
    }

    private static bool TryResolveTarget(
        ManagedObject mo,
        out RegistryHive hive,
        out string subKey,
        out string valueName,
        out string error)
    {
        hive = RegistryHive.LocalMachine;
        subKey = string.Empty;
        valueName = string.Empty;
        error = string.Empty;

        // Prefer live observed layer path when present.
        var path = mo.Observation?.Layers?
            .Select(l => l.SourcePath)
            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) &&
                                 (p.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) ||
                                  p.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrWhiteSpace(path))
            path = mo.DiscoveryMethod;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "No registry path is defined for this setting.";
            return false;
        }

        // Firewall / service paths are not registry value writes.
        if (path.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("*", StringComparison.Ordinal))
        {
            error = "This setting is not a single registry value and cannot be changed here yet.";
            return false;
        }

        path = path.Replace('/', '\\').Trim();

        if (path.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = RegistryHive.LocalMachine;
            path = path["HKLM\\".Length..];
        }
        else if (path.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = RegistryHive.CurrentUser;
            path = path["HKCU\\".Length..];
        }
        else if (path.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = RegistryHive.LocalMachine;
            path = path["HKEY_LOCAL_MACHINE\\".Length..];
        }
        else if (path.StartsWith("HKEY_CURRENT_USER\\", StringComparison.OrdinalIgnoreCase))
        {
            hive = RegistryHive.CurrentUser;
            path = path["HKEY_CURRENT_USER\\".Length..];
        }
        else
        {
            error = "Unsupported path format: " + path;
            return false;
        }

        // Catalog sometimes uses "...\\ConsentStore\\location\\Value" — last segment is value name.
        var lastSlash = path.LastIndexOf('\\');
        if (lastSlash <= 0 || lastSlash >= path.Length - 1)
        {
            error = "Could not split key and value name from: " + path;
            return false;
        }

        subKey = path[..lastSlash];
        valueName = path[(lastSlash + 1)..];

        if (string.IsNullOrWhiteSpace(subKey) || string.IsNullOrWhiteSpace(valueName))
        {
            error = "Empty key or value name after parse.";
            return false;
        }

        return true;
    }

    private static bool IsAllDigits(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        foreach (var c in s)
        {
            if (c is < '0' or > '9')
                return false;
        }
        return true;
    }

    private static string FormatHive(RegistryHive hive) =>
        hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
}
