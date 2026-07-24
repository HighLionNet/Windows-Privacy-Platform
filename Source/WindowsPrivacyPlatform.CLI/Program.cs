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
    /// <summary>
    /// Presentation-only host. Discovery, binding, and reasoning live elsewhere.
    /// Non-interactive; read-only; no elevation.
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var fullReport = HasFlag(args, "--full");
            if (HasFlag(args, "--help") || HasFlag(args, "-h"))
            {
                WriteHelp();
                return;
            }

            Console.WriteLine("Windows Privacy Platform - Prototype v0.6.5");
            Console.WriteLine("Read-only discovery + bind + validate + explanation");
            Console.WriteLine(fullReport
                ? "Report mode: full categorized detail"
                : "Report mode: summary + high-risk + conflicts with decision cards");
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

            var validationResults = validator.ValidateAll(knowledgeBase.GetAll());
            var passed = validationResults.Count(r => r.IsValid);
            var failed = validationResults.Count(r => !r.IsValid);
            Console.WriteLine($"Validator batch: passed={passed}, failed={failed}, total={validationResults.Count}");
            if (failed > 0)
            {
                foreach (var bad in validationResults.Where(r => !r.IsValid).Take(10))
                    Console.WriteLine($"  FAIL {bad.ObjectId}: {string.Join("; ", bad.Errors)}");
                if (failed > 10)
                    Console.WriteLine($"  ... and {failed - 10} more");
            }

            if (snapshot is not null)
            {
                var summary = InventoryStateBinder.BuildSummary(snapshot, catalog, passed, failed);
                var query = new SettingsQuery(catalog);
                var nav = NavigationBuilder.BuildDomainTree(catalog);

                WriteRiskSummary(summary, query, nav);

                if (fullReport)
                    WriteFullCategorizedReport(catalog, query);
                else
                {
                    WriteHighRiskDetail(summary);
                    WriteConflictCards(query);
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
            Console.WriteLine("  (default)   Summary + high-risk + conflict decision cards");
            Console.WriteLine("  --full      Full categorized catalog report");
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

        private static void WriteRiskSummary(ObservationSummary summary, SettingsQuery query, NavigationNode nav)
        {
            var conflictCount = query.GetConflicts().Count();
            var reviewCount = query.GetSettingsNeedingReview().Count();

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
            Console.WriteLine($"Needs review (query)   : {reviewCount}");
            Console.WriteLine($"Nav domains            : {nav.ChildCount} (conflicts in tree: {nav.ConflictCount})");
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

        private static void WriteConflictCards(SettingsQuery query)
        {
            var conflicts = query.GetConflicts().ToList();
            Console.WriteLine("=== Effective-Layer Conflicts (decision cards) ===");
            if (conflicts.Count == 0)
            {
                Console.WriteLine("  (none detected among known relationship pairs)");
                Console.WriteLine();
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mo in conflicts.OrderBy(m => m.ProductDomain).ThenBy(m => m.ObjectName))
            {
                var reason = mo.Observation?.Resolution?.ResolutionReason
                             ?? mo.Observation?.Effective?.Explanation
                             ?? mo.ObjectId;
                if (!seen.Add(reason))
                    continue;

                var card = NavigationBuilder.BuildDetail(mo, query);
                if (card is null)
                    continue;

                Console.WriteLine(new string('-', 56));
                Console.WriteLine(card.Title);
                Console.WriteLine(new string('-', 56));
                Console.WriteLine($"Domain        : {card.DomainPath}");
                Console.WriteLine($"Risk          : {card.RiskLevel} — {card.Explanation.RiskSummary}");
                Console.WriteLine($"What is this  : {card.Explanation.WhatIsIt}");
                Console.WriteLine($"Why it matters: {card.Explanation.WhyItMatters}");
                Console.WriteLine($"Current raw   : {card.CurrentStateDisplay}");
                Console.WriteLine($"Effective     : {card.EffectiveValueDisplay ?? "(unknown)"}");
                Console.WriteLine($"Source        : {card.EffectiveSourceDisplay} (confidence: {card.Confidence})");
                Console.WriteLine($"Why it wins   : {card.ResolutionReason}");

                if (card.Layers.Count > 0)
                {
                    Console.WriteLine("Observed layers:");
                    foreach (var layer in card.Layers)
                        Console.WriteLine($"  - {layer.LayerName}: {layer.ValueDisplay}");
                }

                if (card.Related.Count > 0)
                {
                    Console.WriteLine("Related settings:");
                    foreach (var rel in card.Related.Take(6))
                        Console.WriteLine($"  - [{rel.Relationship}] {rel.Title}");
                }

                if (card.Explanation.RelatedApplications.Count > 0)
                {
                    Console.WriteLine("Often related apps:");
                    foreach (var app in card.Explanation.RelatedApplications)
                        Console.WriteLine($"  - {app}");
                }

                Console.WriteLine($"User impact   : {card.Explanation.UserImpact}");
                Console.WriteLine($"Guidance      : {card.Explanation.DecisionGuidance}");
                Console.WriteLine();
            }
        }

        private static void WriteFullCategorizedReport(IReadOnlyList<ManagedObject> catalog, SettingsQuery query)
        {
            Console.WriteLine("=== Full Categorized Privacy & Policy Report ===");
            Console.WriteLine();

            foreach (var domainGroup in catalog.GroupBy(m => m.ProductDomain).OrderBy(g => g.Key))
            {
                Console.WriteLine($"## Domain: {domainGroup.Key}");
                Console.WriteLine();

                foreach (var subGroup in domainGroup
                             .GroupBy(m => m.SubCategory ?? domainGroup.Key.ToString())
                             .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[{subGroup.Key}]");

                    foreach (var mo in subGroup.OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"  {mo.ObjectName}");
                        Console.WriteLine($"    Id          : {mo.ObjectId}");
                        Console.WriteLine($"    Risk        : {mo.RiskLevel} | Control: {mo.ControlLevel}");
                        Console.WriteLine($"    Current     : {mo.CurrentState ?? "(unbound)"}");

                        var res = mo.Observation?.Resolution ?? mo.Observation?.Effective is { } eff
                            ? new ConfigurationResolution
                            {
                                EffectiveValue = eff.EffectiveValue,
                                EffectiveSource = eff.EffectiveSource,
                                Confidence = eff.Confidence,
                                ResolutionReason = eff.Explanation,
                                HasConflict = eff.HasConflict
                            }
                            : null;

                        if (res is not null && (!string.IsNullOrWhiteSpace(res.EffectiveValue) || res.HasConflict))
                        {
                            Console.WriteLine($"    Effective   : {res.EffectiveValue ?? "(unknown)"} [{res.EffectiveSource}]");
                            Console.WriteLine($"    Reason      : {res.ResolutionReason}");
                        }

                        var explanation = SettingExplanationFactory.FromDefinition(mo);
                        Console.WriteLine($"    What        : {explanation.WhatIsIt}");
                        Console.WriteLine($"    Why         : {explanation.WhyItMatters}");
                        Console.WriteLine();
                    }
                }
            }

            _ = query;
        }
    }
}
