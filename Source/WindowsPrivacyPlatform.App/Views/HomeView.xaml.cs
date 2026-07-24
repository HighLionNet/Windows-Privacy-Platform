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
        ArchText.Text = $"Architecture: {Disp(o.Architecture)}";
        DeviceText.Text = $"{Disp(o.DeviceManufacturer)} {Disp(o.DeviceModel)}".Trim();
        CpuText.Text = $"Processor: {Disp(o.Processor)}";
        MemText.Text = o.TotalPhysicalMemoryMiB > 0
            ? $"Memory: {o.TotalPhysicalMemoryMiB} MiB"
            : "Memory: Unknown";

        SecureBootText.Text = $"Secure Boot: {Disp(o.SecureBootState)}";
        TpmText.Text = $"TPM: {Disp(o.TpmPresent)} / {Disp(o.TpmVersion)}";
        BitLockerText.Text = $"BitLocker: {Disp(o.BitLockerProtectionStatus)}";

        FirewallText.Text = $"Firewall: {Disp(o.FirewallServiceState)} · {Disp(o.FirewallProfilesSummary)}";
        DefenderText.Text = $"Defender: {Disp(o.DefenderServiceState)}";
        DomainText.Text = $"Domain: {Disp(o.DomainMembership)} · Entra: {Disp(o.AzureAdJoined)}";

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
            ConflictsCard.Style = (Style)FindResource("CardConflict");
            ConflictsText.Text = $"{conflictCount} setting(s) report a layer conflict.";
            OpenConflictsBtn.Visibility = Visibility.Visible;
            if (openConflicts is not null)
                OpenConflictsBtn.Click += (_, _) => openConflicts();
        }

        void AddQuick(string label, ProductDomain domain)
        {
            var btn = new Button
            {
                Content = label,
                Style = (Style)FindResource("SecondaryButton"),
                Margin = new Thickness(0, 0, 6, 4),
                Padding = new Thickness(8, 3, 8, 3),
                ToolTip = $"Open {label}"
            };
            btn.Click += (_, _) => navigateDomain(domain);
            QuickNavPanel.Children.Add(btn);
        }

        AddQuick("App permissions", ProductDomain.ConsentStore);
        AddQuick("Telemetry", ProductDomain.Telemetry);
        AddQuick("Firewall", ProductDomain.Firewall);
        AddQuick("Microsoft Defender", ProductDomain.Defender);
        AddQuick("Windows Update", ProductDomain.WindowsUpdate);
        AddQuick("Location", ProductDomain.Location);
        AddQuick("Activity History", ProductDomain.ActivityHistory);
    }

    private static string Disp(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? "Unknown"
            : v;
}
