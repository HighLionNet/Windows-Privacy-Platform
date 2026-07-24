using System;
using System.Linq;
using System.Windows;
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
            SubtitleText.Text = "No layer conflicts detected on this scan.";
            List.Items.Add(new TextBlock
            {
                Text = "No disagreements among known relationship pairs.",
                Foreground = (Brush)FindResource("BrushTextMuted")
            });
            return;
        }

        SubtitleText.Text = $"{conflicts.Count} setting(s) with layer conflict";

        foreach (var mo in conflicts.OrderBy(m => m.ProductDomain).ThenBy(m => m.ObjectName))
        {
            var card = NavigationBuilder.BuildDetail(mo, scan.Query);
            if (card is null) continue;

            var border = new Border { Style = (Style)FindResource("ListRowConflict") };
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
            panel.Children.Add(new TextBlock
            {
                Text = $"Effective: {card.EffectiveValueDisplay ?? "Unknown"} · {card.ResolutionReason}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 12
            });
            border.Child = panel;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            border.ToolTip = "Open setting details";
            List.Items.Add(border);
        }
    }
}
