using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsPrivacyPlatform.KnowledgeBase;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;
using WindowsPrivacyPlatform.Validator;

namespace WindowsPrivacyPlatform.App.Services;

/// <summary>
/// Presentation-side composition host for the scan pipeline.
/// Maintains last completed valid scan separately from in-progress / failed attempts.
/// </summary>
public sealed class ScanService
{
    public const string CatalogSchemaVersion = ManagedObjectCatalog.CatalogVersion;
    public const string KnowledgeBaseVersionValue = ManagedObjectCatalog.CatalogVersion;

    public IReadOnlyList<ManagedObject> Catalog { get; private set; } = Array.Empty<ManagedObject>();
    public SettingsQuery? Query { get; private set; }
    public MachineOverview? Overview { get; private set; }
    public ObservationSummary? Summary { get; private set; }
    public NavigationNode? NavigationRoot { get; private set; }
    public int ValidationPassed { get; private set; }
    public int ValidationFailed { get; private set; }
    public IReadOnlyList<ValidationResult> ValidationResults { get; private set; } = Array.Empty<ValidationResult>();
    public string LastError { get; private set; } = string.Empty;
    public ScanResult? LastScanResult { get; private set; }
    public bool HasScan => Overview is not null;
    public IReadOnlyList<ManagedObject> SettingsCatalog => Catalog.Where(m => m.Bucket == CatalogBucket.Settings).ToList();
    public IReadOnlyList<ManagedObject> InventoryCatalog => Catalog.Where(m => m.Bucket == CatalogBucket.SystemInventory).ToList();

    // Last known-good completed scan (not replaced by canceled/failed runs).
    private MachineOverview? _lastGoodOverview;
    private ObservationSummary? _lastGoodSummary;
    private IReadOnlyList<ManagedObject>? _lastGoodCatalog;

    public event Action<string>? StatusChanged;
    public event Action? ScanCompleted;

