using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private bool _sidebarCollapsed;
    private readonly string _prefsPath;

    public MainWindow()
    {
        InitializeComponent();

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPrivacyPlatform");
        Directory.CreateDirectory(appData);
        _prefsPath = Path.Combine(appData, "window.prefs");

        RestoreWindowBounds();

        Loaded += async (_, _) =>
        {
            CollectNavButtons();
            if (_sidebarCollapsed)
                NavPanel.Visibility = Visibility.Collapsed;
            UpdateBreadcrumbs("Machine Overview");
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
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
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
            Text = "Windows Privacy Platform\n\nPress Scan (F5) to discover local configuration.\n\nInspect mode · read-only · no elevation.",
            FontSize = 13,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4)
        };
        UpdateBreadcrumbs("Ready");
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
            CatalogCountLabel.Text = $"Objects: {_scan.Catalog.Count}";
            ValidationLabel.Text = _scan.ValidationFailed == 0
                ? $"Validation: {_scan.ValidationPassed} ok"
                : $"Validation: {_scan.ValidationPassed} ok / {_scan.ValidationFailed} fail";

            var conflicts = _scan.Query?.GetConflicts().Count() ?? 0;
            ConflictCountLabel.Text = $"Conflicts: {conflicts}";
            ConflictCountLabel.Foreground = conflicts > 0
                ? (Brush)FindResource("BrushConflict")
                : (Brush)FindResource("BrushTextMuted");
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
            ContentHost.Content = new HomeView(_scan, OpenSetting, NavigateDomain, OpenConflicts);
            UpdateBreadcrumbs("Machine Overview");
            return;
        }

        if (tag == "conflicts")
        {
            ContentHost.Content = new ConflictsView(_scan, OpenSetting);
            UpdateBreadcrumbs("Conflicts");
            return;
        }

        if (tag == "knowledge")
        {
            ContentHost.Content = new KnowledgeExplorerView(_scan, OpenSetting);
            UpdateBreadcrumbs("Knowledge Explorer");
            return;
        }

        if (tag == "about")
        {
            ContentHost.Content = new AboutView();
            UpdateBreadcrumbs("About");
            return;
        }

        if (tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase))
        {
            var name = tag["domain:".Length..];
            if (Enum.TryParse<ProductDomain>(name, out var domain))
            {
                ContentHost.Content = new DomainView(_scan, domain, OpenSetting);
                UpdateBreadcrumbs(NavigationBuilder.HumanizeDomain(domain), group: DomainGroup(domain));
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

    private void OpenConflicts()
    {
        _currentNav = "conflicts";
        var btn = _navButtons.FirstOrDefault(b => b.Tag as string == "conflicts");
        if (btn is not null)
            HighlightNav(btn);
        Navigate("conflicts");
    }

    private void NavigateDomain(ProductDomain domain)
    {
        var tag = $"domain:{domain}";
        _currentNav = tag;
        var btn = _navButtons.FirstOrDefault(b => b.Tag as string == tag);
        if (btn is not null)
            HighlightNav(btn);
        Navigate(tag);
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
        UpdateBreadcrumbs(detail.Title, group: DomainGroup(mo.ProductDomain), domain: NavigationBuilder.HumanizeDomain(mo.ProductDomain));
    }

    private static string DomainGroup(ProductDomain d) => d switch
    {
        ProductDomain.ConsentStore or ProductDomain.AppPrivacy or ProductDomain.Telemetry
            or ProductDomain.Advertising or ProductDomain.Location or ProductDomain.ActivityHistory
            or ProductDomain.CloudContent => "Privacy",
        ProductDomain.Defender or ProductDomain.Firewall or ProductDomain.Biometrics => "Security",
        ProductDomain.WindowsUpdate or ProductDomain.Search or ProductDomain.Speech or ProductDomain.Device => "Windows",
        ProductDomain.Edge => "Applications",
        _ => "Domains"
    };

    private void UpdateBreadcrumbs(string current, string? group = null, string? domain = null)
    {
        BreadcrumbPanel.Children.Clear();

        void AddLink(string text, string? tag)
        {
            if (tag is null)
            {
                BreadcrumbPanel.Children.Add(new TextBlock
                {
                    Text = text,
                    Style = (Style)FindResource("BreadcrumbText"),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("BrushTextPrimary")
                });
                return;
            }

            var btn = new Button
            {
                Content = text,
                Style = (Style)FindResource("BreadcrumbLink"),
                Tag = tag
            };
            btn.Click += (_, _) =>
            {
                _currentNav = tag;
                var match = _navButtons.FirstOrDefault(b => b.Tag as string == tag);
                if (match is not null)
                    HighlightNav(match);
                Navigate(tag);
            };
            BreadcrumbPanel.Children.Add(btn);
        }

        void AddSep()
        {
            BreadcrumbPanel.Children.Add(new TextBlock
            {
                Text = " › ",
                Style = (Style)FindResource("BreadcrumbText"),
                Margin = new Thickness(2, 0, 2, 0)
            });
        }

        AddLink("Home", "home");
        if (group is not null)
        {
            AddSep();
            BreadcrumbPanel.Children.Add(new TextBlock
            {
                Text = group,
                Style = (Style)FindResource("BreadcrumbText")
            });
        }
        if (domain is not null)
        {
            AddSep();
            BreadcrumbPanel.Children.Add(new TextBlock
            {
                Text = domain,
                Style = (Style)FindResource("BreadcrumbText")
            });
        }
        if (!string.Equals(current, "Machine Overview", StringComparison.Ordinal) &&
            !string.Equals(current, "Ready", StringComparison.Ordinal))
        {
            AddSep();
            AddLink(current, null);
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchBox.Text = string.Empty;
            Keyboard.ClearFocus();
            e.Handled = true;
            return;
        }

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
        UpdateBreadcrumbs($"Search: {q}");
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 || (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control))
        {
            e.Handled = true;
            _ = RunScanAsync();
            return;
        }

        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            SearchBox.Focus();
            SearchBox.SelectAll();
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (SearchBox.IsKeyboardFocusWithin)
            {
                SearchBox.Text = string.Empty;
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModeCombo.SelectedIndex == 1)
        {
            MessageBox.Show(
                "Modify mode is a future capability.\n\n" +
                "It will require privilege elevation, restore points, validation, and a separate safety architecture.\n\n" +
                "Version 1.0 is Inspect-only. No write functionality exists.",
                "Modify mode (future)",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            ModeCombo.SelectedIndex = 0;
        }
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        SidebarColumn.Width = _sidebarCollapsed ? new GridLength(48) : new GridLength(240);
        NavPanel.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RestoreWindowBounds()
    {
        try
        {
            if (!File.Exists(_prefsPath))
                return;
            var lines = File.ReadAllLines(_prefsPath);
            if (lines.Length < 4)
                return;
            if (double.TryParse(lines[0], out var left) &&
                double.TryParse(lines[1], out var top) &&
                double.TryParse(lines[2], out var width) &&
                double.TryParse(lines[3], out var height))
            {
                Left = left;
                Top = top;
                Width = Math.Max(MinWidth, width);
                Height = Math.Max(MinHeight, height);
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
            if (lines.Length >= 5 && bool.TryParse(lines[4], out var collapsed))
                _sidebarCollapsed = collapsed;
        }
        catch
        {
            // ignore preference load failures
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            File.WriteAllLines(_prefsPath, new[]
            {
                Left.ToString("F0"),
                Top.ToString("F0"),
                Width.ToString("F0"),
                Height.ToString("F0"),
                _sidebarCollapsed.ToString()
            });
        }
        catch
        {
            // ignore preference save failures
        }
    }
}
