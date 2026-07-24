// Source/WindowsPrivacyPlatform.Scanner/Binding/RelationshipBinder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Post-bind relationship wiring and best-effort effective-state resolution.
    /// Does not hide conflicts. Read-only — no system writes.
    /// </summary>
    public sealed class RelationshipBinder
    {
        public string Name => nameof(RelationshipBinder);

        // Known same-feature pairs: user consent vs machine AppPrivacy GPO.
        private static readonly (string UserId, string PolicyId, string Feature)[] ConsentPolicyPairs =
        {
            ("privacy.consentstore.location", "policy.appprivacy.location", "Location"),
            ("privacy.consentstore.webcam", "policy.appprivacy.camera", "Camera"),
            ("privacy.consentstore.microphone", "policy.appprivacy.microphone", "Microphone"),
            ("privacy.consentstore.broadFileSystemAccess", "policy.appprivacy.filesystem", "FileSystem")
        };

        // Alternate registry paths for the same semantic setting.
        private static readonly (string PrimaryId, string AlternateId, string Feature)[] AlternatePathPairs =
        {
            ("policy.telemetry.allowtelemetry", "policy.telemetry.allowtelemetry.currentversion", "AllowTelemetry")
        };

        public void Apply(IReadOnlyList<ManagedObject> catalog)
        {
            if (catalog is null || catalog.Count == 0)
                return;

            var byId = catalog
                .Where(m => m is not null && !string.IsNullOrWhiteSpace(m.ObjectId))
                .ToDictionary(m => m.ObjectId, StringComparer.OrdinalIgnoreCase);

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

                AddRelatedFeature(user, policyId);
                AddRelatedFeature(policy, userId);

                ResolveConsentVsPolicy(user, policy, feature);
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

                AddRelatedFeature(primary, alternateId);
                AddRelatedFeature(alternate, primaryId);

                ResolveAlternatePolicyPaths(primary, alternate, feature);
            }
        }

        private static void ResolveConsentVsPolicy(ManagedObject user, ManagedObject policy, string feature)
        {
            var userVal = NormalizePrivacy(user.CurrentState);
            var policyRaw = ExtractRawPolicyValue(policy.CurrentState);
            var policyConfigured = !string.IsNullOrWhiteSpace(policyRaw) &&
                                   !policyRaw.Contains("Not configured", StringComparison.OrdinalIgnoreCase) &&
                                   !policyRaw.Contains("Not observed", StringComparison.OrdinalIgnoreCase);

            // Typical AppPrivacy GPO: 0=User control, 1=Force allow, 2=Force deny
            string? effective;
            ConfigurationLayer source;
            string explanation;
            bool conflict = false;
            var confidence = EffectiveConfidence.Medium;

            if (!policyConfigured)
            {
                effective = user.CurrentState;
                source = ConfigurationLayer.UserPreference;
                explanation = $"{feature}: no machine AppPrivacy force policy observed; user ConsentStore value applies.";
            }
            else if (policyRaw == "0")
            {
                effective = user.CurrentState;
                source = ConfigurationLayer.UserPreference;
                explanation = $"{feature}: machine policy is user-controlled (0); ConsentStore value is effective.";
            }
            else if (policyRaw == "1")
            {
                effective = "Allow (forced by machine policy)";
                source = ConfigurationLayer.MachinePolicy;
                explanation = $"{feature}: machine AppPrivacy forces Allow (1), overriding user ConsentStore ({userVal}).";
                conflict = !string.Equals(userVal, "Allow", StringComparison.OrdinalIgnoreCase);
            }
            else if (policyRaw == "2")
            {
                effective = "Deny (forced by machine policy)";
                source = ConfigurationLayer.MachinePolicy;
                explanation = $"{feature}: machine AppPrivacy forces Deny (2), overriding user ConsentStore ({userVal}).";
                conflict = !string.Equals(userVal, "Deny", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                effective = null;
                source = ConfigurationLayer.Unknown;
                explanation = $"{feature}: machine policy value '{policyRaw}' is not a known AppPrivacy force code; effective state unknown.";
                confidence = EffectiveConfidence.Low;
            }

            var layers = CollectLayers(user, policy);
            var eff = new EffectiveState
            {
                EffectiveValue = effective,
                EffectiveSource = source,
                Confidence = confidence,
                Explanation = explanation,
                HasConflict = conflict,
                ContributingLayers = layers
            };

            user.Observation ??= new SettingObservation();
            user.Observation.Effective = eff;
            policy.Observation ??= new SettingObservation();
            policy.Observation.Effective = new EffectiveState
            {
                EffectiveValue = effective,
                EffectiveSource = source,
                Confidence = confidence,
                Explanation = explanation,
                HasConflict = conflict,
                ContributingLayers = layers
            };
        }

        private static void ResolveAlternatePolicyPaths(ManagedObject primary, ManagedObject alternate, string feature)
        {
            var primaryRaw = ExtractRawPolicyValue(primary.CurrentState);
            var alternateRaw = ExtractRawPolicyValue(alternate.CurrentState);

            var primaryConfigured = IsConfiguredRaw(primaryRaw);
            var alternateConfigured = IsConfiguredRaw(alternateRaw);

            string? effective;
            ConfigurationLayer source;
            string explanation;
            bool conflict;
            var confidence = EffectiveConfidence.Medium;

            if (primaryConfigured && alternateConfigured)
            {
                conflict = !string.Equals(primaryRaw, alternateRaw, StringComparison.OrdinalIgnoreCase);
                // Prefer explicit SOFTWARE\Policies (MachinePolicy) when both present.
                effective = primaryRaw;
                source = ConfigurationLayer.MachinePolicy;
                explanation = conflict
                    ? $"{feature}: both policy stores configured with different values (Policies={primaryRaw}, CurrentVersion={alternateRaw}). Preferring Group Policy store; treat as conflict until MDM/baseline rules exist."
                    : $"{feature}: both policy stores agree ({primaryRaw}).";
            }
            else if (primaryConfigured)
            {
                conflict = false;
                effective = primaryRaw;
                source = ConfigurationLayer.MachinePolicy;
                explanation = $"{feature}: only Group Policy store configured ({primaryRaw}).";
            }
            else if (alternateConfigured)
            {
                conflict = false;
                effective = alternateRaw;
                source = ConfigurationLayer.AlternatePolicyStore;
                explanation = $"{feature}: only CurrentVersion policy store configured ({alternateRaw}).";
            }
            else
            {
                conflict = false;
                effective = null;
                source = ConfigurationLayer.Unknown;
                explanation = $"{feature}: neither policy store configured.";
                confidence = EffectiveConfidence.Low;
            }

            var layers = CollectLayers(primary, alternate);
            var eff = new EffectiveState
            {
                EffectiveValue = effective,
                EffectiveSource = source,
                Confidence = confidence,
                Explanation = explanation,
                HasConflict = conflict,
                ContributingLayers = layers
            };

            primary.Observation ??= new SettingObservation();
            primary.Observation.Effective = eff;
            alternate.Observation ??= new SettingObservation();
            alternate.Observation.Effective = new EffectiveState
            {
                EffectiveValue = effective,
                EffectiveSource = source,
                Confidence = confidence,
                Explanation = explanation,
                HasConflict = conflict,
                ContributingLayers = layers
            };
        }

        private static List<ConfigurationObservation> CollectLayers(params ManagedObject[] objects)
        {
            var list = new List<ConfigurationObservation>();
            foreach (var mo in objects)
            {
                if (mo?.Observation?.Layers is null)
                    continue;
                list.AddRange(mo.Observation.Layers);
            }
            return list;
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

        private static string NormalizePrivacy(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;
            var token = state.Trim();
            if (token.StartsWith("Allow", StringComparison.OrdinalIgnoreCase)) return "Allow";
            if (token.StartsWith("Deny", StringComparison.OrdinalIgnoreCase)) return "Deny";
            if (token.StartsWith("Prompt", StringComparison.OrdinalIgnoreCase)) return "Prompt";
            return token;
        }

        private static string ExtractRawPolicyValue(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;
            // Display format from PolicyBinder: "{value} ({hive})"
            var s = state.Trim();
            var idx = s.IndexOf(" (", StringComparison.Ordinal);
            return idx > 0 ? s[..idx].Trim() : s;
        }

        private static bool IsConfiguredRaw(string? raw) =>
            !string.IsNullOrWhiteSpace(raw) &&
            !raw.Contains("Not configured", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("Not observed", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("Error reading", StringComparison.OrdinalIgnoreCase);
    }
}
