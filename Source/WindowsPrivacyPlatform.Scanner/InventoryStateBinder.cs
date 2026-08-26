// Source/WindowsPrivacyPlatform.Scanner/InventoryStateBinder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.Models;
using WindowsPrivacyPlatform.Scanner.Binding;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only bind orchestrator. Delegates to domain binders; does not contain domain logic itself.
    /// Does not write to the system. Does not elevate.
    /// Also maps firewall and curated native inventory into catalog entries.
    /// </summary>
    public static class InventoryStateBinder
    {
        private static readonly IStateBinder[] Binders =
        {
            new PrivacyBinder(),
            new PolicyBinder()
        };

        private static readonly RelationshipBinder Relationships = new();

        public static void Bind(InventorySnapshot snapshot, IEnumerable<ManagedObject> catalog)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));
            if (catalog is null)
                throw new ArgumentNullException(nameof(catalog));

            var list = catalog.Where(m => m is not null).ToList();

            foreach (var mo in list)
            {
                if (BindNativeInventory(snapshot, mo))
                    continue;

                if (mo.ProductDomain == ProductDomain.Firewall)
                {
                    BindFirewall(snapshot, mo);
                    continue;
                }

                var binder = Binders.FirstOrDefault(b => b.CanBind(mo));
                if (binder is not null)
                {
                    binder.Bind(snapshot, mo);
                }
                else
                {
                    mo.CurrentState = "Not observed in this scan";
                    mo.LastVerified = DateTime.UtcNow;
                    mo.Observation ??= new SettingObservation();
                    mo.Observation.CurrentValue = mo.CurrentState;
                    mo.Observation.ObservedAt = mo.LastVerified;
                }
            }

            Relationships.Apply(list);
        }

        private static bool BindNativeInventory(InventorySnapshot snapshot, ManagedObject mo)
        {
            string? value = null;
            var source = mo.TechnicalLocation;

            if (mo.FeatureCategory == FeatureCategory.WindowsService)
            {
                var name = mo.WritableTarget?.Kind == WritableTargetKind.Service
                    ? mo.WritableTarget.Identifier
                    : mo.DiscoveryMethod.StartsWith("ServiceController:", StringComparison.OrdinalIgnoreCase)
                        ? mo.DiscoveryMethod["ServiceController:".Length..]
                        : string.Empty;
                var service = snapshot.Services.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                value = service is null ? "Not installed" : $"Startup:{service.StartMode}; State:{service.State}";
            }
            else if (mo.FeatureCategory == FeatureCategory.ScheduledTask)
            {
                var path = mo.WritableTarget?.Identifier ?? string.Empty;
                var task = snapshot.ScheduledTasks.FirstOrDefault(t => t.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
                value = task is null ? "Not installed" :
                    task.State.Contains("Disabled", StringComparison.OrdinalIgnoreCase) ? "Disabled" : "Enabled";
            }
            else if (mo.FeatureCategory == FeatureCategory.AppxPackage)
            {
                var package = mo.WritableTarget?.Identifier ?? string.Empty;
                value = snapshot.InstalledPackages.Contains(package, StringComparer.OrdinalIgnoreCase)
                    ? "Installed"
                    : "Not installed";
            }
            else if (mo.FeatureCategory == FeatureCategory.OptionalFeature)
            {
                var feature = mo.WritableTarget?.Identifier ?? string.Empty;
                var observed = snapshot.OptionalFeatures.FirstOrDefault(f => f.Name.Equals(feature, StringComparison.OrdinalIgnoreCase));
                value = observed?.State ?? "Not observed in this scan";
            }

            if (value is null)
                return false;

            mo.CurrentState = value;
            mo.LastVerified = DateTime.UtcNow;
            mo.Observation ??= new SettingObservation();
            mo.Observation.CurrentValue = value;
            mo.Observation.ObservedAt = mo.LastVerified;
            mo.Observation.SourceSummary = source;
            mo.Observation.Layers =
            [
                new ConfigurationObservation
                {
                    ObjectId = mo.ObjectId,
                    Layer = ConfigurationLayer.MachinePolicy,
                    RawValue = value,
                    SourcePath = source,
                    ObservedAt = DateTime.UtcNow,
                    ConfidenceScore = value.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ? 40 : 85,
                    CollectorName = "InventoryStateBinder",
                    EvidenceSource = source,
                    EffectiveConfidence = value.Contains("Not observed", StringComparison.OrdinalIgnoreCase)
                        ? EffectiveConfidence.Low
                        : EffectiveConfidence.High
                }
            ];
            return true;
        }

        private static void BindFirewall(InventorySnapshot snapshot, ManagedObject mo)
        {
            mo.Observation ??= new SettingObservation();
            mo.LastVerified = DateTime.UtcNow;

            string value = "Not observed in this scan";
            string source = string.Empty;
            string notes = snapshot.Networking.FirewallCollectionNotes ?? string.Empty;

            if (mo.ObjectId.Equals("firewall.service.mpssvc", StringComparison.OrdinalIgnoreCase))
            {
                value = string.IsNullOrWhiteSpace(snapshot.Networking.FirewallServiceState)
                    ? "Unknown"
                    : snapshot.Networking.FirewallServiceState;
                source = "ServiceController:MpsSvc";
            }
            else if (mo.ObjectId.Equals("firewall.logging.summary", StringComparison.OrdinalIgnoreCase))
            {
                var logs = snapshot.Networking.FirewallProfiles
                    .Where(p => !string.IsNullOrWhiteSpace(p.LoggingEnabled) &&
                                !p.LoggingEnabled.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                    .Select(p => $"{p.ProfileName}:{p.LoggingEnabled}")
                    .ToList();
                value = logs.Count > 0 ? string.Join("; ", logs) : "Unknown";
                source = "FirewallPolicy/*/Logging";
            }
            else
            {
                var profileName = mo.ObjectId.Contains(".domain.", StringComparison.OrdinalIgnoreCase) ? "Domain"
                    : mo.ObjectId.Contains(".private.", StringComparison.OrdinalIgnoreCase) ? "Private"
                    : mo.ObjectId.Contains(".public.", StringComparison.OrdinalIgnoreCase) ? "Public"
                    : null;

                var profile = profileName is null
                    ? null
                    : snapshot.Networking.FirewallProfiles.FirstOrDefault(p =>
                        p.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase));

                if (profile is not null)
                {
                    source = profile.SourcePath;
                    if (mo.ObjectId.EndsWith(".enabled", StringComparison.OrdinalIgnoreCase))
                        value = profile.Enabled;
                    else if (mo.ObjectId.EndsWith(".inbound", StringComparison.OrdinalIgnoreCase))
                        value = profile.DefaultInboundAction;
                    else if (mo.ObjectId.EndsWith(".outbound", StringComparison.OrdinalIgnoreCase))
                        value = profile.DefaultOutboundAction;
                    else if (mo.ObjectId.EndsWith(".notifications", StringComparison.OrdinalIgnoreCase))
                        value = profile.InboundNotifications;
                    else
                        value = "Unknown";
                    notes = profile.CollectionNotes;
                }
            }

            mo.CurrentState = value;
            mo.Observation.CurrentValue = value;
            mo.Observation.ObservedAt = mo.LastVerified;
            mo.Observation.SourceSummary = source;
            mo.Observation.Layers =
            [
                new ConfigurationObservation
                {
                    ObjectId = mo.ObjectId,
                    Layer = ConfigurationLayer.MachinePolicy,
                    RawValue = value,
                    SourcePath = source,
                    ObservedAt = DateTime.UtcNow,
                    ConfidenceScore = value is "Unknown" or "Not observed in this scan" ? 40 : 85,
                    CollectorName = "FirewallCollector",
                    EvidenceSource = string.IsNullOrWhiteSpace(source) ? "FirewallCollector" : source,
                    CollectionNotes = notes,
                    EffectiveConfidence = value is "Unknown" or "Not observed in this scan"
                        ? EffectiveConfidence.Low
                        : EffectiveConfidence.High
                }
            ];
        }

        public static ObservationSummary BuildSummary(
            InventorySnapshot snapshot,
            IReadOnlyList<ManagedObject> catalog,
            int validationPassed,
            int validationFailed)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));
            if (catalog is null)
                throw new ArgumentNullException(nameof(catalog));

            var summary = new ObservationSummary
            {
                CatalogTotal = catalog.Count,
                CatalogValidationPassed = validationPassed,
                CatalogValidationFailed = validationFailed,
                GeneratedAt = DateTime.UtcNow
            };

            foreach (var mo in catalog)
            {
                if (mo is null)
                    continue;

                var state = mo.CurrentState ?? "Not observed in this scan";
                var observed = BinderHelpers.IsObserved(state);

                if (observed)
                    summary.ObservedCount++;
                else
                    summary.NotObservedCount++;

                switch (mo.RiskLevel)
                {
                    case RiskLevel.High:
                        summary.HighRiskCount++;
                        if (observed && !BinderHelpers.IsNotConfigured(state))
                            summary.HighRiskItems.Add(ToItem(mo, state));
                        break;
                    case RiskLevel.Medium:
                        summary.MediumRiskCount++;
                        if (observed && !BinderHelpers.IsNotConfigured(state))
                            summary.MediumRiskItems.Add(ToItem(mo, state));
                        break;
                    default:
                        summary.LowRiskCount++;
                        break;
                }

                if (mo.FeatureCategory == FeatureCategory.PrivacyPermission ||
                    string.Equals(mo.ObjectType, "PrivacySetting", StringComparison.OrdinalIgnoreCase))
                {
                    TallyPrivacyValue(state, summary);
                }
            }

            foreach (var p in snapshot.PolicySettings)
            {
                if (BinderHelpers.IsNotConfigured(p.Value) || BinderHelpers.IsError(p.Value))
                    summary.NotConfiguredPolicyCount++;
                else
                    summary.ConfiguredPolicyCount++;
            }

            return summary;
        }

        public static string ResolveCurrentValue(InventorySnapshot snapshot, ManagedObject mo)
        {
            if (mo?.CurrentState is not null)
                return mo.CurrentState;

            if (snapshot is null || mo is null)
                return "Not observed in this scan";

            var binder = Binders.FirstOrDefault(b => b.CanBind(mo));
            binder?.Bind(snapshot, mo);
            return mo.CurrentState ?? "Not observed in this scan";
        }

        private static void TallyPrivacyValue(string state, ObservationSummary summary)
        {
            if (string.IsNullOrWhiteSpace(state))
                return;

            var token = state.Trim();
            if (token.Equals("Allow", StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith("Allow ", StringComparison.OrdinalIgnoreCase))
            {
                summary.PrivacyAllowCount++;
            }
            else if (token.Equals("Deny", StringComparison.OrdinalIgnoreCase) ||
                     token.StartsWith("Deny ", StringComparison.OrdinalIgnoreCase))
            {
                summary.PrivacyDenyCount++;
            }
            else if (token.Equals("Prompt", StringComparison.OrdinalIgnoreCase) ||
                     token.StartsWith("Prompt ", StringComparison.OrdinalIgnoreCase))
            {
                summary.PrivacyPromptCount++;
            }
        }

        private static ObservedItem ToItem(ManagedObject mo, string state) =>
            new()
            {
                ObjectId = mo.ObjectId,
                ObjectName = mo.ObjectName,
                ProductDomain = mo.ProductDomain,
                SubCategory = mo.SubCategory ?? string.Empty,
                RiskLevel = mo.RiskLevel,
                CurrentState = state
            };
    }
}
