// Source/WindowsPrivacyPlatform.Scanner/Binding/PolicyBinder.cs
using System;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Binds curated policy/GPO registry probes onto catalog policy settings.
    /// Distinguishes MachinePolicy vs AlternatePolicyStore vs UserPreference by hive/path.
    /// Read-only. Populates full ConfigurationObservation provenance (v0.9 evidence maturity).
    /// </summary>
    public sealed class PolicyBinder : IStateBinder
    {
        public string Name => nameof(PolicyBinder);

        public bool CanBind(ManagedObject managedObject)
        {
            if (managedObject is null)
                return false;

            return string.Equals(managedObject.ObjectType, "PolicySetting", StringComparison.OrdinalIgnoreCase) ||
                   managedObject.FeatureCategory is FeatureCategory.RegistryPolicy
                       or FeatureCategory.DefenderSetting
                       or FeatureCategory.EdgePolicy
                       or FeatureCategory.NetworkSetting
                       or FeatureCategory.CloudComponent;
        }

        public void Bind(InventorySnapshot snapshot, ManagedObject managedObject)
        {
            if (snapshot is null || managedObject is null)
                return;

            var policy = snapshot.PolicySettings.FirstOrDefault(p =>
                string.Equals(p.Name, managedObject.ObjectId, StringComparison.OrdinalIgnoreCase));

            if (policy is null)
            {
                BinderHelpers.ApplyObservation(managedObject, "Not observed in this scan", null);
                return;
            }

            var typeMismatch = policy.Status == RegistryObservationStatus.Present &&
                               managedObject.WritableTarget is { Kind: WritableTargetKind.Registry } target &&
                               !KindMatches(policy.ValueKind, target.ValueKind);
            var display = policy.Status switch
            {
                RegistryObservationStatus.AccessDenied => "Access denied",
                RegistryObservationStatus.Error => "Error reading policy",
                _ when typeMismatch => $"Unexpected registry type ({policy.ValueKind})",
                _ => $"{policy.Value} ({policy.Hive})"
            };
            var layerKind = ClassifyLayer(policy);

            var sourcePath = string.IsNullOrWhiteSpace(policy.Path)
                ? managedObject.DiscoveryMethod
                : $"{policy.Hive}\\{policy.Path}\\{policy.ValueName}";

            var isNotConfigured = policy.Status != RegistryObservationStatus.Present ||
                                  typeMismatch || string.IsNullOrWhiteSpace(policy.Value);

            var layer = new ConfigurationObservation
            {
                ObjectId = managedObject.ObjectId,
                Layer = layerKind,
                RawValue = policy.Value,
                SourcePath = sourcePath,
                Hive = policy.Hive,
                ObservedAt = DateTime.UtcNow,
                ConfidenceScore = isNotConfigured ? 40 : managedObject.ConfidenceScore > 0 ? managedObject.ConfidenceScore : 85,
                CollectorName = "PolicyCollector",
                EvidenceSource = $"Registry {sourcePath}",
                CollectionNotes = policy.Status switch
                {
                    RegistryObservationStatus.NotConfigured => "The registry path was read and the value name was absent.",
                    RegistryObservationStatus.AccessDenied => "Windows denied access to the policy source.",
                    RegistryObservationStatus.Error => "The policy source failed with category " + policy.ErrorCategory + ".",
                    _ when typeMismatch => $"Expected {managedObject.WritableTarget!.ValueKind}; observed {policy.ValueKind}. The value is not interpreted.",
                    _ => string.Empty
                },
                EffectiveConfidence = isNotConfigured ? EffectiveConfidence.Low : EffectiveConfidence.High
            };

            BinderHelpers.ApplyObservation(managedObject, display, layer);
        }

        private static bool KindMatches(string observed, RegistryValueKindExpected expected) => expected switch
        {
            RegistryValueKindExpected.DWord => observed.Equals("DWord", StringComparison.OrdinalIgnoreCase),
            RegistryValueKindExpected.QWord => observed.Equals("QWord", StringComparison.OrdinalIgnoreCase),
            RegistryValueKindExpected.String => observed.Equals("String", StringComparison.OrdinalIgnoreCase),
            RegistryValueKindExpected.ExpandString => observed.Equals("ExpandString", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        private static ConfigurationLayer ClassifyLayer(PolicySettingInfo policy)
        {
            var hive = policy.Hive ?? string.Empty;
            var path = policy.Path ?? string.Empty;

            if (hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase))
                return ConfigurationLayer.UserPreference;

            // Alternate machine policy store (CurrentVersion\Policies vs SOFTWARE\Policies)
            if (path.Contains(@"CurrentVersion\Policies", StringComparison.OrdinalIgnoreCase))
                return ConfigurationLayer.AlternatePolicyStore;

            if (path.Contains(@"SOFTWARE\Policies", StringComparison.OrdinalIgnoreCase) ||
                path.Contains(@"Software\Policies", StringComparison.OrdinalIgnoreCase))
                return ConfigurationLayer.MachinePolicy;

            // UX settings and similar non-Policies paths still machine-scoped when HKLM
            if (hive.Equals("HKLM", StringComparison.OrdinalIgnoreCase))
                return ConfigurationLayer.MachinePolicy;

            return ConfigurationLayer.Unknown;
        }
    }
}
