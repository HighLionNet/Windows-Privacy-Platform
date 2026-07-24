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
                var binder = Binders.FirstOrDefault(b => b.CanBind(mo));
                if (binder is not null)
                {
                    binder.Bind(snapshot, mo);
                }
                else
                {
                    // Fallback: preserve prior behavior for unknown types
                    mo.CurrentState = "Not observed in this scan";
                    mo.LastVerified = DateTime.UtcNow;
                    mo.Observation ??= new SettingObservation();
                    mo.Observation.CurrentValue = mo.CurrentState;
                    mo.Observation.ObservedAt = mo.LastVerified;
                }
            }

            // Relationships + effective-state foundation (after all raw binds)
            Relationships.Apply(list);
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

        /// <summary>
        /// Compatibility helper used by older call sites; prefers already-bound CurrentState.
        /// </summary>
        public static string ResolveCurrentValue(InventorySnapshot snapshot, ManagedObject mo)
        {
            if (mo?.CurrentState is not null)
                return mo.CurrentState;

            if (snapshot is null || mo is null)
                return "Not observed in this scan";

            // Transient bind for a single object if needed
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
