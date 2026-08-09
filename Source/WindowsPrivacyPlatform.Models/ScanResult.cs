namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Result of a full inventory scan including per-collector diagnostics.
/// Success alone is insufficient; inspect Status and CollectorResults.
/// </summary>
public class ScanResult
{
    public InventorySnapshot Snapshot { get; set; } = new();

    /// <summary>True only when the overall scan completed without fatal failure.</summary>
    public bool Success { get; set; }

    public ScanStatus Status { get; set; } = ScanStatus.Unknown;

    public string Message { get; set; } = string.Empty;

    public List<string> Warnings { get; set; } = new();

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public TimeSpan Duration => EndUtc >= StartUtc ? EndUtc - StartUtc : TimeSpan.Zero;

    public List<CollectorDiagnostic> CollectorResults { get; set; } = new();

    public bool HasCollectorFailures =>
        CollectorResults.Any(c => c.Status is CollectorStatus.Failed or CollectorStatus.Error or CollectorStatus.AccessDenied);

    public bool IsPartial =>
        Status is ScanStatus.Partial or ScanStatus.CompletedWithWarnings || HasCollectorFailures;
}

public enum ScanStatus
{
    Unknown = 0,
    Completed = 1,
    CompletedWithWarnings = 2,
    Partial = 3,
    Canceled = 4,
    Failed = 5,
    Unavailable = 6
}

public enum CollectorStatus
{
    Unknown = 0,
    Completed = 1,
    CompletedEmpty = 2,
    Partial = 3,
    Failed = 4,
    Error = 5,
    AccessDenied = 6,
    Timeout = 7,
    Canceled = 8,
    Unsupported = 9,
    Skipped = 10
}

public sealed class CollectorDiagnostic
{
    public string CollectorName { get; set; } = string.Empty;
    public CollectorStatus Status { get; set; } = CollectorStatus.Unknown;
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int ItemCount { get; set; }
    public string? ErrorCategory { get; set; }
}
