using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Domain page: index of categories (SubCategory groups) within a ProductDomain.
/// Does not list individual setting state — that belongs on CategoryView.
/// </summary>
public partial class DomainView : UserControl
{
    public DomainView(ScanService scan, ProductDomain domain, Action<ProductDomain, string> openCategory)
    {
        InitializeComponent();
        TitleText.Text = NavigationBuilder.HumanizeDomain(domain);

        var items = scan.Catalog.Where(m => m.ProductDomain == domain).ToList();
        if (items.Count == 0)
        {
            SubtitleText.Text = "No curated entries.";
            CategoryList.Children.Add(new TextBlock
            {
                Text = "No settings in catalog for this domain.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(12, 10, 12, 10)
            });
            return;
        }

        var groups = items
            .GroupBy(m => string.IsNullOrWhiteSpace(m.SubCategory) ? domain.ToString() : m.SubCategory!)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var conflictTotal = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        SubtitleText.Text = conflictTotal > 0
            ? $"{groups.Count} categor{(groups.Count == 1 ? "y" : "ies")} · {items.Count} settings · {conflictTotal} conflict(s)"
            : $"{groups.Count} categor{(groups.Count == 1 ? "y" : "ies")} · {items.Count} settings";

        foreach (var group in groups)
        {
            var conflicts = group.Count(m =>
                m.Observation?.Resolution?.HasConflict == true ||
                m.Observation?.Effective?.HasConflict == true);
            var unknowns = group.Count(IsUnknown);

            CategoryList.Children.Add(BuildCategoryRow(
                group.Key,
                group.Count(),
                conflicts,
                unknowns,
                () => openCategory(domain, group.Key)));
        }
    }

    private Border BuildCategoryRow(string name, int count, int conflicts, int unknowns, Action open)
    {
        var row = new Border
        {
            Style = (Style)FindResource(conflicts > 0 ? "ListRowConflict" : "ListRow"),
            ToolTip = "Open category"
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        var left = new StackPanel();
        left.Children.Add(new TextBlock
        {
            Text = name,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });

        if (conflicts > 0)
        {
            left.Children.Add(new TextBlock
            {
                Text = $"{conflicts} conflict{(conflicts == 1 ? "" : "s")}",
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushConflict"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        else if (unknowns == count)
        {
            left.Children.Add(new TextBlock
            {
                Text = "All unknown on this scan",
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var countBlock = new TextBlock
        {
            Text = count.ToString(),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        };
        Grid.SetColumn(countBlock, 1);
        grid.Children.Add(countBlock);

        var attention = new TextBlock
        {
            Text = conflicts > 0 ? conflicts.ToString() : (unknowns > 0 ? "—" : ""),
            FontSize = 13,
            FontWeight = conflicts > 0 ? FontWeights.SemiBold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
            Foreground = conflicts > 0
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextMuted")
        };
        Grid.SetColumn(attention, 2);
        grid.Children.Add(attention);

        row.Child = grid;
        row.MouseLeftButtonUp += (_, _) => open();
        return row;
    }

    private static bool IsUnknown(ManagedObject mo)
    {
        var observed = mo.CurrentState ?? "";
        return string.IsNullOrWhiteSpace(mo.CurrentState) ||
               observed.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ||
               observed.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
               observed.Contains("Not configured", StringComparison.OrdinalIgnoreCase);
    }
}
