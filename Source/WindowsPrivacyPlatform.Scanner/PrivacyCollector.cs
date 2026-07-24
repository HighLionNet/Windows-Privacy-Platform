// Source/WindowsPrivacyPlatform.Scanner/PrivacyCollector.cs
using System;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for privacy-related settings.
    /// Primary: HKCU CapabilityAccessManager\ConsentStore (current user, no elevation).
    /// Secondary: a focused set of related HKCU privacy / advertising preferences.
    /// Never writes. Never requests elevation.
    /// </summary>
    public sealed class PrivacyCollector : IInventoryCollector
    {
        public string Name => "PrivacyCollector";

        // High-value ConsentStore capability keys (Win10/Win11).
        private static readonly string[] CapabilityNames =
        {
            "location",
            "webcam",
            "microphone",
            "userAccountInformation",
            "contacts",
            "appointments",
            "phoneCall",
            "phoneCallHistory",
            "email",
            "userDataSystem",
            "chat",
            "radios",
            "bluetoothSync",
            "appDiagnostics",
            "documentsLibrary",
            "picturesLibrary",
            "videosLibrary",
            "musicLibrary",
            "downloadsFolder",
            "broadFileSystemAccess",
            "gazeInput",
            "activity",
            "activityData",
            "humanPresence",
            "graphicsCaptureProgrammatic",
            "graphicsCaptureWithoutBorder",
            "cellularData",
            "wifiData"
        };

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            CollectConsentStore(snapshot);
            CollectRelatedPrivacyValues(snapshot);
        }

        private static void CollectConsentStore(InventorySnapshot snapshot)
        {
            try
            {
                const string basePath =
                    @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

                using var root = Registry.CurrentUser.OpenSubKey(basePath, writable: false);
                if (root is null)
                    return;

                foreach (var name in CapabilityNames)
                {
                    try
                    {
                        using var key = root.OpenSubKey(name, writable: false);
                        if (key is null)
                            continue;

                        // Value "Value" is typically "Allow", "Deny", or "Prompt"
                        var value = key.GetValue("Value") as string ?? "Not set";
                        snapshot.PrivacySettings.Add(new PrivacySettingInfo
                        {
                            Name = name,
                            Value = value
                        });
                    }
                    catch
                    {
                        // Individual key may be missing or inaccessible; skip.
                    }
                }
            }
            catch
            {
                // Registry access failure must not abort the overall scan.
            }
        }

        /// <summary>
        /// Additional current-user privacy preferences that matter for the product.
        /// All reads are HKCU, writable:false.
        /// </summary>
        private static void CollectRelatedPrivacyValues(InventorySnapshot snapshot)
        {
            // Advertising ID
            TryAddRegistryValue(
                snapshot,
                @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                "Enabled",
                "AdvertisingId.Enabled");

            // Tailored experiences (diagnostic data used for personalization)
            TryAddRegistryValue(
                snapshot,
                @"Software\Microsoft\Windows\CurrentVersion\Privacy",
                "TailoredExperiencesWithDiagnosticDataEnabled",
                "Privacy.TailoredExperiences");

            // Content Delivery / suggested content (Start, Settings tips)
            TryAddRegistryValue(
                snapshot,
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SubscribedContent-338388Enabled",
                "ContentDelivery.SubscribedContent-338388");

            TryAddRegistryValue(
                snapshot,
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SubscribedContent-338389Enabled",
                "ContentDelivery.SubscribedContent-338389");

            TryAddRegistryValue(
                snapshot,
                @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager",
                "SystemPaneSuggestionsEnabled",
                "ContentDelivery.SystemPaneSuggestions");

            // Online speech recognition preference (user)
            TryAddRegistryValue(
                snapshot,
                @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy",
                "HasAccepted",
                "Speech.OnlineSpeechPrivacy");
        }

        private static void TryAddRegistryValue(
            InventorySnapshot snapshot,
            string subKeyPath,
            string valueName,
            string displayName)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(subKeyPath, writable: false);
                if (key is null)
                    return;

                var raw = key.GetValue(valueName);
                if (raw is null)
                    return;

                snapshot.PrivacySettings.Add(new PrivacySettingInfo
                {
                    Name = displayName,
                    Value = raw.ToString() ?? "Not set"
                });
            }
            catch
            {
                // Missing key or value is normal; skip.
            }
        }
    }
}
