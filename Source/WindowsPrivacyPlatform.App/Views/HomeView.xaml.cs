using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class HomeView : UserControl
{
    private readonly Action<string> _openPosture;
    private readonly Action _openConflicts;

    public HomeView(ScanService scan, Action<SettingsListTarget> openSettingsList, Action<string> openPosture,
        Action openConflicts)
    {
        InitializeComponent();
        _openPosture = openPosture;
        _openConflicts = openConflicts;
        var overview = scan.Overview;
        if (overview is null) return;

        var posture = PostureAssessment.Build(scan.SettingsCatalog);
        HighCountText.Text = posture.HighCount.ToString();
        ReviewCountText.Text = posture.ReviewCount.ToString();
        ProtectedCountText.Text = posture.ProtectedCount.ToString();
        ConflictCountText.Text = (scan.Query?.GetConflictGroups().Count ?? 0).ToString();

        ComputerText.Text = Display(overview.ComputerName);
        UserText.Text = Display(overview.SignedInUser);
        AccountText.Text = Display(overview.AccountType);
        WindowsText.Text = $"{Display(overview.WindowsEdition)} · {Display(overview.WindowsVersion)} · build {overview.BuildNumber}";
        ArchitectureText.Text = Display(overview.Architecture);
        HardwareText.Text = string.Join(" · ", new[] { overview.DeviceManufacturer, overview.DeviceModel }.Where(value => !string.IsNullOrWhiteSpace(value)));
        ProcessorText.Text = Display(overview.Processor);
        MemoryText.Text = overview.TotalPhysicalMemoryMiB > 0 ? $"{overview.TotalPhysicalMemoryMiB / 1024d:F1} GiB" : "Unknown";
        JoinText.Text = $"Domain: {Display(overview.DomainMembership)} · Entra: {Display(overview.AzureAdJoined)}";
        SecureBootText.Text = Display(overview.SecureBootState);
        TpmText.Text = $"{Display(overview.TpmPresent)} · {Display(overview.TpmVersion)}";
        BitLockerText.Text = Display(overview.BitLockerProtectionStatus);
        ProtectionText.Text = $"Firewall: {Display(overview.FirewallServiceState)} · Defender: {Display(overview.DefenderServiceState)}";
        ProtectionProductText.Text = overview.ProtectionProductSummary;
        ProtectionProductText.Foreground = (Brush)FindResource(overview.ProtectionProductStatus switch
        {
            ProtectionProductObservationStatus.Observed => "BrushSuccess",
            ProtectionProductObservationStatus.AccessDenied or ProtectionProductObservationStatus.Error => "BrushWarning",
            _ => "BrushTextMuted"
        });
        LastScanText.Text = $"{overview.LastScanUtc:yyyy-MM-dd HH:mm:ss} UTC";

        var highFindings = posture.Findings.Where(finding => finding.Severity == PostureFindingSeverity.High).ToList();
        foreach (var finding in highFindings)
        {
            var row = new Button { Style = (Style)FindResource("ListRowButton"), Tag = finding.ObjectId };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = finding.Title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock { Text = finding.Summary, FontSize = 11, Foreground = (Brush)FindResource("BrushTextSecondary"), TextWrapping = TextWrapping.Wrap });
            row.Content = panel;
            row.Click += (_, _) =>
            {
                var item = scan.SettingsCatalog.First(setting => setting.ObjectId == (string)row.Tag);
                openSettingsList(SettingsListTarget.For(item));
            };
            FindingsList.Children.Add(row);
        }

        if (highFindings.Count == 0)
            FindingsList.Children.Add(new TextBlock
            {
                Text = "No high-attention findings.",
                Margin = new Thickness(12, 10, 12, 10), TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("BrushTextSecondary")
            });
    }

    private void HighTile_Click(object sender, RoutedEventArgs e) => _openPosture("high");
    private void ReviewTile_Click(object sender, RoutedEventArgs e) => _openPosture("review");
    private void ProtectionsTile_Click(object sender, RoutedEventArgs e) => _openPosture("protections");
    private void ConflictsTile_Click(object sender, RoutedEventArgs e) => _openConflicts();
    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
}
