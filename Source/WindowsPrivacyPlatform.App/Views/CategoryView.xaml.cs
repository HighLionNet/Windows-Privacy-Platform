using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class CategoryView : UserControl
{
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private readonly Func<Task> _refreshScan;
    private readonly Window? _owner;
    private readonly Action<string> _openSetting;
    private readonly Action<string> _openConflict;
    private readonly SettingsQuery? _query;
    private readonly CategoryFilterState _filterState;
    private readonly IReadOnlyList<ManagedObject> _allItems;
    private readonly Dictionary<string, string?> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _windowsVersion;
    private readonly string _edition;
    private readonly string? _highlightObjectId;
    private readonly Action? _completeModifyOperation;
    private readonly bool _outcomeMode;
    private bool _applyInProgress;

    public CategoryView(ScanService scan, ProductDomain domain, string category, Action<string> openSetting,
        Action<string> openConflict,
        ElevationService elevation, PolicyChangeService changes, Func<Task> refreshScan, Window? owner = null,
        string? initialFilter = null, string? highlightObjectId = null, Action? completeModifyOperation = null,
        CategoryFilterState? filterState = null)
    {
        _elevation = elevation; _changes = changes; _refreshScan = refreshScan; _owner = owner;
        _openSetting = openSetting; _openConflict = openConflict; _query = scan.Query;
        _highlightObjectId = highlightObjectId; _completeModifyOperation = completeModifyOperation;
        _filterState = filterState ?? new CategoryFilterState();
        _windowsVersion = scan.Overview?.WindowsVersion ?? string.Empty;
        _edition = scan.Overview?.WindowsEdition ?? string.Empty;
        var categoryItems = scan.SettingsCatalog.Where(item => item.ProductDomain == domain && string.Equals(
                string.IsNullOrWhiteSpace(item.SubCategory) ? domain.ToString() : item.SubCategory,
                category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase).ToList();
        _outcomeMode = domain is ProductDomain.ConsentStore or ProductDomain.AppPrivacy;
        if (_outcomeMode)
        {
            var ids = categoryItems.Select(item => item.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in OutcomeConflictEngine.ConsentFamilies.Where(pair => ids.Contains(pair.UserId) || ids.Contains(pair.PolicyId)))
            {
                ids.Add(pair.UserId);
                ids.Add(pair.PolicyId);
            }
            _allItems = scan.SettingsCatalog.Where(item => ids.Contains(item.ObjectId))
                .OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase).ToList();
        }
        else _allItems = categoryItems;

        InitializeComponent();
        ApplyPendingButton.Visibility = _elevation.IsAdminAuthorized ? Visibility.Visible : Visibility.Collapsed;

        TitleText.Text = category;
        DescriptionText.Text = CategoryContent.For(domain, category).Description;
        SubtitleText.Text = $"{_allItems.Count} settings in {NavigationBuilder.HumanizeDomain(domain)}. " +
                            (_elevation.IsAdminAuthorized ? "Administrator mode is active." : "View-only mode is active.");
        FilterBox.Text = initialFilter ?? _filterState.Search;
        _filterState.Search = FilterBox.Text;
        StateBox.SelectedItem = StateBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
            string.Equals(item.Content?.ToString(), _filterState.State, StringComparison.OrdinalIgnoreCase)) ?? StateBox.Items[0];
        if (!_allItems.Any(FeaturedSettings.IsFeatured)) _filterState.Scope = "All";
        ScopeBox.SelectedItem = ScopeBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
            string.Equals(item.Content?.ToString(), _filterState.Scope, StringComparison.OrdinalIgnoreCase)) ?? ScopeBox.Items[0];
        ModeStatusText.Text = _elevation.IsAdminAuthorized
            ? "Administrator mode — Apply writes the confirmed batch."
            : "View-only mode — choices are unavailable.";
        Render();
    }

    private void FilterChanged(object sender, RoutedEventArgs e) => Render();
    private void FilterChanged(object sender, SelectionChangedEventArgs e) => Render();

    private void Render()
    {
        if (SettingsList is null || SummaryPanel is null || FilterBox is null || StateBox is null || ScopeBox is null) return;
        var filter = FilterBox.Text.Trim();
        if (filter.Length > 200) filter = filter[..200];
        var selectedState = (StateBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        var selectedScope = (ScopeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Featured";
        _filterState.Search = filter;
        _filterState.State = selectedState;
        _filterState.Scope = selectedScope;
        FilterBox.Background = (Brush)FindResource(filter.Length == 0 ? "BrushBgCard" : "BrushAccentSoft");
        StateBox.Background = (Brush)FindResource(selectedState == "All" ? "BrushBgCard" : "BrushAccentSoft");
        ScopeBox.Background = (Brush)FindResource(selectedScope == "All" ? "BrushBgCard" : "BrushAccentSoft");
        var items = _allItems.Where(item => filter.Length == 0 || SearchText(item).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Where(item => selectedScope == "All" || IsFeaturedForThisView(item))
            .Where(item => selectedState == "Not on this PC"
                ? !item.IsApplicableHere
                : item.IsApplicableHere && (selectedState == "All" ||
                    EvidenceStateSemantics.Label(EvidenceStateSemantics.Classify(item))
                        .Equals(selectedState, StringComparison.OrdinalIgnoreCase))).ToList();

        SettingsList.Children.Clear();
        if (_outcomeMode)
        {
            foreach (var family in OutcomeGrouping.Build(items))
            {
                var familyItems = family.ObjectIds.Select(id => items.First(item => item.ObjectId.Equals(id, StringComparison.OrdinalIgnoreCase))).ToList();
                SettingsList.Children.Add(BuildOutcomeCard(family.Family, familyItems));
            }
        }
        else foreach (var item in items)
        {
            SettingsList.Children.Add(BuildSettingBar(item));
        }
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SummaryPanel.Children.Clear();
        var summary = CategoryStateSummary.From(items);
        AddSummary("Visible", summary.Visible, "BadgeUnknown"); AddSummary("Configured", summary.Configured, "BadgeSuccess");
        AddSummary("Not configured", summary.NotConfigured, "BadgeUnknown"); AddSummary("Unknown", summary.Unknown, "BadgeWarning");
        AddSummary("Not on this PC", summary.Unsupported, "BadgeUnknown"); AddSummary("Access denied", summary.AccessDenied, "BadgeWarning");
        AddSummary("Stale", summary.Stale, "BadgeWarning"); AddSummary("Errors", summary.Error, "BadgeConflict");
        ApplyPendingButton.Content = $"Apply ({_pending.Count})";
        ApplyPendingButton.IsEnabled = _pending.Count > 0 && _elevation.IsAdminAuthorized && !_applyInProgress;
    }

    private Border BuildOutcomeCard(string family, IReadOnlyList<ManagedObject> items)
    {
        var card = new Border
        {
            Style = (Style)FindResource("Card"),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(10, 8, 10, 3)
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = family, FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(2, 0, 0, 7) });
        foreach (var item in items.OrderBy(item => item.ProductDomain == ProductDomain.ConsentStore ? 0 : 1))
            panel.Children.Add(BuildSettingBar(item));
        card.Child = panel;
        return card;
    }

    private SettingBar BuildSettingBar(ManagedObject item)
    {
        var conflict = _query?.GetConflictGroup(item.ObjectId);
        var otherId = conflict?.ObjectIds.FirstOrDefault(id => !id.Equals(item.ObjectId, StringComparison.OrdinalIgnoreCase));
        var otherName = string.IsNullOrWhiteSpace(otherId) ? null : _query?.GetById(otherId)?.ObjectName;
        return new SettingBar(item, _elevation.IsAdminAuthorized, _applyInProgress, _windowsVersion, _edition,
            _pending.TryGetValue(item.ObjectId, out var pendingRaw), pendingRaw,
            raw => { _pending[item.ObjectId] = raw; Render(); }, () => _openSetting(item.ObjectId),
            conflict, otherName, _openConflict);
    }

    private bool IsFeaturedForThisView(ManagedObject item)
    {
        if (FeaturedSettings.IsFeatured(item)) return true;
        if (!_outcomeMode) return false;
        var pair = OutcomeConflictEngine.ConsentFamilies.FirstOrDefault(candidate =>
            candidate.UserId.Equals(item.ObjectId, StringComparison.OrdinalIgnoreCase) ||
            candidate.PolicyId.Equals(item.ObjectId, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(pair.UserId)) return false;
        var counterpart = pair.UserId.Equals(item.ObjectId, StringComparison.OrdinalIgnoreCase) ? pair.PolicyId : pair.UserId;
        return _allItems.Any(candidate => candidate.ObjectId.Equals(counterpart, StringComparison.OrdinalIgnoreCase) && FeaturedSettings.IsFeatured(candidate));
    }

    private async void ApplyPending_Click(object sender, RoutedEventArgs e)
    {
        if (_applyInProgress || _pending.Count == 0) return;
        _applyInProgress = true; Render();
        try
        {
            var requests = _pending.Select(pair => new PendingPolicyChange(
                _allItems.First(item => item.ObjectId.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)), pair.Value)).ToList();
            var success = _changes.TryApplyBatch(requests, _owner, out var outcomes);
            var summary = PolicyBatchSummary.From(outcomes);
            if (success) _pending.Clear();
            var failures = string.Join("\n", outcomes.Where(outcome => !outcome.Success).Select(outcome => outcome.Message).Distinct());
            MessageBox.Show(_owner, $"Verified: {summary.Verified}. Not accepted: {summary.NotVerified}." +
                (failures.Length == 0 ? string.Empty : "\n\n" + failures),
                success ? "Changes verified" : "Apply completed with unverified changes",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await _refreshScan();
            _completeModifyOperation?.Invoke();
        }
        finally { _applyInProgress = false; Render(); }
    }

    private void AddSummary(string label, int count, string style)
    {
        var badge = new Border { Style = (Style)FindResource(style), Margin = new Thickness(0, 0, 6, 5) };
        badge.Child = new TextBlock { Text = $"{label}: {count}", FontSize = 10.5, FontWeight = FontWeights.SemiBold };
        SummaryPanel.Children.Add(badge);
    }

    private static string SearchText(ManagedObject item) => string.Join(' ', new[]
    { item.ObjectName, item.Description, item.Narrative.Summary, item.CurrentState }.Where(value => !string.IsNullOrWhiteSpace(value)));

}
