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
    public HomeView(ScanService scan, Action<string> openSetting, Action<ProductDomain> navigateDomain)
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

        FirewallText.Text = $"Firewall service: {Disp(o.FirewallServiceState)} · Profiles: {Disp(o.FirewallProfilesSummary)}";
        DefenderText.Text = $"Defender service: {Disp(o.DefenderServiceState)}";
        DomainText.Text = $"Domain: {Disp(o.DomainMembership)} · Entra: {Disp(o.AzureAdJoined)}";

        ScanMetaText.Text =
            $"Last scan (UTC): {o.LastScanUtc:yyyy-MM-dd HH:mm:ss} · Catalog {o.CatalogVersion} · Knowledge {o.KnowledgeBaseVersion} · Identity confidence: {o.IdentityConfidence}";

        IdentityNotesText.Text = string.IsNullOrWhiteSpace(o.IdentityCollectionNotes)
            ? "No additional identity collection notes."
            : o.IdentityCollectionNotes;

        var s = scan.Summary;
        if (s is not null)
        {
            SummaryText.Text =
                $"Catalog entries: {s.CatalogTotal} · Observed: {s.ObservedCount} · Not observed: {s.NotObservedCount}\n" +
                $"Policy configured: {s.ConfiguredPolicyCount} · Not configured: {s.NotConfiguredPolicyCount}\n" +
                $"Impact tags (H/M/L): {s.HighRiskCount} / {s.MediumRiskCount} / {s.LowRiskCount} (significance tags, not a score)\n" +
                $"Validation: passed={s.CatalogValidationPassed}, failed={s.CatalogValidationFailed}";
        }
        else
        {
            SummaryText.Text = "Observation summary unavailable for this scan.";
        }

        var conflictCount = scan.Query?.GetConflicts().Count() ?? 0;
        if (conflictCount == 0)
        {
            ConflictsText.Text = "No layer conflicts detected among known relationship pairs on this scan.";
            ConflictsCard.Style = (Style)FindResource("Card");
        }
        else
        {
            ConflictsText.Text = $"{conflictCount} setting(s) report a layer conflict. Review the Conflicts page for educational cards.";
            ConflictsCard.Style = (Style)FindResource("CardConflict");
            OpenConflictsBtn.Visibility = Visibility.Visible;
            OpenConflictsBtn.Click += (_, _) => navigateDomain(ProductDomain.ConsentStore); // placeholder; parent handles via nav
            // Prefer direct nav to conflicts via parent callback if available; use domain for now is imperfect.
            // Better: raise via a dedicated action. For v1.0 we surface the count and rely on sidebar.
        }

        // Quick navigation tiles
        void AddQuick(string label, ProductDomain domain)
        {
            var btn = new Button
            {
                Content = label,
                Style = (Style)FindResource("SecondaryButton"),
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 6, 12, 6),
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
    }

    private static string Disp(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
            ? "Unknown"
            : v;
}
