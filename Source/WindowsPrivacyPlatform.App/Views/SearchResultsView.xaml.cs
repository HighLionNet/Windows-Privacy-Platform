using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SearchResultsView : UserControl
{
    public SearchResultsView(ScanService scan, string query, Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = $"Search: {query}";

        var q = query.Trim();
        var results = (scan.Query?.Search(q) ?? scan.Catalog.Where(m =>
                Contains(m.ObjectName, q) ||
                Contains(m.ObjectId, q) ||
                Contains(m.Description, q) ||
                Contains(m.SubCategory, q)))
            .OrderBy(m => m.ObjectName)
            .ToList();

        if (results.Count == 0)
        {
            SubtitleText.Text = "No matches";
            List.Items.Add(new TextBlock
            {
                Text = "Try ObjectId fragment, name, domain, or description term.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(10, 8, 10, 8),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        SubtitleText.Text = $"{results.Count} match(es)";

        foreach (var mo in results)
        {
            var border = new Border { Style = (Style)FindResource("ListRow") };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = mo.ObjectName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"{mo.ProductDomain} · {mo.ObjectId}",
                Style = (Style)FindResource("MetaText"),
                Margin = new Thickness(0, 1, 0, 0)
            });
            if (!string.IsNullOrWhiteSpace(mo.Description))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = mo.Description,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0),
                    FontSize = 11,
                    Foreground = (Brush)FindResource("BrushTextSecondary")
                });
            }
            border.Child = panel;
            border.ToolTip = "Open setting details";
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            List.Items.Add(border);
        }
    }

    private static bool Contains(string? hay, string needle) =>
        !string.IsNullOrEmpty(hay) &&
        hay.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
