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

        /// <summary>
        /// User preference vs machine GPO for the same feature (non-ConsentStore).
        /// </summary>
        private static readonly (string UserId, string PolicyId, string Feature)[] UserVsGpoPairs =
        {
            ("privacy.advertisingid.enabled", "policy.advertising.disabledbygpo", "Advertising ID")
        };

        /// <summary>
        /// Related feature groups (no automatic precedence; documentation edges).
        /// </summary>
        private static readonly (string FromId, string ToId, RelationshipKind Kind, string Explanation)[] RelatedPairs =
        {
            // Location ecosystem
            ("privacy.consentstore.location", "policy.location.disablelocation",
                RelationshipKind.OverriddenBy,
                "Machine DisableLocation policy can turn off the location platform regardless of ConsentStore."),
            ("policy.location.disablelocation", "privacy.consentstore.location",
                RelationshipKind.Overrides,
                "Machine DisableLocation is a stronger kill-switch than per-app ConsentStore location."),
            ("policy.location.disablelocation", "policy.appprivacy.location",
                RelationshipKind.Related,
                "Both are machine-level location controls; DisableLocation targets the platform, AppPrivacy targets app access."),
            ("policy.findmydevice.allow", "policy.location.disablelocation",
                RelationshipKind.DependsOn,
                "Find My Device relies on location services; disabling location reduces Find My Device usefulness."),

            // Advertising / telemetry personalization
            ("privacy.advertisingid.enabled", "privacy.tailoredexperiences",
                RelationshipKind.Related,
                "Advertising ID and tailored experiences both relate to personalized content, but control different mechanisms."),
            ("privacy.tailoredexperiences", "policy.telemetry.allowtelemetry",
                RelationshipKind.Related,
                "Tailored experiences reuse diagnostic data; the diagnostic level is controlled separately by AllowTelemetry."),
            ("privacy.tailoredexperiences", "policy.telemetry.allowtelemetry.currentversion",
                RelationshipKind.Related,
                "Tailored experiences reuse diagnostic data; alternate telemetry policy path may be the effective store."),

            // Activity history group
            ("policy.activity.enableactivityfeed", "policy.activity.publishuseractivities",
                RelationshipKind.Related,
                "Activity feed and publish-user-activities together control local Timeline behavior."),
            ("policy.activity.publishuseractivities", "policy.activity.uploaduseractivities",
                RelationshipKind.Related,
                "Upload is a higher-privacy-impact step than local publish-only activity history."),
            ("policy.activity.uploaduseractivities", "policy.activity.enableactivityfeed",
                RelationshipKind.DependsOn,
                "Cloud upload of activities is meaningful only when activity feed features are enabled."),

            // Search group
            ("policy.search.disablewebsearch", "policy.search.connectedsearchuseweb",
                RelationshipKind.Related,
                "Both reduce or remove web-backed search results from Windows Search."),
            ("policy.search.allowsearchlocation", "privacy.consentstore.location",
                RelationshipKind.Related,
                "Search location use is a separate consumer of location data from general app ConsentStore location."),
            ("policy.search.allowcortana", "policy.search.disablewebsearch",
                RelationshipKind.Related,
                "Cortana/cloud assistant features and web search are related cloud search surfaces.")
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
            WireUserVsGpoPairs(byId);
            WireRelatedPairs(byId);
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

        private static void WireUserVsGpoPairs(Dictionary<string, ManagedObject> byId)
        {
            foreach (var (userId, policyId, feature) in UserVsGpoPairs)
            {
                if (!byId.TryGetValue(userId, out var user) || !byId.TryGetValue(policyId, out var policy))
                    continue;

                AddRelationship(user, policyId, RelationshipKind.OverriddenBy,
                    $"Machine Group Policy can force {feature} off regardless of the user toggle.");
                AddRelationship(policy, userId, RelationshipKind.Overrides,
                    $"Machine Group Policy overrides the user {feature} preference.");
                AddRelationship(user, policyId, RelationshipKind.ConflictsWith,
                    $"User preference and machine policy may disagree for {feature}.");

                AddRelatedFeature(user, policyId);
                AddRelatedFeature(policy, userId);

                // Generic layer-rank resolution (MachinePolicy > UserPreference)
                var userLayer = user.Observation?.Layers?.FirstOrDefault();
                var policyLayer = policy.Observation?.Layers?.FirstOrDefault();
                var resolution = PolicyPrecedenceResolver.ResolveByLayerRank(
                    new[] { userLayer, policyLayer }.Where(l => l is not null).Cast<ConfigurationObservation>().ToList(),
                    feature);

                ApplyResolution(user, resolution);
                ApplyResolution(policy, resolution);
            }
        }

        private static void WireRelatedPairs(Dictionary<string, ManagedObject> byId)
        {
            foreach (var (fromId, toId, kind, explanation) in RelatedPairs)
            {
                if (!byId.TryGetValue(fromId, out var from) || !byId.ContainsKey(toId))
                    continue;

                AddRelationship(from, toId, kind, explanation);
                AddRelatedFeature(from, toId);
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
