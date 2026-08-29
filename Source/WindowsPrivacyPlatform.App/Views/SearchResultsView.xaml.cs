using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SearchResultsView : UserControl
{
    public SearchResultsView(ScanService scan, string query, Action<SettingsListTarget> openSettingsList, Action<string> openInventoryDetail)
    {
        InitializeComponent();
        TitleText.Text = $"Search: {query}";

        var q = query.Trim();
        var results = (scan.Query?.Search(q) ?? scan.Catalog.Where(m =>
                Contains(m.ObjectName, q) ||
                Contains(m.ObjectId, q) ||
                Contains(m.Description, q) ||
                Contains(m.SubCategory, q) ||
                Contains(m.TechnicalLocation, q)))
            .OrderBy(m => m.ObjectName)
            .ToList();

        if (results.Count == 0)
        {
            SubtitleText.Text = "No matches";
            List.Items.Add(new TextBlock
            {
                Text = "No results.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(10, 8, 10, 8)
            });
            return;
        }

        SubtitleText.Text = $"{results.Count} match(es)";

        foreach (var mo in results)
        {
            var row = new Button { Style = (Style)FindResource("ListRowButton") };
            var panel = new StackPanel();
            var heading = new DockPanel { LastChildFill = false };
            var name = new TextBlock
            {
                Text = mo.ObjectName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            };
            DockPanel.SetDock(name, Dock.Left);
            heading.Children.Add(name);
            var badge = new Border
            {
                Style = (Style)FindResource(mo.Bucket == CatalogBucket.SystemInventory ? "BadgeUnknown" : "BadgeSuccess"),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock
            {
                Text = mo.Bucket == CatalogBucket.SystemInventory ? "SYSTEM INVENTORY" : "SETTINGS",
                FontSize = 9,
                FontWeight = FontWeights.SemiBold
            };
            DockPanel.SetDock(badge, Dock.Right);
            heading.Children.Add(badge);
            panel.Children.Add(heading);
            panel.Children.Add(new TextBlock
            {
                Text = $"{NavigationBuilder.HumanizeDomain(mo.ProductDomain)} · {(mo.IsWritable ? "Change available" : "View only")}",
                Style = (Style)FindResource("MetaText"),
                Margin = new Thickness(0, 1, 0, 0)
            });
            if (!string.IsNullOrWhiteSpace(mo.Description))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = mo.Description,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0),
                    FontSize = 11,
                    Foreground = (Brush)FindResource("BrushTextSecondary")
                });
            }
            if (!mo.IsApplicableHere)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = CatalogPolicy.ApplicabilityBadgeText(mo.Applicability) + " — " + mo.ApplicabilityReason,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 3, 0, 0),
                    FontSize = 11,
                    Foreground = (Brush)FindResource("BrushWarning")
                });
            }
            row.Content = panel;
            AutomationProperties.SetName(row,
                $"{mo.ObjectName}. {(mo.Bucket == CatalogBucket.SystemInventory ? "System inventory" : "Settings")}. {(mo.IsWritable ? "Change available" : "View only")}.");
            row.ToolTip = mo.Bucket == CatalogBucket.Settings
                ? "Open the matching category list and highlight this result"
                : "Open inventory details";
            var id = mo.ObjectId;
            row.Click += (_, _) =>
            {
                if (mo.Bucket == CatalogBucket.Settings)
                    openSettingsList(SettingsListTarget.For(mo, q));
                else
                    openInventoryDetail(id);
            };
            List.Items.Add(row);
        }
    }

    private static bool Contains(string? hay, string needle) =>
        !string.IsNullOrEmpty(hay) &&
        hay.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
