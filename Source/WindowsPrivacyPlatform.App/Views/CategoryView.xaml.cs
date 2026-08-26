using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Compact category page. Every visible item has an approved write contract.
/// </summary>
public partial class CategoryView : UserControl
{
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private readonly Func<Task> _refreshScan;
    private readonly Window? _owner;
    private readonly string _windowsVersion;
    private readonly string _edition;
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
        _windowsVersion = scan.Overview?.WindowsVersion ?? string.Empty;
        _edition = scan.Overview?.WindowsEdition ?? string.Empty;

        TitleText.Text = category;

        var items = scan.SettingsCatalog
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

        var modeHint = _elevation.IsModifyAuthorized ? "Modify mode" : "Inspect mode";

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
        var observed = NormalizeObserved(mo.CurrentState);

        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(12, 10, 12, 10),
            Background = hasConflict
                ? (Brush)FindResource("BrushConflictSoft")
                : (Brush)FindResource("BrushBgContent"),
            BorderBrush = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushBorderStrong"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };

        var grid = new Grid { Margin = new Thickness(4, 0, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new StackPanel();

        var heading = new DockPanel { LastChildFill = true };
        var id = mo.ObjectId;
        var name = new Button
        {
            Content = mo.ObjectName,
            Style = (Style)FindResource("LinkButton"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(0),
            ToolTip = "Open setting details"
        };
        name.Click += (_, _) => openSetting(id);
        AutomationProperties.SetName(name, $"Open {mo.ObjectName}");
        heading.Children.Add(name);
        if (!mo.IsApplicableHere)
            heading.Children.Add(BuildBadge(CatalogPolicy.ApplicabilityBadgeText(mo.Applicability), "BadgeWarning"));
        left.Children.Add(heading);

        var blurb = ShortBlurb(mo);
        if (!string.IsNullOrWhiteSpace(blurb))
        {
            left.Children.Add(new TextBlock
            {
                Text = blurb,
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushTextSecondary"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 8, 0)
            });
        }

        left.Children.Add(new TextBlock
        {
            Text = TechnicalLocationFormatter.DirectPath(mo.TechnicalLocation),
            FontSize = 10,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            Foreground = (Brush)FindResource("BrushTextMuted"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 8, 0)
        });

        left.Children.Add(new TextBlock
        {
            Text = hasConflict ? $"Current: {observed} · Conflict" : $"Current: {observed}",
            FontSize = 11,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            Foreground = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextPrimary"),
            Margin = new Thickness(0, 4, 8, 0)
        });

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var right = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        right.Children.Add(new TextBlock
        {
            Text = "Options",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextMuted"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var options = BuildOptionList(mo, _windowsVersion, _edition);
        if (options.Count == 0)
        {
            right.Children.Add(new TextBlock
            {
                Text = mo.IsWritable
                    ? "No supported values are available on this Windows edition."
                    : CatalogPolicy.ExclusionReasonText(mo.ExclusionReason),
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushTextMuted"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        else
        {
            foreach (var opt in options)
            {
                var isCurrent = opt.IsClear
                    ? IsNotConfigured(observed)
                    : IsCurrent(observed, opt.Raw!);

                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 3) };

                var btn = MakeValueButton(
                    opt.RawDisplay,
                    isCurrent,
                    () => ApplyValue(mo, opt.IsClear ? null : opt.Raw),
                    opt.Tooltip,
                    mo.IsWritable && mo.IsApplicableHere && opt.IsApplicable);

                DockPanel.SetDock(btn, Dock.Left);
                row.Children.Add(btn);

                if (!string.IsNullOrWhiteSpace(opt.Note))
                {
                    row.Children.Add(new TextBlock
                    {
                        Text = opt.Note,
                        FontSize = 11,
                        Foreground = (Brush)FindResource("BrushTextSecondary"),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 0, 0)
                    });
                }

                right.Children.Add(row);
            }
        }

        Grid.SetColumn(right, 1);
        grid.Children.Add(right);

        card.Child = grid;
        return card;
    }

    private sealed class OptionItem
    {
        public string? Raw;
        public string RawDisplay = string.Empty;
        public string Note = string.Empty;
        /// <summary>Longer explanation for tooltip.</summary>
        public string? Tooltip;
        public bool IsClear;
        public bool IsApplicable = true;
    }

    /// <summary>
    /// Options come only from catalog semantics and approved supported values.
    /// </summary>
    private static List<OptionItem> BuildOptionList(ManagedObject mo, string windowsVersion, string edition)
    {
        var list = new List<OptionItem>();

        if (mo.ValueSemantics is { Count: > 0 })
        {
            foreach (var v in mo.ValueSemantics)
            {
                if (v is null || string.IsNullOrWhiteSpace(v.RawValue))
                    continue;
                if (list.Any(o => string.Equals(o.Raw, v.RawValue, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var copy = SettingOptionLanguage.For(mo, v);

                list.Add(new OptionItem
                {
                    Raw = v.RawValue,
                    RawDisplay = copy.Action,
                    Note = copy.Effect,
                    Tooltip = $"Registry value: {v.RawValue}\n{copy.Effect}",
                    IsApplicable = ApplicabilityEvaluator.IsValueApplicable(v, windowsVersion, edition)
                });
                if (!list[^1].IsApplicable)
                    list[^1].Note += " (not available on this edition or Windows version)";
            }
        }

        if (list.Count > 0 && mo.WritableTarget is { Kind: WritableTargetKind.Registry, SupportsDeletion: true })
        {
            var clear = SettingOptionLanguage.Clear();
            list.Add(new OptionItem
            {
                Raw = null,
                RawDisplay = clear.Action,
                Note = clear.Effect,
                Tooltip = "Remove the registry value so Windows treats this setting as not configured.",
                IsClear = true
            });
        }
        return list;
    }

    private Button MakeValueButton(string label, bool isCurrent, Action onClick, string? effectTooltip, bool targetAvailable)
    {
        var enabled = targetAvailable && _elevation.IsModifyAuthorized && !_applyInProgress;
        var tip = effectTooltip;
        if (!string.IsNullOrWhiteSpace(tip))
            tip = enabled ? $"{tip}\n\nApply requires confirmation; success only if system matches." : $"{tip}\n\nSwitch to Modify to change.";
        else
            tip = enabled ? "Apply (verified against system)" : "Switch to Modify to change values";

        var btn = new Button
        {
            Content = label,
            Style = (Style)FindResource("OptionButton"),
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(8, 2, 8, 2),
            FontSize = 11,
            MinWidth = 56,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsEnabled = enabled,
            Cursor = enabled ? Cursors.Hand : Cursors.Arrow,
            ToolTip = tip
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

    private Border BuildBadge(string text, string styleKey)
    {
        var badge = new Border
        {
            Style = (Style)FindResource(styleKey),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        badge.Child = new TextBlock
        {
            Text = text,
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextSecondary")
        };
        DockPanel.SetDock(badge, Dock.Right);
        return badge;
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
                    msg + "\n\nThe UI was not updated as successful because the system could not be verified.",
                    "Change not accepted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                await _refreshScan();
            }
        }
        finally
        {
            _applyInProgress = false;
        }
    }

    private static string NormalizeObserved(string? state) => NavigationBuilder.DisplayValue(state);

    private static string ShortBlurb(ManagedObject mo)
    {
        var text = !string.IsNullOrWhiteSpace(mo.Description)
            ? mo.Description
            : mo.Rationale ?? string.Empty;
        text = text.Trim();
        if (text.Length <= 100)
            return text;
        return text[..97].TrimEnd() + "\u2026";
    }

    private static bool IsCurrent(string observed, string raw)
    {
        if (string.IsNullOrWhiteSpace(observed) || string.IsNullOrWhiteSpace(raw))
            return false;
        var token = observed.Split(' ', '(', ')')[0].Trim();
        return string.Equals(token, raw, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotConfigured(string observed) =>
        string.IsNullOrWhiteSpace(observed)
        || observed.Equals("Not configured", StringComparison.OrdinalIgnoreCase);
}
