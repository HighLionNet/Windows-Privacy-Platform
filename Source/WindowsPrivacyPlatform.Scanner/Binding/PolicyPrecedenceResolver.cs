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

        public static string ExtractRawPolicyValue(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;
            var s = state.Trim();
            var idx = s.IndexOf(" (", StringComparison.Ordinal);
            return idx > 0 ? s[..idx].Trim() : s;
        }

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
        /// AppPrivacy codes: 0 = user control, 1 = force allow, 2 = force deny.
        /// </summary>
        public static ConfigurationResolution ResolveConsentVsAppPrivacy(
            ConfigurationObservation? userLayer,
            ConfigurationObservation? policyLayer,
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
                        $"{featureName}: no machine AppPrivacy force policy is configured, so the user preference applies.",
                    HasConflict = false
                };
            }

            if (policyRaw == "0")
            {
                return new ConfigurationResolution
                {
                    RawObservations = observations,
                    EffectiveValue = string.IsNullOrEmpty(userVal) ? userLayer?.RawValue : userVal,
                    EffectiveSource = ConfigurationLayer.UserPreference,
                    Confidence = EffectiveConfidence.High,
                    ResolutionReason =
                        $"{featureName}: machine AppPrivacy is set to user-controlled (0). The ConsentStore value is effective.",
                    HasConflict = false
                };
            }

            if (policyRaw == "1")
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
                        $"{featureName}: machine policy forces Allow (AppPrivacy=1) and takes precedence over user preference ({userVal}).",
                    HasConflict = conflict
                };
            }

            if (policyRaw == "2")
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
                        $"{featureName}: machine policy forces Deny (AppPrivacy=2) and takes precedence over user preference ({userVal}).",
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
                    $"{featureName}: machine AppPrivacy value '{policyRaw}' is not a known force code (expected 0, 1, or 2). Effective state is unknown.",
                HasConflict = true
            };
        }

        /// <summary>
        /// Resolve the same semantic setting stored in two machine policy paths.
        /// Prefers SOFTWARE\Policies (MachinePolicy) over CurrentVersion\Policies when both differ.
        /// </summary>
        public static ConfigurationResolution ResolveAlternateMachinePolicyPaths(
            ConfigurationObservation? primaryPoliciesPath,
            ConfigurationObservation? alternateCurrentVersionPath,
            string featureName)
        {
            var observations = new List<ConfigurationObservation>();
            if (primaryPoliciesPath is not null) observations.Add(primaryPoliciesPath);
            if (alternateCurrentVersionPath is not null) observations.Add(alternateCurrentVersionPath);

            var primaryRaw = ExtractRawPolicyValue(primaryPoliciesPath?.RawValue);
            var alternateRaw = ExtractRawPolicyValue(alternateCurrentVersionPath?.RawValue);
            var primaryOk = IsConfiguredValue(primaryRaw);
            var alternateOk = IsConfiguredValue(alternateRaw);

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
                        ? $"{featureName}: both machine policy stores are configured with different values (Group Policy store={primaryRaw}, CurrentVersion store={alternateRaw}). Preferring the Group Policy store; treat as a conflict until MDM/baseline rules exist."
                        : $"{featureName}: both machine policy stores agree on value {primaryRaw}.",
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
                        $"{featureName}: only the Group Policy store is configured ({primaryRaw}).",
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
                        $"{featureName}: only the CurrentVersion policy store is configured ({alternateRaw}).",
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
                HasConflict = false
            };
        }

        /// <summary>
        /// Generic rank-based comparison when only layer ranks are known.
        /// Returns Unknown when ranks tie or values cannot be interpreted.
        /// </summary>
        public static ConfigurationResolution ResolveByLayerRank(
            IEnumerable<ConfigurationObservation> layers,
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
                        $"{featureName}: multiple configured values at the same precedence rank; winner cannot be determined safely.",
                    HasConflict = true
                };
            }

            var conflict = ordered.Skip(1).Any(l =>
                !string.Equals(
                    ExtractRawPolicyValue(l.RawValue),
                    ExtractRawPolicyValue(winner.RawValue),
                    StringComparison.OrdinalIgnoreCase));

            return new ConfigurationResolution
            {
                RawObservations = list,
                EffectiveValue = ExtractRawPolicyValue(winner.RawValue),
                EffectiveSource = winner.Layer,
                Confidence = conflict ? EffectiveConfidence.Medium : EffectiveConfidence.High,
                ResolutionReason = conflict
                    ? $"{featureName}: {winner.Layer} wins by precedence rank over lower layers with different values."
                    : $"{featureName}: {winner.Layer} is the highest configured layer.",
                HasConflict = conflict
            };
        }
    }
}
