using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class HomeView : UserControl
{
    public HomeView(ScanService scan, Action<string> openSetting, Action<ProductDomain> navigateDomain, Action? openConflicts = null)
    {
        InitializeComponent();
        var o = scan.Overview;
        if (o is null) return;

        OsText.Text = $"{Disp(o.WindowsVersion)} · {Disp(o.WindowsEdition)} · Build {o.BuildNumber}";
        ArchText.Text = Disp(o.Architecture);

        var device = $"{Disp(o.DeviceManufacturer)} {Disp(o.DeviceModel)}".Trim();
        DeviceText.Text = string.IsNullOrWhiteSpace(device) || device == "Unknown Unknown"
            ? "Unknown"
            : device;

        var mem = o.TotalPhysicalMemoryMiB > 0
            ? $"{o.TotalPhysicalMemoryMiB} MiB"
            : "Unknown";
        HwText.Text = $"{Disp(o.Processor)} · {mem}";

        DomainText.Text = $"{Disp(o.DomainMembership)} · Entra: {Disp(o.AzureAdJoined)}";

        SecureBootText.Text = Disp(o.SecureBootState);
        TpmText.Text = $"{Disp(o.TpmPresent)} / {Disp(o.TpmVersion)}";
        BitLockerText.Text = Disp(o.BitLockerProtectionStatus);
        FirewallText.Text = $"{Disp(o.FirewallServiceState)} · {Disp(o.FirewallProfilesSummary)}";
        DefenderText.Text = Disp(o.DefenderServiceState);

        ScanMetaText.Text =
            $"{o.LastScanUtc:yyyy-MM-dd HH:mm:ss} UTC · Catalog {o.CatalogVersion} · Knowledge {o.KnowledgeBaseVersion} · Identity confidence: {o.IdentityConfidence}";

        var conflictCount = scan.Query?.GetConflicts().Count() ?? 0;
        if (conflictCount > 0)
        {
            ConflictsCard.Visibility = Visibility.Visible;
            ConflictsText.Text = $"{conflictCount} setting(s) report a layer conflict.";
            OpenConflictsBtn.Visibility = Visibility.Visible;
            if (openConflicts is not null)
                OpenConflictsBtn.Click += (_, _) => openConflicts();
        }
    }

    private static string Disp(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? "Unknown"
            : v;
}
