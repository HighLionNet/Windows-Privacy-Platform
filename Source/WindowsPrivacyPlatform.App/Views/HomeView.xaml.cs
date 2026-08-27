using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class HomeView : UserControl
{
    public HomeView(ScanService scan, Action<SettingsListTarget> openSettingsList)
    {
        InitializeComponent();
        var overview = scan.Overview;
        if (overview is null) return;

        var posture = PostureAssessment.Build(scan.SettingsCatalog);
        HighCountText.Text = posture.HighCount.ToString();
        ReviewCountText.Text = posture.ReviewCount.ToString();
        ProtectedCountText.Text = posture.ProtectedCount.ToString();

        foreach (var finding in posture.Findings.Take(6))
        {
            var row = new Button { Style = (Style)FindResource("ListRowButton"), Tag = finding.ObjectId };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(new TextBlock
            {
                Text = finding.Severity == PostureFindingSeverity.High ? "HIGH" : "REVIEW",
                Foreground = (Brush)FindResource(finding.Severity == PostureFindingSeverity.High ? "BrushConflict" : "BrushWarning"),
                FontWeight = FontWeights.SemiBold, FontSize = 10, VerticalAlignment = VerticalAlignment.Center
            });
            var copy = new StackPanel();
            copy.Children.Add(new TextBlock { Text = finding.Title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            copy.Children.Add(new TextBlock { Text = finding.Summary, FontSize = 11, Foreground = (Brush)FindResource("BrushTextSecondary") });
            Grid.SetColumn(copy, 1); grid.Children.Add(copy); row.Content = grid;
            row.Click += (_, _) =>
            {
                var item = scan.SettingsCatalog.First(setting => setting.ObjectId == (string)row.Tag);
                openSettingsList(SettingsListTarget.For(item));
            };
            FindingsList.Children.Add(row);
        }

        if (posture.Findings.Count == 0)
            FindingsList.Children.Add(new TextBlock
            {
                Text = "No high-impact configured issues were found. Unconfigured and unknown values are not counted as safe.",
                Margin = new Thickness(12, 10, 12, 10), TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("BrushTextSecondary")
            });

        SecureBootText.Text = Display(overview.SecureBootState);
        TpmText.Text = $"{Display(overview.TpmPresent)} · {Display(overview.TpmVersion)}";
        BitLockerText.Text = Display(overview.BitLockerProtectionStatus);
        FirewallText.Text = $"{Display(overview.FirewallServiceState)} · {Display(overview.FirewallProfilesSummary)}";
        DefenderText.Text = Display(overview.DefenderServiceState);
        PlatformText.Text = $"{Display(overview.WindowsVersion)} · {Display(overview.WindowsEdition)} · build {overview.BuildNumber} · scanned {overview.LastScanUtc:yyyy-MM-dd HH:mm} UTC";
    }

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
}
