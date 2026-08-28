using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class KnowledgeExplorerView : UserControl
{
    private readonly IReadOnlyList<ManagedObject> _catalog;
    private readonly Action<SettingsListTarget> _openSettings;
    private readonly Action<string> _openDetail;
    private readonly KnowledgeFilterState _state;

    public KnowledgeExplorerView(ScanService scan, Action<SettingsListTarget> openSettings,
        Action<string> openDetail, KnowledgeFilterState state)
    {
        _catalog = scan.Catalog.Where(item => item.Bucket != CatalogBucket.SystemInventory)
            .OrderBy(item => item.ProductDomain).ThenBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase).ToList();
        _openSettings = openSettings; _openDetail = openDetail; _state = state;
        InitializeComponent();
        SearchBox.Text = state.Search;
        Render();
    }

    private void SearchChanged(object sender, TextChangedEventArgs e) => Render();

    private void Render()
    {
        if (ResultsPanel is null || SearchBox is null) return;
        _state.Search = SearchBox.Text.Trim();
        SearchBox.Background = (Brush)FindResource(_state.Search.Length == 0 ? "BrushBgCard" : "BrushAccentSoft");
        var items = _catalog.Where(item => _state.Search.Length == 0 || SearchText(item).Contains(_state.Search, StringComparison.OrdinalIgnoreCase)).ToList();
        CountText.Text = $"{items.Count} of {_catalog.Count}";
        ResultsPanel.Children.Clear();
        foreach (var item in items)
        {
            var button = new Button { Style = (Style)FindResource("ListRowButton"), Tag = item.ObjectId, Margin = new Thickness(0, 0, 0, 2) };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var copy = new StackPanel();
            copy.Children.Add(new TextBlock { Text = item.ObjectName, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            copy.Children.Add(new TextBlock
            {
                Text = $"{NavigationBuilder.HumanizeDomain(item.ProductDomain)} · {item.SubCategory} · {Access(item)}",
                FontSize = 11, Foreground = (Brush)FindResource("BrushTextSecondary"), TextWrapping = TextWrapping.Wrap
            });
            grid.Children.Add(copy);
            var badge = new Border { Style = (Style)FindResource(item.IsWritable ? "BadgeSuccess" : "BadgeUnknown"), Margin = new Thickness(12, 0, 0, 0) };
            badge.Child = new TextBlock { Text = item.IsApplicableHere ? (item.IsWritable ? "SETTING" : "REFERENCE") : "NOT ON THIS PC", FontSize = 9, FontWeight = FontWeights.Bold };
            Grid.SetColumn(badge, 1);
            grid.Children.Add(badge);
            button.Content = grid;
            button.Click += (_, _) =>
            {
                if (item.Bucket == CatalogBucket.Settings && item.IsWritable)
                    _openSettings(SettingsListTarget.For(item));
                else
                    _openDetail(item.ObjectId);
            };
            ResultsPanel.Children.Add(button);
        }
        if (items.Count == 0)
            ResultsPanel.Children.Add(new TextBlock { Text = "No catalog entries match.", Margin = new Thickness(4, 8, 4, 8), Foreground = (Brush)FindResource("BrushTextMuted") });
    }

    private static string Access(ManagedObject item) => !item.IsApplicableHere ? "Not on this PC" : item.IsWritable ? "Editable setting" : "Reference";
    private static string SearchText(ManagedObject item) => string.Join(' ', new[]
    {
        item.ObjectName, item.Description, item.Narrative.Summary, item.ObjectId, item.TechnicalLocation,
        NavigationBuilder.HumanizeDomain(item.ProductDomain), item.SubCategory,
        string.Join(' ', item.SearchAliases ?? [])
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
