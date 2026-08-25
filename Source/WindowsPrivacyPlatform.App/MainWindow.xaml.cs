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
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App;

/// <summary>
/// Application shell. Hierarchy: Home → Domain → Category → Setting detail.
/// v1.6: Modify mode with confirmed registry writes from category value buttons.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ScanService _scan = new();
    private readonly IAuditLogger _log = new AuditLogger();
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private CancellationTokenSource? _cts;
    private string _currentNav = "home";
    private readonly List<Button> _navButtons = new();
    private bool _sidebarCollapsed;
    private readonly string _prefsPath;
    private bool _modeChangeInProgress;
    private readonly bool _startupModifyRequested;

    public MainWindow(bool startupModifyRequested = false)
    {
        _startupModifyRequested = startupModifyRequested;
        // Must construct before InitializeComponent: ModeCombo SelectionChanged fires during XAML load.
        _elevation = new ElevationService(_log);
        _changes = new PolicyChangeService(_elevation, _log);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPrivacyPlatform");
        Directory.CreateDirectory(appData);
        _prefsPath = Path.Combine(appData, "window.prefs");

        InitializeComponent();

        RestoreWindowBounds();

        Loaded += async (_, _) =>
        {
            BuildDomainNavigation();
            CollectNavButtons();
            if (_sidebarCollapsed)
            {
                SidebarColumn.Width = new GridLength(0);
                NavPanel.Visibility = Visibility.Collapsed;
            }
            UpdateBreadcrumbs("Machine Overview");
            ShowWelcome();
            if (_startupModifyRequested)
                EnterModifyMode();
            await RunScanAsync();
        };

        _scan.StatusChanged += status =>
            Dispatcher.Invoke(() =>
            {
                if (StatusText is not null)
                    StatusText.Text = status;
            });
    }

    private void BuildDomainNavigation()
    {
        DomainNavPanel.Children.Clear();

        var domains = ManagedObjectCatalog.All
            .Select(m => m.ProductDomain)
            .Distinct()
            .OrderBy(DomainGroupOrder)
            .ThenBy(NavigationBuilder.HumanizeDomain)
            .GroupBy(DomainGroup);

        foreach (var group in domains)
        {
            DomainNavPanel.Children.Add(new Border
            {
                Style = (Style)FindResource("SectionRule"),
                Margin = new Thickness(10, 8, 10, 2)
            });

            var label = new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                Style = (Style)FindResource("SidebarGroupLabel")
            };

            var brushKey = group.Key switch
            {
                "Privacy" => "BrushDomainPrivacy",
                "Security" => "BrushDomainSecurity",
                "Windows" => "BrushDomainWindows",
                "Applications" => "BrushDomainApps",
                _ => "BrushTextMuted"
            };
            label.Foreground = (Brush)FindResource(brushKey);
            DomainNavPanel.Children.Add(label);

            foreach (var domain in group)
            {
                var button = new Button
                {
                    Content = NavigationBuilder.HumanizeDomain(domain),
                    Style = (Style)FindResource("SidebarButton"),
                    Tag = $"domain:{domain}"
                };
                button.Click += Nav_Click;
                DomainNavPanel.Children.Add(button);
            }
        }
    }

    private static int DomainGroupOrder(ProductDomain domain) => DomainGroup(domain) switch
    {
        "Privacy" => 0,
        "Security" => 1,
        "Windows" => 2,
        "Applications" => 3,
        _ => 4
    };

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
            Text = "Press Scan (F5) to discover local configuration.\n\nInspect mode · read-only. Switch to Modify (elevated) to change values from category pages.",
            FontSize = 13,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4)
        };
        UpdateBreadcrumbs("Ready");
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await RunScanAsync();
    private async void MenuScan_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuHome_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("home");
    private void MenuConflicts_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("conflicts");
    private void MenuKnowledge_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("knowledge");
    private void MenuAbout_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("about");

    private void MenuSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void NavigateFromMenu(string tag)
    {
        _currentNav = tag;
        var btn = _navButtons.FirstOrDefault(b => b.Tag as string == tag);
        if (btn is not null)
            HighlightNav(btn);
        Navigate(tag);
    }

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
                ContentHost.Content = new DomainView(_scan, domain, OpenCategory);
                UpdateBreadcrumbs(
                    NavigationBuilder.HumanizeDomain(domain),
                    group: DomainGroup(domain),
                    domainName: NavigationBuilder.HumanizeDomain(domain),
                    domainTag: $"domain:{domain}");
                return;
            }
        }

        if (tag.StartsWith("category:", StringComparison.OrdinalIgnoreCase))
        {
            var rest = tag["category:".Length..];
            var sep = rest.IndexOf(':');
            if (sep > 0)
            {
                var domainName = rest[..sep];
                var category = rest[(sep + 1)..];
                if (Enum.TryParse<ProductDomain>(domainName, out var domain))
                {
                    ContentHost.Content = new CategoryView(
                        _scan,
                        domain,
                        category,
                        OpenSetting,
                        _elevation,
                        _changes,
                        RunScanAsync,
                        this);
                    UpdateBreadcrumbs(
                        category,
                        group: DomainGroup(domain),
                        domainName: NavigationBuilder.HumanizeDomain(domain),
                        domainTag: $"domain:{domain}",
                        categoryName: category,
                        categoryTag: tag);
                    return;
                }
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
        NavigateFromMenu("conflicts");
    }

    private void NavigateDomain(ProductDomain domain)
    {
        NavigateFromMenu($"domain:{domain}");
    }

    private void OpenCategory(ProductDomain domain, string category)
    {
        var tag = $"category:{domain}:{category}";
        _currentNav = tag;

        var domainTag = $"domain:{domain}";
        var btn = _navButtons.FirstOrDefault(b => b.Tag as string == domainTag);
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

        var category = string.IsNullOrWhiteSpace(mo.SubCategory)
            ? mo.ProductDomain.ToString()
            : mo.SubCategory!;

        ContentHost.Content = new SettingDetailPage(detail, OpenSetting);
        UpdateBreadcrumbs(
            detail.Title,
            group: DomainGroup(mo.ProductDomain),
            domainName: NavigationBuilder.HumanizeDomain(mo.ProductDomain),
            domainTag: $"domain:{mo.ProductDomain}",
            categoryName: category,
            categoryTag: $"category:{mo.ProductDomain}:{category}");

        _currentNav = $"setting:{objectId}";
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

    private void UpdateBreadcrumbs(
        string current,
        string? group = null,
        string? domainName = null,
        string? domainTag = null,
        string? categoryName = null,
        string? categoryTag = null)
    {
        BreadcrumbPanel.Children.Clear();

        AddBreadcrumbLink("Home", "home");

        if (!string.IsNullOrEmpty(group))
        {
            AddBreadcrumbSep();
            AddBreadcrumbText(group);
        }

        if (!string.IsNullOrEmpty(domainName) && !string.IsNullOrEmpty(domainTag))
        {
            AddBreadcrumbSep();
            AddBreadcrumbLink(domainName, domainTag);
        }

        if (!string.IsNullOrEmpty(categoryName) && !string.IsNullOrEmpty(categoryTag))
        {
            AddBreadcrumbSep();
            if (string.Equals(current, categoryName, StringComparison.Ordinal))
                AddBreadcrumbText(categoryName);
            else
                AddBreadcrumbLink(categoryName, categoryTag);
        }

        var isRoot = string.Equals(current, "Machine Overview", StringComparison.Ordinal)
                     || string.Equals(current, "Ready", StringComparison.Ordinal);
        var domainAlreadyShown = !string.IsNullOrEmpty(domainName)
                                  && string.Equals(current, domainName, StringComparison.Ordinal);
        var categoryAlreadyShown = !string.IsNullOrEmpty(categoryName)
                                    && string.Equals(current, categoryName, StringComparison.Ordinal);

        if (!isRoot && !domainAlreadyShown && !categoryAlreadyShown)
        {
            AddBreadcrumbSep();
            AddBreadcrumbText(current);
        }
    }

    private void AddBreadcrumbSep()
    {
        BreadcrumbPanel.Children.Add(new TextBlock
        {
            Text = " › ",
            Style = (Style)FindResource("BreadcrumbText"),
            Margin = new Thickness(2, 0, 2, 0)
        });
    }

    private void AddBreadcrumbText(string text)
    {
        BreadcrumbPanel.Children.Add(new TextBlock
        {
            Text = text,
            Style = (Style)FindResource("BreadcrumbText"),
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("BrushTextPrimary")
        });
    }

    private void AddBreadcrumbLink(string text, string tag)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)FindResource("BreadcrumbLink"),
            Tag = tag,
            Cursor = Cursors.Hand
        };
        btn.Click += (_, _) =>
        {
            _currentNav = tag;
            var highlightTag = tag;
            if (tag.StartsWith("category:", StringComparison.OrdinalIgnoreCase))
            {
                var rest = tag["category:".Length..];
                var sep = rest.IndexOf(':');
                if (sep > 0)
                    highlightTag = "domain:" + rest[..sep];
            }
            var match = _navButtons.FirstOrDefault(b => b.Tag as string == highlightTag);
            if (match is not null)
                HighlightNav(match);
            Navigate(tag);
        };
        BreadcrumbPanel.Children.Add(btn);
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
        if (_modeChangeInProgress || ModeCombo is null || StatusText is null)
            return;

        if (ModeCombo.SelectedIndex == 1)
        {
            EnterModifyMode();
        }
        else
        {
            _elevation.ExitModifyMode();
            StatusText.Text = "Inspect mode · read-only.";
            Navigate(_currentNav);
        }
    }

    private void EnterModifyMode()
    {
        if (_modeChangeInProgress)
            return;

        _modeChangeInProgress = true;
        try
        {
            if (!_elevation.TryEnterModifyMode(this))
            {
                ModeCombo.SelectedIndex = 0;
                return;
            }

            ModeCombo.SelectedIndex = 1;
            StatusText.Text = "Modify mode authorized — use value buttons on category pages to change settings.";
            _log.Change("MainWindow", "Modify mode entered. Registry writes enabled with confirmation.");
            Navigate(_currentNav);
        }
        finally
        {
            _modeChangeInProgress = false;
        }
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        SidebarColumn.Width = _sidebarCollapsed ? new GridLength(0) : new GridLength(240);
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
