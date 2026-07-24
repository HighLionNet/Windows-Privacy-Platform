using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.App.Views;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App;

/// <summary>
/// Application shell. Presentation only — navigates views over ScanService results.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ScanService _scan = new();
    private CancellationTokenSource? _cts;
    private string _currentNav = "home";
    private readonly List<Button> _navButtons = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            CollectNavButtons();
            ShowWelcome();
            await RunScanAsync();
        };

        _scan.StatusChanged += status =>
            Dispatcher.Invoke(() => StatusText.Text = status);
    }

    private void CollectNavButtons()
    {
        void Walk(DependencyObject parent)
        {
            var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is Button b && b.Tag is string)
                    _navButtons.Add(b);
                Walk(child);
            }
        }
        Walk(this);
    }

    private void ShowWelcome()
    {
        ContentHost.Content = new TextBlock
        {
            Text = "Windows Privacy Platform\n\nRead-only knowledge explorer.\nPress Scan (or F5) to discover local configuration.\n\nThis application never modifies Windows.\nNo elevation · No telemetry · Unknown stays Unknown.",
            FontSize = 15,
            Foreground = (System.Windows.Media.Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8)
        };
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private async Task RunScanAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        ScanButton.IsEnabled = false;
        ScanButton.Content = "Scanning…";

        try
        {
            await _scan.RunScanAsync(_cts.Token);
            UpdateChrome();
            Navigate(_currentNav);
        }
        finally
        {
            ScanButton.IsEnabled = true;
            ScanButton.Content = "Scan";
        }
    }

    private void UpdateChrome()
    {
        if (_scan.Overview is not null)
        {
            ScanTimeLabel.Text = $"Scanned {_scan.Overview.LastScanUtc:yyyy-MM-dd HH:mm} UTC";
            CatalogCountLabel.Text = $"Catalog: {_scan.Catalog.Count}";
            ValidationLabel.Text = $"Validation: {_scan.ValidationPassed} ok / {_scan.ValidationFailed} fail";

            var conflicts = _scan.Query?.GetConflicts().Count() ?? 0;
            ConflictCountLabel.Text = $"Conflicts: {conflicts}";
            if (conflicts > 0)
                ConflictCountLabel.Foreground = (System.Windows.Media.Brush)FindResource("BrushConflict");
            else
                ConflictCountLabel.Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted");
        }
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string tag)
        {
            _currentNav = tag;
            HighlightNav(b);
            Navigate(tag);
        }
    }

    private void HighlightNav(Button selected)
    {
        foreach (var b in _navButtons)
        {
            b.Style = (Style)FindResource(
                ReferenceEquals(b, selected) ? "SidebarButtonSelected" : "SidebarButton");
        }
    }

    private void Navigate(string tag)
    {
        if (!_scan.HasScan && tag != "about")
        {
            ShowWelcome();
            return;
        }

        if (tag == "home")
        {
            ContentHost.Content = new HomeView(_scan);
            return;
        }

        if (tag == "conflicts")
        {
            ContentHost.Content = new ConflictsView(_scan, OpenSetting);
            return;
        }

        if (tag == "knowledge")
        {
            ContentHost.Content = new KnowledgeExplorerView(_scan, OpenSetting);
            return;
        }

        if (tag == "about")
        {
            ContentHost.Content = new AboutView();
            return;
        }

        if (tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase))
        {
            var name = tag["domain:".Length..];
            if (Enum.TryParse<ProductDomain>(name, out var domain))
            {
                ContentHost.Content = new DomainView(_scan, domain, OpenSetting);
                return;
            }
        }

        if (tag.StartsWith("setting:", StringComparison.OrdinalIgnoreCase))
        {
            OpenSetting(tag["setting:".Length..]);
            return;
        }

        ShowWelcome();
    }

    private void OpenSetting(string objectId)
    {
        var mo = _scan.Catalog.FirstOrDefault(m =>
            string.Equals(m.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
        if (mo is null)
            return;

        var detail = NavigationBuilder.BuildDetail(mo, _scan.Query);
        if (detail is null)
            return;

        ContentHost.Content = new SettingDetailPage(detail, OpenSetting);
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        RunSearch();
        e.Handled = true;
    }

    private void RunSearch()
    {
        var q = SearchBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(q) || _scan.Query is null)
            return;

        ContentHost.Content = new SearchResultsView(_scan, q, OpenSetting);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // F5 or Ctrl+R → rescan
        if (e.Key == Key.F5 || (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control))
        {
            e.Handled = true;
            _ = RunScanAsync();
            return;
        }

        // Ctrl+F → focus search
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            SearchBox.Focus();
            SearchBox.SelectAll();
            return;
        }

        // Escape → clear search focus / return home if on search results
        if (e.Key == Key.Escape)
        {
            if (SearchBox.IsKeyboardFocusWithin)
            {
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }
    }
}
