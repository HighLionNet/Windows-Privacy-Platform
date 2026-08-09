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
/// </summary>
public sealed class ScanService
{
    public const string CatalogSchemaVersion = "2.0";
    public const string KnowledgeBaseVersionValue = "2.0";

    public IReadOnlyList<ManagedObject> Catalog { get; private set; } = Array.Empty<ManagedObject>();
    public SettingsQuery? Query { get; private set; }
    public MachineOverview? Overview { get; private set; }
    public ObservationSummary? Summary { get; private set; }
    public NavigationNode? NavigationRoot { get; private set; }
    public int ValidationPassed { get; private set; }
    public int ValidationFailed { get; private set; }
    public string LastError { get; private set; } = string.Empty;
    public bool HasScan => Overview is not null;

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

            InventorySnapshot? snapshot = null;
            var scanResult = scanner.Scan();
            if (scanResult.Success && scanResult.Snapshot is not null)
                snapshot = scanResult.Snapshot;
            else
                LastError = scanResult.Message ?? "Scan returned no snapshot.";

            cancellationToken.ThrowIfCancellationRequested();
            Report("Loading knowledge catalog…");

            var catalog = ManagedObjectCatalog.All.ToList();

            if (snapshot is not null)
            {
                Report("Binding observations…");
                InventoryStateBinder.Bind(snapshot, catalog);
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
            ValidationPassed = validationResults.Count(r => r.IsValid);
            ValidationFailed = validationResults.Count(r => !r.IsValid);

            Catalog = catalog;
            Query = new SettingsQuery(catalog);
            NavigationRoot = NavigationBuilder.BuildDomainTree(catalog);

            if (snapshot is not null)
            {
                Overview = MachineOverview.FromSnapshot(snapshot, catalog.Count);
                Overview.CatalogVersion = CatalogSchemaVersion;
                Overview.KnowledgeBaseVersion = KnowledgeBaseVersionValue;
                Summary = InventoryStateBinder.BuildSummary(snapshot, catalog, ValidationPassed, ValidationFailed);
                Report("Scan complete.");
            }
            else
            {
                Overview = new MachineOverview
                {
                    CatalogVersion = CatalogSchemaVersion,
                    KnowledgeBaseVersion = KnowledgeBaseVersionValue,
                    LastScanUtc = DateTime.UtcNow,
                    IdentityCollectionNotes = LastError
                };
                Report(string.IsNullOrEmpty(LastError) ? "Scan finished with no snapshot." : $"Scan incomplete: {LastError}");
            }

            logger.Info("App", "GUI pipeline complete");
            ScanCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            Report("Scan cancelled.");
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Report($"Scan error: {ex.Message}");
            ScanCompleted?.Invoke();
        }
    }

    private void Report(string status) => StatusChanged?.Invoke(status);
}
