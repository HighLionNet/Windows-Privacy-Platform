// Source/WindowsPrivacyPlatform.Scanner/Binding/RelationshipBinder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Post-bind relationship wiring and effective-state resolution.
    /// Delegates precedence decisions to PolicyPrecedenceResolver.
    /// Does not hide conflicts. Read-only.
    /// </summary>
    public sealed class RelationshipBinder
    {
        public string Name => nameof(RelationshipBinder);

        private static readonly (string UserId, string PolicyId, string Feature)[] ConsentPolicyPairs =
        {
            ("privacy.consentstore.location", "policy.appprivacy.location", "Location"),
            ("privacy.consentstore.webcam", "policy.appprivacy.camera", "Camera"),
            ("privacy.consentstore.microphone", "policy.appprivacy.microphone", "Microphone"),
            ("privacy.consentstore.broadFileSystemAccess", "policy.appprivacy.filesystem", "File system")
        };

        private static readonly (string PrimaryId, string AlternateId, string Feature)[] AlternatePathPairs =
        {
            ("policy.telemetry.allowtelemetry", "policy.telemetry.allowtelemetry.currentversion", "Allow Telemetry")
        };

        public void Apply(IReadOnlyList<ManagedObject> catalog)
        {
            if (catalog is null || catalog.Count == 0)
                return;

            var byId = catalog
                .Where(m => m is not null && !string.IsNullOrWhiteSpace(m.ObjectId))
                .GroupBy(m => m.ObjectId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            WireConsentPolicyPairs(byId);
            WireAlternatePathPairs(byId);
        }

        private static void WireConsentPolicyPairs(Dictionary<string, ManagedObject> byId)
        {
            foreach (var (userId, policyId, feature) in ConsentPolicyPairs)
            {
                if (!byId.TryGetValue(userId, out var user) || !byId.TryGetValue(policyId, out var policy))
                    continue;

                AddRelationship(user, policyId, RelationshipKind.OverriddenBy,
                    $"Machine AppPrivacy policy can override user ConsentStore for {feature}.");
                AddRelationship(policy, userId, RelationshipKind.Overrides,
                    $"Machine AppPrivacy policy overrides user ConsentStore for {feature}.");
                AddRelationship(user, policyId, RelationshipKind.ConflictsWith,
                    $"User preference and machine policy may disagree for {feature}.");
                AddRelationship(policy, userId, RelationshipKind.Affects,
                    $"Changes the effective {feature} capability for applications.");

                AddRelatedFeature(user, policyId);
                AddRelatedFeature(policy, userId);

                var userLayer = user.Observation?.Layers?.FirstOrDefault();
                var policyLayer = policy.Observation?.Layers?.FirstOrDefault();
                var resolution = PolicyPrecedenceResolver.ResolveConsentVsAppPrivacy(
                    userLayer, policyLayer, feature);

                ApplyResolution(user, resolution);
                ApplyResolution(policy, resolution);
            }
        }

        private static void WireAlternatePathPairs(Dictionary<string, ManagedObject> byId)
        {
            foreach (var (primaryId, alternateId, feature) in AlternatePathPairs)
            {
                if (!byId.TryGetValue(primaryId, out var primary) || !byId.TryGetValue(alternateId, out var alternate))
                    continue;

                AddRelationship(primary, alternateId, RelationshipKind.SameFeatureAlternatePath,
                    $"{feature}: alternate machine policy store path.");
                AddRelationship(alternate, primaryId, RelationshipKind.SameFeatureAlternatePath,
                    $"{feature}: primary Group Policy store path.");
                AddRelationship(primary, alternateId, RelationshipKind.ConflictsWith,
                    $"{feature}: dual policy stores can disagree.");

                AddRelatedFeature(primary, alternateId);
                AddRelatedFeature(alternate, primaryId);

                var primaryLayer = primary.Observation?.Layers?.FirstOrDefault();
                var alternateLayer = alternate.Observation?.Layers?.FirstOrDefault();
                var resolution = PolicyPrecedenceResolver.ResolveAlternateMachinePolicyPaths(
                    primaryLayer, alternateLayer, feature);

                ApplyResolution(primary, resolution);
                ApplyResolution(alternate, resolution);
            }
        }

        private static void ApplyResolution(ManagedObject mo, ConfigurationResolution resolution)
        {
            mo.Observation ??= new SettingObservation();
            mo.Observation.Resolution = resolution;
            mo.Observation.Effective = resolution.ToEffectiveState();
        }

        private static void AddRelationship(ManagedObject from, string toId, RelationshipKind kind, string explanation)
        {
            from.StructuredRelationships ??= new List<SettingRelationship>();
            if (from.StructuredRelationships.Any(r =>
                    string.Equals(r.ToObjectId, toId, StringComparison.OrdinalIgnoreCase) && r.Kind == kind))
                return;

            from.StructuredRelationships.Add(new SettingRelationship
            {
                FromObjectId = from.ObjectId,
                ToObjectId = toId,
                Kind = kind,
                Explanation = explanation
            });
        }

        private static void AddRelatedFeature(ManagedObject mo, string relatedId)
        {
            mo.RelatedFeature ??= new List<string>();
            if (!mo.RelatedFeature.Any(id => string.Equals(id, relatedId, StringComparison.OrdinalIgnoreCase)))
                mo.RelatedFeature.Add(relatedId);
        }
    }
}
