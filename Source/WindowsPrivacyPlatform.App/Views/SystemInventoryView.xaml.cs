using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SystemInventoryView : UserControl
{
    private readonly IReadOnlyList<ManagedObject> _items;
    private readonly Action<string> _openSetting;

    public SystemInventoryView(ScanService scan, Action<string> openSetting)
    {
        InitializeComponent();
        _items = scan.InventoryCatalog;
        _openSetting = openSetting;

        CategoryFilter.Items.Add("All inventory");
        foreach (var category in _items.Select(i => i.SubCategory ?? i.FeatureCategory.ToString())
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            CategoryFilter.Items.Add(category);
        CategoryFilter.SelectedIndex = 0;

        FilterBox.TextChanged += (_, _) => Refresh();
        CategoryFilter.SelectionChanged += (_, _) => Refresh();
        Refresh();
    }

    private void Refresh()
    {
        var text = FilterBox.Text?.Trim() ?? string.Empty;
        var category = CategoryFilter.SelectedItem as string ?? "All inventory";
        var filtered = _items
            .Where(item => category == "All inventory" ||
                           string.Equals(item.SubCategory ?? item.FeatureCategory.ToString(), category, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrEmpty(text) || Matches(item, text))
            .OrderBy(item => CategoryOrder(item.FeatureCategory))
            .ThenBy(item => item.SubCategory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        CountText.Text = $"{filtered.Count:N0} of {_items.Count:N0} observed inventory items";
        InventoryList.Items.Clear();
        foreach (var item in filtered)
            InventoryList.Items.Add(BuildRow(item));

        if (filtered.Count == 0)
        {
            InventoryList.Items.Add(new TextBlock
            {
                Text = "No inventory items match the current filters.",
                Margin = new Thickness(12, 12, 12, 12),
                Foreground = (Brush)FindResource("BrushTextMuted")
            });
        }
    }

    private Button BuildRow(ManagedObject item)
    {
        var row = new Button { Style = (Style)FindResource("ListRowButton"), ToolTip = "Open inventory details" };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

        var left = new StackPanel();
        var heading = new DockPanel { LastChildFill = true };
        var badge = new Border
        {
            Style = (Style)FindResource("BadgeUnknown"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.Child = new TextBlock { Text = "VIEW ONLY", FontSize = 9, FontWeight = FontWeights.SemiBold };
        DockPanel.SetDock(badge, Dock.Right);
        heading.Children.Add(badge);
        heading.Children.Add(new TextBlock
        {
            Text = item.ObjectName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });
        left.Children.Add(heading);
        left.Children.Add(new TextBlock
        {
            Text = $"{item.SubCategory} · {item.Narrative.Summary}",
            FontSize = 11,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 10, 0)
        });
        left.Children.Add(new TextBlock
        {
            Text = item.TechnicalLocation,
            FontSize = 10,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            Foreground = (Brush)FindResource("BrushTextMuted"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 10, 0)
        });
        grid.Children.Add(left);

        var state = new TextBlock
        {
            Text = NavigationBuilder.DisplayValue(item.CurrentState),
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        };
        Grid.SetColumn(state, 1);
        grid.Children.Add(state);

        row.Content = grid;
        AutomationProperties.SetName(row,
            $"{item.ObjectName}. View only. Observed state: {NavigationBuilder.DisplayValue(item.CurrentState)}.");
        row.Click += (_, _) => _openSetting(item.ObjectId);
        return row;
    }

    private static bool Matches(ManagedObject item, string term) =>
        item.ObjectName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        item.ObjectId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        item.TechnicalLocation.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        item.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        (item.CurrentState?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);

    private static int CategoryOrder(FeatureCategory category) => category switch
    {
        FeatureCategory.WindowsService => 0,
        FeatureCategory.ScheduledTask => 1,
        FeatureCategory.AppxPackage => 2,
        FeatureCategory.ProvisionedPackage => 3,
        FeatureCategory.OptionalFeature => 4,
        FeatureCategory.WindowsCapability => 5,
        FeatureCategory.FirewallRule => 6,
        _ => 7
    };
}
