using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            List.Items.Add(new TextBlock
            {
                Text = "No layer conflicts detected among known relationship pairs on this scan.",
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted")
            });
            return;
        }

        foreach (var mo in conflicts.OrderBy(m => m.ProductDomain).ThenBy(m => m.ObjectName))
        {
            var card = NavigationBuilder.BuildDetail(mo, scan.Query);
            if (card is null) continue;

            var border = new Border { Style = (Style)FindResource("Card"), Cursor = System.Windows.Input.Cursors.Hand };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = card.Title, FontWeight = FontWeights.SemiBold, FontSize = 14 });
            panel.Children.Add(new TextBlock
            {
                Text = card.DomainPath,
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"Effective: {card.EffectiveValueDisplay ?? "Unknown"} · {card.ResolutionReason}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });
            border.Child = panel;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            List.Items.Add(border);
        }
    }
}
