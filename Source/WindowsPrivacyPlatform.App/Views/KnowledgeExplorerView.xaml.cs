using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
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
            List.Items.Add(new Border
            {
                Background = (Brush)FindResource("BrushBgHeader"),
                BorderBrush = (Brush)FindResource("BrushBorder"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 10, 5),
                Child = new TextBlock
                {
                    Text = NavigationBuilder.HumanizeDomain(group.Key),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("BrushTextSecondary")
                }
            });

            foreach (var mo in group.OrderBy(m => m.ObjectName))
            {
                var row = new Button { Style = (Style)FindResource("ListRowButton") };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock
                {
                    Text = mo.ObjectName,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
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
                        Margin = new Thickness(0, 2, 0, 0),
                        FontSize = 11,
                        Foreground = (Brush)FindResource("BrushTextSecondary")
                    });
                }
                row.Content = panel;
                AutomationProperties.SetName(row, $"{mo.ObjectName}. {mo.Description}");
                var id = mo.ObjectId;
                row.Click += (_, _) => openSetting(id);
                List.Items.Add(row);
            }
        }
    }
}
