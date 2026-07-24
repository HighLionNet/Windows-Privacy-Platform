using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SearchResultsView : UserControl
{
    public SearchResultsView(ScanService scan, string query, Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = $"Search: {query}";

        var q = query.Trim();
        var results = scan.Catalog.Where(m =>
                Contains(m.ObjectName, q) ||
                Contains(m.ObjectId, q) ||
                Contains(m.Description, q) ||
                Contains(m.DiscoveryMethod, q) ||
                Contains(m.SubCategory, q))
            .OrderBy(m => m.ObjectName)
            .ToList();

        SubtitleText.Text = $"{results.Count} matching catalog entries.";

        foreach (var mo in results)
        {
            var border = new Border { Style = (Style)FindResource("Card"), Cursor = System.Windows.Input.Cursors.Hand };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock { Text = mo.ObjectName, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock
            {
                Text = $"{mo.ProductDomain} · {mo.ObjectId}",
                FontSize = 11,
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = mo.Description,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            border.Child = panel;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            List.Items.Add(border);
        }
    }

    private static bool Contains(string? hay, string needle) =>
        !string.IsNullOrEmpty(hay) &&
        hay.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
