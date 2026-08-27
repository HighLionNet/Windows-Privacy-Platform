// Source/WindowsPrivacyPlatform.Scanner/Binding/PrivacyBinder.cs
using System;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Binds HKCU ConsentStore / privacy preference inventory onto catalog privacy settings.
    /// Read-only. Populates full ConfigurationObservation provenance (v0.9 evidence maturity).
    /// </summary>
    public sealed class PrivacyBinder : IStateBinder
    {
        public string Name => nameof(PrivacyBinder);

        public bool CanBind(ManagedObject managedObject)
        {
            if (managedObject is null)
                return false;

            return managedObject.FeatureCategory == FeatureCategory.PrivacyPermission ||
                   string.Equals(managedObject.ObjectType, "PrivacySetting", StringComparison.OrdinalIgnoreCase);
        }

        public void Bind(InventorySnapshot snapshot, ManagedObject managedObject)
        {
            if (snapshot is null || managedObject is null)
                return;

            var shortName = BinderHelpers.ExtractShortName(managedObject.ObjectId);

            var privacy = snapshot.PrivacySettings.FirstOrDefault(p =>
                string.Equals(p.Name, shortName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, managedObject.ObjectId, StringComparison.OrdinalIgnoreCase));

            if (privacy is null)
            {
                privacy = snapshot.PrivacySettings.FirstOrDefault(p =>
                    BinderHelpers.NamesLooselyMatch(managedObject.ObjectId, p.Name) ||
                    BinderHelpers.NamesLooselyMatch(shortName, p.Name));
            }

            if (privacy is null)
            {
                BinderHelpers.ApplyObservation(managedObject, "Not observed in this scan", null);
                return;
            }

            var sourcePath = string.IsNullOrWhiteSpace(managedObject.DiscoveryMethod)
                ? "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore"
                : managedObject.DiscoveryMethod;

            var expected = managedObject.WritableTarget?.ValueKind;
            var typeMismatch = privacy.Status == RegistryObservationStatus.Present && expected is not null &&
                               !KindMatches(privacy.ValueKind, expected.Value);
            var display = privacy.Status switch
            {
                RegistryObservationStatus.AccessDenied => "Access denied",
                RegistryObservationStatus.Error => "Error reading privacy setting",
                _ when typeMismatch => $"Unexpected registry type ({privacy.ValueKind})",
                _ => privacy.Value
            };
            var isUnknown = privacy.Status != RegistryObservationStatus.Present || typeMismatch ||
                            string.IsNullOrWhiteSpace(privacy.Value);

            var layer = new ConfigurationObservation
            {
                ObjectId = managedObject.ObjectId,
                Layer = ConfigurationLayer.UserPreference,
                RawValue = display,
                SourcePath = sourcePath,
                Hive = "HKCU",
                ObservedAt = DateTime.UtcNow,
                ConfidenceScore = isUnknown ? 40 : managedObject.ConfidenceScore > 0 ? managedObject.ConfidenceScore : 85,
                CollectorName = "PrivacyCollector",
                EvidenceSource = $"Registry {sourcePath}",
                CollectionNotes = privacy.Status switch
                {
                    RegistryObservationStatus.NotConfigured => "The current-user source was read and the value was absent.",
                    RegistryObservationStatus.AccessDenied => "Windows denied access to the current-user privacy source.",
                    _ when typeMismatch => $"Expected {expected}; observed {privacy.ValueKind}. The value is not interpreted.",
                    _ => string.Empty
                },
                EffectiveConfidence = isUnknown ? EffectiveConfidence.Low : EffectiveConfidence.High
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
    }
}
