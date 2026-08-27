using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class PostureFindingsView : UserControl
{
    public PostureFindingsView(ScanService scan, string destination, Action<SettingsListTarget> openSettingsList)
    {
        InitializeComponent();
        var posture = PostureAssessment.Build(scan.SettingsCatalog);
        IReadOnlyList<(ManagedObject Item, string Summary)> rows;
        if (destination == "protections")
        {
            TitleText.Text = "Protections";
            SubtitleText.Text = "Configured protection states observed on this scan.";
            rows = scan.SettingsCatalog.Where(item => PostureAssessment.Build([item]).ProtectedCount > 0)
                .Select(item => (item, "Protection is configured and observed.")).ToList();
        }
        else
        {
            var severity = destination == "high" ? PostureFindingSeverity.High : PostureFindingSeverity.Review;
            TitleText.Text = severity == PostureFindingSeverity.High ? "High attention" : "Review";
            SubtitleText.Text = severity == PostureFindingSeverity.High
                ? "High-risk findings from configured protection settings."
                : "Privacy and sharing settings worth reviewing.";
            rows = posture.Findings.Where(finding => finding.Severity == severity)
                .Select(finding => (scan.SettingsCatalog.First(item => item.ObjectId == finding.ObjectId), finding.Summary)).ToList();
        }

        foreach (var row in rows)
        {
            var button = new Button { Style = (Style)FindResource("ListRowButton") };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = row.Item.ObjectName, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock { Text = row.Summary, Foreground = (Brush)FindResource("BrushTextSecondary"), FontSize = 11 });
            button.Content = panel;
            button.Click += (_, _) => openSettingsList(SettingsListTarget.For(row.Item));
            Rows.Children.Add(button);
        }
        if (rows.Count == 0)
            Rows.Children.Add(new TextBlock { Text = "No matching configured evidence was observed.", Margin = new Thickness(12), Foreground = (Brush)FindResource("BrushTextMuted") });
    }
}
