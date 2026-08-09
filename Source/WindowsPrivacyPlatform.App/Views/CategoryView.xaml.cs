using System;
using System.Collections.Generic;
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
/// Category page: compact two-column cards.
/// Left: name, short blurb, path/type, current value.
/// Right: option buttons showing only the raw value (from ValueSemantics).
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
            ? "Modify — options on the right apply changes (verified)"
            : "Inspect — switch to Modify to change values";

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
            CornerRadius = new CornerRadius(2)
        };

        var grid = new Grid { Margin = new Thickness(4, 0, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // ---- LEFT: identity + blurb + path/type + current ----
        var left = new StackPanel();

        var name = new TextBlock
        {
            Text = mo.ObjectName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)FindResource("BrushTextPrimary"),
            TextWrapping = TextWrapping.Wrap,
            Cursor = Cursors.Hand
        };
        var id = mo.ObjectId;
        name.MouseLeftButtonUp += (_, _) => openSetting(id);
        left.Children.Add(name);

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
            Text = FormatPathType(mo),
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

        // ---- RIGHT: options (raw value only, no numbering) ----
        var right = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        right.Children.Add(new TextBlock
        {
            Text = "Options",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextMuted"),
            Margin = new Thickness(0, 0, 0, 4)
        });

        var options = BuildOptionList(mo);
        if (options.Count == 0)
        {
            right.Children.Add(new TextBlock
            {
                Text = "Modification not supported for this setting.",
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

                // Button content is ONLY the raw value (or "—" for clear).
                var btn = MakeValueButton(
                    opt.RawDisplay,
                    isCurrent,
                    () => ApplyValue(mo, opt.IsClear ? null : opt.Raw),
                    opt.Effect);

                DockPanel.SetDock(btn, Dock.Left);
                row.Children.Add(btn);

                if (!string.IsNullOrWhiteSpace(opt.Effect))
                {
                    row.Children.Add(new TextBlock
                    {
                        Text = opt.Effect,
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
        public string Effect = string.Empty;
        public bool IsClear;
    }

    /// <summary>
    /// Options come only from explicit ValueSemantics.
    /// Never invent 0/1. If none exist, the UI shows "Modification not supported".
    /// </summary>
    private static List<OptionItem> BuildOptionList(ManagedObject mo)
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

                var effect = !string.IsNullOrWhiteSpace(v.Description)
                    ? Truncate(v.Description!, 72)
                    : (!string.IsNullOrWhiteSpace(v.DisplayLabel) ? v.DisplayLabel : v.Canonical);

                list.Add(new OptionItem
                {
                    Raw = v.RawValue,
                    RawDisplay = v.RawValue,
                    Effect = effect ?? string.Empty
                });
            }
        }

        // Always offer clear (delete) when we have any semantics-backed options.
        // Clear itself is only meaningful for settings that have a registry value target;
        // the change service will reject unsupported targets.
        if (list.Count > 0)
        {
            list.Add(new OptionItem
            {
                Raw = null,
                RawDisplay = "—",
                Effect = "Not configured (delete value)",
                IsClear = true
            });
        }

        return list;
    }

    private static string FormatPathType(ManagedObject mo)
    {
        var layer = mo.Observation?.Layers?.FirstOrDefault();
        var path = layer?.SourcePath;
        if (string.IsNullOrWhiteSpace(path))
            path = mo.DiscoveryMethod;

        var hive = layer?.Hive;
        if (string.IsNullOrWhiteSpace(hive) && !string.IsNullOrWhiteSpace(path))
        {
            if (path.StartsWith("HKLM", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("HKEY_LOCAL_MACHINE", StringComparison.OrdinalIgnoreCase))
                hive = "HKLM";
            else if (path.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("HKEY_CURRENT_USER", StringComparison.OrdinalIgnoreCase))
                hive = "HKCU";
        }

        var kind = ClassifyKind(mo, path, hive, layer?.Layer);
        var shortPath = ShortPath(path);

        if (string.IsNullOrWhiteSpace(shortPath))
            return kind;

        return $"{kind} · {shortPath}";
    }

    private static string ClassifyKind(ManagedObject mo, string? path, string? hive, ConfigurationLayer? layer)
    {
        path ??= string.Empty;

        if (path.Contains(@"SOFTWARE\Policies", StringComparison.OrdinalIgnoreCase) ||
            path.Contains(@"Software\Policies", StringComparison.OrdinalIgnoreCase))
            return hive is "HKCU" ? "User GPO (HKCU Policies)" : "GPO (HKLM Policies)";

        if (path.Contains(@"CurrentVersion\Policies", StringComparison.OrdinalIgnoreCase))
            return "Alternate policy store (HKLM)";

        if (path.Contains(@"WindowsUpdate\UX", StringComparison.OrdinalIgnoreCase))
            return "Windows Update UX (HKLM)";

        if (layer == ConfigurationLayer.UserPreference || hive is "HKCU")
            return "User preference (HKCU)";

        if (mo.FeatureCategory == FeatureCategory.DefenderSetting)
            return "Defender policy (HKLM)";

        if (mo.FeatureCategory == FeatureCategory.EdgePolicy)
            return "Edge policy (HKLM)";

        if (mo.ControlLevel == ControlLevel.AdministratorControlled)
            return hive is "HKCU" ? "Admin policy (HKCU)" : "Admin policy (HKLM)";

        return string.IsNullOrWhiteSpace(hive) ? "Registry" : $"Registry ({hive})";
    }

    private static string ShortPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var p = path.Replace("HKEY_LOCAL_MACHINE\\", "HKLM\\", StringComparison.OrdinalIgnoreCase)
                    .Replace("HKEY_CURRENT_USER\\", "HKCU\\", StringComparison.OrdinalIgnoreCase);

        var parts = p.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 4)
            return p;

        return $"{parts[0]}\\\u2026\\{parts[^3]}\\{parts[^2]}\\{parts[^1]}";
    }

    private Button MakeValueButton(string label, bool isCurrent, Action onClick, string? effectTooltip)
    {
        var enabled = _elevation.IsModifyAuthorized && !_applyInProgress;
        var tip = effectTooltip;
        if (!string.IsNullOrWhiteSpace(tip))
            tip = enabled ? $"{tip}\n\nApply requires confirmation; success only if system matches." : $"{tip}\n\nSwitch to Modify to change.";
        else
            tip = enabled ? "Apply (verified against system)" : "Switch to Modify to change values";

        var btn = new Button
        {
            Content = label,
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

    private static string NormalizeObserved(string? state)
    {
        return NavigationBuilder.DisplayValue(state);
    }

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

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..(max - 1)].TrimEnd() + "\u2026";
    }

    private static bool IsCurrent(string observed, string raw)
    {
        if (string.IsNullOrWhiteSpace(observed) || string.IsNullOrWhiteSpace(raw))
            return false;
        var token = observed.Split(' ', '(', ')')[0].Trim();
        return string.Equals(token, raw, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNotConfigured(string observed)
    {
        return string.IsNullOrWhiteSpace(observed)
               || observed.Equals("Not configured", StringComparison.OrdinalIgnoreCase);
    }
}
