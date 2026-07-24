// Source/WindowsPrivacyPlatform.Scanner/InventoryStateBinder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only binder: maps InventorySnapshot values onto ManagedObject.CurrentState.
    /// Does not write to the system. Does not elevate.
    /// </summary>
    public static class InventoryStateBinder
    {
        public static void Bind(InventorySnapshot snapshot, IEnumerable<ManagedObject> catalog)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));
            if (catalog is null)
                throw new ArgumentNullException(nameof(catalog));

            var now = DateTime.UtcNow;

            foreach (var mo in catalog)
            {
                if (mo is null)
                    continue;

                mo.CurrentState = ResolveCurrentValue(snapshot, mo);
                mo.LastVerified = now;
            }
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

                var state = mo.CurrentState ?? ResolveCurrentValue(snapshot, mo);
                var observed = IsObserved(state);

                if (observed)
                    summary.ObservedCount++;
                else
                    summary.NotObservedCount++;

                switch (mo.RiskLevel)
                {
                    case RiskLevel.High:
                        summary.HighRiskCount++;
                        if (observed && !IsNotConfigured(state))
                            summary.HighRiskItems.Add(ToItem(mo, state));
                        break;
                    case RiskLevel.Medium:
                        summary.MediumRiskCount++;
                        if (observed && !IsNotConfigured(state))
                            summary.MediumRiskItems.Add(ToItem(mo, state));
                        break;
                    default:
                        summary.LowRiskCount++;
                        break;
                }

                // Only ConsentStore / privacy preference values use Allow|Deny|Prompt semantics.
                if (mo.FeatureCategory == FeatureCategory.PrivacyPermission ||
                    string.Equals(mo.ObjectType, "PrivacySetting", StringComparison.OrdinalIgnoreCase))
                {
                    TallyPrivacyValue(state, summary);
                }
            }

            foreach (var p in snapshot.PolicySettings)
            {
                if (IsNotConfigured(p.Value) || IsError(p.Value))
                    summary.NotConfiguredPolicyCount++;
                else
                    summary.ConfiguredPolicyCount++;
            }

            return summary;
        }

        public static string ResolveCurrentValue(InventorySnapshot snapshot, ManagedObject mo)
        {
            if (snapshot is null || mo is null)
                return "Not observed in this scan";

            var policy = snapshot.PolicySettings.FirstOrDefault(p =>
                string.Equals(p.Name, mo.ObjectId, StringComparison.OrdinalIgnoreCase));
            if (policy is not null)
                return $"{policy.Value} ({policy.Hive})";

            var shortName = ExtractShortName(mo.ObjectId);

            var privacy = snapshot.PrivacySettings.FirstOrDefault(p =>
                string.Equals(p.Name, shortName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, mo.ObjectId, StringComparison.OrdinalIgnoreCase));

            if (privacy is not null)
                return privacy.Value;

            privacy = snapshot.PrivacySettings.FirstOrDefault(p =>
                NamesLooselyMatch(mo.ObjectId, p.Name) ||
                NamesLooselyMatch(shortName, p.Name));

            return privacy?.Value ?? "Not observed in this scan";
        }

        private static string ExtractShortName(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
                return string.Empty;

            var idx = objectId.LastIndexOf('.');
            return idx >= 0 && idx < objectId.Length - 1
                ? objectId[(idx + 1)..]
                : objectId;
        }

        private static bool NamesLooselyMatch(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            static string Norm(string s) =>
                s.Replace(".", "", StringComparison.Ordinal)
                 .Replace("-", "", StringComparison.Ordinal)
                 .Replace("_", "", StringComparison.Ordinal)
                 .ToLowerInvariant();

            var na = Norm(a);
            var nb = Norm(b);
            return na.Contains(nb, StringComparison.Ordinal) ||
                   nb.Contains(na, StringComparison.Ordinal);
        }

        private static bool IsObserved(string? state) =>
            !string.IsNullOrWhiteSpace(state) &&
            !string.Equals(state, "Not observed in this scan", StringComparison.OrdinalIgnoreCase);

        private static bool IsNotConfigured(string? state) =>
            !string.IsNullOrWhiteSpace(state) &&
            state.Contains("Not configured", StringComparison.OrdinalIgnoreCase);

        private static bool IsError(string? state) =>
            !string.IsNullOrWhiteSpace(state) &&
            state.Contains("Error reading", StringComparison.OrdinalIgnoreCase);

        private static void TallyPrivacyValue(string state, ObservationSummary summary)
        {
            if (string.IsNullOrWhiteSpace(state))
                return;

            // Exact-ish ConsentStore tokens; avoid matching unrelated strings.
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
