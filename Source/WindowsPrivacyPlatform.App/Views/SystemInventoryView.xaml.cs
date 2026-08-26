using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SystemInventoryView : UserControl
{
    private readonly IReadOnlyList<ManagedObject> _items;
    private readonly Action<string> _openSetting;
    private readonly DispatcherTimer _debounce;

    public SystemInventoryView(ScanService scan, Action<string> openSetting)
    {
        InitializeComponent();
        _items = scan.InventoryCatalog;
        _openSetting = openSetting;
        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Refresh(); };

        TypeFilter.Items.Add("All component types");
        foreach (var type in _items.Select(i => SystemExplorerGrouping.TypeLabel(i.FeatureCategory)).Distinct().OrderBy(x => x))
            TypeFilter.Items.Add(type);
        TypeFilter.SelectedIndex = 0;

        FilterBox.TextChanged += (_, _) => { _debounce.Stop(); _debounce.Start(); };
        TypeFilter.SelectionChanged += (_, _) => { PopulateGroups(); Refresh(); };
        GroupFilter.SelectionChanged += (_, _) => Refresh();
        PopulateGroups();
        Refresh();
    }

    private void PopulateGroups()
    {
        var selected = GroupFilter.SelectedItem as string;
        var type = TypeFilter.SelectedItem as string ?? "All component types";
        GroupFilter.Items.Clear();
        GroupFilter.Items.Add("All groups");
        foreach (var group in _items
                     .Where(i => type == "All component types" || SystemExplorerGrouping.TypeLabel(i.FeatureCategory) == type)
                     .Select(SystemExplorerGrouping.GroupFor).Distinct().OrderBy(x => x))
            GroupFilter.Items.Add(group);
        GroupFilter.SelectedItem = GroupFilter.Items.Contains(selected) ? selected : "All groups";
    }

    private void Refresh()
    {
        if (ExplorerList is null || GroupFilter is null) return;
        var term = FilterBox.Text?.Trim() ?? string.Empty;
        var type = TypeFilter.SelectedItem as string ?? "All component types";
        var group = GroupFilter.SelectedItem as string ?? "All groups";

        var rows = _items
            .Select(i => new ExplorerRow(i))
            .Where(r => type == "All component types" || r.Type == type)
            .Where(r => group == "All groups" || r.Group == group)
            .Where(r => string.IsNullOrEmpty(term) || r.Search.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => TypeOrder(r.Type)).ThenBy(r => r.Group).ThenBy(r => r.Name)
            .ToList();

        ExplorerList.ItemsSource = rows;
        CountText.Text = $"{rows.Count:N0} of {_items.Count:N0}";
    }

    private void ExplorerRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id }) _openSetting(id);
    }

    private static int TypeOrder(string type) => type switch
    {
        "Services" => 0,
        "Scheduled tasks" => 1,
        "Installed apps" => 2,
        "Provisioned apps" => 3,
        "Optional features" => 4,
        "Capabilities" => 5,
        "Firewall rules" => 6,
        _ => 7
    };

    private sealed class ExplorerRow
    {
        public ExplorerRow(ManagedObject item)
        {
            ObjectId = item.ObjectId;
            Name = item.ObjectName;
            Type = SystemExplorerGrouping.TypeLabel(item.FeatureCategory);
            Group = SystemExplorerGrouping.GroupFor(item);
            Path = TechnicalLocationFormatter.DirectPath(item.TechnicalLocation);
            State = NavigationBuilder.DisplayValue(item.CurrentState);
            Search = $"{Name} {Type} {Group} {Path} {State} {ObjectId}";
        }
        public string ObjectId { get; }
        public string Name { get; }
        public string Type { get; }
        public string Group { get; }
        public string Path { get; }
        public string State { get; }
        public string Search { get; }
    }
}
