// Source/WindowsPrivacyPlatform.CLI/Program.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.KnowledgeBase;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner;
using WindowsPrivacyPlatform.Validator;

namespace WindowsPrivacyPlatform.CLI
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Windows Privacy Platform - Prototype v0.5");
            Console.WriteLine("Read-only discovery + model + categorized report");
            Console.WriteLine();

            var logger = new AuditLogger();
            logger.Info("CLI", "Pipeline start");

            var knowledgeBase = new InMemoryKnowledgeBaseRepository();

            IEnumerable<IInventoryCollector> collectors = new IInventoryCollector[]
            {
                new WindowsIdentityCollector(),
                new CapabilityCollector(),
                new PackageCollector(),
                new ServiceCollector(),
                new ScheduledTaskCollector(),
                new PrivacyCollector(),
                new PolicyCollector()
            };

            var scanner = new InventoryScanner(logger, collectors);
            var validator = new SchemaValidator(logger);

            // 1. Scan
            InventorySnapshot? snapshot = null;
            var scanResult = scanner.Scan();
            if (scanResult.Success && scanResult.Snapshot is not null)
            {
                snapshot = scanResult.Snapshot;
                var s = snapshot;
                var configuredPolicies = s.PolicySettings.Count(p =>
                    !string.Equals(p.Value, "Not configured", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(p.Value, "Error reading", StringComparison.OrdinalIgnoreCase));

                Console.WriteLine(
                    $"Identity : {s.WindowsVersion} | {s.Edition} | Build {s.BuildNumber}");
                Console.WriteLine(
                    $"Capabilities : {s.InstalledCapabilities.Count} | Packages : {s.InstalledPackages.Count} | " +
                    $"Services : {s.Services.Count} | Tasks : {s.ScheduledTasks.Count} | " +
                    $"Privacy settings : {s.PrivacySettings.Count} | Policy probes : {s.PolicySettings.Count} " +
                    $"(configured: {configuredPolicies})");
            }
            else
            {
                Console.WriteLine($"Scanner failed: {scanResult.Message}");
            }

            // 2. Load full ManagedObject catalog into KnowledgeBase
            var catalog = ManagedObjectCatalog.All;
            foreach (var managedObject in catalog)
            {
                var entry = new KnowledgeBaseEntry
                {
                    ObjectId = managedObject.ObjectId,
                    Object = managedObject,
                    Metadata = new KnowledgeBaseMetadata
                    {
                        Source = "ManagedObjectCatalog",
                        SourceReliabilityScore = managedObject.ConfidenceScore
                    }
                };
                knowledgeBase.Add(entry);
            }

            Console.WriteLine(
                $"KnowledgeBase: stored {catalog.Count} catalog entries, count={knowledgeBase.Count}");

            // 3. Validate first catalog object
            if (catalog.Count > 0)
            {
                var firstEntry = knowledgeBase.GetByObjectId(catalog[0].ObjectId);
                if (firstEntry is not null)
                {
                    var validationResult = validator.Validate(firstEntry);
                    Console.WriteLine(
                        $"Validator result: IsValid={validationResult.IsValid} (ObjectId={catalog[0].ObjectId})");
                    if (!validationResult.IsValid && validationResult.Errors is not null)
                    {
                        foreach (var err in validationResult.Errors)
                            Console.WriteLine($"  - {err}");
                    }
                }
            }

            // 4. Categorized report (model layer explaining inventory)
            if (snapshot is not null)
                WriteCategorizedReport(snapshot, catalog);

            logger.Info("CLI", "Pipeline complete");

            Console.WriteLine();
            Console.WriteLine("SAFETY CONFIRMATION: No Windows registry, services, tasks, packages, or policies were modified.");
            Console.WriteLine("No elevation or UAC prompt occurred.");
            Console.WriteLine("Prototype remains strictly read-only.");
        }

        private static void WriteCategorizedReport(
            InventorySnapshot snapshot,
            IReadOnlyList<ManagedObject> catalog)
        {
            Console.WriteLine();
            Console.WriteLine("=== Categorized Privacy & Policy Report ===");
            Console.WriteLine("(Explains discovered settings using the ManagedObject catalog.)");
            Console.WriteLine();

            var byCategory = catalog
                .GroupBy(m => m.SubCategory ?? m.FeatureCategory.ToString())
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in byCategory)
            {
                Console.WriteLine($"[{group.Key}]");

                foreach (var mo in group.OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase))
                {
                    var current = ResolveCurrentValue(snapshot, mo);
                    Console.WriteLine($"  {mo.ObjectName}");
                    Console.WriteLine($"    Id          : {mo.ObjectId}");
                    Console.WriteLine($"    Risk        : {mo.RiskLevel} | Control: {mo.ControlLevel}");
                    Console.WriteLine($"    Current     : {current}");
                    Console.WriteLine($"    Description : {mo.Description}");
                    if (!string.IsNullOrWhiteSpace(mo.Rationale))
                        Console.WriteLine($"    Rationale   : {mo.Rationale}");
                    Console.WriteLine();
                }
            }
        }

        private static string ResolveCurrentValue(InventorySnapshot snapshot, ManagedObject mo)
        {
            // Policy probes use ObjectId as PolicySettingInfo.Name
            var policy = snapshot.PolicySettings.FirstOrDefault(p =>
                string.Equals(p.Name, mo.ObjectId, StringComparison.OrdinalIgnoreCase));
            if (policy is not null)
                return $"{policy.Value} ({policy.Hive})";

            // ConsentStore short names: ObjectId ends with .name (e.g. privacy.consentstore.location)
            var shortName = mo.ObjectId.Contains('.')
                ? mo.ObjectId[(mo.ObjectId.LastIndexOf('.') + 1)..]
                : mo.ObjectId;

            var privacy = snapshot.PrivacySettings.FirstOrDefault(p =>
                string.Equals(p.Name, shortName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, mo.ObjectId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, mo.ObjectName, StringComparison.OrdinalIgnoreCase));

            if (privacy is not null)
                return privacy.Value;

            // Related privacy keys with dotted display names
            privacy = snapshot.PrivacySettings.FirstOrDefault(p =>
                mo.ObjectId.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(shortName, StringComparison.OrdinalIgnoreCase));

            return privacy?.Value ?? "Not observed in this scan";
        }
    }
}
