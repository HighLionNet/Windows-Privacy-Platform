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
    private readonly IReadOnlyList<ManagedObject> _allItems;
    private readonly Dictionary<string, string?> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _windowsVersion;
    private readonly string _edition;
    private readonly string? _highlightObjectId;
    private readonly Action? _completeModifyOperation;
    private bool _applyInProgress;

    public CategoryView(ScanService scan, ProductDomain domain, string category, Action<string> openSetting,
        ElevationService elevation, PolicyChangeService changes, Func<Task> refreshScan, Window? owner = null,
        string? initialFilter = null, string? highlightObjectId = null, Action? completeModifyOperation = null)
    {
        InitializeComponent();
        _elevation = elevation; _changes = changes; _refreshScan = refreshScan; _owner = owner;
        _openSetting = openSetting; _highlightObjectId = highlightObjectId; _completeModifyOperation = completeModifyOperation;
        _windowsVersion = scan.Overview?.WindowsVersion ?? string.Empty;
        _edition = scan.Overview?.WindowsEdition ?? string.Empty;
        _allItems = scan.SettingsCatalog.Where(item => item.ProductDomain == domain && string.Equals(
                string.IsNullOrWhiteSpace(item.SubCategory) ? domain.ToString() : item.SubCategory,
                category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase).ToList();

        TitleText.Text = category;
        DescriptionText.Text = CategoryContent.For(domain, category).Description;
        SubtitleText.Text = $"{NavigationBuilder.HumanizeDomain(domain)} · {_allItems.Count} settings · " +
                            (_elevation.IsAdminAuthorized ? "Admin" : "View-only");
        FilterBox.Text = initialFilter ?? string.Empty;
        Render();
    }

    private void FilterChanged(object sender, RoutedEventArgs e) => Render();
    private void FilterChanged(object sender, SelectionChangedEventArgs e) => Render();

    private void Render()
    {
        if (SettingsList is null || SummaryPanel is null || FilterBox is null || StateBox is null) return;
        var filter = FilterBox.Text.Trim();
        if (filter.Length > 200) filter = filter[..200];
        var selectedState = (StateBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        var items = _allItems.Where(item => filter.Length == 0 || SearchText(item).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Where(item => selectedState == "All" || EvidenceStateSemantics.Label(EvidenceStateSemantics.Classify(item))
                .Equals(selectedState, StringComparison.OrdinalIgnoreCase)).ToList();

        SettingsList.Children.Clear();
        foreach (var item in items) SettingsList.Children.Add(BuildCard(item));
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SummaryPanel.Children.Clear();
        var summary = CategoryStateSummary.From(items);
        AddSummary("Visible", summary.Visible, "BadgeUnknown"); AddSummary("Configured", summary.Configured, "BadgeSuccess");
        AddSummary("Not configured", summary.NotConfigured, "BadgeUnknown"); AddSummary("Unknown", summary.Unknown, "BadgeWarning");
        AddSummary("Unsupported", summary.Unsupported, "BadgeUnknown"); AddSummary("Access denied", summary.AccessDenied, "BadgeWarning");
        AddSummary("Stale", summary.Stale, "BadgeWarning"); AddSummary("Errors", summary.Error, "BadgeConflict");
        ApplyPendingButton.Content = $"Apply ({_pending.Count})";
        ApplyPendingButton.IsEnabled = _pending.Count > 0 && _elevation.IsAdminAuthorized && !_applyInProgress;
    }

    private Border BuildCard(ManagedObject item)
    {
        var highlighted = item.ObjectId.Equals(_highlightObjectId, StringComparison.OrdinalIgnoreCase);
        var evidence = EvidenceStateSemantics.Classify(item);
        var accentKey = evidence switch
        {
            EvidenceState.Configured => "BrushSuccess",
            EvidenceState.AccessDenied or EvidenceState.Error => "BrushConflict",
            EvidenceState.Unknown or EvidenceState.NotObserved or EvidenceState.Stale => "BrushWarning",
            _ => "BrushAccent"
        };
        var accent = (Brush)FindResource(accentKey);
        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(0),
            Background = highlighted ? (Brush)FindResource("BrushBgSelected") : (Brush)FindResource("BrushBgCard"),
            BorderBrush = highlighted ? (Brush)FindResource("BrushAccent") : (Brush)FindResource("BrushBorderStrong"),
            BorderThickness = new Thickness(highlighted ? 2 : 1), CornerRadius = new CornerRadius(8)
        };
        var layout = new Grid();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.Children.Add(new Border { Background = accent, CornerRadius = new CornerRadius(7, 0, 0, 7) });
        var root = new StackPanel { Margin = new Thickness(14, 11, 15, 13) };
        Grid.SetColumn(root, 1);

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(Text(item.ObjectName, 14.5, "BrushTextPrimary", 0, FontWeights.SemiBold));
        var stateBadge = new Border
        {
            Style = (Style)FindResource(evidence is EvidenceState.AccessDenied or EvidenceState.Error ? "BadgeConflict" :
                evidence is EvidenceState.Unknown or EvidenceState.NotObserved or EvidenceState.Stale ? "BadgeWarning" :
                evidence == EvidenceState.Configured ? "BadgeSuccess" : "BadgeUnknown"),
            Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top
        };
        stateBadge.Child = new TextBlock
        {
            Text = item.IsWritable ? EvidenceStateSemantics.Label(evidence).ToUpperInvariant() : "MONITORED · VIEW ONLY",
            FontSize = 9, FontWeight = FontWeights.Bold
        };
        Grid.SetColumn(stateBadge, 1);
        header.Children.Add(stateBadge);
        root.Children.Add(header);
        root.Children.Add(Text(Introduction(item), 12.5, "BrushTextSecondary", 4));
        var current = CurrentChoice(item);
        root.Children.Add(Text("Observed  ·  " + ObservedText(item, current), 12, "BrushTextPrimary", 9, FontWeights.SemiBold));
        root.Children.Add(Text("Effective  ·  " + EffectiveText(item, current), 12, "BrushTextSecondary", 3));

        var options = BuildOptions(item);
        if (options.Count > 0)
        {
            var choices = new WrapPanel { Margin = new Thickness(0, 12, 0, 1) };
            foreach (var option in options)
            {
                var raw = option.Raw;
                var selected = _pending.TryGetValue(item.ObjectId, out var pendingRaw) &&
                               string.Equals(pendingRaw, raw, StringComparison.OrdinalIgnoreCase);
                var observed = string.Equals(current?.Raw, raw, StringComparison.OrdinalIgnoreCase);
                var button = new Button
                {
                    Content = option.Label + (observed && !selected ? "  ·  Current" : string.Empty),
                    Style = (Style)FindResource(ChoiceStyle(option.Label, selected)),
                    MinWidth = 124,
                    Margin = new Thickness(0, 0, 8, 7),
                    IsEnabled = item.IsWritable && item.IsApplicableHere && option.IsApplicable &&
                                _elevation.IsAdminAuthorized && !_applyInProgress,
                    ToolTip = option.Effect + " Nothing is written until Apply is confirmed."
                };
                if (observed && !selected)
                {
                    button.BorderBrush = (Brush)FindResource("BrushTextPrimary");
                    button.BorderThickness = new Thickness(2);
                }
                button.Click += (_, _) => { _pending[item.ObjectId] = raw; Render(); };
                AutomationProperties.SetName(button, $"{option.Label} for {item.ObjectName}");
                choices.Children.Add(button);
            }
            root.Children.Add(choices);
            if (_pending.TryGetValue(item.ObjectId, out var staged))
            {
                var selected = options.FirstOrDefault(option => string.Equals(option.Raw, staged, StringComparison.OrdinalIgnoreCase));
                root.Children.Add(Text("Ready to apply: " + (selected?.Label ?? "Use Windows default"), 11, "BrushAccent", 2, FontWeights.SemiBold));
            }
        }
        else
            root.Children.Add(Text(item.IsWritable ? "No supported choice is available on this device." :
                    item.ExclusionReason == ExclusionReason.ReadOnlyByDesign
                        ? "Monitored only. This policy has no write route in WPP."
                        : CatalogPolicy.ExclusionReasonText(item.ExclusionReason),
                11, "BrushTextMuted", 10));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
        var detail = new Button { Content = "Details", Style = (Style)FindResource("ActionNeutral"), MinWidth = 105 };
        detail.Click += (_, _) => _openSetting(item.ObjectId);
        actions.Children.Add(detail);
        if (_pending.ContainsKey(item.ObjectId))
        {
            var clear = new Button { Content = "Clear choice", Style = (Style)FindResource("ActionNeutral"), Margin = new Thickness(8, 0, 0, 0) };
            clear.Click += (_, _) => { _pending.Remove(item.ObjectId); Render(); };
            actions.Children.Add(clear);
        }
        root.Children.Add(actions);
        layout.Children.Add(root);
        card.Child = layout;
        AutomationProperties.SetName(card, $"{item.ObjectName}. {ObservedText(item, current)}.");
        return card;
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

    private static TextBlock Text(string? value, double size, string brush, double top, FontWeight? weight = null) => new()
    {
        Text = string.IsNullOrWhiteSpace(value) ? "Not documented." : value, FontSize = size,
        FontWeight = weight ?? FontWeights.Normal, Foreground = (Brush)Application.Current.FindResource(brush),
        TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0)
    };

    private static string Introduction(ManagedObject item)
    {
        var text = string.IsNullOrWhiteSpace(item.Description) ? item.Narrative.Summary : item.Description;
        if (string.Equals(text?.TrimEnd('.'), item.ObjectName.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
            text = item.Narrative.Summary;
        return string.IsNullOrWhiteSpace(text) ? "Windows policy setting." : text;
    }

    private static string ObservedText(ManagedObject item, Choice? current)
    {
        var observed = NavigationBuilder.DisplayValue(item.CurrentState);
        if (current is not null) return $"{observed} — {current.Effect}";
        return $"{observed} — {EvidenceStateSemantics.Detail(EvidenceStateSemantics.Classify(item))}";
    }

    private static string EffectiveText(ManagedObject item, Choice? current)
    {
        var semantic = item.Observation?.Resolution?.SemanticDisplay ?? item.Observation?.Effective?.SemanticDisplay;
        if (!string.IsNullOrWhiteSpace(semantic)) return semantic;
        if (current is not null) return current.Effect;
        var value = item.Observation?.Resolution?.EffectiveValue ?? item.Observation?.Effective?.EffectiveValue ?? item.CurrentState;
        return NavigationBuilder.DisplayValue(value);
    }

    private static string ChoiceStyle(string label, bool selected)
    {
        if (selected) return "ActionChoiceSelected";
        if (label.Contains("Disable", StringComparison.OrdinalIgnoreCase) || label.Contains("Block", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Deny", StringComparison.OrdinalIgnoreCase) || label.Contains("Turn off", StringComparison.OrdinalIgnoreCase)) return "ActionDanger";
        if (label.Contains("Enable", StringComparison.OrdinalIgnoreCase) || label.Contains("Allow", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("Turn on", StringComparison.OrdinalIgnoreCase)) return "ActionSuccess";
        return "ActionChoice";
    }

    private static string SearchText(ManagedObject item) => string.Join(' ', new[]
    { item.ObjectName, item.Description, item.Narrative.Summary, item.CurrentState }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private sealed record Choice(string? Raw, string Label, string Effect, bool IsApplicable);

    private Choice? CurrentChoice(ManagedObject item)
    {
        var raw = RawToken(item.CurrentState);
        return BuildOptions(item).FirstOrDefault(choice => string.Equals(choice.Raw, raw, StringComparison.OrdinalIgnoreCase));
    }

    private static string? RawToken(string? state)
    {
        if (string.IsNullOrWhiteSpace(state)) return null;
        var value = state.Trim();
        if (value.Equals("Not configured", StringComparison.OrdinalIgnoreCase)) return null;
        return value.Split(' ', '(', ')', ';')[0];
    }

    private List<Choice> BuildOptions(ManagedObject item)
    {
        var result = item.ValueSemantics.Where(value => !string.IsNullOrWhiteSpace(value.RawValue))
            .DistinctBy(value => value.RawValue, StringComparer.OrdinalIgnoreCase).Select(value =>
            {
                var copy = SettingOptionLanguage.For(item, value);
                return new Choice(value.RawValue, copy.Action, copy.Effect,
                    ApplicabilityEvaluator.IsValueApplicable(value, _windowsVersion, _edition));
            }).ToList();
        if (result.Count > 0 && item.WritableTarget is { Kind: WritableTargetKind.Registry, SupportsDeletion: true })
        {
            var clear = SettingOptionLanguage.Clear();
            result.Add(new Choice(null, clear.Action, clear.Effect, true));
        }
        return result;
    }
}
