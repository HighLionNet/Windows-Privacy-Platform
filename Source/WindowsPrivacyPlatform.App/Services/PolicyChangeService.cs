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
/// Controlled, catalog-backed system changes.
/// Contract:
/// - DEFAULT deny. Only settings with an explicit complete WritableTarget may be modified.
/// - Write target comes ONLY from WritableTarget (never from Observation or DiscoveryMethod).
/// - RegistryValueKind comes from WritableTarget (no guessing).
/// - Success is returned ONLY when an independent read-back of the exact target matches both value and kind.
/// - Read failures are never treated as "Not configured".
/// </summary>
public sealed class PolicyChangeService
{
    private readonly ElevationService _elevation;
    private readonly IAuditLogger _log;
    private readonly HighImpactStepUpService _stepUp;

    public PolicyChangeService(ElevationService elevation, IAuditLogger log)
    {
        _elevation = elevation ?? throw new ArgumentNullException(nameof(elevation));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _stepUp = new HighImpactStepUpService(_log);
    }

    /// <summary>
    /// Apply a catalog value. rawValue null/empty = delete value (Not configured) when SupportsDeletion.
    /// Returns true only when the system registry read-back matches the intended state including kind.
    /// </summary>
    public bool TryApply(ManagedObject mo, string? rawValue, Window? owner, out string message)
        => TryApplyCore(mo, rawValue, owner, skipConfirmation: false, out message);

    public bool TryApplyBatch(
        IReadOnlyList<PendingPolicyChange> changes,
        Window? owner,
        out IReadOnlyList<PolicyChangeOutcome> outcomes)
    {
        var results = new List<PolicyChangeOutcome>();
        outcomes = results;
        if (changes is null || changes.Count == 0)
            return false;
        if (changes.Count > 32 || changes.Any(c => c?.Setting is null) ||
            changes.Select(c => c.Setting.ObjectId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != changes.Count)
        {
            results.Add(new PolicyChangeOutcome(string.Empty, false, "The pending batch is malformed or exceeds 32 unique settings."));
            return false;
        }

        if (!ManagedObjectCatalog.HasValidAuthorizationHash())
        {
            results.Add(new PolicyChangeOutcome(string.Empty, false,
                "The installed authorization catalog failed its integrity check. No changes were made."));
            _log.Change("PolicyChangeService", "DENIED batch: authorization catalog hash mismatch.");
            return false;
        }

        var highImpact = changes.Where(change => CatalogImpact.RequiresStepUp(change.Setting, change.RawValue)).ToList();
        if (highImpact.Count > 0)
        {
            if (!BinaryIntegrityGuard.HighImpactAllowed)
            {
                results.Add(new PolicyChangeOutcome(string.Empty, false,
                    "The running binary does not match the last verified startup hash. High-impact changes are blocked."));
                _log.Change("PolicyChangeService", "result=Denied reason=BinaryIntegrity highImpact=true");
                return false;
            }
            if (!_stepUp.TryAuthorize(highImpact, owner))
            {
                foreach (var change in changes)
                    results.Add(new PolicyChangeOutcome(change.Setting.ObjectId, false, "High-impact verification was cancelled or denied."));
                return false;
            }
        }

        var preview = new List<ChangeConfirmationItem>();
        foreach (var change in changes)
        {
            if (!TryPrepare(change.Setting, change.RawValue, out var before, out var error))
            {
                results.Add(new PolicyChangeOutcome(change.Setting.ObjectId, false, error));
                return false;
            }
            var intended = string.IsNullOrWhiteSpace(change.RawValue) ? "Not configured" : OptionLabel(change.Setting, change.RawValue!);
            preview.Add(new ChangeConfirmationItem(change.Setting.ObjectName, before, intended));
        }

        var confirmation = new ChangeConfirmationDialog(preview) { Owner = owner };
        if (confirmation.ShowDialog() != true)
        {
            foreach (var change in changes)
                results.Add(new PolicyChangeOutcome(change.Setting.ObjectId, false, "Change cancelled."));
            return false;
        }

        foreach (var change in changes)
        {
            var success = TryApplyCore(change.Setting, change.RawValue, owner, skipConfirmation: true, out var message);
            results.Add(new PolicyChangeOutcome(change.Setting.ObjectId, success, message));
        }

        return results.All(r => r.Success);
    }

    private bool TryApplyCore(ManagedObject mo, string? rawValue, Window? owner, bool skipConfirmation, out string message)
    {
        message = string.Empty;

        if (mo is null)
        {
            message = "Setting is null.";
            return false;
        }

        if (!_elevation.IsAdminAuthorized)
        {
            message = "Administrator mode is not authorized for this session.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: Administrator mode not authorized.");
            return false;
        }

        if (!skipConfirmation && CatalogImpact.RequiresStepUp(mo, rawValue))
        {
            if (!BinaryIntegrityGuard.HighImpactAllowed)
            {
                message = "The running binary failed the startup integrity check. High-impact changes are blocked.";
                return false;
            }
            if (!_stepUp.TryAuthorize([new PendingPolicyChange(mo, rawValue)], owner))
            {
                message = "High-impact verification was cancelled or denied.";
                return false;
            }
        }

        if (!ManagedObjectCatalog.HasValidAuthorizationHash())
        {
            message = "The installed authorization catalog failed its integrity check. No change was made.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: authorization catalog hash mismatch.");
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

        // Respect RequiresElevation from the explicit target.
        if (target.RequiresElevation && !ElevationService.IsProcessElevated())
        {
            message = "This setting requires elevation and the process is not elevated.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: RequiresElevation but process not elevated.");
            return false;
        }

