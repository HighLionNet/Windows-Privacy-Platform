using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

/// <summary>
/// Domain page: index of categories (SubCategory groups) within a ProductDomain.
/// Does not list individual setting state — that belongs on CategoryView.
/// </summary>
public partial class DomainView : UserControl
{
    public DomainView(
        ScanService scan,
        ProductDomain domain,
        Action<ProductDomain, string> openCategory,
        Action<string> openSetting)
    {
        _ = openSetting; // Detail navigation is intentionally unavailable from a domain index.
        InitializeComponent();
        TitleText.Text = NavigationBuilder.HumanizeDomain(domain);

        if (domain == ProductDomain.Defender && scan.Overview is not null)
        {
            ProtectionProductPanel.Visibility = Visibility.Visible;
            ProtectionProductText.Text = scan.Overview.ProtectionProductSummary;
            ProtectionProductText.Foreground = (Brush)FindResource(scan.Overview.ProtectionProductStatus switch
            {
                ProtectionProductObservationStatus.Observed => "BrushSuccess",
                ProtectionProductObservationStatus.AccessDenied or ProtectionProductObservationStatus.Error => "BrushWarning",
                _ => "BrushTextMuted"
            });
        }

        if (domain == ProductDomain.Edge)
        {
            EdgePresencePanel.Visibility = Visibility.Visible;
            var browsers = scan.LastScanResult?.Snapshot?.Applications.Browsers ?? new BrowserPresenceSnapshot();
            EdgePresenceText.Text = ProductLine(browsers.Edge);
            WebViewPresenceText.Text = ProductLine(browsers.WebView2);
            DefaultBrowserText.Text = "Default browser (observed): " + browsers.DefaultBrowser.Summary;
        }

        var items = scan.SettingsCatalog.Where(m => m.ProductDomain == domain).ToList();
        if (items.Count == 0)
        {
            SubtitleText.Text = "No curated entries.";
            CategoryList.Children.Add(new TextBlock
            {
                Text = "No settings in catalog for this domain.",
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(12, 10, 12, 10)
            });
            return;
        }

        var groups = items
            .GroupBy(m => string.IsNullOrWhiteSpace(m.SubCategory) ? domain.ToString() : m.SubCategory!)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var conflictTotal = items.Count(m =>
            m.Observation?.Resolution?.HasConflict == true ||
            m.Observation?.Effective?.HasConflict == true);

        var categoryCount = $"{groups.Count} categor{(groups.Count == 1 ? "y" : "ies")}";
        SubtitleText.Text = conflictTotal > 0
            ? $"{categoryCount} covering {items.Count} settings. {conflictTotal} need attention."
            : $"{categoryCount} covering {items.Count} settings.";

        foreach (var group in groups)
        {
            var conflicts = group.Count(m =>
                m.Observation?.Resolution?.HasConflict == true ||
                m.Observation?.Effective?.HasConflict == true);
            var unknowns = group.Count(IsUnknown);

            CategoryList.Children.Add(BuildCategoryRow(
                group.Key,
                group.Count(),
                conflicts,
                unknowns,
                CategoryContent.For(domain, group.Key),
                () => openCategory(domain, group.Key)));
        }
    }

    private static string ProductLine(BrowserProductInfo product) => product.Evidence == EvidenceState.Configured
        ? $"{product.Name}: {product.Version} · {product.InstallPath}"
        : $"{product.Name}: {EvidenceStateSemantics.Label(product.Evidence)}";

    private void DefaultApps_Click(object sender, RoutedEventArgs e) => OpenSettings("ms-settings:defaultapps");
    private void AppsFeatures_Click(object sender, RoutedEventArgs e) => OpenSettings("ms-settings:appsfeatures");
    private static void OpenSettings(string uri)
    {
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch { }
    }

    private Button BuildSettingRow(ManagedObject mo, Action open)
    {
        var conflict = mo.Observation?.Resolution?.HasConflict == true ||
                       mo.Observation?.Effective?.HasConflict == true;
        var row = new Button
        {
            Style = (Style)FindResource(conflict ? "ListRowButtonConflict" : "ListRowButton"),
            ToolTip = "Open setting details"
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new StackPanel();
        text.Children.Add(new TextBlock
        {
            Text = mo.ObjectName,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });
        text.Children.Add(new TextBlock
        {
            Text = mo.Narrative.Summary,
            FontSize = 11,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 8, 0)
        });
        grid.Children.Add(text);

        var badges = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (!mo.IsWritable)
            badges.Children.Add(Badge("VIEW ONLY", "BadgeUnknown"));
        if (!mo.IsApplicableHere)
            badges.Children.Add(Badge(CatalogPolicy.ApplicabilityBadgeText(mo.Applicability), "BadgeWarning"));
        Grid.SetColumn(badges, 1);
        grid.Children.Add(badges);

        row.Content = grid;
        AutomationProperties.SetName(row, $"{mo.ObjectName}. {(mo.IsWritable ? "Change available" : "View only")}. {mo.Narrative.Summary}");
        row.Click += (_, _) => open();
        return row;
    }

    private Border Badge(string text, string style)
    {
        var badge = new Border { Style = (Style)FindResource(style), Margin = new Thickness(6, 0, 0, 0) };
        badge.Child = new TextBlock { Text = text, FontSize = 9, FontWeight = FontWeights.SemiBold };
        return badge;
    }

    private Button BuildCategoryRow(string name, int count, int conflicts, int unknowns, CategoryCopy copy, Action open)
    {
        var row = new Button
        {
            Style = (Style)FindResource(conflicts > 0 ? "ListRowButtonConflict" : "ListRowButton"),
            ToolTip = "Open category"
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        var left = new StackPanel();
        left.Children.Add(new TextBlock
        {
            Text = name,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });
        left.Children.Add(new TextBlock
        {
            Text = copy.Description,
            FontSize = 11,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 12, 0)
        });

        if (conflicts > 0)
        {
            left.Children.Add(new TextBlock
            {
                Text = $"{conflicts} conflict{(conflicts == 1 ? "" : "s")}",
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushConflict"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        else if (unknowns == count)
        {
            left.Children.Add(new TextBlock
            {
                Text = "All unknown on this scan",
                FontSize = 11,
                Foreground = (Brush)FindResource("BrushTextMuted"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        Grid.SetColumn(left, 0);
        grid.Children.Add(left);

        var countBlock = new TextBlock
        {
            Text = count.ToString(),
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        };
        Grid.SetColumn(countBlock, 1);
        grid.Children.Add(countBlock);

        var attention = new TextBlock
        {
            Text = conflicts > 0 ? conflicts.ToString() : (unknowns > 0 ? "—" : ""),
            FontSize = 13,
            FontWeight = conflicts > 0 ? FontWeights.SemiBold : FontWeights.Normal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
            Foreground = conflicts > 0
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextMuted")
        };
        Grid.SetColumn(attention, 2);
        grid.Children.Add(attention);

        row.Content = grid;
        AutomationProperties.SetName(row, $"{name}. {count} settings. {conflicts} conflicts. {copy.Description}");
        row.Click += (_, _) => open();
        return row;
    }

    private static bool IsUnknown(ManagedObject mo)
    {
        var observed = mo.CurrentState ?? "";
        return string.IsNullOrWhiteSpace(mo.CurrentState) ||
               observed.Contains("Not observed", StringComparison.OrdinalIgnoreCase) ||
               observed.Contains("Unknown", StringComparison.OrdinalIgnoreCase) ||
               observed.Contains("Not configured", StringComparison.OrdinalIgnoreCase);
    }
}
