using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Category page: taller setting cards with a one-line explanation and value action buttons.
/// Changes are applied from this page (Modify mode). Success requires system read-back verification.
/// </summary>
public partial class CategoryView : UserControl
{
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private readonly Func<Task> _refreshScan;
    private readonly Window? _owner;
    private bool _applyInProgress;

    public CategoryView(
        ScanService scan,
        ProductDomain domain,
        string category,
        Action<string> openSetting,
        ElevationService elevation,
        PolicyChangeService changes,
        Func<Task> refreshScan,
        Window? owner = null)
    {
        InitializeComponent();

        _elevation = elevation;
        _changes = changes;
        _refreshScan = refreshScan;
        _owner = owner;

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
                Margin = new Thickness(4, 10, 4, 10)
            });
            return;
        }

        var conflicts = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        var modeHint = _elevation.IsModifyAuthorized
            ? "Modify mode — click a value to change it (verified against the system)"
            : "Inspect mode — switch to Modify to change values";

        SubtitleText.Text = conflicts > 0
            ? $"{NavigationBuilder.HumanizeDomain(domain)} · {items.Count} settings · {conflicts} conflict(s) · {modeHint}"
            : $"{NavigationBuilder.HumanizeDomain(domain)} · {items.Count} settings · {modeHint}";

        foreach (var mo in items)
            SettingsList.Children.Add(BuildCard(mo, openSetting));
    }

    private Border BuildCard(ManagedObject mo, Action<string> openSetting)
    {
        var hasConflict = mo.Observation?.Resolution?.HasConflict == true ||
                          mo.Observation?.Effective?.HasConflict == true;
        var observed = string.IsNullOrWhiteSpace(mo.CurrentState) ? "Not configured" : mo.CurrentState.Trim();

        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(14, 12, 14, 12),
            Background = hasConflict
                ? (Brush)FindResource("BrushConflictSoft")
                : (Brush)FindResource("BrushBgContent"),
            BorderBrush = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushBorderStrong"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2)
        };

        var accent = new Border
        {
            Width = 3,
            Background = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushAccent"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(-14, -12, 0, -12)
        };

        var root = new Grid();
        root.Children.Add(accent);

        var body = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };

        var name = new TextBlock
        {
            Text = mo.ObjectName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            Foreground = (Brush)FindResource("BrushTextPrimary"),
            TextWrapping = TextWrapping.Wrap,
            Cursor = Cursors.Hand
        };
        var id = mo.ObjectId;
        name.MouseLeftButtonUp += (_, _) => openSetting(id);
        body.Children.Add(name);

        var blurb = ShortBlurb(mo);
        if (!string.IsNullOrWhiteSpace(blurb))
        {
            body.Children.Add(new TextBlock
            {
                Text = blurb,
                FontSize = 12,
                Foreground = (Brush)FindResource("BrushTextSecondary"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        body.Children.Add(new TextBlock
        {
            Text = hasConflict ? $"Current: {observed}  ·  Conflict" : $"Current: {observed}",
            FontSize = 12,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            Foreground = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextPrimary"),
            Margin = new Thickness(0, 8, 0, 6)
        });

        var actions = new WrapPanel();
        var options = mo.ValueSemantics?
            .Select(v => v.RawValue)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new System.Collections.Generic.List<string>();

        if (options.Count == 0)
        {
            options.Add("0");
            options.Add("1");
        }

        foreach (var raw in options)
        {
            var label = LabelFor(mo, raw);
            var isCurrent = IsCurrent(observed, raw);
            actions.Children.Add(MakeValueButton(label, isCurrent, () => ApplyValue(mo, raw)));
        }

        actions.Children.Add(MakeValueButton("Not configured", IsNotConfigured(observed), () => ApplyValue(mo, null)));

        body.Children.Add(actions);
        root.Children.Add(body);
        card.Child = root;
        return card;
    }

    private Button MakeValueButton(string label, bool isCurrent, Action onClick)
    {
        var enabled = _elevation.IsModifyAuthorized && !_applyInProgress;
        var btn = new Button
        {
            Content = label,
            Margin = new Thickness(0, 0, 8, 4),
            Padding = new Thickness(10, 4, 10, 4),
            FontSize = 12,
            MinWidth = 72,
            IsEnabled = enabled,
            Cursor = enabled ? Cursors.Hand : Cursors.Arrow,
            ToolTip = enabled
                ? "Apply this value — success only if system read-back matches"
                : "Switch to Modify mode to change values"
        };

        if (isCurrent)
        {
            btn.FontWeight = FontWeights.SemiBold;
            btn.BorderBrush = (Brush)FindResource("BrushAccent");
            btn.BorderThickness = new Thickness(2);
        }

        btn.Click += (_, _) => onClick();
        return btn;
    }

    private async void ApplyValue(ManagedObject mo, string? rawValue)
    {
        if (_applyInProgress)
            return;

        if (!_elevation.IsModifyAuthorized)
        {
            MessageBox.Show(
                _owner,
                "Switch Mode to Modify and authorize elevation first.",
                "Modify required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _applyInProgress = true;
        try
        {
            if (_changes.TryApply(mo, rawValue, _owner, out var msg))
            {
                // Only refresh UI after verified system success.
                await _refreshScan();
                MessageBox.Show(
                    _owner,
                    msg + "\n\nA fresh scan was applied so the UI matches the system.",
                    "Change verified",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else if (!string.Equals(msg, "Change cancelled.", StringComparison.Ordinal))
            {
                MessageBox.Show(
                    _owner,
                    msg + "\n\nThe UI was not updated because the system could not be verified.",
                    "Change not accepted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                // Still rescan so any partial state is visible honestly.
                await _refreshScan();
            }
        }
        finally
        {
            _applyInProgress = false;
        }
    }

    private static string ShortBlurb(ManagedObject mo)
    {
        var text = !string.IsNullOrWhiteSpace(mo.Description)
            ? mo.Description
            : mo.Rationale ?? string.Empty;
        text = text.Trim();
        if (text.Length <= 110)
            return text;
        return text[..107].TrimEnd() + "…";
    }

    private static string LabelFor(ManagedObject mo, string raw)
    {
        var match = mo.ValueSemantics?.FirstOrDefault(v =>
            string.Equals(v.RawValue, raw, StringComparison.OrdinalIgnoreCase));
        if (match is not null && !string.IsNullOrWhiteSpace(match.DisplayLabel))
            return $"{raw} · {match.DisplayLabel}";
        return raw;
    }

    private static bool IsCurrent(string observed, string raw)
    {
        if (string.IsNullOrWhiteSpace(observed))
            return false;
        var token = observed.Split(' ', '(', ')')[0].Trim();
        return string.Equals(token, raw, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotConfigured(string observed)
    {
        return string.IsNullOrWhiteSpace(observed)
               || observed.Contains("Not configured", StringComparison.OrdinalIgnoreCase)
               || observed.Contains("Not observed", StringComparison.OrdinalIgnoreCase);
    }
}