    public Task RunScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RunScanCore(cancellationToken), cancellationToken);
    }

    private void RunScanCore(CancellationToken cancellationToken)
    {
        try
        {
            Report("Starting scan…");
            var logger = new AuditLogger();
            logger.Info("App", "GUI pipeline start");

            var knowledgeBase = new InMemoryKnowledgeBaseRepository();

            IEnumerable<IInventoryCollector> collectors = new IInventoryCollector[]
            {
                new WindowsIdentityCollector(),
                new CapabilityCollector(),
                new PackageCollector(),
                new ServiceCollector(),
                new ScheduledTaskCollector(),
                new PrivacyCollector(),
                new PolicyCollector(),
                new FirewallCollector()
            };

            cancellationToken.ThrowIfCancellationRequested();
            Report("Collecting inventory…");

            var scanner = new InventoryScanner(logger, collectors);
            var validator = new SchemaValidator(logger);

            var scanResult = scanner.Scan(cancellationToken);
            LastScanResult = scanResult;

            InventorySnapshot? snapshot = null;
            if (scanResult.Status is ScanStatus.Completed or ScanStatus.CompletedWithWarnings or ScanStatus.Partial
                && scanResult.Snapshot is not null)
            {
                snapshot = scanResult.Snapshot;
            }
            else if (scanResult.Status == ScanStatus.Canceled)
            {
                LastError = "Scan canceled.";
                Report("Scan cancelled.");
                // Do not replace last good scan.
                RestoreLastGoodIfNeeded();
                ScanCompleted?.Invoke();
                return;
            }
            else
            {
                LastError = scanResult.Message ?? "Scan returned no snapshot.";
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report("Loading knowledge catalog…");

            // Fresh catalog instances for this scan (avoid stale observation mutation across scans).
            var catalog = ManagedObjectCatalog.All.Select(CloneDefinition).ToList();

            if (snapshot is not null)
            {
                Report("Binding observations…");
                InventoryStateBinder.Bind(snapshot, catalog);

                Report("Building system inventory…");
                catalog.AddRange(DynamicInventoryCatalog.Create(snapshot, catalog));

                foreach (var item in catalog)
                {
                    if (item.IsDynamicInventory)
                        continue;
                    var applicability = ApplicabilityEvaluator.Evaluate(
                        item,
                        snapshot.Identity.WindowsVersion,
                        snapshot.Identity.Edition,
                        snapshot.Identity.BuildNumber);
                    item.Applicability = applicability.State;
                    item.ApplicabilityReason = applicability.Reason;

                    if (item.WritableTarget?.Kind is WritableTargetKind.Service or
                        WritableTargetKind.ScheduledTask or WritableTargetKind.AppxPackage &&
                        item.CurrentState?.Equals("Not installed", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        item.Applicability = ApplicabilityState.NotPresentOnDevice;
                        item.ApplicabilityReason = "This curated component is not installed on the scanned device.";
                    }
                }
            }

            foreach (var managedObject in catalog)
            {
                knowledgeBase.Add(new KnowledgeBaseEntry
                {
                    ObjectId = managedObject.ObjectId,
                    Object = managedObject,
                    Metadata = new KnowledgeBaseMetadata
                    {
                        Source = "ManagedObjectCatalog",
                        SourceReliabilityScore = managedObject.ConfidenceScore
                    }
                });
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report("Validating catalog…");

            var validationResults = validator.ValidateAll(knowledgeBase.GetAll());
            ValidationResults = validationResults;
            ValidationPassed = validationResults.Count(r => r.IsValid);
            ValidationFailed = validationResults.Count(r => !r.IsValid);

            Catalog = catalog;
            Query = new SettingsQuery(catalog);
            NavigationRoot = NavigationBuilder.BuildDomainTree(catalog.Where(m => m.Bucket == CatalogBucket.Settings).ToList());

            if (snapshot is not null)
            {
                Overview = MachineOverview.FromSnapshot(snapshot, catalog.Count);
                Overview.CatalogVersion = CatalogSchemaVersion;
                Overview.KnowledgeBaseVersion = KnowledgeBaseVersionValue;
                Summary = InventoryStateBinder.BuildSummary(snapshot, catalog, ValidationPassed, ValidationFailed);

                // Promote to last-good only on successful / warning completion.
                if (scanResult.Status is ScanStatus.Completed or ScanStatus.CompletedWithWarnings)
                {
                    _lastGoodOverview = Overview;
                    _lastGoodSummary = Summary;
                    _lastGoodCatalog = catalog;
                }

                var msg = scanResult.Status == ScanStatus.CompletedWithWarnings
                    ? "Scan complete (with collector warnings)."
                    : "Scan complete.";
                Report(msg);
            }
            else
            {
                // Failed scan: keep previous good data visible if available.
                RestoreLastGoodIfNeeded();
                if (Overview is null)
                {
                    Overview = new MachineOverview
                    {
                        CatalogVersion = CatalogSchemaVersion,
                        KnowledgeBaseVersion = KnowledgeBaseVersionValue,
                        LastScanUtc = DateTime.UtcNow,
                        IdentityCollectionNotes = LastError
                    };
                }
                Report(string.IsNullOrEmpty(LastError) ? "Scan finished with no snapshot." : $"Scan incomplete: {LastError}");
            }

            logger.Info("App", "GUI pipeline complete");
            ScanCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            LastError = "Scan canceled.";
            Report("Scan cancelled.");
            RestoreLastGoodIfNeeded();
            ScanCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Report($"Scan error: {ex.Message}");
            RestoreLastGoodIfNeeded();
            ScanCompleted?.Invoke();
        }
    }

    private void RestoreLastGoodIfNeeded()
    {
        if (_lastGoodOverview is not null)
        {
            Overview = _lastGoodOverview;
            Summary = _lastGoodSummary;
            if (_lastGoodCatalog is not null)
            {
                Catalog = _lastGoodCatalog;
                Query = new SettingsQuery(_lastGoodCatalog);
                NavigationRoot = NavigationBuilder.BuildDomainTree(_lastGoodCatalog.Where(m => m.Bucket == CatalogBucket.Settings).ToList());
            }
        }
    }

    /// <summary>
    /// Shallow definition clone so runtime Observation on one scan cannot contaminate the static catalog
    /// or another scan's objects. WritableTarget is reference-shared (immutable contract).
    /// </summary>
    private static ManagedObject CloneDefinition(ManagedObject src)
    {
        return new ManagedObject
        {
            ObjectId = src.ObjectId,
            ObjectName = src.ObjectName,
            ObjectType = src.ObjectType,
            CanonicalPath = src.CanonicalPath,
            TechnicalLocation = src.TechnicalLocation,
            FeatureCategory = src.FeatureCategory,
            ProductDomain = src.ProductDomain,
            SubCategory = src.SubCategory,
            RiskLevel = src.RiskLevel,
            ImpactLevel = src.ImpactLevel,
            MinimumBuild = src.MinimumBuild,
            MaximumBuild = src.MaximumBuild,
            SupportedEditions = src.SupportedEditions,
            SupportedWindowsVersions = src.SupportedWindowsVersions,
            Description = src.Description,
            Rationale = src.Rationale,
            References = src.References,
            WhenIgnored = src.WhenIgnored,
            CommonMisconception = src.CommonMisconception,
            TypicalEnterpriseUse = src.TypicalEnterpriseUse,
            ConsumerImpact = src.ConsumerImpact,
            Narrative = src.Narrative,
            ValueSemantics = src.ValueSemantics,
            InterfaceName = src.InterfaceName,
            InterfaceScope = src.InterfaceScope,
            ConfigurationType = src.ConfigurationType,
            TargetValue = src.TargetValue,
            BuildConstraint = src.BuildConstraint,
            EditionConstraint = src.EditionConstraint,
            ComponentConstraint = src.ComponentConstraint,
            HardwareConstraint = src.HardwareConstraint,
            SoftwareConstraint = src.SoftwareConstraint,
            VirtualizationConstraint = src.VirtualizationConstraint,
            DiscoveryMethod = src.DiscoveryMethod,
            ComplianceMethod = src.ComplianceMethod,
            RemediationMethod = src.RemediationMethod,
            RemediationScope = src.RemediationScope,
            Reversibility = src.Reversibility,
            BackupRequired = src.BackupRequired,
            RebootRequirement = src.RebootRequirement,
            PriorityLevel = src.PriorityLevel,
            ControlLevel = src.ControlLevel,
            ComponentOwner = src.ComponentOwner,
            PrivacyImpact = src.PrivacyImpact,
            SecurityImpact = src.SecurityImpact,
            PerformanceImpact = src.PerformanceImpact,
            UserExperienceImpact = src.UserExperienceImpact,
            SystemStabilityImpact = src.SystemStabilityImpact,
            KnownSideEffects = src.KnownSideEffects,
            CumulativeUpdateBehavior = src.CumulativeUpdateBehavior,
            FeatureUpdateBehavior = src.FeatureUpdateBehavior,
            ApplicationUpdateBehavior = src.ApplicationUpdateBehavior,
            SchemaVersion = src.SchemaVersion,
            CreatedBy = src.CreatedBy,
            CreatedTimestamp = src.CreatedTimestamp,
            LastModifiedBy = src.LastModifiedBy,
            LastModifiedTimestamp = src.LastModifiedTimestamp,
            LogLevel = src.LogLevel,
            AuditRequired = src.AuditRequired,
            LifecycleState = src.LifecycleState,
            ConfidenceScore = src.ConfidenceScore,
            ConfidenceSource = src.ConfidenceSource,
            VerificationMethod = src.VerificationMethod,
            ExpectedResult = src.ExpectedResult,
            VerificationReliability = src.VerificationReliability,
            EvidenceType = src.EvidenceType,
            EvidenceLocation = src.EvidenceLocation,
            EvidenceHash = src.EvidenceHash,
            WritableTarget = src.WritableTarget,
            ExclusionReason = src.ExclusionReason,
            Bucket = src.Bucket,
            IsDynamicInventory = src.IsDynamicInventory,
            NativeTool = src.NativeTool,
            Applicability = src.Applicability,
            ApplicabilityReason = src.ApplicabilityReason,
            Requires = src.Requires,
            Recommended = src.Recommended,
            ConflictsWith = src.ConflictsWith,
            Ordering = src.Ordering,
            RebootDependency = src.RebootDependency,
            RelatedFeature = src.RelatedFeature,
            Replacement = src.Replacement,
            Alternative = src.Alternative,
            ConflictExplanation = src.ConflictExplanation,
            StructuredRelationships = src.StructuredRelationships,
            // Observation left fresh for this scan.
            Observation = new SettingObservation()
        };
    }

    private void Report(string status) => StatusChanged?.Invoke(status);
}
