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

            // 2. Create one well-formed test ManagedObject
            var testObject = new ManagedObject
            {
                ObjectId = "prototype-test-001",
                ObjectName = "Test Managed Object"
            };

            var entry = new KnowledgeBaseEntry
            {
                ObjectId = testObject.ObjectId,
                Object = testObject,
                Metadata = new KnowledgeBaseMetadata
                {
                    Source = "CLI-Test",
                    SourceReliabilityScore = 1
                }
            };

            knowledgeBase.Add(entry);
            Console.WriteLine($"KnowledgeBase: stored entry, count={knowledgeBase.Count}");

            // 3. Validate
            var validationResult = validator.Validate(entry);
            Console.WriteLine($"Validator result: IsValid={validationResult.IsValid}");
            if (!validationResult.IsValid && validationResult.Errors is not null)
            {
                foreach (var err in validationResult.Errors)
                {
                    Console.WriteLine($"  - {err}");
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
