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
    /// Non-interactive by default; optional --tui for read-only keyboard navigation.
    /// No elevation. No writes.
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var fullReport = HasFlag(args, "--full");
            var tuiMode = HasFlag(args, "--tui");
            if (HasFlag(args, "--help") || HasFlag(args, "-h"))
            {
                WriteHelp();
                return;
            }

            if (!tuiMode)
            {
                Console.WriteLine("Windows Privacy Platform — Prototype v0.8");
                Console.WriteLine("Read-only discovery, explanation, and effective-layer understanding");
                Console.WriteLine(fullReport
                    ? "Report mode: full categorized detail"
                    : "Report mode: machine overview + observation summary + high-impact watch list + conflict cards");
                Console.WriteLine();
            }

            var logger = new AuditLogger();
            logger.Info("CLI", tuiMode ? "TUI pipeline start" : "Pipeline start");

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

            var scanner = new InventoryScanner(logger, collectors);
            var validator = new SchemaValidator(logger);

            InventorySnapshot? snapshot = null;
            var scanResult = scanner.Scan();
            if (scanResult.Success && scanResult.Snapshot is not null)
            {
                snapshot = scanResult.Snapshot;
                if (!tuiMode)
                {
                    var configuredPolicies = snapshot.PolicySettings.Count(p =>
                        !string.Equals(p.Value, "Not configured", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(p.Value, "Error reading", StringComparison.OrdinalIgnoreCase));

                    Console.WriteLine(
                        $"Identity : {snapshot.WindowsVersion} | {snapshot.Edition} | Build {snapshot.BuildNumber}");
                    Console.WriteLine(
                        $"Capabilities : {snapshot.InstalledCapabilities.Count} | Packages : {snapshot.InstalledPackages.Count} | " +
                        $"Services : {snapshot.Services.Count} | Tasks : {snapshot.ScheduledTasks.Count} | " +
                        $"Privacy settings : {snapshot.PrivacySettings.Count} | Policy probes : {snapshot.PolicySettings.Count} " +
                        $"(configured: {configuredPolicies}) | Firewall profiles : {snapshot.Networking.FirewallProfiles.Count}");

                    if (snapshot.InstalledCapabilities.Count == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Capabilities note:");
                        Console.WriteLine("  No Windows capabilities were returned by the read-only collectors.");
                        Console.WriteLine("  Get-WindowsCapability and DISM often require elevation or are restricted");
                        Console.WriteLine("  on this host. The pipeline continues with partial inventory; this is Unknown,");
                        Console.WriteLine("  not an assertion that zero capabilities are installed.");
                    }
                }
            }
            else if (!tuiMode)
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

            var validationResults = validator.ValidateAll(knowledgeBase.GetAll());
            var passed = validationResults.Count(r => r.IsValid);
            var failed = validationResults.Count(r => !r.IsValid);

            if (!tuiMode)
            {
                Console.WriteLine(
                    $"KnowledgeBase: {catalog.Count} catalog entries stored");
                Console.WriteLine($"Validator batch: passed={passed}, failed={failed}, total={validationResults.Count}");
                if (failed > 0)
                {
                    foreach (var bad in validationResults.Where(r => !r.IsValid).Take(10))
                        Console.WriteLine($"  FAIL {bad.ObjectId}: {string.Join("; ", bad.Errors)}");
                    if (failed > 10)
                        Console.WriteLine($"  ... and {failed - 10} more");
                }
            }

            if (snapshot is not null)
            {
                var summary = InventoryStateBinder.BuildSummary(snapshot, catalog, passed, failed);
                var query = new SettingsQuery(catalog);
                var nav = NavigationBuilder.BuildDomainTree(catalog);
                var overview = MachineOverview.FromSnapshot(snapshot, catalog.Count);

                if (tuiMode)
                {
                    TuiHost.Run(catalog, query, overview);
                }
                else
                {
                    WriteMachineOverview(overview);
                    WriteObservationSummary(summary, query, nav);

                    if (fullReport)
                        WriteFullCategorizedReport(catalog, query);
                    else
                    {
                        WriteHighImpactWatchList(summary);
                        WriteConflictCards(query);
                    }
                }
            }
            else if (tuiMode)
            {
                var query = new SettingsQuery(catalog);
                TuiHost.Run(catalog, query, null);
            }

            logger.Info("CLI", tuiMode ? "TUI session complete" : "Pipeline complete");

            if (!tuiMode)
            {
                Console.WriteLine();
                Console.WriteLine("SAFETY CONFIRMATION: No Windows registry, services, tasks, packages, policies, or firewall rules were modified.");
                Console.WriteLine("No elevation or UAC prompt occurred.");
                Console.WriteLine("This build remains strictly read-only.");
            }
        }

        private static void WriteMachineOverview(MachineOverview o)
        {
            Console.WriteLine();
            Console.WriteLine("=== Machine Overview (device context) ===");
            Console.WriteLine("Observed platform information — not a security score.");
            Console.WriteLine();
            Console.WriteLine($"  OS              : {Display(o.WindowsVersion)} | {Display(o.WindowsEdition)} | Build {o.BuildNumber}");
            Console.WriteLine($"  Architecture    : {Display(o.Architecture)}");
            Console.WriteLine($"  Manufacturer    : {Display(o.DeviceManufacturer)}");
            Console.WriteLine($"  Model           : {Display(o.DeviceModel)}");
            Console.WriteLine($"  Processor       : {Display(o.Processor)}");
            Console.WriteLine($"  Memory (MiB)    : {(o.TotalPhysicalMemoryMiB > 0 ? o.TotalPhysicalMemoryMiB.ToString() : "Unknown")}");
            Console.WriteLine($"  Secure Boot     : {Display(o.SecureBootState)}");
            Console.WriteLine($"  TPM             : {Display(o.TpmPresent)} / {Display(o.TpmVersion)}");
            Console.WriteLine($"  BitLocker       : {Display(o.BitLockerProtectionStatus)}");
            Console.WriteLine($"  Domain          : {Display(o.DomainMembership)}");
            Console.WriteLine($"  Entra / Azure AD: {Display(o.AzureAdJoined)}");
            Console.WriteLine($"  PowerShell      : {Display(o.PowerShellVersion)}");
            Console.WriteLine($"  .NET runtime    : {Display(o.DotNetRuntimeVersion)}");
            Console.WriteLine($"  Firewall svc    : {Display(o.FirewallServiceState)}");
            Console.WriteLine($"  Firewall profiles: {Display(o.FirewallProfilesSummary)}");
            Console.WriteLine($"  Defender svc    : {Display(o.DefenderServiceState)}");
            Console.WriteLine($"  Last scan (UTC) : {o.LastScanUtc:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Catalog / KB    : {o.CatalogVersion} / {o.KnowledgeBaseVersion}");
            Console.WriteLine($"  Identity conf.  : {o.IdentityConfidence}");
            if (!string.IsNullOrWhiteSpace(o.IdentityCollectionNotes))
            {
                Console.WriteLine();
                Console.WriteLine("  Collection notes:");
                Console.WriteLine($"    {o.IdentityCollectionNotes}");
            }
            Console.WriteLine();
        }

        private static string Display(string? value) =>
            string.IsNullOrWhiteSpace(value) || value.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                ? "Unknown"
                : value;

        private static void WriteHelp()
        {
            Console.WriteLine("Windows Privacy Platform — Prototype v0.8");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -c Release -- [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  (default)   Machine overview + observation summary + high-impact watch list + conflict cards");
            Console.WriteLine("  --full      Full categorized catalog report");
            Console.WriteLine("  --tui       Interactive read-only terminal explorer");
            Console.WriteLine("  --help, -h  Show this help");
            Console.WriteLine();
            Console.WriteLine("TUI keys: ↑↓ navigate · Enter open · Esc back · / search · Q quit");
            Console.WriteLine();
            Console.WriteLine("Always read-only. No elevation. No system changes.");
            Console.WriteLine("Impact labels describe significance; they are not a security score.");
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args is null || args.Length == 0)
                return false;
            return args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
        }

        private static void WriteObservationSummary(ObservationSummary summary, SettingsQuery query, NavigationNode nav)
        {
            var conflictCount = query.GetConflicts().Count();
            var reviewCount = query.GetSettingsNeedingReview().Count();

            Console.WriteLine();
            Console.WriteLine("=== Observation summary ===");
            Console.WriteLine($"Catalog total             : {summary.CatalogTotal}");
            Console.WriteLine($"Observed / not observed   : {summary.ObservedCount} / {summary.NotObservedCount}");
            Console.WriteLine($"Policy configured / not   : {summary.ConfiguredPolicyCount} / {summary.NotConfiguredPolicyCount}");
            Console.WriteLine($"Impact tags (H/M/L catalog): {summary.HighRiskCount} / {summary.MediumRiskCount} / {summary.LowRiskCount}");
            Console.WriteLine($"Privacy Allow/Deny/Prompt : " +
                              $"{summary.PrivacyAllowCount} / {summary.PrivacyDenyCount} / {summary.PrivacyPromptCount}");
            Console.WriteLine($"Catalog validation        : passed={summary.CatalogValidationPassed}, failed={summary.CatalogValidationFailed}");
            Console.WriteLine($"High-impact configured    : {summary.HighRiskItems.Count}  (watch list, not a score)");
            Console.WriteLine($"Medium-impact configured  : {summary.MediumRiskItems.Count}");
            Console.WriteLine($"Layer conflicts           : {conflictCount}");
            Console.WriteLine($"Needs review (query)      : {reviewCount}");
            Console.WriteLine($"Nav domains               : {nav.ChildCount} (conflicts in tree: {nav.ConflictCount})");
            Console.WriteLine();
        }

        private static void WriteHighImpactWatchList(ObservationSummary summary)
        {
            Console.WriteLine("=== High-impact configured items (watch list) ===");
            Console.WriteLine("These are high-impact topics with an observed value — not a pass/fail grade.");
            Console.WriteLine();

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
                Console.WriteLine($"    Id       : {item.ObjectId}");
                Console.WriteLine($"    Observed : {item.CurrentState}");
                Console.WriteLine();
            }
        }

        private static void WriteConflictCards(SettingsQuery query)
        {
            var conflicts = query.GetConflicts().ToList();
            Console.WriteLine("=== Layer conflicts (explanation cards) ===");
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

                Console.WriteLine(new string('═', 60));
                Console.WriteLine(card.Title);
                Console.WriteLine(new string('═', 60));
                Console.WriteLine($"Domain        : {card.DomainPath}");
                Console.WriteLine($"Impact        : {card.Explanation.ImpactLabel}");
                Console.WriteLine();
                Console.WriteLine("What this is");
                Console.WriteLine($"  {card.Explanation.WhatIsIt}");
                Console.WriteLine();
                Console.WriteLine("Why it matters");
                Console.WriteLine($"  {card.Explanation.WhyItMatters}");
                Console.WriteLine();
                Console.WriteLine("Observed");
                Console.WriteLine($"  Raw value    : {card.CurrentStateDisplay ?? "Unknown"}");
                if (card.Layers.Count > 0)
                {
                    foreach (var layer in card.Layers)
                        Console.WriteLine($"  · {layer.LayerName}: {layer.ValueDisplay}");
                }
                Console.WriteLine();
                Console.WriteLine("Interpretation");
                Console.WriteLine($"  Effective    : {card.EffectiveValueDisplay ?? "Unknown"}");
                Console.WriteLine($"  Layer        : {card.EffectiveSourceDisplay ?? "Unknown"}  (confidence: {card.Confidence})");
                Console.WriteLine($"  Reason       : {card.ResolutionReason ?? "Unknown"}");
                if (card.HasConflict)
                    Console.WriteLine("  Note         : Observed layers disagree.");

                if (card.Related.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Related configuration");
                    foreach (var rel in card.Related.Take(8))
                        Console.WriteLine($"  · {HumanRelationship(rel.Relationship)}: {rel.Title}");
                }

                if (!string.IsNullOrWhiteSpace(card.Explanation.CommonMisconceptions))
                {
                    Console.WriteLine();
                    Console.WriteLine("Common misconception");
                    Console.WriteLine($"  {card.Explanation.CommonMisconceptions}");
                }

                Console.WriteLine();
            }
        }

        private static string HumanRelationship(string kind) => kind switch
        {
            "Overrides" => "Can override",
            "OverriddenBy" => "Controlled by",
            "ConflictsWith" => "Potential conflict with",
            "DependsOn" or "Requires" => "Depends on",
            "Affects" => "Affects",
            "SameFeatureAlternatePath" => "Alternate path",
            "Related" => "Also related to",
            _ => "Related to"
        };

        private static void WriteFullCategorizedReport(IReadOnlyList<ManagedObject> catalog, SettingsQuery query)
        {
            Console.WriteLine("=== Full categorized report ===");
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
                        var explanation = SettingExplanationFactory.FromDefinition(mo);
                        Console.WriteLine($"  {mo.ObjectName}");
                        Console.WriteLine($"    Id          : {mo.ObjectId}");
                        Console.WriteLine($"    Impact      : {explanation.ImpactLabel} | Control: {mo.ControlLevel}");
                        Console.WriteLine($"    Observed    : {mo.CurrentState ?? "Unknown"}");

                        var res = GetResolution(mo);
                        if (res is not null && (!string.IsNullOrWhiteSpace(res.EffectiveValue) || res.HasConflict))
                        {
                            Console.WriteLine($"    Effective   : {res.EffectiveValue ?? "Unknown"} [{res.EffectiveSource}]");
                            Console.WriteLine($"    Reason      : {res.ResolutionReason}");
                        }

                        Console.WriteLine($"    What        : {explanation.WhatIsIt}");
                        Console.WriteLine($"    Why         : {explanation.WhyItMatters}");
                        Console.WriteLine();
                    }
                }
            }

            _ = query;
        }

        private static ConfigurationResolution? GetResolution(ManagedObject mo)
        {
            if (mo.Observation?.Resolution is not null)
                return mo.Observation.Resolution;

            var eff = mo.Observation?.Effective;
            if (eff is null)
                return null;

            return new ConfigurationResolution
            {
                EffectiveValue = eff.EffectiveValue,
                EffectiveSource = eff.EffectiveSource,
                Confidence = eff.Confidence,
                ResolutionReason = eff.Explanation,
                HasConflict = eff.HasConflict,
                RawObservations = eff.ContributingLayers ?? new List<ConfigurationObservation>()
            };
        }
    }
}