        if (!mo.IsApplicableHere)
        {
            message = "This setting cannot be changed on the scanned device. " + mo.ApplicabilityReason;
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: not applicable on this device.");
            return false;
        }
        if (!_elevation.CanModifyHive(target.Hive, out var identityError))
        {
            message = identityError;
            return false;
        }
        if (!ManagedObjectCatalog.IsAuthorizedWriteTarget(mo))
        {
            message = "Modification is not authorized by the installed catalog.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: runtime target differs from catalog authorization.");
            return false;
        }

        // Individual firewall rules remain outside the write boundary. Only curated profile values proceed.
        if (mo.ProductDomain == ProductDomain.Firewall && mo.FeatureCategory != FeatureCategory.FirewallProfile)
        {
            message = "Individual firewall rules are view-only. Use Windows Firewall with Advanced Security for rule engineering.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: firewall-rule boundary.");
            return false;
        }

        if (target.Kind != WritableTargetKind.Registry)
        {
            message = "Only verified registry policies can be changed in this release.";
            _log.Change("PolicyChangeService", $"DENIED {mo.ObjectId}: non-registry target {target.Kind}.");
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
        if (before.Status == RegistryReadStatus.AccessDenied || before.Status == RegistryReadStatus.Error)
        {
            message = $"Cannot read current system state for verification ({before.Status}). Change refused.";
            _log.Change("PolicyChangeService", $"PRE_READ_FAIL {mo.ObjectId} | {targetPath} | status={before.Status}");
            return false;
        }

        _log.Change("PolicyChangeService", AuditContext.Change(
            mo.ObjectId, mo.ObjectName, FormatRead(before), intendedDisplay, "Prepared", targetPath));

        if (!skipConfirmation)
        {
            var confirm = MessageBox.Show(
                owner,
                $"Change setting:\n\n" +
                $"{mo.ObjectName}\n\n" +
                $"Current (system): {FormatRead(before)}\n" +
                $"New: {OptionLabel(mo, rawValue)}\n\n" +
                "Technical target and raw value are available in the setting disclosure and audit record.\n" +
                "The change is accepted only if a fresh read-back matches value and type.\n\nContinue?",
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
        }

        if (StateMatches(before, rawValue, target.ValueKind))
        {
            message = $"Already at intended state (verified): {intendedDisplay}";
            _log.Change("PolicyChangeService", AuditContext.Change(
                mo.ObjectId, mo.ObjectName, FormatRead(before), intendedDisplay, "Verified", "Already matched"));
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

                // A read error after write is verification failure, not success.
                if (after.Status == RegistryReadStatus.AccessDenied || after.Status == RegistryReadStatus.Error)
                {
                    _log.Change("PolicyChangeService",
                        $"VERIFY_READ_FAIL {mo.ObjectId} | attempt={attempt} | status={after.Status}");
                    break;
                }

                verified = StateMatches(after, rawValue, target.ValueKind);
                if (verified)
                    break;

                System.Threading.Thread.Sleep(40 * attempt);
            }

            _log.Change("PolicyChangeService", AuditContext.Change(
                mo.ObjectId, mo.ObjectName, FormatRead(before), FormatRead(after),
                verified ? "ReadBackMatched" : "ReadBackMismatch", targetPath));

            if (!verified)
            {
                message =
                    "Write completed but verification FAILED.\n" +
                    $"Intended: {intendedDisplay}\n" +
                    $"System read-back: {FormatRead(after)}\n\n" +
                    "The application will not report this change as successful.";
                _log.Change("PolicyChangeService",
                    $"VERIFY_FAIL {mo.ObjectId} | intended={intendedDisplay} | actual={FormatRead(after)}");
                return false;
            }

            message = intendedDelete
                ? "Verified: value is absent (Not configured)."
                : $"Verified: system value is {after.Normalized} ({after.Kind}).";
            _log.Change("PolicyChangeService", AuditContext.Change(
                mo.ObjectId, mo.ObjectName, FormatRead(before), FormatRead(after), "Verified", targetPath));
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            message = "Access denied. Process may not be fully elevated.";
            _log.Change("PolicyChangeService", $"ACCESS_DENIED {mo.ObjectId}");
            return false;
        }
        catch (Exception ex)
        {
            message = "The system change failed. No success was reported; review the audit category and rescan.";
            _log.Change("PolicyChangeService", $"EXCEPTION {mo.ObjectId}: category={ex.GetType().Name}");
            return false;
        }
    }

    private bool TryPrepare(ManagedObject item, string? rawValue, out string beforeDisplay, out string error)
    {
        beforeDisplay = "Unknown";
        error = string.Empty;
        if (!_elevation.IsAdminAuthorized)
        {
            error = "Administrator mode is not authorized for this session.";
            return false;
        }
        if (!ManagedObjectCatalog.HasValidAuthorizationHash())
        {
            error = "The installed authorization catalog failed its integrity check.";
            return false;
        }
        var target = item.WritableTarget;
        if (target is null || !target.IsComplete || target.Kind != WritableTargetKind.Registry)
        {
            error = "The setting has no complete, authorized registry target.";
            return false;
        }
        if (!_elevation.CanModifyHive(target.Hive, out error))
            return false;
        if (!ManagedObjectCatalog.IsAuthorizedWriteTarget(item))
        {
            error = "The runtime target does not match the installed catalog authorization.";
            return false;
        }
        if (!item.IsApplicableHere)
        {
            error = "The setting is not applicable on this Windows edition or build.";
            return false;
        }
        if (target.RequiresElevation && !ElevationService.IsProcessElevated())
        {
            error = "This batch requires an elevated Administrator session.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(rawValue) && !target.SupportsDeletion)
        {
            error = "Not configured is not supported for this setting.";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(rawValue) &&
            !target.SupportedRawValues.Contains(rawValue, StringComparer.OrdinalIgnoreCase))
        {
            error = "The selected value is outside the setting allowlist.";
            return false;
        }
        if (!TryParseHive(target.Hive, out var hive))
        {
            error = "The target hive is invalid.";
            return false;
        }
        var before = ReadValue(hive, MapView(target.View), target.SubKey.Trim(), target.ValueName.Trim());
        beforeDisplay = FormatRead(before);
        if (before.Status is RegistryReadStatus.AccessDenied or RegistryReadStatus.Error)
        {
            error = "The current system state could not be read, so the batch was not started.";
            return false;
        }
        return true;
    }

    private static string OptionLabel(ManagedObject item, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return "Not configured";
        var meaning = item.ValueSemantics.FirstOrDefault(v =>
            string.Equals(v.RawValue, rawValue, StringComparison.OrdinalIgnoreCase));
        return meaning is null ? "Selected supported value" : SettingOptionLanguage.For(item, meaning).Action;
    }

    // ---------- registry primitives (exact view) ----------

    private enum RegistryReadStatus
    {
        Present,
        Absent,
        AccessDenied,
        Error
    }

    private readonly struct RegistryRead
    {
        public RegistryReadStatus Status { get; init; }
        public RegistryValueKind Kind { get; init; }
        public string? Normalized { get; init; }
        public object? Raw { get; init; }

        public bool Present => Status == RegistryReadStatus.Present;
    }

    private static RegistryRead ReadValue(RegistryHive hive, RegistryView view, string subKey, string valueName)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(subKey, writable: false);
            if (key is null)
                return new RegistryRead { Status = RegistryReadStatus.Absent };

            var raw = key.GetValue(valueName, defaultValue: null, options: RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is null)
            {
                var names = key.GetValueNames();
                var exists = names.Any(n => string.Equals(n, valueName, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                    return new RegistryRead { Status = RegistryReadStatus.Absent };

                // Value name exists but raw is null — treat as absent for practical purposes.
                return new RegistryRead { Status = RegistryReadStatus.Absent };
            }

            var kind = key.GetValueKind(valueName);
            return new RegistryRead
            {
                Status = RegistryReadStatus.Present,
                Kind = kind,
                Raw = raw,
                Normalized = NormalizeRaw(raw, kind)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new RegistryRead { Status = RegistryReadStatus.AccessDenied };
        }
        catch (System.Security.SecurityException)
        {
            return new RegistryRead { Status = RegistryReadStatus.AccessDenied };
        }
        catch
        {
            return new RegistryRead { Status = RegistryReadStatus.Error };
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

    /// <summary>
    /// Value + kind must both match. Textual match with wrong RegistryValueKind is failure.
    /// </summary>
    private static bool StateMatches(RegistryRead read, string? intendedRaw, RegistryValueKindExpected expectedKind)
    {
        var wantAbsent = string.IsNullOrWhiteSpace(intendedRaw);

        if (wantAbsent)
            return read.Status == RegistryReadStatus.Absent;

        if (read.Status != RegistryReadStatus.Present || read.Normalized is null)
            return false;

        // Kind must match expected WritableTarget kind.
        if (!KindMatches(read.Kind, expectedKind))
            return false;

        return string.Equals(
            read.Normalized.Trim(),
            intendedRaw!.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool KindMatches(RegistryValueKind actual, RegistryValueKindExpected expected) => expected switch
    {
        RegistryValueKindExpected.DWord => actual == RegistryValueKind.DWord,
        RegistryValueKindExpected.QWord => actual == RegistryValueKind.QWord,
        RegistryValueKindExpected.String => actual == RegistryValueKind.String,
        RegistryValueKindExpected.ExpandString => actual == RegistryValueKind.ExpandString,
        _ => false
    };

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

    private static string FormatRead(RegistryRead read) => read.Status switch
    {
        RegistryReadStatus.Present => $"{read.Normalized} ({read.Kind})",
        RegistryReadStatus.Absent => "Not configured",
        RegistryReadStatus.AccessDenied => "Access denied",
        RegistryReadStatus.Error => "Read error",
        _ => "Unknown"
    };

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
