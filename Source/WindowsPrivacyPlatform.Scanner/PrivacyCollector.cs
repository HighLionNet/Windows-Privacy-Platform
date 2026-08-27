// Source/WindowsPrivacyPlatform.Scanner/PrivacyCollector.cs
using System;
using System.Threading;
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

        public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            CollectConsentStore(snapshot, cancellationToken);
            CollectRelatedPrivacyValues(snapshot, cancellationToken);
        }

        private static void CollectConsentStore(InventorySnapshot snapshot, CancellationToken cancellationToken)
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
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        using var key = root.OpenSubKey(name, writable: false);
                        if (key is null)
                        {
                            snapshot.PrivacySettings.Add(new PrivacySettingInfo
                            {
                                Name = name,
                                Value = "Not configured",
                                Status = RegistryObservationStatus.NotConfigured
                            });
                            continue;
                        }

                        var raw = key.GetValue("Value", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        var present = raw is not null && key.GetValueNames().Contains("Value", StringComparer.OrdinalIgnoreCase);
                        snapshot.PrivacySettings.Add(new PrivacySettingInfo
                        {
                            Name = name,
                            Value = present ? raw!.ToString() ?? "Unknown" : "Not configured",
                            ValueKind = present ? key.GetValueKind("Value").ToString() : string.Empty,
                            Status = present ? RegistryObservationStatus.Present : RegistryObservationStatus.NotConfigured
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        snapshot.PrivacySettings.Add(new PrivacySettingInfo
                        {
                            Name = name,
                            Value = "Access denied",
                            Status = RegistryObservationStatus.AccessDenied
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // Individual key may be missing or inaccessible; skip.
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
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
        private static void CollectRelatedPrivacyValues(InventorySnapshot snapshot, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                {
                    snapshot.PrivacySettings.Add(new PrivacySettingInfo
                    {
                        Name = displayName,
                        Value = "Not configured",
                        Status = RegistryObservationStatus.NotConfigured
                    });
                    return;
                }

                var raw = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (raw is null)
                {
                    snapshot.PrivacySettings.Add(new PrivacySettingInfo
                    {
                        Name = displayName,
                        Value = "Not configured",
                        Status = RegistryObservationStatus.NotConfigured
                    });
                    return;
                }

                snapshot.PrivacySettings.Add(new PrivacySettingInfo
                {
                    Name = displayName,
                    Value = raw.ToString() ?? "Unknown",
                    ValueKind = key.GetValueKind(valueName).ToString(),
                    Status = RegistryObservationStatus.Present
                });
            }
            catch (UnauthorizedAccessException)
            {
                snapshot.PrivacySettings.Add(new PrivacySettingInfo
                {
                    Name = displayName,
                    Value = "Access denied",
                    Status = RegistryObservationStatus.AccessDenied
                });
            }
            catch
            {
                // Missing key or value is normal; skip.
            }
        }
    }
}
