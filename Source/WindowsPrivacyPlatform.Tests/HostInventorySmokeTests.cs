using WindowsPrivacyPlatform.KnowledgeBase;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;
using WindowsPrivacyPlatform.Validator;
using Xunit;

namespace WindowsPrivacyPlatform.Tests;

public sealed class HostInventorySmokeTests
{
    [Fact]
    public void Live_read_only_inventory_produces_valid_dynamic_entries()
    {
        var logger = new AuditLogger();
        IInventoryCollector[] collectors =
        [
            new WindowsIdentityCollector(),
            new CapabilityCollector(),
            new PackageCollector(),
            new ServiceCollector(),
            new ScheduledTaskCollector(),
            new PrivacyCollector(),
            new PolicyCollector(),
            new FirewallCollector(),
            new SecurityCenterCollector(),
            new NetworkingCollector(),
            new BrowserInventoryCollector()
        ];

        var result = new InventoryScanner(logger, collectors).Scan();
        Assert.NotNull(result.Snapshot);
        Assert.NotEqual(default, result.Snapshot.Networking.Dns.CapturedAtUtc);
        Assert.All(new[] { "Microsoft Edge", "Google Chrome", "Mozilla Firefox", "VPN applications" }, name =>
            Assert.Contains(result.Snapshot.Networking.Dns.ExternalApps, app =>
                app.Application == name && app.Source == "ExternalApp" && app.Evidence == EvidenceState.Unknown));
        Assert.All(new[]
        {
            result.Snapshot.Applications.Browsers.Edge,
            result.Snapshot.Applications.Browsers.WebView2
        }, browser => Assert.False(string.IsNullOrWhiteSpace(browser.Name)));

        var dynamicEntries = DynamicInventoryCatalog.Create(result.Snapshot!, ManagedObjectCatalog.All);
        var repository = new InMemoryKnowledgeBaseRepository();
        foreach (var item in dynamicEntries)
        {
            repository.Add(new KnowledgeBaseEntry
            {
                ObjectId = item.ObjectId,
                Object = item,
                Metadata = new KnowledgeBaseMetadata { Source = "Host inventory smoke test" }
            });
        }

        var failures = new SchemaValidator(logger).ValidateAll(repository.GetAll())
            .Where(validation => !validation.IsValid)
            .Select(validation => $"{validation.ObjectId}: {string.Join("; ", validation.Errors)}")
            .ToList();

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
