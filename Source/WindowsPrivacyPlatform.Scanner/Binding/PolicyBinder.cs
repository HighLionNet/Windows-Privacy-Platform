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

            var display = $"{policy.Value} ({policy.Hive})";
            var layerKind = ClassifyLayer(policy);

            var sourcePath = string.IsNullOrWhiteSpace(policy.Path)
                ? managedObject.DiscoveryMethod
                : $"{policy.Hive}\\{policy.Path}\\{policy.ValueName}";

            var isNotConfigured = BinderHelpers.IsNotConfigured(policy.Value) ||
                                  BinderHelpers.IsError(policy.Value) ||
                                  string.IsNullOrWhiteSpace(policy.Value);

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
                CollectionNotes = isNotConfigured
                    ? "Policy value absent or not configured at the probed path; treated as not configured."
                    : string.Empty,
                EffectiveConfidence = isNotConfigured ? EffectiveConfidence.Low : EffectiveConfidence.High
            };

            BinderHelpers.ApplyObservation(managedObject, display, layer);
        }

        private static ConfigurationLayer ClassifyLayer(PolicySettingInfo policy)
        {
            var hive = policy.Hive ?? string.Empty;
            var path = policy.Path ?? string.Empty;

            if (hive.Equals("HKCU", StringComparison.OrdinalIgnoreCase))
                return ConfigurationLayer.UserPreference;

            if (hive.Equals("SECEDIT", StringComparison.OrdinalIgnoreCase))
                return ConfigurationLayer.MachinePolicy;

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
