// Source/WindowsPrivacyPlatform.Scanner/Binding/PrivacyBinder.cs
using System;
using System.Linq;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Binds HKCU ConsentStore / privacy preference inventory onto catalog privacy settings.
    /// Read-only.
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

            var layer = new ConfigurationObservation
            {
                ObjectId = managedObject.ObjectId,
                Layer = ConfigurationLayer.UserPreference,
                RawValue = privacy.Value,
                SourcePath = managedObject.DiscoveryMethod,
                Hive = "HKCU",
                ObservedAt = DateTime.UtcNow,
                ConfidenceScore = managedObject.ConfidenceScore
            };

            BinderHelpers.ApplyObservation(managedObject, privacy.Value, layer);
        }
    }
}
