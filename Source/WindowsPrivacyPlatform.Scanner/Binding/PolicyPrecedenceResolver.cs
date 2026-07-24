// Source/WindowsPrivacyPlatform.Scanner/Binding/PolicyPrecedenceResolver.cs
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Central place for configuration-layer precedence and effective-value reasoning.
    /// Read-only pure logic — no registry access, no writes, no elevation.
    /// Semantic meaning of raw codes comes only from catalog ValueSemantics (via ValueSemanticsInterpreter).
    /// Never silently guesses: unknown inputs yield Unknown confidence and clear reasons.
    /// </summary>
    public static class PolicyPrecedenceResolver
    {
        /// <summary>
        /// Relative strength for generic multi-layer comparison (higher wins when both configured).
        /// </summary>
        public static int LayerRank(ConfigurationLayer layer) => layer switch
        {
            ConfigurationLayer.SecurityBaseline => 60,
            ConfigurationLayer.MDMPolicy => 50,
            ConfigurationLayer.MachinePolicy => 40,
            ConfigurationLayer.AlternatePolicyStore => 30,
            ConfigurationLayer.ApplicationPreference => 20,
            ConfigurationLayer.UserPreference => 10,
            _ => 0
        };

        public static bool IsConfiguredValue(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            return !raw.Contains("Not configured", StringComparison.OrdinalIgnoreCase) &&
                   !raw.Contains("Not observed", StringComparison.OrdinalIgnoreCase) &&
                   !raw.Contains("Error reading", StringComparison.OrdinalIgnoreCase);
        }

        public static string ExtractRawPolicyValue(string? state) =>
            ValueSemanticsInterpreter.Normalize(state);

        public static string NormalizePrivacyToken(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;
            var token = state.Trim();
            if (token.StartsWith("Allow", StringComparison.OrdinalIgnoreCase)) return "Allow";
            if (token.StartsWith("Deny", StringComparison.OrdinalIgnoreCase)) return "Deny";
            if (token.StartsWith("Prompt", StringComparison.OrdinalIgnoreCase)) return "Prompt";
            return token;
        }

        /// <summary>
        /// Resolve user ConsentStore vs machine AppPrivacy LetApps* policy.
        /// Meaning of AppPrivacy codes (0/1/2) is taken from the policy catalog entry's ValueSemantics only.
        /// </summary>
        public static ConfigurationResolution ResolveConsentVsAppPrivacy(
            ConfigurationObservation? userLayer,
            ConfigurationObservation? policyLayer,
            ManagedObject? policyDefinition,
            string featureName)
        {
            var observations = new List<ConfigurationObservation>();
            if (userLayer is not null) observations.Add(userLayer);
            if (policyLayer is not null) observations.Add(policyLayer);

            var userVal = NormalizePrivacyToken(userLayer?.RawValue);
            var policyRaw = ExtractRawPolicyValue(policyLayer?.RawValue);
            var policyConfigured = IsConfiguredValue(policyRaw);

            if (!policyConfigured)
            {
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = string.IsNullOrEmpty(userVal) ? userLayer?.RawValue : userVal,
                    EffectiveSource = ConfigurationLayer.UserPreference,
                    Confidence = string.IsNullOrEmpty(userVal) ? EffectiveConfidence.Low : EffectiveConfidence.High,
                    ResolutionReason =
                        $"{featureName}: no machine AppPrivacy force policy is configured at the probed path. " +
                        "Windows therefore evaluates the per-user ConsentStore preference for this capability.",
                    ConfidenceReason =
                        string.IsNullOrEmpty(userVal)
                            ? "User preference not observed; confidence low."
                            : "Policy absent; user preference observed directly from ConsentStore.",
                    HasConflict = false
                };
            }

            var meaning = ValueSemanticsInterpreter.Interpret(policyDefinition, policyRaw);
            var canonical = meaning?.Canonical ?? string.Empty;
            var display = meaning?.DisplayLabel ?? policyRaw;

            // Knowledge-driven interpretation of force codes. If map missing, stay Unknown — never invent.
            if (meaning is null)
            {
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = null,
                    EffectiveSource = ConfigurationLayer.Unknown,
                    Confidence = EffectiveConfidence.Low,
                    ResolutionReason =
                        $"{featureName}: machine AppPrivacy value '{policyRaw}' has no semantic mapping in the knowledge catalog. " +
                        "Effective access cannot be stated without inventing meaning.",
                    ConfidenceReason = "Raw policy value present but no ValueSemantics entry for this ObjectId/raw pair.",
                    SemanticValue = null,
                    SemanticDisplay = null,
                    HasConflict = true
                };
            }

            if (string.Equals(canonical, "UserControlled", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(canonical, "User", StringComparison.OrdinalIgnoreCase))
            {
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = string.IsNullOrEmpty(userVal) ? userLayer?.RawValue : userVal,
                    EffectiveSource = ConfigurationLayer.UserPreference,
                    Confidence = EffectiveConfidence.High,
                    ResolutionReason =
                        $"{featureName}: machine AppPrivacy is set to {display} ({policyRaw}). " +
                        "This code means the user ConsentStore value remains effective; Windows does not force allow or deny.",
                    ConfidenceReason = "Known AppPrivacy code from catalog ValueSemantics; user preference observed.",
                    SemanticValue = string.IsNullOrEmpty(userVal) ? null : NormalizePrivacyToken(userVal),
                    SemanticDisplay = string.IsNullOrEmpty(userVal) ? null : userVal,
                    HasConflict = false
                };
            }

            if (string.Equals(canonical, "ForceAllow", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(canonical, "Allow", StringComparison.OrdinalIgnoreCase))
            {
                var conflict = !string.Equals(userVal, "Allow", StringComparison.OrdinalIgnoreCase) &&
                               IsConfiguredValue(userVal);
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = "Allow",
                    EffectiveSource = ConfigurationLayer.MachinePolicy,
                    Confidence = EffectiveConfidence.High,
                    ResolutionReason =
                        $"{featureName}: machine AppPrivacy forces Allow ({display} / raw {policyRaw}). " +
                        "Machine policy is evaluated before per-user ConsentStore for capability access, so the user preference ({userVal}) is ignored while this policy remains.",
                    ConfidenceReason = "Known ForceAllow mapping from catalog; machine layer rank exceeds user preference.",
                    SemanticValue = "Allow",
                    SemanticDisplay = meaning.DisplayLabel,
                    HasConflict = conflict
                };
            }

            if (string.Equals(canonical, "ForceDeny", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(canonical, "Deny", StringComparison.OrdinalIgnoreCase))
            {
                var conflict = !string.Equals(userVal, "Deny", StringComparison.OrdinalIgnoreCase) &&
                               IsConfiguredValue(userVal);
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = "Deny",
                    EffectiveSource = ConfigurationLayer.MachinePolicy,
                    Confidence = EffectiveConfidence.High,
                    ResolutionReason =
                        $"{featureName}: machine AppPrivacy forces Deny ({display} / raw {policyRaw}). " +
                        "Machine policy is evaluated before per-user ConsentStore for capability access, so the user preference ({userVal}) is ignored while this policy remains.",
                    ConfidenceReason = "Known ForceDeny mapping from catalog; machine layer rank exceeds user preference.",
                    SemanticValue = "Deny",
                    SemanticDisplay = meaning.DisplayLabel,
                    HasConflict = conflict
                };
            }

            return new ConfigurationResolution
            {
                RawObservations = observations,
                EffectiveValue = null,
                EffectiveSource = ConfigurationLayer.Unknown,
                Confidence = EffectiveConfidence.Low,
                ResolutionReason =
                    $"{featureName}: machine AppPrivacy value '{policyRaw}' maps to '{canonical}' but is not a recognized force mode (UserControlled / ForceAllow / ForceDeny). Effective state is unknown.",
                ConfidenceReason = "Catalog map present but canonical form is not one of the three AppPrivacy force modes used by Windows.",
                SemanticValue = canonical,
                SemanticDisplay = display,
                HasConflict = true
            };
        }

        /// <summary>
        /// Resolve the same semantic setting stored in two machine policy paths.
        /// Prefers SOFTWARE\\Policies (MachinePolicy) over CurrentVersion\\Policies when both differ.
        /// </summary>
        public static ConfigurationResolution ResolveAlternateMachinePolicyPaths(
            ConfigurationObservation? primaryPoliciesPath,
            ConfigurationObservation? alternateCurrentVersionPath,
            ManagedObject? primaryDefinition,
            string featureName)
        {
            var observations = new List<ConfigurationObservation>();
            if (primaryPoliciesPath is not null) observations.Add(primaryPoliciesPath);
            if (alternateCurrentVersionPath is not null) observations.Add(alternateCurrentVersionPath);

            var primaryRaw = ExtractRawPolicyValue(primaryPoliciesPath?.RawValue);
            var alternateRaw = ExtractRawPolicyValue(alternateCurrentVersionPath?.RawValue);
            var primaryOk = IsConfiguredValue(primaryRaw);
            var alternateOk = IsConfiguredValue(alternateRaw);

            ValueMeaning? meaning = null;
            if (primaryOk)
                meaning = ValueSemanticsInterpreter.Interpret(primaryDefinition, primaryRaw);
            else if (alternateOk)
                meaning = ValueSemanticsInterpreter.Interpret(primaryDefinition, alternateRaw);

            if (primaryOk && alternateOk)
            {
                var conflict = !string.Equals(primaryRaw, alternateRaw, StringComparison.OrdinalIgnoreCase);
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = primaryRaw,
                    EffectiveSource = ConfigurationLayer.MachinePolicy,
                    Confidence = conflict ? EffectiveConfidence.Medium : EffectiveConfidence.High,
                    ResolutionReason = conflict
                        ? $"{featureName}: both machine policy stores are configured with different values " +
                          $"(Group Policy store={primaryRaw}, CurrentVersion store={alternateRaw}). " +
                          "Windows typically honors the SOFTWARE\\Policies path for administrative templates; " +
                          "treat the disagreement as a conflict until MDM or security baseline layers are collected."
                        : $"{featureName}: both machine policy stores agree on value {primaryRaw}.",
                    ConfidenceReason = conflict
                        ? "Two configured stores disagree; preferring Group Policy store by documented path precedence."
                        : "Both stores agree; high confidence.",
                    SemanticValue = meaning?.Canonical,
                    SemanticDisplay = meaning?.DisplayLabel,
                    HasConflict = conflict
                };
            }

            if (primaryOk)
            {
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = primaryRaw,
                    EffectiveSource = ConfigurationLayer.MachinePolicy,
                    Confidence = EffectiveConfidence.High,
                    ResolutionReason =
                        $"{featureName}: only the Group Policy store (SOFTWARE\\Policies) is configured ({primaryRaw}). " +
                        "That path is the primary administrative template store for this setting.",
                    ConfidenceReason = "Single configured store at MachinePolicy rank; map applied when present.",
                    SemanticValue = meaning?.Canonical,
                    SemanticDisplay = meaning?.DisplayLabel,
                    HasConflict = false
                };
            }

            if (alternateOk)
            {
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = alternateRaw,
                    EffectiveSource = ConfigurationLayer.AlternatePolicyStore,
                    Confidence = EffectiveConfidence.Medium,
                    ResolutionReason =
                        $"{featureName}: only the CurrentVersion policy store is configured ({alternateRaw}). " +
                        "This alternate path can be effective on some images when the Group Policy store is empty.",
                    ConfidenceReason = "Alternate store only; medium confidence until primary path is also known.",
                    SemanticValue = meaning?.Canonical,
                    SemanticDisplay = meaning?.DisplayLabel,
                    HasConflict = false
                };
            }

            return new ConfigurationResolution
            {
                RawObservations = observations,
                EffectiveValue = null,
                EffectiveSource = ConfigurationLayer.Unknown,
                Confidence = EffectiveConfidence.Low,
                ResolutionReason = $"{featureName}: neither machine policy store is configured.",
                ConfidenceReason = "No configured value at either probed path.",
                HasConflict = false
            };
        }

        /// <summary>
        /// Generic rank-based comparison when only layer ranks are known.
        /// Returns Unknown when ranks tie or values cannot be interpreted.
        /// </summary>
        public static ConfigurationResolution ResolveByLayerRank(
            IEnumerable<ConfigurationObservation> layers,
            ManagedObject? definition,
            string featureName)
        {
            var list = layers?.Where(l => l is not null && IsConfiguredValue(l.RawValue)).ToList()
                       ?? new List<ConfigurationObservation>();

            if (list.Count == 0)
            {
                return new ConfigurationResolution
                {
                    RawObservations = layers?.ToList() ?? new List<ConfigurationObservation>(),
                    EffectiveValue = null,
                    EffectiveSource = ConfigurationLayer.Unknown,
                    Confidence = EffectiveConfidence.Low,
                    ResolutionReason = $"{featureName}: no configured layers to compare.",
                    ConfidenceReason = "No configured observations.",
                    HasConflict = false
                };
            }

            var ordered = list.OrderByDescending(l => LayerRank(l.Layer)).ToList();
            var winner = ordered[0];
            var sameRankDifferentValue = ordered
                .Where(l => LayerRank(l.Layer) == LayerRank(winner.Layer))
                .Select(l => ExtractRawPolicyValue(l.RawValue))
                .Where(IsConfiguredValue)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2)
                .Count() > 1;

            if (sameRankDifferentValue)
            {
                return new ConfigurationResolution
                {
                    RawObservations = list,
                    EffectiveValue = null,
                    EffectiveSource = ConfigurationLayer.Unknown,
                    Confidence = EffectiveConfidence.Low,
                    ResolutionReason =
                        $"{featureName}: multiple configured values at the same precedence rank; " +
                        "a winner cannot be determined without inventing priority. Possible causes include " +
                        "overlapping local Group Policy, domain GPO, MDM, or third-party hardening writing the same rank.",
                    ConfidenceReason = "Same-rank conflict; refused to pick a winner.",
                    HasConflict = true
                };
            }

            var conflict = ordered.Skip(1).Any(l =>
                !string.Equals(
                    ExtractRawPolicyValue(l.RawValue),
                    ExtractRawPolicyValue(winner.RawValue),
                    StringComparison.OrdinalIgnoreCase));

            var winnerRaw = ExtractRawPolicyValue(winner.RawValue);
            var meaning = ValueSemanticsInterpreter.Interpret(definition, winnerRaw);

            return new ConfigurationResolution
            {
                RawObservations = list,
                EffectiveValue = winnerRaw,
                EffectiveSource = winner.Layer,
                Confidence = conflict ? EffectiveConfidence.Medium : EffectiveConfidence.High,
                ResolutionReason = conflict
                    ? $"{featureName}: {winner.Layer} wins by documented precedence rank over lower layers that carry different values. " +
                      "Windows applies higher-ranking administrative configuration before user or application preferences."
                    : $"{featureName}: {winner.Layer} is the highest configured layer for this setting.",
                ConfidenceReason = conflict
                    ? "Higher layer wins; lower layers disagree so confidence is medium."
                    : "Single clear winner by layer rank; value map applied when present.",
                SemanticValue = meaning?.Canonical,
                SemanticDisplay = meaning?.DisplayLabel,
                HasConflict = conflict
            };
        }
    }
}
