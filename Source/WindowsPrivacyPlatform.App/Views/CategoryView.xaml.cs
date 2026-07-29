using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Category page: scannable setting index. Columns carry meaning — no per-row labels,
/// no options table, no detail cards. Click opens SettingDetailPage.
/// </summary>
public partial class CategoryView : UserControl
{
    public CategoryView(
        ScanService scan,
        ProductDomain domain,
        string category,
        Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = category;

        var items = scan.Catalog
            .Where(m => m.ProductDomain == domain &&
                        string.Equals(
                            string.IsNullOrWhiteSpace(m.SubCategory) ? domain.ToString() : m.SubCategory,
                            category,
                            StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (items.Count == 0)
        {
            SubtitleText.Text = NavigationBuilder.HumanizeDomain(domain);
            SettingsList.Children.Add(new TextBlock
            {
                Text = "No settings in this category.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(12, 10, 12, 10)
            });
            return;
        }

        var conflicts = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        SubtitleText.Text = conflicts > 0
            ? $"{NavigationBuilder.HumanizeDomain(domain)} · {items.Count} settings · {conflicts} conflict(s)"
            : $"{NavigationBuilder.HumanizeDomain(domain)} · {items.Count} settings";

        var alt = false;
        foreach (var mo in items)
        {
            SettingsList.Children.Add(BuildSettingRow(mo, openSetting, alt));
            alt = !alt;
        }
    }

    private Border BuildSettingRow(ManagedObject mo, Action<string> openSetting, bool altRow)
    {
        var hasConflict = mo.Observation?.Resolution?.HasConflict == true ||
                          mo.Observation?.Effective?.HasConflict == true;
        var observed = string.IsNullOrWhiteSpace(mo.CurrentState) ? "Not observed" : mo.CurrentState;
        var isUnknown = observed.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ||
                        observed.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
                        observed.Contains("Not configured", StringComparison.OrdinalIgnoreCase);

        var effective = mo.Observation?.Resolution?.EffectiveValue
                        ?? mo.Observation?.Effective?.EffectiveValue
                        ?? observed;

        var row = new Border
        {
            Style = (Style)FindResource(hasConflict ? "ListRowConflict" : "ListRow"),
            Background = hasConflict
                ? (Brush)FindResource("BrushConflictSoft")
                : altRow
                    ? (Brush)FindResource("BrushBgAltRow")
                    : (Brush)FindResource("BrushBgContent"),
            ToolTip = "Open setting details"
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });

        var nameBlock = new TextBlock
        {
            Text = mo.ObjectName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        };
        Grid.SetColumn(nameBlock, 0);
        grid.Children.Add(nameBlock);

        var currentBlock = new TextBlock
        {
            Text = Truncate(observed, 48),
            FontSize = 12,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 4, 0),
            Foreground = (Brush)FindResource("BrushTextPrimary"),
            ToolTip = observed
        };
        Grid.SetColumn(currentBlock, 1);
        grid.Children.Add(currentBlock);

        var effectiveBlock = new TextBlock
        {
            Text = Truncate(effective, 48),
            FontSize = 12,
            FontWeight = hasConflict ? FontWeights.SemiBold : FontWeights.Normal,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 4, 0),
            Foreground = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextPrimary"),
            ToolTip = effective
        };
        Grid.SetColumn(effectiveBlock, 2);
        grid.Children.Add(effectiveBlock);

        var statusText = hasConflict ? "Conflict" : isUnknown ? "Unknown" : "";
        var statusBrush = hasConflict
            ? (Brush)FindResource("BrushConflict")
            : isUnknown
                ? (Brush)FindResource("BrushTextMuted")
                : (Brush)FindResource("BrushTextMuted");

        var statusBlock = new TextBlock
        {
            Text = statusText,
            FontSize = 11,
            FontWeight = hasConflict ? FontWeights.SemiBold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
            Foreground = statusBrush
        };
        Grid.SetColumn(statusBlock, 3);
        grid.Children.Add(statusBlock);

        row.Child = grid;
        var id = mo.ObjectId;
        row.MouseLeftButtonUp += (_, _) => openSetting(id);
        return row;
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..(max - 1)] + "…";
    }
}
