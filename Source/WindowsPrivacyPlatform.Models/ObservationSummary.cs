namespace WindowsPrivacyPlatform.Models;

/// <summary>
/// Read-only aggregate of catalog observations against a live inventory snapshot.
/// Pure data — populated by the report/bind pipeline, not by collectors.
/// </summary>
public class ObservationSummary
{
    public int CatalogTotal { get; set; }
    public int ObservedCount { get; set; }
    public int NotObservedCount { get; set; }
    public int ConfiguredPolicyCount { get; set; }
    public int NotConfiguredPolicyCount { get; set; }

    public int HighRiskCount { get; set; }
    public int MediumRiskCount { get; set; }
    public int LowRiskCount { get; set; }

    public int PrivacyAllowCount { get; set; }
    public int PrivacyDenyCount { get; set; }
    public int PrivacyPromptCount { get; set; }

    public int CatalogValidationPassed { get; set; }
    public int CatalogValidationFailed { get; set; }

    public List<ObservedItem> HighRiskItems { get; set; } = new();
    public List<ObservedItem> MediumRiskItems { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class ObservedItem
{
    public string ObjectId { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public ProductDomain ProductDomain { get; set; }
    public string SubCategory { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public string CurrentState { get; set; } = string.Empty;
}
