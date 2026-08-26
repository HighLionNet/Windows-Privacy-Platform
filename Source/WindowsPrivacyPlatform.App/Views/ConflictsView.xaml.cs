using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ConflictsView : UserControl
{
    public ConflictsView(ScanService scan, Action<string> openSetting)
    {
        InitializeComponent();
        if (scan.Query is null) return;

        var conflicts = scan.Query.GetConflicts().ToList();
        if (conflicts.Count == 0)
        {
            SubtitleText.Text = "None detected on this scan.";
            List.Items.Add(new TextBlock
            {
                Text = "No layer conflicts.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(10, 8, 10, 8)
            });
            return;
        }

        SubtitleText.Text = $"{conflicts.Count} setting(s) with layer disagreement";

        foreach (var mo in conflicts.OrderBy(m => m.ProductDomain).ThenBy(m => m.ObjectName))
        {
            var card = NavigationBuilder.BuildDetail(mo, scan.Query);
            if (card is null) continue;

            var row = new Button { Style = (Style)FindResource("ListRowButtonConflict") };
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = card.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            });
            panel.Children.Add(new TextBlock
            {
                Text = card.DomainPath,
                Style = (Style)FindResource("MetaText"),
                Margin = new Thickness(0, 1, 0, 0)
            });

            var effective = card.EffectiveValueDisplay ?? "Unknown";
            var source = card.EffectiveSourceDisplay ?? "Unknown";
            panel.Children.Add(new TextBlock
            {
                Text = $"Effective: {effective}  [{source}]",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = (Brush)FindResource("BrushTextPrimary")
            });

            // Concise actionability — not a wall of metadata.
            var writable = mo.IsWritable ? "Writable in Modify mode" : "Observation only";
            panel.Children.Add(new TextBlock
            {
                Text = writable,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = (Brush)FindResource("BrushTextMuted")
            });

            if (!string.IsNullOrWhiteSpace(card.ResolutionReason))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = card.ResolutionReason,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Margin = new Thickness(0, 3, 0, 0),
                    Foreground = (Brush)FindResource("BrushTextSecondary"),
                    LineHeight = 17
                });
            }

            row.Content = panel;
            AutomationProperties.SetName(row, $"{card.Title}. Layer conflict. Effective value: {effective}. Source: {source}.");
            var id = mo.ObjectId;
            row.Click += (_, _) => openSetting(id);
            row.ToolTip = "Open setting details";
            List.Items.Add(row);
        }
    }
}
