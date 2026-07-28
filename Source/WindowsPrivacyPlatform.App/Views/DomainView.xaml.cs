using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class DomainView : UserControl
{
    public DomainView(ScanService scan, ProductDomain domain, Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = NavigationBuilder.HumanizeDomain(domain);

        var items = scan.Catalog.Where(m => m.ProductDomain == domain)
            .OrderBy(m => m.SubCategory)
            .ThenBy(m => m.ObjectName)
            .ToList();

        if (items.Count == 0)
        {
            SubtitleText.Text = "No curated entries.";
            SettingsList.Children.Add(new TextBlock
            {
                Text = "No settings in catalog for this domain.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(4, 8, 4, 8)
            });
            return;
        }

        var conflicts = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        SubtitleText.Text = conflicts > 0
            ? $"{items.Count} settings · {conflicts} conflict(s)"
            : $"{items.Count} settings";

        string? lastSub = null;
        foreach (var mo in items)
        {
            var sub = string.IsNullOrWhiteSpace(mo.SubCategory) ? null : mo.SubCategory;
            if (sub is not null && !string.Equals(sub, lastSub, StringComparison.Ordinal))
            {
                lastSub = sub;
                SettingsList.Children.Add(new Border
                {
                    Background = (Brush)FindResource("BrushBgHeader"),
                    BorderBrush = (Brush)FindResource("BrushBorderStrong"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 10, 0, 4),
                    Child = new TextBlock
                    {
                        Text = sub.ToUpperInvariant(),
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 11,
                        Foreground = (Brush)FindResource("BrushTextMuted"),
                        FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI")
                    }
                });
            }

            SettingsList.Children.Add(BuildSettingCard(mo, openSetting));
        }
    }

    private Border BuildSettingCard(ManagedObject mo, Action<string> openSetting)
    {
        var hasConflict = mo.Observation?.Resolution?.HasConflict == true ||
                          mo.Observation?.Effective?.HasConflict == true;
        var observed = mo.CurrentState ?? "Not observed";
        var isUnknown = string.IsNullOrWhiteSpace(mo.CurrentState) ||
                        observed.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ||
                        observed.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
                        observed.Contains("Not configured", StringComparison.OrdinalIgnoreCase);

        var effective = mo.Observation?.Resolution?.EffectiveValue
                        ?? mo.Observation?.Effective?.EffectiveValue
                        ?? observed;
        var source = mo.Observation?.Resolution?.EffectiveSource?.ToString()
                     ?? mo.Observation?.Effective?.EffectiveSource?.ToString()
                     ?? "—";
        var reason = mo.Observation?.Resolution?.ResolutionReason
                     ?? mo.Observation?.Effective?.Explanation;

        var accentBrush = hasConflict
            ? (Brush)FindResource("BrushConflict")
            : isUnknown
                ? (Brush)FindResource("BrushUnknown")
                : (Brush)FindResource("BrushAccent");

        var card = new Border
        {
            Background = (Brush)FindResource("BrushBgContent"),
            BorderBrush = (Brush)FindResource("BrushBorderStrong"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Open setting details"
        };

        // left accent bar via outer grid
        var outer = new Grid();
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var accent = new Border { Background = accentBrush };
        Grid.SetColumn(accent, 0);
        outer.Children.Add(accent);

        var body = new Grid { Margin = new Thickness(12, 10, 12, 10) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(body, 1);
        outer.Children.Add(body);

        // Left: name + current + effective
        var left = new StackPanel();
        left.Children.Add(new TextBlock
        {
            Text = mo.ObjectName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });

        if (hasConflict)
        {
            left.Children.Add(new Border
            {
                Style = (Style)FindResource("BadgeConflict"),
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = "Conflict",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("BrushConflict")
                }
            });
        }
        else if (isUnknown)
        {
            left.Children.Add(new Border
            {
                Style = (Style)FindResource("BadgeUnknown"),
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = "Unknown",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("BrushUnknown")
                }
            });
        }

        left.Children.Add(new TextBlock
        {
            Text = "Current setting",
            FontSize = 11,
            Foreground = (Brush)FindResource("BrushTextMuted"),
            Margin = new Thickness(0, 10, 0, 2)
        });
        left.Children.Add(new TextBlock
        {
            Text = observed,
            FontSize = 13,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });

        left.Children.Add(new TextBlock
        {
            Text = "Effective state",
            FontSize = 11,
            Foreground = (Brush)FindResource("BrushTextMuted"),
            Margin = new Thickness(0, 8, 0, 2)
        });
        left.Children.Add(new TextBlock
        {
            Text = effective,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = hasConflict
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextPrimary")
        });

        if (!string.IsNullOrWhiteSpace(reason) && hasConflict)
        {
            left.Children.Add(new TextBlock
            {
                Text = reason,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("BrushTextSecondary"),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
        else if (!string.Equals(effective, observed, StringComparison.OrdinalIgnoreCase))
        {
            left.Children.Add(new TextBlock
            {
                Text = $"Source: {source}",
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        Grid.SetColumn(left, 0);
        body.Children.Add(left);

        // Right: options from ValueSemantics
        var right = new Border
        {
            Background = (Brush)FindResource("BrushBgHeader"),
            BorderBrush = (Brush)FindResource("BrushBorder"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var optPanel = new StackPanel();
        optPanel.Children.Add(new TextBlock
        {
            Text = "Options",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextMuted"),
            Margin = new Thickness(0, 0, 0, 6)
        });

        if (mo.ValueSemantics is { Count: > 0 })
        {
            foreach (var v in mo.ValueSemantics.Take(8))
            {
                if (v is null) continue;
                var label = string.IsNullOrWhiteSpace(v.DisplayLabel) ? v.Canonical : v.DisplayLabel;
                optPanel.Children.Add(new TextBlock
                {
                    Text = $"{v.RawValue}  ·  {label}",
                    FontSize = 12,
                    FontFamily = new FontFamily("Cascadia Code, Consolas, Segoe UI"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 3),
                    Foreground = (Brush)FindResource("BrushTextPrimary")
                });
            }
        }
        else
        {
            var desc = string.IsNullOrWhiteSpace(mo.Description)
                ? "No value map in catalog."
                : (mo.Description.Length > 120 ? mo.Description[..117] + "…" : mo.Description);
            optPanel.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("BrushTextSecondary")
            });
        }

        right.Child = optPanel;
        Grid.SetColumn(right, 1);
        body.Children.Add(right);

        card.Child = outer;
        var id = mo.ObjectId;
        card.MouseLeftButtonUp += (_, _) => openSetting(id);
        return card;
    }
}
