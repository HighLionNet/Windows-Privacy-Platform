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
            SettingsList.Items.Add(new TextBlock
            {
                Text = "No settings in catalog for this domain.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(10, 8, 10, 8)
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
                SettingsList.Items.Add(new Border
                {
                    Background = (Brush)FindResource("BrushBgHeader"),
                    BorderBrush = (Brush)FindResource("BrushBorderStrong"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(10, 4, 10, 4),
                    Child = new TextBlock
                    {
                        Text = sub,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 11,
                        Foreground = (Brush)FindResource("BrushTextSecondary")
                    }
                });
            }

            var hasConflict = mo.Observation?.Resolution?.HasConflict == true ||
                              mo.Observation?.Effective?.HasConflict == true;
            var observed = mo.CurrentState ?? "Not observed";
            var isUnknown = string.IsNullOrWhiteSpace(mo.CurrentState) ||
                            observed.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ||
                            observed.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
                            observed.Contains("Not configured", StringComparison.OrdinalIgnoreCase);

            var border = new Border
            {
                Style = (Style)FindResource(hasConflict ? "ListRowConflict" : "ListRow")
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = mo.ObjectName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });
            left.Children.Add(new TextBlock
            {
                Text = mo.ObjectId,
                Style = (Style)FindResource("MetaText"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var effective = mo.Observation?.Resolution?.EffectiveValue
                            ?? mo.Observation?.Effective?.EffectiveValue;
            var stateLine = effective is not null && !string.Equals(effective, observed, StringComparison.Ordinal)
                ? $"{observed}  →  {effective}"
                : observed;

            var stateBlock = new TextBlock
            {
                Text = stateLine,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = (Brush)FindResource("BrushTextSecondary"),
                LineHeight = 17
            };
            Grid.SetColumn(stateBlock, 1);
            grid.Children.Add(stateBlock);

            if (hasConflict || isUnknown)
            {
                var badge = new Border
                {
                    Style = (Style)FindResource(hasConflict ? "BadgeConflict" : "BadgeUnknown"),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                badge.Child = new TextBlock
                {
                    Text = hasConflict ? "Conflict" : "Unknown",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource(hasConflict ? "BrushConflict" : "BrushUnknown")
                };
                Grid.SetColumn(badge, 2);
                grid.Children.Add(badge);
            }

            border.Child = grid;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            border.ToolTip = "Open setting details";
            SettingsList.Items.Add(border);
        }
    }
}
