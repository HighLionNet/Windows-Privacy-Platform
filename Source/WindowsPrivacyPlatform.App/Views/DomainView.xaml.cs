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
            SubtitleText.Text = "This domain has no managed settings in the current catalog.";
            SettingsList.Items.Add(new TextBlock
            {
                Text = "No curated entries for this domain yet.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 8, 0, 0)
            });
            return;
        }

        var conflicts = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        SubtitleText.Text = conflicts > 0
            ? $"{items.Count} settings · {conflicts} with layer conflict. Click a card for the full explanation."
            : $"{items.Count} settings in this domain. Click a card to open the full explanation.";

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
                Style = (Style)FindResource(hasConflict ? "CardConflict" : "Card"),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var panel = new StackPanel();

            var header = new DockPanel { LastChildFill = true };
            var title = new TextBlock
            {
                Text = mo.ObjectName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            DockPanel.SetDock(title, Dock.Left);
            header.Children.Add(title);

            if (hasConflict || isUnknown)
            {
                var badge = new Border
                {
                    Style = (Style)FindResource(hasConflict ? "BadgeConflict" : "BadgeUnknown"),
                    Margin = new Thickness(12, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                badge.Child = new TextBlock
                {
                    Text = hasConflict ? "Conflict" : "Unknown",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource(hasConflict ? "BrushConflict" : "BrushUnknown")
                };
                DockPanel.SetDock(badge, Dock.Right);
                header.Children.Add(badge);
            }

            panel.Children.Add(header);

            panel.Children.Add(new TextBlock
            {
                Text = mo.ObjectId,
                Foreground = (Brush)FindResource("BrushTextMuted"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            var effective = mo.Observation?.Resolution?.EffectiveValue
                            ?? mo.Observation?.Effective?.EffectiveValue;
            var line = effective is not null
                ? $"Observed: {observed} · Effective: {effective}"
                : $"Observed: {observed}";

            panel.Children.Add(new TextBlock
            {
                Text = line,
                Margin = new Thickness(0, 6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(mo.SubCategory))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = mo.SubCategory,
                    FontSize = 11,
                    Foreground = (Brush)FindResource("BrushTextMuted"),
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            border.Child = panel;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            border.ToolTip = "Open full knowledge card";
            SettingsList.Items.Add(border);
        }
    }
}
