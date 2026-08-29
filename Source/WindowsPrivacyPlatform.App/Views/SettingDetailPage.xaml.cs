using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SettingDetailPage : UserControl
{
    private readonly ManagedObject _item;
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private readonly Func<Task> _refresh;
    private readonly Window? _owner;
    private readonly SettingsQuery? _query;
    private readonly string _windowsVersion;
    private readonly string _edition;
    private readonly Action<string> _openConflict;
    private string? _pending;
    private bool _hasPending;
    private bool _busy;

    public SettingDetailPage(SettingDetailView detail, ManagedObject item, SettingsQuery? query,
        ElevationService elevation, PolicyChangeService changes, Func<Task> refresh, Window? owner,
        string windowsVersion, string edition, Action<string> openConflict)
    {
        InitializeComponent();
        _item = item; _query = query; _elevation = elevation; _changes = changes; _refresh = refresh;
        _owner = owner; _windowsVersion = windowsVersion; _edition = edition;
        _openConflict = openConflict;
        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.Bucket == CatalogBucket.SystemInventory
            ? "System Explorer · observed component"
            : detail.DomainPath;
        ObservedText.Text = detail.CurrentStateDisplay ?? "Unknown";
        EffectiveText.Text = detail.EffectiveValueDisplay ?? "Unknown";
        SourceText.Text = TechnicalLocationFormatter.DirectPath(detail.TechnicalLocation);
        if (detail.HasConflict) EffectiveText.Foreground = (Brush)FindResource("BrushConflict");

        TechnicalLocationText.Text = TechnicalLocationFormatter.DirectPath(detail.TechnicalLocation);
        WhatText.Text = detail.Explanation.WhatIsIt;
        DoesText.Text = detail.Narrative.Mechanics;
        WhyText.Text = detail.Explanation.WhyItMatters;
        GuidanceText.Text = detail.Explanation.DecisionGuidance + " " + detail.Explanation.UserImpact;
        TradeoffsText.Text = string.Join("\n", new[]
        {
            detail.Explanation.SideEffects,
            detail.Explanation.Exceptions,
            detail.Explanation.CommonMisconceptions
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        RestartText.Text = detail.Applicability == ApplicabilityState.Applicable
            ? $"Supported on this scanned device. Restart expectation: {detail.RestartExpectation}."
            : detail.ApplicabilityReason;
        VerificationText.Text = $"Effective source: {detail.EffectiveSourceDisplay ?? "Unknown"}\n" +
                                $"Evidence confidence: {detail.Confidence}\n" +
                                $"Resolution: {detail.ResolutionReason ?? "No additional resolution explanation."}\n" +
                                $"Catalog ID: {detail.ObjectId}";

        if (detail.Bucket == CatalogBucket.SystemInventory)
        {
            AccessBadgeText.Text = "READ ONLY";
            AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AccessBadgeText.Text = detail.IsWritable ? "CATALOG EDITABLE" : "VIEW ONLY";
            if (!detail.IsWritable) AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsList.ItemsSource = detail.Options;
        }
        ApplyDetailButton.Visibility = detail.IsWritable ? Visibility.Visible : Visibility.Collapsed;

        if (detail.Applicability != ApplicabilityState.Applicable)
        {
            ApplicabilityPanel.Visibility = Visibility.Visible;
            ApplicabilityText.Text = detail.ApplicabilityReason;
        }
        RenderBar();
    }

    private void RenderBar()
    {
        var conflict = _query?.GetConflictGroup(_item.ObjectId);
        var otherId = conflict?.ObjectIds.FirstOrDefault(id => !id.Equals(_item.ObjectId, StringComparison.OrdinalIgnoreCase));
        var otherName = string.IsNullOrWhiteSpace(otherId) ? null : _query?.GetById(otherId)?.ObjectName;
        SettingBarHost.Content = new SettingBar(_item, _elevation.IsAdminAuthorized, _busy,
            _windowsVersion, _edition, _hasPending, _pending,
            raw => { _pending = raw; _hasPending = true; RenderBar(); }, null,
            conflict, otherName, _openConflict);
        ApplyDetailButton.IsEnabled = _item.IsWritable && _hasPending && _elevation.IsAdminAuthorized && !_busy;
    }

    private async void ApplyDetail_Click(object sender, RoutedEventArgs e)
    {
        if (!_hasPending || _busy) return;
        _busy = true; RenderBar();
        try
        {
            var success = _changes.TryApply(_item, _pending, _owner, out var message);
            if (success) _hasPending = false;
            MessageBox.Show(_owner, message, success ? "Change verified" : "Change not verified",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            await _refresh();
        }
        finally { _busy = false; RenderBar(); }
    }
}
