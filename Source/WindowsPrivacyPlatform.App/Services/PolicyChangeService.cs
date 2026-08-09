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
/// Controlled registry changes for catalog-backed settings.
/// Contract: success is returned ONLY when an independent read-back matches the intended state.
/// Requires ElevationService.IsModifyAuthorized. Every attempt is audited (before, after, verify).
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
    /// Apply a catalog value. rawValue null/empty = delete value (Not configured).
    /// Returns true only when the system registry read-back matches the intended state.
    /// </summary>
    public bool TryApply(ManagedObject mo, string? rawValue, Window? owner, out string message)
    {
        message = string.Empty;

        if (mo is null)
        {
            message = "Setting is null.";
            return false;
        }

        if (!_elevation.IsModifyAuthorized || !ElevationService.IsProcessElevated())
        {
            message = "Modify mode is not authorized or the process is not elevated.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: not authorized or not elevated.");
            return false;
        }

        if (!TryResolveTarget(mo, out var hive, out var subKey, out var valueName, out var resolveError))
        {
            message = resolveError;
            _log.Change("PolicyChangeService", $"RESOLVE_FAIL {mo.ObjectId}: {resolveError}");
            return false;
        }

        var targetPath = $"{FormatHive(hive)}\\{subKey}\\{valueName}";
        var intendedDelete = string.IsNullOrWhiteSpace(rawValue);
        var intendedDisplay = intendedDelete ? "(absent / Not configured)" : rawValue!;

        // --- Pre-read (independent of catalog observation) ---
        var before = ReadValue(hive, subKey, valueName);
        _log.Change("PolicyChangeService",
            $"BEFORE {mo.ObjectId} | {targetPath} | present={before.Present} kind={before.Kind} value={before.Normalized ?? "(absent)"}");

        var confirm = MessageBox.Show(
            owner,
            $"Change setting:\n\n" +
            $"{mo.ObjectName}\n" +
            $"ObjectId: {mo.ObjectId}\n\n" +
            $"Registry: {targetPath}\n" +
            $"Current (system): {(before.Present ? before.Normalized : "Not configured")}\n" +
            $"Intended: {intendedDisplay}\n\n" +
            "The change is only accepted if a fresh registry read-back matches.\n" +
            "Continue?",
            "Confirm change",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (confirm != MessageBoxResult.Yes)
        {
            message = "Change cancelled.";
            _log.Change("PolicyChangeService", $"CANCELLED {mo.ObjectId} → {intendedDisplay}");
            return false;
        }

        // Idempotent short-circuit: already at intended state
        if (StateMatches(before, rawValue))
        {
            message = $"Already at intended state (verified): {intendedDisplay}";
            _log.Change("PolicyChangeService", $"NOOP_VERIFIED {mo.ObjectId} | {targetPath} = {intendedDisplay}");
            return true;
        }

        try
        {
            if (intendedDelete)
            {
                if (!TryDeleteValue(hive, subKey, valueName, out var delError))
                {
                    message = delError;
                    _log.Change("PolicyChangeService", $"DELETE_FAIL {mo.ObjectId}: {delError}");
                    return false;
                }
            }
            else
            {
                if (!TryWriteValue(hive, subKey, valueName, rawValue!, before.Kind, out var writeError))
                {
                    message = writeError;
                    _log.Change("PolicyChangeService", $"WRITE_FAIL {mo.ObjectId}: {writeError}");
                    return false;
                }
            }

            // --- Mandatory independent read-back ---
            // Close handles above before reading. Small retry for registry propagation.
            RegistryRead after = default;
            var verified = false;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                after = ReadValue(hive, subKey, valueName);
                verified = StateMatches(after, rawValue);
                if (verified)
                    break;
                System.Threading.Thread.Sleep(40 * attempt);
            }

            _log.Change("PolicyChangeService",
                $"AFTER {mo.ObjectId} | {targetPath} | present={after.Present} kind={after.Kind} value={after.Normalized ?? "(absent)"} | verified={verified}");

            if (!verified)
            {
                message =
                    "Write completed but verification FAILED.\n" +
                    $"Intended: {intendedDisplay}\n" +
                    $"System read-back: {(after.Present ? after.Normalized : "Not configured")}\n\n" +
                    "The application will not report this change as successful.";
                _log.Change("PolicyChangeService",
                    $"VERIFY_FAIL {mo.ObjectId} | intended={intendedDisplay} | actual={(after.Present ? after.Normalized : "(absent)")}");
                return false;
            }

            message = intendedDelete
                ? "Verified: value is absent (Not configured)."
                : $"Verified: system value is {after.Normalized}.";
            _log.Change("PolicyChangeService",
                $"VERIFIED {mo.ObjectId} | {targetPath} = {after.Normalized ?? "(absent)"} | by {Environment.UserName}");
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            message = "Access denied. Process may not be fully elevated.";
            _log.Change("PolicyChangeService", $"ACCESS_DENIED {mo.ObjectId}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            message = "Change failed: " + ex.Message;
            _log.Change("PolicyChangeService", $"EXCEPTION {mo.ObjectId}: {ex}");
            return false;
        }
    }

    // ---------- registry primitives ----------

    private readonly struct RegistryRead
    {
        public bool Present { get; init; }
        public RegistryValueKind Kind { get; init; }
        public string? Normalized { get; init; }
        public object? Raw { get; init; }
    }

    private static RegistryRead ReadValue(RegistryHive hive, string subKey, string valueName)
    {
        // Prefer 64-bit view (Policies live here). Fall back to default view if needed.
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Default })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey(subKey, writable: false);
                if (key is null)
                    continue;

                var names = key.GetValueNames();
                var exists = names.Any(n => string.Equals(n, valueName, StringComparison.OrdinalIgnoreCase))
                             || (string.IsNullOrEmpty(valueName) && key.GetValue(null) is not null);

                // GetValueNames does not include default value; handle named values only for policies.
                var raw = key.GetValue(valueName, defaultValue: null, options: RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (raw is null && !exists)
                    continue; // try next view / treat as missing after both

                if (raw is null)
                    return new RegistryRead { Present = false };

                var kind = key.GetValueKind(valueName);
                return new RegistryRead
                {
                    Present = true,
                    Kind = kind,
                    Raw = raw,
                    Normalized = NormalizeRaw(raw, kind)
                };
            }
            catch
            {
                // try next view
            }
        }

        return new RegistryRead { Present = false };
    }

    private static bool TryWriteValue(
        RegistryHive hive,
        string subKey,
        string valueName,
        string rawValue,
        RegistryValueKind existingKind,
        out string error)
    {
        error = string.Empty;
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKey, writable: true);
        if (key is null)
        {
            error = "Could not open or create registry key.";
            return false;
        }

        // Match existing kind when present; otherwise DWORD for pure digits, String otherwise.
        if (IsAllDigits(rawValue))
        {
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dword))
            {
                error = "Invalid numeric value.";
                return false;
            }

            var kind = existingKind is RegistryValueKind.DWord or RegistryValueKind.QWord
                ? RegistryValueKind.DWord
                : RegistryValueKind.DWord;
            key.SetValue(valueName, dword, kind);
        }
        else
        {
            // ConsentStore / string policies
            if (existingKind == RegistryValueKind.DWord && int.TryParse(rawValue, out var asDword))
            {
                key.SetValue(valueName, asDword, RegistryValueKind.DWord);
            }
            else
            {
                key.SetValue(valueName, rawValue, RegistryValueKind.String);
            }
        }

        key.Flush();
        return true;
    }

    private static bool TryDeleteValue(RegistryHive hive, string subKey, string valueName, out string error)
    {
        error = string.Empty;
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(subKey, writable: true);
        if (key is null)
        {
            // Key missing ⇒ value is already absent
            return true;
        }

        key.DeleteValue(valueName, throwOnMissingValue: false);
        key.Flush();
        return true;
    }

    private static bool StateMatches(RegistryRead read, string? intendedRaw)
    {
        var wantAbsent = string.IsNullOrWhiteSpace(intendedRaw);
        if (wantAbsent)
            return !read.Present;

        if (!read.Present || read.Normalized is null)
            return false;

        return string.Equals(
            read.Normalized.Trim(),
            intendedRaw!.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRaw(object raw, RegistryValueKind kind)
    {
        return kind switch
        {
            RegistryValueKind.DWord => Convert.ToInt32(raw, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture),
            RegistryValueKind.QWord => Convert.ToInt64(raw, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture),
            RegistryValueKind.Binary when raw is byte[] bytes => BitConverter.ToString(bytes),
            RegistryValueKind.MultiString when raw is string[] arr => string.Join(";", arr),
            _ => raw.ToString()?.Trim() ?? string.Empty
        };
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

        var path = mo.Observation?.Layers?
            .Select(l => l.SourcePath)
            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) &&
                                 (p.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) ||
                                  p.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) ||
                                  p.StartsWith("HKEY_", StringComparison.OrdinalIgnoreCase)));

        if (string.IsNullOrWhiteSpace(path))
            path = mo.DiscoveryMethod;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "No registry path is defined for this setting.";
            return false;
        }

        if (path.Contains("...", StringComparison.Ordinal) ||
            path.Contains('*') ||
            path.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase))
        {
            error = "This setting does not map to a single concrete registry value and cannot be changed safely here.";
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
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var c in s)
            if (c is < '0' or > '9') return false;
        return true;
    }

    private static string FormatHive(RegistryHive hive) =>
        hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
}
