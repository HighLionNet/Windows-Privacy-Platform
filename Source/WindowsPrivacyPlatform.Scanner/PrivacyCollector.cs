// Source/WindowsPrivacyPlatform.Scanner/PrivacyCollector.cs
using System;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner
{
    /// <summary>
    /// Read-only collector for a focused set of privacy-related settings.
    /// Reads HKCU CapabilityAccessManager\ConsentStore (current user, no elevation).
    /// Intentionally limited to high-value privacy capabilities.
    /// </summary>
    public sealed class PrivacyCollector : IInventoryCollector
    {
        public string Name => "PrivacyCollector";

        // Common privacy capability keys under ConsentStore
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
            "broadFileSystemAccess"
        };

        public void Collect(InventorySnapshot snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            try
            {
                const string basePath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore";

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
    }
}
