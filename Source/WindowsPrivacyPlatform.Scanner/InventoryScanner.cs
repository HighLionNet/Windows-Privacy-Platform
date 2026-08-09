// Source/WindowsPrivacyPlatform.Scanner/InventoryScanner.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

public sealed class InventoryScanner : IInventoryScanner
{
    private readonly IAuditLogger _logger;
    private readonly IReadOnlyList<IInventoryCollector> _collectors;

    public InventoryScanner(IAuditLogger logger, IEnumerable<IInventoryCollector> collectors)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (collectors is null) throw new ArgumentNullException(nameof(collectors));

        _collectors = collectors.ToList().AsReadOnly();
    }

    public ScanResult Scan() => Scan(CancellationToken.None);

    public ScanResult Scan(CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        _logger.Info("Scanner", "Scan start");

        var snapshot = new InventorySnapshot();
        var diagnostics = new List<CollectorDiagnostic>();
        var warnings = new List<string>();

        try
        {
            foreach (var collector in _collectors)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var diag = new CollectorDiagnostic { CollectorName = collector.Name };
                var sw = Stopwatch.StartNew();

                try
                {
                    _logger.Debug("Scanner", $"Collector start: {collector.Name}");
                    collector.Collect(snapshot);
                    sw.Stop();

                    diag.Duration = sw.Elapsed;
                    diag.Status = CollectorStatus.Completed;
                    diag.Message = "Completed";
                    diag.ItemCount = EstimateItemCount(snapshot, collector.Name);

                    _logger.Debug("Scanner", $"Collector finish: {collector.Name} ({diag.Duration.TotalMilliseconds:F0} ms)");
                }
                catch (OperationCanceledException)
                {
                    sw.Stop();
                    diag.Duration = sw.Elapsed;
                    diag.Status = CollectorStatus.Canceled;
                    diag.Message = "Canceled";
                    diagnostics.Add(diag);
                    throw;
                }
                catch (UnauthorizedAccessException ex)
                {
                    sw.Stop();
                    diag.Duration = sw.Elapsed;
                    diag.Status = CollectorStatus.AccessDenied;
                    diag.Message = "Access denied";
                    diag.ErrorCategory = "UnauthorizedAccess";
                    warnings.Add($"{collector.Name}: access denied");
                    _logger.Error("Scanner", $"{collector.Name} access denied: {ex.Message}");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    diag.Duration = sw.Elapsed;
                    diag.Status = CollectorStatus.Error;
                    diag.Message = "Collector error";
                    diag.ErrorCategory = ex.GetType().Name;
                    warnings.Add($"{collector.Name}: {ex.GetType().Name}");
                    _logger.Error("Scanner", $"{collector.Name} failed: {ex.Message}");
                }

                diagnostics.Add(diag);
            }

            var end = DateTime.UtcNow;
            var hasFailures = diagnostics.Any(d =>
                d.Status is CollectorStatus.Failed or CollectorStatus.Error or CollectorStatus.AccessDenied or CollectorStatus.Timeout);

            var status = hasFailures
                ? (diagnostics.All(d => d.Status is CollectorStatus.Error or CollectorStatus.Failed)
                    ? ScanStatus.Failed
                    : ScanStatus.CompletedWithWarnings)
                : ScanStatus.Completed;

            _logger.Info("Scanner", $"Scan finish status={status}");

            return new ScanResult
            {
                Success = status is ScanStatus.Completed or ScanStatus.CompletedWithWarnings,
                Status = status,
                Snapshot = snapshot,
                Message = status == ScanStatus.Completed
                    ? "Scan completed successfully."
                    : $"Scan completed with warnings ({diagnostics.Count(d => d.Status != CollectorStatus.Completed)} collector issues).",
                Warnings = warnings,
                StartUtc = start,
                EndUtc = end,
                CollectorResults = diagnostics
            };
        }
        catch (OperationCanceledException)
        {
            _logger.Info("Scanner", "Scan canceled");
            return new ScanResult
            {
                Success = false,
                Status = ScanStatus.Canceled,
                Snapshot = snapshot,
                Message = "Scan canceled.",
                Warnings = warnings,
                StartUtc = start,
                EndUtc = DateTime.UtcNow,
                CollectorResults = diagnostics
            };
        }
        catch (Exception ex)
        {
            _logger.Error("Scanner", $"Scan failed: {ex.Message}");
            return new ScanResult
            {
                Success = false,
                Status = ScanStatus.Failed,
                Snapshot = snapshot,
                Message = ex.Message,
                Warnings = warnings,
                StartUtc = start,
                EndUtc = DateTime.UtcNow,
                CollectorResults = diagnostics
            };
        }
    }

    private static int EstimateItemCount(InventorySnapshot snapshot, string collectorName)
    {
        return collectorName switch
        {
            "CapabilityCollector" => snapshot.InstalledCapabilities?.Count ?? 0,
            "PackageCollector" => snapshot.InstalledPackages?.Count ?? 0,
            "ServiceCollector" => snapshot.Services?.Count ?? 0,
            "ScheduledTaskCollector" => snapshot.ScheduledTasks?.Count ?? 0,
            "PrivacyCollector" => snapshot.PrivacySettings?.Count ?? 0,
            "PolicyCollector" => snapshot.PolicySettings?.Count ?? 0,
            _ => 0
        };
    }
}
