// Source/WindowsPrivacyPlatform.CLI/Program.cs
using System;
using System.Collections.Generic;
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
            Console.WriteLine("Windows Privacy Platform - Prototype v0.4");
            Console.WriteLine("Read-only discovery pipeline");
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
                new PrivacyCollector()
            };

            var scanner = new InventoryScanner(logger, collectors);
            var validator = new SchemaValidator(logger);

            // 1. Scan
            var scanResult = scanner.Scan();
            if (scanResult.Success && scanResult.Snapshot is not null)
            {
                var s = scanResult.Snapshot;
                Console.WriteLine(
                    $"Identity : {s.WindowsVersion} | {s.Edition} | Build {s.BuildNumber}");
                Console.WriteLine(
                    $"Capabilities : {s.InstalledCapabilities.Count} | Packages : {s.InstalledPackages.Count} | " +
                    $"Services : {s.Services.Count} | Tasks : {s.ScheduledTasks.Count} | " +
                    $"Privacy settings : {s.PrivacySettings.Count}");
            }
            else
            {
                Console.WriteLine($"Scanner failed: {scanResult.Message}");
            }

            // 2. Load first-batch privacy ManagedObjects from catalog into KnowledgeBase
            var catalog = ManagedObjectCatalog.PrivacySettings;
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

            // 3. Validate first catalog object as structural smoke check
            if (catalog.Count > 0)
            {
                var firstEntry = knowledgeBase.Get(catalog[0].ObjectId);
                if (firstEntry is not null)
                {
                    var validationResult = validator.Validate(firstEntry);
                    Console.WriteLine(
                        $"Validator result: IsValid={validationResult.IsValid} (ObjectId={catalog[0].ObjectId})");
                    if (!validationResult.IsValid && validationResult.Errors is not null)
                    {
                        foreach (var err in validationResult.Errors)
                        {
                            Console.WriteLine($"  - {err}");
                        }
                    }
                }
            }

            logger.Info("CLI", "Pipeline complete");

            Console.WriteLine();
            Console.WriteLine("SAFETY CONFIRMATION: No Windows registry, services, tasks, packages, or policies were modified.");
            Console.WriteLine("No elevation or UAC prompt occurred.");
            Console.WriteLine("Prototype remains strictly read-only.");
        }
    }
}
