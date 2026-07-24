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
            SubtitleText.Text = "No managed settings in the current catalog for this domain.";
            SettingsList.Items.Add(new TextBlock
            {
                Text = "No curated entries yet.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 6, 0, 0)
            });
            return;
        }

        var conflicts = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        SubtitleText.Text = conflicts > 0
            ? $"{items.Count} settings · {conflicts} with layer conflict"
            : $"{items.Count} settings";

        foreach (var mo in items)
        {
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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = mo.ObjectName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            });
            left.Children.Add(new TextBlock
            {
                Text = mo.ObjectId,
                Style = (Style)FindResource("MetaText"),
                Margin = new Thickness(0, 1, 0, 0)
            });

            var effective = mo.Observation?.Resolution?.EffectiveValue
                            ?? mo.Observation?.Effective?.EffectiveValue;
            var stateLine = effective is not null
                ? $"Observed: {observed}  ·  Effective: {effective}"
                : $"Observed: {observed}";

            left.Children.Add(new TextBlock
            {
                Text = stateLine,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            });

            if (!string.IsNullOrWhiteSpace(mo.SubCategory))
            {
                left.Children.Add(new TextBlock
                {
                    Text = mo.SubCategory,
                    Style = (Style)FindResource("MetaText"),
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            if (hasConflict || isUnknown)
            {
                var badge = new Border
                {
                    Style = (Style)FindResource(hasConflict ? "BadgeConflict" : "BadgeUnknown"),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                badge.Child = new TextBlock
                {
                    Text = hasConflict ? "Conflict" : "Unknown",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource(hasConflict ? "BrushConflict" : "BrushUnknown")
                };
                Grid.SetColumn(badge, 1);
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
