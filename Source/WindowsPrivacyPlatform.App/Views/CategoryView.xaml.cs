using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>Category-first settings list with scoped filtering and an explicit pending batch.</summary>
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
        _elevation = elevation;
        _changes = changes;
        _refreshScan = refreshScan;
        _owner = owner;
        _openSetting = openSetting;
        _windowsVersion = scan.Overview?.WindowsVersion ?? string.Empty;
        _edition = scan.Overview?.WindowsEdition ?? string.Empty;
        _highlightObjectId = highlightObjectId;
        _completeModifyOperation = completeModifyOperation;
        _allItems = scan.SettingsCatalog.Where(m => m.ProductDomain == domain && string.Equals(
                string.IsNullOrWhiteSpace(m.SubCategory) ? domain.ToString() : m.SubCategory,
                category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.ObjectName, StringComparer.OrdinalIgnoreCase).ToList();

        TitleText.Text = category;
        var copy = CategoryContent.For(domain, category);
        DescriptionText.Text = copy.Description;
        WhyText.Text = "Why it matters: " + copy.WhyItMatters;
        SubtitleText.Text = $"{NavigationBuilder.HumanizeDomain(domain)} · {_allItems.Count} settings · " +
                            (_elevation.IsModifyAuthorized ? "Modify mode" : "Inspect mode (read-only)");
        FilterBox.Text = initialFilter ?? string.Empty;
        Render();
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e) => Render();

    private void Render()
    {
        if (SettingsList is null || SummaryPanel is null || FilterBox is null)
            return;
        var filter = FilterBox.Text.Trim();
        if (filter.Length > 200) filter = filter[..200];
        var items = _allItems.Where(item => filter.Length == 0 || SearchText(item)
            .Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        SettingsList.Children.Clear();
        foreach (var item in items) SettingsList.Children.Add(BuildCard(item));
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        SummaryPanel.Children.Clear();
        var summary = CategoryStateSummary.From(items);
        AddSummary("Visible", summary.Visible, "BadgeUnknown");
        AddSummary("Configured", summary.Configured, "BadgeSuccess");
        AddSummary("Not configured", summary.NotConfigured, "BadgeUnknown");
        AddSummary("Unknown", summary.Unknown, "BadgeWarning");
        AddSummary("Unsupported", summary.Unsupported, "BadgeUnknown");
        AddSummary("Access denied", summary.AccessDenied, "BadgeWarning");
        AddSummary("Stale", summary.Stale, "BadgeWarning");
        AddSummary("Errors", summary.Error, "BadgeConflict");
        ApplyPendingButton.Content = $"Apply pending ({_pending.Count})";
        ApplyPendingButton.IsEnabled = _pending.Count > 0 && _elevation.IsModifyAuthorized && !_applyInProgress;
    }

    private Border BuildCard(ManagedObject item)
    {
        var evidence = EvidenceStateSemantics.Classify(item);
        var highlighted = string.Equals(item.ObjectId, _highlightObjectId, StringComparison.OrdinalIgnoreCase);
        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(14, 12, 14, 12),
            Background = highlighted ? (Brush)FindResource("BrushBgSelected") : (Brush)FindResource("BrushBgCard"),
            BorderBrush = highlighted ? (Brush)FindResource("BrushAccent") : (Brush)FindResource("BrushBorderStrong"),
            BorderThickness = new Thickness(highlighted ? 2 : 1), CornerRadius = new CornerRadius(8)
        };
        var root = new StackPanel();
        var heading = new DockPanel();
        heading.Children.Add(Badge(EvidenceStateSemantics.Label(evidence), BadgeStyle(evidence)));
        heading.Children.Add(new TextBlock { Text = item.ObjectName, FontSize = 14, FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextPrimary"), TextWrapping = TextWrapping.Wrap });
        root.Children.Add(heading);
        root.Children.Add(Text(item.Narrative.Summary, 12, "BrushTextSecondary", 4));
        root.Children.Add(Text("Privacy / security consequence: " + First(item.Rationale, item.WhenIgnored, item.Narrative.WhyItMatters), 11, "BrushTextSecondary", 4));
        root.Children.Add(Text($"Observed: {NavigationBuilder.DisplayValue(item.CurrentState)} · Scope: {FriendlyScope(item)} · Applicability: {Applicability(item)}", 11, "BrushTextPrimary", 7));
        root.Children.Add(Text(EvidenceStateSemantics.Detail(evidence), 10, "BrushTextMuted", 2));

        var options = BuildOptions(item);
        if (options.Count > 0)
        {
            var optionsPanel = new WrapPanel { Margin = new Thickness(0, 9, 0, 0) };
            foreach (var option in options)
            {
                var raw = option.Raw;
                var pending = _pending.TryGetValue(item.ObjectId, out var selected) && string.Equals(selected, raw, StringComparison.OrdinalIgnoreCase);
                var button = new Button
                {
                    Content = pending ? $"Proposed: {option.Label}" : option.Label,
                    Style = (Style)FindResource("OptionButton"), Margin = new Thickness(0, 0, 7, 5),
                    IsEnabled = item.IsWritable && item.IsApplicableHere && option.IsApplicable && _elevation.IsModifyAuthorized && !_applyInProgress,
                    ToolTip = option.Effect + " Selection is pending until Apply pending is confirmed."
                };
                if (pending) { button.BorderBrush = (Brush)FindResource("BrushAccent"); button.BorderThickness = new Thickness(2); }
                button.Click += (_, _) => { _pending[item.ObjectId] = raw; Render(); };
                AutomationProperties.SetName(button, $"Propose {option.Label} for {item.ObjectName}");
                optionsPanel.Children.Add(button);
            }
            root.Children.Add(optionsPanel);
            if (_pending.TryGetValue(item.ObjectId, out var proposed))
            {
                var proposal = options.FirstOrDefault(o => string.Equals(o.Raw, proposed, StringComparison.OrdinalIgnoreCase));
                root.Children.Add(Text($"Pending comparison — observed: {NavigationBuilder.DisplayValue(item.CurrentState)}; proposed: {proposal?.Label ?? "Use Windows default"}", 11, "BrushAccent", 2));
            }
        }
        else root.Children.Add(Text(item.IsWritable ? "No supported choice is available on this device." : CatalogPolicy.ExclusionReasonText(item.ExclusionReason), 11, "BrushTextMuted", 8));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        var detail = new Button { Content = "Open setting details", Style = (Style)FindResource("SecondaryButton"), Padding = new Thickness(10, 4, 10, 4), ToolTip = "Open the full explanation and technical disclosure" };
        detail.Click += (_, _) => _openSetting(item.ObjectId);
        actions.Children.Add(detail);
        if (_pending.ContainsKey(item.ObjectId))
        {
            var clear = new Button { Content = "Remove pending", Style = (Style)FindResource("LinkButton"), Margin = new Thickness(8, 0, 0, 0) };
            clear.Click += (_, _) => { _pending.Remove(item.ObjectId); Render(); };
            actions.Children.Add(clear);
        }
        root.Children.Add(actions);
        var tech = new Expander { Header = "Technical details", Style = (Style)FindResource("DetailExpander") };
        tech.Content = Text($"Object ID: {item.ObjectId}\nSource: {item.DiscoveryMethod}\nTarget: {TechnicalLocationFormatter.DirectPath(item.TechnicalLocation)}\nVerification: {item.VerificationMethod}\nReboot: {item.RebootRequirement}", 10, "BrushTextMuted", 4);
        root.Children.Add(tech);
        card.Child = root;
        AutomationProperties.SetName(card, $"{item.ObjectName}. {EvidenceStateSemantics.Label(evidence)}. {Applicability(item)}.");
        return card;
    }

    private async void ApplyPending_Click(object sender, RoutedEventArgs e)
    {
        if (_applyInProgress || _pending.Count == 0) return;
        _applyInProgress = true;
        Render();
        try
        {
            var requests = _pending.Select(pair => new PendingPolicyChange(
                _allItems.First(item => item.ObjectId.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)), pair.Value)).ToList();
            var success = _changes.TryApplyBatch(requests, _owner, out var outcomes);
            var summary = PolicyBatchSummary.From(outcomes);
            var verified = summary.Verified;
            var failed = summary.NotVerified;
            if (success) _pending.Clear();
            MessageBox.Show(_owner, $"Verified: {verified}. Not accepted: {failed}.\n\n" +
                string.Join("\n", outcomes.Where(o => !o.Success).Select(o => o.Message).Distinct()),
                success ? "Pending changes verified" : "Batch completed with unverified changes",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await _refreshScan();
            _completeModifyOperation?.Invoke();
        }
        finally { _applyInProgress = false; Render(); }
    }

    private void AddSummary(string label, int count, string style)
    {
        var badge = Badge($"{label}: {count}", style);
        badge.Margin = new Thickness(0, 0, 6, 5);
        SummaryPanel.Children.Add(badge);
    }

    private Border Badge(string value, string style)
    {
        var badge = new Border { Style = (Style)FindResource(style), Margin = new Thickness(8, 0, 0, 0) };
        DockPanel.SetDock(badge, Dock.Right);
        badge.Child = new TextBlock { Text = value, FontSize = 9, FontWeight = FontWeights.SemiBold };
        return badge;
    }

    private static string BadgeStyle(EvidenceState state) => state switch
    {
        EvidenceState.Configured => "BadgeSuccess", EvidenceState.Error => "BadgeConflict",
        EvidenceState.AccessDenied or EvidenceState.Stale or EvidenceState.Unknown => "BadgeWarning", _ => "BadgeUnknown"
    };

    private static TextBlock Text(string? value, double size, string brush, double top) => new()
    {
        Text = string.IsNullOrWhiteSpace(value) ? "Not documented." : value, FontSize = size,
        Foreground = (Brush)Application.Current.FindResource(brush), TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, top, 0, 0)
    };

    private static string SearchText(ManagedObject item) => string.Join(' ', new[]
    {
        item.ObjectName, item.Description, item.Narrative.Summary, item.CurrentState,
        item.InterfaceScope, item.ApplicabilityReason, item.ObjectId
    }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string First(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "No additional consequence is documented.";
    private static string FriendlyScope(ManagedObject item) =>
        string.IsNullOrWhiteSpace(item.InterfaceScope) ? item.RemediationScope?.ToString() ?? "Not documented" : item.InterfaceScope!;
    private static string Applicability(ManagedObject item) => item.IsApplicableHere
        ? "Supported here" : CatalogPolicy.ApplicabilityBadgeText(item.Applicability) + " — " + item.ApplicabilityReason;

    private sealed record Choice(string? Raw, string Label, string Effect, bool IsApplicable);
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
