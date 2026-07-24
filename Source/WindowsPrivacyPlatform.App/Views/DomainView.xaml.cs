using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class DomainView : UserControl
{
    public DomainView(ScanService scan, ProductDomain domain, Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = domain.ToString();
        var items = scan.Catalog.Where(m => m.ProductDomain == domain)
            .OrderBy(m => m.SubCategory)
            .ThenBy(m => m.ObjectName)
            .ToList();

        SubtitleText.Text = $"{items.Count} settings in this domain. Click a card to open the full explanation.";

        foreach (var mo in items)
        {
            var border = new Border { Style = (Style)FindResource("Card"), Cursor = System.Windows.Input.Cursors.Hand };
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = mo.ObjectName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14
            });
            panel.Children.Add(new TextBlock
            {
                Text = mo.ObjectId,
                Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            var observed = mo.CurrentState ?? "Not observed";
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

            if (mo.Observation?.Resolution?.HasConflict == true || mo.Observation?.Effective?.HasConflict == true)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Layer conflict detected",
                    Foreground = (System.Windows.Media.Brush)FindResource("BrushConflict"),
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            border.Child = panel;
            var id = mo.ObjectId;
            border.MouseLeftButtonUp += (_, _) => openSetting(id);
            SettingsList.Items.Add(border);
        }
    }
}
