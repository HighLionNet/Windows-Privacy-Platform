using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class KnowledgeExplorerView : UserControl
{
    public KnowledgeExplorerView(ScanService scan, Action<string> openSetting)
    {
        InitializeComponent();

        foreach (var group in scan.Catalog.GroupBy(m => m.ProductDomain).OrderBy(g => g.Key))
        {
            List.Items.Add(new TextBlock
            {
                Text = NavigationBuilder.HumanizeDomain(group.Key),
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 12, 0, 4)
            });

            foreach (var mo in group.OrderBy(m => m.ObjectName))
            {
                var border = new Border
                {
                    Style = (Style)FindResource("ListRow"),
                    Padding = new Thickness(10, 8, 10, 8)
                };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock
                {
                    Text = mo.ObjectName,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13
                });
                panel.Children.Add(new TextBlock
                {
                    Text = mo.ObjectId,
                    Style = (Style)FindResource("MetaText"),
                    Margin = new Thickness(0, 1, 0, 0)
                });
                if (!string.IsNullOrWhiteSpace(mo.Description))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = mo.Description,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 3, 0, 0),
                        FontSize = 12,
                        Foreground = (Brush)FindResource("BrushTextSecondary")
                    });
                }
                border.Child = panel;
                var id = mo.ObjectId;
                border.MouseLeftButtonUp += (_, _) => openSetting(id);
                List.Items.Add(border);
            }
        }
    }
}
