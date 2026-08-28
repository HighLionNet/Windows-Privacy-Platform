using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ConflictsView : UserControl
{
    private readonly ScanService _scan;
    private readonly IReadOnlyList<ConflictGroup> _openGroups;
    private readonly IReadOnlyList<ConflictGroup> _resolvedGroups;
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private readonly Func<Task> _refresh;
    private readonly Window? _owner;
    private readonly Action<string> _openSetting;
    private readonly ConflictFilterState _state;
    private readonly string? _focusGroupId;
    private readonly Dictionary<string, string?> _pending = new(StringComparer.OrdinalIgnoreCase);
    private bool _busy;

    public ConflictsView(ScanService scan, IReadOnlyList<ConflictGroup> resolvedGroups,
        ElevationService elevation, PolicyChangeService changes, Func<Task> refresh, Window? owner,
        Action<string> openSetting, ConflictFilterState state, string? focusGroupId = null)
    {
        _scan = scan; _openGroups = scan.Query?.GetConflictGroups() ?? [];
        _resolvedGroups = resolvedGroups; _elevation = elevation; _changes = changes;
        _refresh = refresh; _owner = owner; _openSetting = openSetting; _state = state;
        _focusGroupId = focusGroupId;
        InitializeComponent();
        SearchBox.Text = state.Search;
        Select(ImpactBox, state.Impact);
        Select(StateBox, state.State);
        Render();
    }

    private void FilterChanged(object sender, RoutedEventArgs e) => Render();
    private void FilterChanged(object sender, SelectionChangedEventArgs e) => Render();

    private void Render()
    {
        if (GroupsPanel is null || SearchBox is null || ImpactBox is null || StateBox is null) return;
        _state.Search = SearchBox.Text.Trim();
        _state.Impact = Selected(ImpactBox, "All");
        _state.State = Selected(StateBox, "Open");
        var source = _state.State == "Resolved this session" ? _resolvedGroups : _openGroups;
        var groups = source.Where(group => (_state.Impact == "All" || group.Impact.ToString() == _state.Impact) &&
            (_state.Search.Length == 0 || SearchText(group).Contains(_state.Search, StringComparison.OrdinalIgnoreCase))).ToList();

        SearchBox.Background = (Brush)FindResource(_state.Search.Length > 0 ? "BrushAccentSoft" : "BrushBgCard");
        ImpactBox.Background = (Brush)FindResource(_state.Impact == "All" ? "BrushBgCard" : "BrushAccentSoft");
        StateBox.Background = (Brush)FindResource(_state.State == "Open" ? "BrushBgCard" : "BrushAccentSoft");
        GroupsPanel.Children.Clear();
        Border? focus = null;
        foreach (var group in groups)
        {
            var border = new Border
            {
                Tag = group.GroupId,
                Margin = new Thickness(0, 0, 0, 12), Padding = new Thickness(12),
                BorderThickness = new Thickness(4, 1, 1, 1), CornerRadius = new CornerRadius(4),
                BorderBrush = ImpactBrush(group.Impact), Background = ImpactBackground(group.Impact)
            };
            var panel = new StackPanel();
            var header = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 8) };
            var impact = new TextBlock
            {
                Text = group.Impact + " impact", FontWeight = FontWeights.Bold, FontSize = 11,
                Foreground = ImpactBrush(group.Impact), HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(impact, Dock.Right);
            header.Children.Add(impact);
            header.Children.Add(new TextBlock { Text = group.Family, FontSize = 16, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(header);
            panel.Children.Add(new TextBlock { Text = group.OutcomeLine, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
            foreach (var id in group.ObjectIds)
            {
                var item = _scan.Query?.GetById(id);
                if (item is null) continue;
                var otherId = group.ObjectIds.FirstOrDefault(candidate => !candidate.Equals(id, StringComparison.OrdinalIgnoreCase));
                var otherName = string.IsNullOrWhiteSpace(otherId) ? null : _scan.Query?.GetById(otherId)?.ObjectName;
                panel.Children.Add(new SettingBar(item, _elevation.IsAdminAuthorized, _busy,
                    _scan.Overview?.WindowsVersion ?? string.Empty, _scan.Overview?.WindowsEdition ?? string.Empty,
                    _pending.TryGetValue(id, out var raw), raw,
                    value => { _pending[id] = value; Render(); }, () => _openSetting(id),
                    _state.State == "Open" ? group : null, otherName, FocusGroup));
            }
            border.Child = panel;
            GroupsPanel.Children.Add(border);
            if (group.GroupId.Equals(_focusGroupId, StringComparison.OrdinalIgnoreCase)) focus = border;
        }
        EmptyText.Visibility = groups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ApplyButton.Content = $"Apply ({_pending.Count})";
        ApplyButton.IsEnabled = _pending.Count > 0 && _elevation.IsAdminAuthorized && !_busy;
        if (focus is not null) Dispatcher.BeginInvoke(() => focus.BringIntoView());
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _pending.Count == 0) return;
        _busy = true; Render();
        try
        {
            var requests = _pending.Select(pair => new PendingPolicyChange(
                _scan.Query!.GetById(pair.Key)!, pair.Value)).ToList();
            var success = _changes.TryApplyBatch(requests, _owner, out var outcomes);
            if (success) _pending.Clear();
            var verified = outcomes.Count(outcome => outcome.Success);
            MessageBox.Show(_owner, $"Verified: {verified}. Not accepted: {outcomes.Count - verified}.",
                success ? "Changes verified" : "Apply completed with unverified changes", MessageBoxButton.OK,
                success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await _refresh();
        }
        finally { _busy = false; Render(); }
    }

    private string SearchText(ConflictGroup group) => string.Join(' ', group.ObjectIds.Select(id => _scan.Query?.GetById(id)?.ObjectName)
        .Append(group.Family).Append(group.OutcomeLine));
    private void FocusGroup(string groupId)
    {
        var target = GroupsPanel.Children.OfType<Border>().FirstOrDefault(border =>
            string.Equals(border.Tag as string, groupId, StringComparison.OrdinalIgnoreCase));
        target?.BringIntoView();
    }
    private Brush ImpactBrush(ConflictImpact impact) => (Brush)FindResource(impact switch
    {
        ConflictImpact.Low => "BrushDomainKnowledge",
        ConflictImpact.Medium => "BrushWarning",
        _ => "BrushConflict"
    });
    private Brush ImpactBackground(ConflictImpact impact) => (Brush)FindResource(impact == ConflictImpact.High ? "BrushConflictSoft" : "BrushWarningSoft");
    private static string Selected(ComboBox box, string fallback) => (box.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? fallback;
    private static void Select(ComboBox box, string value) => box.SelectedItem = box.Items.OfType<ComboBoxItem>()
        .FirstOrDefault(item => string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase)) ?? box.Items[0];
}
