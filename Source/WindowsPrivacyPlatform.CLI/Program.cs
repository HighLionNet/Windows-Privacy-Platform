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
            // Non-interactive flags only. No prompts.
            var fullReport = HasFlag(args, "--full");
            if (HasFlag(args, "--help") || HasFlag(args, "-h"))
            {
                WriteHelp();
                return;
            }

            Console.WriteLine("Windows Privacy Platform - Prototype v0.6.5");
            Console.WriteLine("Read-only discovery + model bind + validate + risk summary");
            Console.WriteLine(fullReport
                ? "Report mode: full categorized detail"
                : "Report mode: summary + high-risk detail (use --full for complete catalog dump)");
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
                var configuredPolicies = snapshot.PolicySettings.Count(p =>
                    !string.Equals(p.Value, "Not configured", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(p.Value, "Error reading", StringComparison.OrdinalIgnoreCase));

                Console.WriteLine(
                    $"Identity : {snapshot.WindowsVersion} | {snapshot.Edition} | Build {snapshot.BuildNumber}");
                Console.WriteLine(
                    $"Capabilities : {snapshot.InstalledCapabilities.Count} | Packages : {snapshot.InstalledPackages.Count} | " +
                    $"Services : {snapshot.Services.Count} | Tasks : {snapshot.ScheduledTasks.Count} | " +
                    $"Privacy settings : {snapshot.PrivacySettings.Count} | Policy probes : {snapshot.PolicySettings.Count} " +
                    $"(configured: {configuredPolicies})");
            }
            else
            {
                Console.WriteLine($"Scanner failed: {scanResult.Message}");
            }

            // 2. Catalog + bind observed state onto ManagedObjects (orchestrated domain binders)
            var catalog = ManagedObjectCatalog.All.ToList();
            if (snapshot is not null)
                InventoryStateBinder.Bind(snapshot, catalog);

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

            Console.WriteLine(
                $"KnowledgeBase: stored {catalog.Count} catalog entries, count={knowledgeBase.Count}");

            // 3. Batch structural validation of entire catalog
            var validationResults = validator.ValidateAll(knowledgeBase.GetAll());
            var passed = validationResults.Count(r => r.IsValid);
            var failed = validationResults.Count(r => !r.IsValid);
            Console.WriteLine($"Validator batch: passed={passed}, failed={failed}, total={validationResults.Count}");
            if (failed > 0)
            {
                foreach (var bad in validationResults.Where(r => !r.IsValid).Take(10))
                {
                    Console.WriteLine($"  FAIL {bad.ObjectId}: {string.Join("; ", bad.Errors)}");
                }
                if (failed > 10)
                    Console.WriteLine($"  ... and {failed - 10} more");
            }

            // 4. Observation / risk summary + report (query layer available for navigation)
            if (snapshot is not null)
            {
                var summary = InventoryStateBinder.BuildSummary(snapshot, catalog, passed, failed);
                var query = new SettingsQuery(catalog);

                WriteRiskSummary(summary, query);

                if (fullReport)
                    WriteFullCategorizedReport(catalog, query);
                else
                {
                    WriteHighRiskDetail(summary);
                    WriteConflictDetail(query);
                }
            }

            logger.Info("CLI", "Pipeline complete");

            Console.WriteLine();
            Console.WriteLine("SAFETY CONFIRMATION: No Windows registry, services, tasks, packages, or policies were modified.");
            Console.WriteLine("No elevation or UAC prompt occurred.");
            Console.WriteLine("Prototype remains strictly read-only.");
        }

        private static void WriteHelp()
        {
            Console.WriteLine("Windows Privacy Platform - Prototype v0.6.5");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -c Release -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  (default)   Risk summary + high-risk configured items + layer conflicts");
            Console.WriteLine("  --full      Full categorized catalog report (long)");
            Console.WriteLine("  --help, -h  Show this help");
            Console.WriteLine();
            Console.WriteLine("Always read-only. No elevation. No system changes.");
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args is null || args.Length == 0)
                return false;
            return args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
        }

        private static void WriteRiskSummary(ObservationSummary summary, SettingsQuery query)
        {
            var conflictCount = query.Conflicts().Count();

            Console.WriteLine();
            Console.WriteLine("=== Observation & Risk Summary ===");
            Console.WriteLine($"Catalog total          : {summary.CatalogTotal}");
            Console.WriteLine($"Observed / not observed: {summary.ObservedCount} / {summary.NotObservedCount}");
            Console.WriteLine($"Policy configured / not: {summary.ConfiguredPolicyCount} / {summary.NotConfiguredPolicyCount}");
            Console.WriteLine($"Risk (H/M/L catalog)   : {summary.HighRiskCount} / {summary.MediumRiskCount} / {summary.LowRiskCount}");
            Console.WriteLine($"Privacy Allow/Deny/Prompt (matched values): " +
                              $"{summary.PrivacyAllowCount} / {summary.PrivacyDenyCount} / {summary.PrivacyPromptCount}");
            Console.WriteLine($"Catalog validation     : passed={summary.CatalogValidationPassed}, failed={summary.CatalogValidationFailed}");
            Console.WriteLine($"High-risk configured   : {summary.HighRiskItems.Count}");
            Console.WriteLine($"Medium-risk configured : {summary.MediumRiskItems.Count}");
            Console.WriteLine($"Layer conflicts        : {conflictCount}");
            Console.WriteLine();
        }

        private static void WriteHighRiskDetail(ObservationSummary summary)
        {
            Console.WriteLine("=== High-Risk Configured Items ===");
            if (summary.HighRiskItems.Count == 0)
            {
                Console.WriteLine("  (none observed as configured in this scan)");
                Console.WriteLine();
                return;
            }

            foreach (var item in summary.HighRiskItems
                         .OrderBy(i => i.ProductDomain)
                         .ThenBy(i => i.SubCategory, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(i => i.ObjectName, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  [{item.ProductDomain}/{item.SubCategory}] {item.ObjectName}");
                Console.WriteLine($"    Id      : {item.ObjectId}");
                Console.WriteLine($"    Current : {item.CurrentState}");
                Console.WriteLine();
            }
        }

        private static void WriteConflictDetail(SettingsQuery query)
        {
            var conflicts = query.Conflicts().ToList();
            Console.WriteLine("=== Effective-Layer Conflicts ===");
            if (conflicts.Count == 0)
            {
                Console.WriteLine("  (none detected among known relationship pairs)");
                Console.WriteLine();
                return;
            }

            // Deduplicate by effective explanation feature pairs
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mo in conflicts.OrderBy(m => m.ProductDomain).ThenBy(m => m.ObjectName))
            {
                var eff = mo.Observation?.Effective;
                if (eff is null)
                    continue;

                var key = eff.Explanation ?? mo.ObjectId;
                if (!seen.Add(key))
                    continue;

                Console.WriteLine($"  [{mo.ProductDomain}] {mo.ObjectName}");
                Console.WriteLine($"    Id         : {mo.ObjectId}");
                Console.WriteLine($"    Effective  : {eff.EffectiveValue ?? "(unknown)"}");
                Console.WriteLine($"    Source     : {eff.EffectiveSource} (confidence: {eff.Confidence})");
                Console.WriteLine($"    Explanation: {eff.Explanation}");
                Console.WriteLine();
            }
        }

        private static void WriteFullCategorizedReport(IReadOnlyList<ManagedObject> catalog, SettingsQuery query)
        {
            Console.WriteLine("=== Full Categorized Privacy & Policy Report ===");
            Console.WriteLine();

            var byDomain = catalog
                .GroupBy(m => m.ProductDomain)
                .OrderBy(g => g.Key);

            foreach (var domainGroup in byDomain)
            {
                Console.WriteLine($"## Domain: {domainGroup.Key}");
                Console.WriteLine();

                var bySub = domainGroup
                    .GroupBy(m => m.SubCategory ?? domainGroup.Key.ToString())
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var subGroup in bySub)
                {
                    Console.WriteLine($"[{subGroup.Key}]");

                    foreach (var mo in subGroup.OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"  {mo.ObjectName}");
                        Console.WriteLine($"    Id          : {mo.ObjectId}");
                        Console.WriteLine($"    Risk        : {mo.RiskLevel} | Control: {mo.ControlLevel}");
                        Console.WriteLine($"    Current     : {mo.CurrentState ?? "(unbound)"}");

                        var eff = mo.Observation?.Effective;
                        if (eff is not null && (!string.IsNullOrWhiteSpace(eff.EffectiveValue) || eff.HasConflict))
                        {
                            Console.WriteLine($"    Effective   : {eff.EffectiveValue ?? "(unknown)"} [{eff.EffectiveSource}]");
                            if (eff.HasConflict)
                                Console.WriteLine($"    Conflict    : {eff.Explanation}");
                        }

                        Console.WriteLine($"    Description : {mo.Description}");
                        if (!string.IsNullOrWhiteSpace(mo.Rationale))
                            Console.WriteLine($"    Rationale   : {mo.Rationale}");
                        Console.WriteLine();
                    }
                }
            }

            // Navigation tree is available for future TUI; not printed in full by default.
            _ = NavigationBuilder.BuildDomainTree(catalog);
            _ = query;
        }
    }
}
