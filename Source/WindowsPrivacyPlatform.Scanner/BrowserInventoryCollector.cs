using System.Diagnostics;
using Microsoft.Win32;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner;

/// <summary>Read-only Edge, WebView2, and default-browser presence observation.</summary>
public sealed class BrowserInventoryCollector : IInventoryCollector
{
    public string Name => "BrowserInventoryCollector";

    public void Collect(InventorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        var browsers = snapshot.Applications.Browsers;
        browsers.Edge = ObserveEdge();
        browsers.WebView2 = ObserveWebView2();
        browsers.DefaultBrowser = ObserveDefaultBrowser();
    }

    private static BrowserProductInfo ObserveEdge()
    {
        var path = ReadAppPath("msedge.exe");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            path = ExistingPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft", "Edge", "Application", "msedge.exe"));
        return Product("Microsoft Edge", path, "App Paths / verified installation path");
    }

    private static BrowserProductInfo ObserveWebView2()
    {
        try
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft", "EdgeWebView", "Application");
            var path = Directory.Exists(root)
                ? Directory.EnumerateFiles(root, "msedgewebview2.exe", SearchOption.AllDirectories).FirstOrDefault()
                : null;
            return Product("Microsoft Edge WebView2 Runtime", path, "Verified EdgeWebView installation path (not the browser)");
        }
        catch (UnauthorizedAccessException)
        {
            return new BrowserProductInfo { Name = "Microsoft Edge WebView2 Runtime", Evidence = EvidenceState.AccessDenied, Source = "EdgeWebView installation directory" };
        }
        catch
        {
            return new BrowserProductInfo { Name = "Microsoft Edge WebView2 Runtime", Evidence = EvidenceState.Unknown, Source = "EdgeWebView installation directory" };
        }
    }

    private static DnsLayerObservation ObserveDefaultBrowser()
    {
        const string keyPath = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            var progId = key?.GetValue("ProgId")?.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(progId)
                ? new DnsLayerObservation { Evidence = EvidenceState.Unknown, Summary = "Default browser could not be observed.", Source = "HKCU\\" + keyPath + "\\ProgId" }
                : new DnsLayerObservation { Evidence = EvidenceState.Configured, Summary = progId, Source = "HKCU\\" + keyPath + "\\ProgId" };
        }
        catch (UnauthorizedAccessException)
        {
            return new DnsLayerObservation { Evidence = EvidenceState.AccessDenied, Summary = "Default-browser association access denied.", Source = "HKCU\\" + keyPath };
        }
        catch
        {
            return new DnsLayerObservation { Evidence = EvidenceState.Unknown, Summary = "Default browser could not be observed.", Source = "HKCU\\" + keyPath };
        }
    }

    private static string? ReadAppPath(string executable)
    {
        const string root = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(root + executable);
                var value = key?.GetValue(null)?.ToString()?.Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(value) && File.Exists(value)) return value;
            }
            catch { }
        }
        return null;
    }

    private static BrowserProductInfo Product(string name, string? path, string source)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new BrowserProductInfo { Name = name, Evidence = EvidenceState.NotObserved, Source = source };
        string version;
        try { version = FileVersionInfo.GetVersionInfo(path).FileVersion ?? "Unknown"; }
        catch { version = "Unknown"; }
        return new BrowserProductInfo
        {
            Name = name,
            Evidence = EvidenceState.Configured,
            InstallPath = path,
            Version = version,
            Source = source
        };
    }

    private static string? ExistingPath(string path) => File.Exists(path) ? path : null;
}
