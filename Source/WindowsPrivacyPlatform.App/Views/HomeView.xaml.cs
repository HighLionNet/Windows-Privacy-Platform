using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;

namespace WindowsPrivacyPlatform.App.Views;

public partial class HomeView : UserControl
{
    public HomeView(ScanService scan)
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
    }

    private static string Disp(string? v) =>
        string.IsNullOrWhiteSpace(v) || v.Equals("Unknown", System.StringComparison.OrdinalIgnoreCase)
            ? "Unknown"
            : v;
}
