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
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(10, 8, 10, 8)
            });
            return;
        }

        SubtitleText.Text = $"{conflicts.Count} setting(s) with layer conflict";

        foreach (var mo in conflicts.OrderBy(m => m.ProductDomain).ThenBy(m => m.ObjectName))
        {
            var card = NavigationBuilder.BuildDetail(mo, scan.Query);
            if (card is null) continue;

            var border = new Border { Style = (Style)FindResource("ListRowConflict") };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = card.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            });
            left.Children.Add(new TextBlock
            {
                Text = card.DomainPath,
                Style = (Style)FindResource("MetaText"),
                Margin = new Thickness(0, 1, 0, 0)
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var right = new TextBlock
            {
                Text = $"{card.EffectiveValueDisplay ?? "Unknown"} · {card.ResolutionReason}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("BrushTextSecondary")
            };
            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            border.Child = grid;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            border.ToolTip = "Open setting details";
            List.Items.Add(border);
        }
    }
}
