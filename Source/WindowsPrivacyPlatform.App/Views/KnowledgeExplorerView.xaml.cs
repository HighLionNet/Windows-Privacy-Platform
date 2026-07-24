using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;

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
                Text = group.Key.ToString(),
                FontWeight = FontWeights.SemiBold,
                FontSize = 15,
                Margin = new Thickness(0, 12, 0, 6)
            });

            foreach (var mo in group.OrderBy(m => m.ObjectName))
            {
                var border = new Border { Style = (Style)FindResource("Card"), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(12) };
                var panel = new StackPanel();
                panel.Children.Add(new TextBlock { Text = mo.ObjectName, FontWeight = FontWeights.SemiBold });
                panel.Children.Add(new TextBlock
                {
                    Text = mo.Description,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = (System.Windows.Media.Brush)FindResource("BrushTextSecondary")
                });
                border.Child = panel;
                var id = mo.ObjectId;
                border.MouseLeftButtonUp += (_, _) => openSetting(id);
                List.Items.Add(border);
            }
        }
    }
}
