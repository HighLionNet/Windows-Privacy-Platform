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
/// Contract:
/// - DEFAULT deny. Only settings with an explicit complete WritableTarget may be modified.
/// - Write target comes ONLY from WritableTarget (never from Observation or DiscoveryMethod).
/// - RegistryValueKind comes from WritableTarget (no guessing from user text).
/// - Success is returned ONLY when an independent read-back of the exact target matches.
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
    /// Apply a catalog value. rawValue null/empty = delete value (Not configured) when SupportsDeletion.
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

        // ---- DENY BY DEFAULT: require explicit WritableTarget ----
        var target = mo.WritableTarget;
        if (target is null || !target.IsComplete)
        {
            message = "Modification is not supported for this setting (no explicit write target in catalog).";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: no WritableTarget.");
            return false;
        }

        // Firewall domain hard boundary: refuse unless explicitly marked (future profile writes only)
        if (mo.ProductDomain == ProductDomain.Firewall)
        {
            message = "Firewall rule and profile mutation through WPP is restricted. Use native Windows Firewall tools for rules.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: firewall domain boundary.");
            return false;
        }

        if (!TryParseHive(target.Hive, out var hive))
        {
            message = "Invalid hive on WritableTarget: " + target.Hive;
            return false;
        }

        var view = MapView(target.View);
        var subKey = target.SubKey.Trim();
        var valueName = target.ValueName.Trim();
        var targetPath = $"{FormatHive(hive)}\\{subKey}\\{valueName}";

        var intendedDelete = string.IsNullOrWhiteSpace(rawValue);
        if (intendedDelete && !target.SupportsDeletion)
        {
            message = "Deletion (Not configured) is not supported for this setting.";
            return false;
        }

        if (!intendedDelete)
        {
            // Supported raw values gate
            if (target.SupportedRawValues.Count > 0 &&
                !target.SupportedRawValues.Any(v => string.Equals(v, rawValue, StringComparison.OrdinalIgnoreCase)))
            {
                message = $"Value '{rawValue}' is not in the supported set for this setting.";
                return false;
            }

            if (target.ValueKind == RegistryValueKindExpected.Unsupported)
            {
                message = "This setting uses an unsupported registry value type and cannot be changed.";
                return false;
            }
        }

        var intendedDisplay = intendedDelete ? "(absent / Not configured)" : rawValue!;

        // --- Pre-read exact target ---
        var before = ReadValue(hive, view, subKey, valueName);
        _log.Change("PolicyChangeService",
            $"BEFORE {mo.ObjectId} | {targetPath} | present={before.Present} kind={before.Kind} value={before.Normalized ?? "(absent)"}");

        var confirm = MessageBox.Show(
            owner,
            $"Change setting:\n\n" +
            $"{mo.ObjectName}\n" +
            $"ObjectId: {mo.ObjectId}\n\n" +
            $"Registry: {targetPath} ({target.View})\n" +
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

        if (StateMatches(before, rawValue, target.ValueKind))
        {
            message = $"Already at intended state (verified): {intendedDisplay}";
            _log.Change("PolicyChangeService", $"NOOP_VERIFIED {mo.ObjectId} | {targetPath} = {intendedDisplay}");
            return true;
        }

        try
        {
            if (intendedDelete)
            {
                if (!TryDeleteValue(hive, view, subKey, valueName, out var delError))
                {
                    message = delError;
                    _log.Change("PolicyChangeService", $"DELETE_FAIL {mo.ObjectId}: {delError}");
                    return false;
                }
            }
            else
            {
                if (!TryWriteValue(hive, view, subKey, valueName, rawValue!, target.ValueKind, out var writeError))
                {
                    message = writeError;
                    _log.Change("PolicyChangeService", $"WRITE_FAIL {mo.ObjectId}: {writeError}");
                    return false;
                }
            }

            // --- Mandatory independent read-back of the EXACT same target ---
            RegistryRead after = default;
            var verified = false;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                after = ReadValue(hive, view, subKey, valueName);
                verified = StateMatches(after, rawValue, target.ValueKind);
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

    // ---------- registry primitives (exact view) ----------

    private readonly struct RegistryRead
    {
        public bool Present { get; init; }
        public RegistryValueKind Kind { get; init; }
        public string? Normalized { get; init; }
        public object? Raw { get; init; }
    }

    private static RegistryRead ReadValue(RegistryHive hive, RegistryView view, string subKey, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key is null)
                return new RegistryRead { Present = false };

            var raw = key.GetValue(valueName, defaultValue: null, options: RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is null)
            {
                // Distinguish missing value vs null default
                var names = key.GetValueNames();
                var exists = names.Any(n => string.Equals(n, valueName, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    return new RegistryRead { Present = false };
            }

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
            return new RegistryRead { Present = false };
        }
    }

    private static bool TryWriteValue(
        RegistryHive hive,
        RegistryView view,
        string subKey,
        string valueName,
        string rawValue,
        RegistryValueKindExpected expectedKind,
        out string error)
    {
        error = string.Empty;
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.CreateSubKey(subKey, writable: true);
        if (key is null)
        {
            error = "Could not open or create registry key.";
            return false;
        }

        switch (expectedKind)
        {
            case RegistryValueKindExpected.DWord:
            {
                if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dword))
                {
                    error = "Invalid DWord value (invariant integer required).";
                    return false;
                }
                key.SetValue(valueName, dword, RegistryValueKind.DWord);
                break;
            }
            case RegistryValueKindExpected.QWord:
            {
                if (!long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qword))
                {
                    error = "Invalid QWord value (invariant integer required).";
                    return false;
                }
                key.SetValue(valueName, qword, RegistryValueKind.QWord);
                break;
            }
            case RegistryValueKindExpected.String:
            case RegistryValueKindExpected.ExpandString:
            {
                var kind = expectedKind == RegistryValueKindExpected.ExpandString
                    ? RegistryValueKind.ExpandString
                    : RegistryValueKind.String;
                key.SetValue(valueName, rawValue, kind);
                break;
            }
            default:
                error = "Unsupported registry value kind.";
                return false;
        }

        key.Flush();
        return true;
    }

    private static bool TryDeleteValue(RegistryHive hive, RegistryView view, string subKey, string valueName, out string error)
    {
        error = string.Empty;
        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        using var key = baseKey.OpenSubKey(subKey, writable: true);
        if (key is null)
            return true; // already absent

        key.DeleteValue(valueName, throwOnMissingValue: false);
        key.Flush();
        return true;
    }

    private static bool StateMatches(RegistryRead read, string? intendedRaw, RegistryValueKindExpected expectedKind)
    {
        var wantAbsent = string.IsNullOrWhiteSpace(intendedRaw);
        if (wantAbsent)
            return !read.Present;

        if (!read.Present || read.Normalized is null)
            return false;

        // Type-aware comparison using invariant normalization
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

    private static bool TryParseHive(string hive, out RegistryHive result)
    {
        result = RegistryHive.LocalMachine;
        if (string.IsNullOrWhiteSpace(hive))
            return false;

        var h = hive.Trim();
        if (h.Equals("HKLM", StringComparison.OrdinalIgnoreCase) ||
            h.Equals("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
        {
            result = RegistryHive.LocalMachine;
            return true;
        }
        if (h.Equals("HKCU", StringComparison.OrdinalIgnoreCase) ||
            h.Equals("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
        {
            result = RegistryHive.CurrentUser;
            return true;
        }
        return false;
    }

    private static RegistryView MapView(RegistryViewKind kind) => kind switch
    {
        RegistryViewKind.Registry32 => RegistryView.Registry32,
        RegistryViewKind.Default => RegistryView.Default,
        _ => RegistryView.Registry64
    };

    private static string FormatHive(RegistryHive hive) =>
        hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
}
