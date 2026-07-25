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

        IdentityNotesText.Text = string.IsNullOrWhiteSpace(o.IdentityCollectionNotes)
            ? "No additional identity collection notes."
            : o.IdentityCollectionNotes;

        var s = scan.Summary;
        if (s is not null)
        {
            SummaryText.Text =
                $"Catalog: {s.CatalogTotal} · Observed: {s.ObservedCount} · Not observed: {s.NotObservedCount}\n" +
                $"Policy configured: {s.ConfiguredPolicyCount} · Not configured: {s.NotConfiguredPolicyCount}\n" +
                $"Impact tags H/M/L: {s.HighRiskCount} / {s.MediumRiskCount} / {s.LowRiskCount}\n" +
                $"Validation: passed={s.CatalogValidationPassed}, failed={s.CatalogValidationFailed}";
        }
        else
        {
            SummaryText.Text = "Observation summary unavailable for this scan.";
        }

        var conflictCount = scan.Query?.GetConflicts().Count() ?? 0;
        if (conflictCount > 0)
        {
            ConflictsCard.Visibility = Visibility.Visible;
            ConflictsText.Text = $"{conflictCount} setting(s) report a layer conflict.";
            OpenConflictsBtn.Visibility = Visibility.Visible;
            if (openConflicts is not null)
                OpenConflictsBtn.Click += (_, _) => openConflicts();
        }

        void AddTile(string label, ProductDomain domain)
        {
            var btn = new Button
            {
                Content = label,
                Style = (Style)FindResource("DomainTile"),
                ToolTip = $"Open {label}"
            };
            btn.Click += (_, _) => navigateDomain(domain);
            QuickNavPanel.Children.Add(btn);
        }

        AddTile("App permissions", ProductDomain.ConsentStore);
        AddTile("Telemetry", ProductDomain.Telemetry);
        AddTile("Firewall", ProductDomain.Firewall);
        AddTile("Microsoft Defender", ProductDomain.Defender);
        AddTile("Windows Update", ProductDomain.WindowsUpdate);
        AddTile("Location", ProductDomain.Location);
        AddTile("Activity History", ProductDomain.ActivityHistory);
        AddTile("Advertising", ProductDomain.Advertising);
    }

    private static string Disp(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? "Unknown"
            : v;
}
