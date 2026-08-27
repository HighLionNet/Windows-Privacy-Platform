using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.App.Views;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App;

/// <summary>
/// Application shell. Hierarchy: Home → Domain → Category → Setting detail.
/// Modify mode exposes only confirmed, catalog-authorized write controls.
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
    private readonly string _appDataRoot;
    private bool _modeChangeInProgress;
    private bool _startedForModify;

    public MainWindow()
    {
        // Must construct before InitializeComponent: ModeCombo SelectionChanged fires during XAML load.
        _elevation = new ElevationService(_log);
        _changes = new PolicyChangeService(_elevation, _log);

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPrivacyPlatform");
        Directory.CreateDirectory(appData);
        _appDataRoot = appData;
        _prefsPath = Path.Combine(appData, "window.prefs");

        InitializeComponent();

        var product = ProductInfoReader.Read();
        Title = product.Name;
        ProductNameText.Text = product.Name;
        HeaderVersionText.Text = "v" + product.Version;
        FooterVersionText.Text = "v" + product.Version;

        RestoreWindowBounds();

        Loaded += async (_, _) =>
        {
            var arguments = Environment.GetCommandLineArgs();
            var modifyRequested = arguments
                .Any(arg => arg.Equals("--authorize-modify", StringComparison.OrdinalIgnoreCase));
            var sidIndex = Array.FindIndex(arguments, arg => arg.Equals("--initiating-sid", StringComparison.OrdinalIgnoreCase));
            var initiatingSid = sidIndex >= 0 && sidIndex + 1 < arguments.Length ? arguments[sidIndex + 1] : null;
            _startedForModify = modifyRequested;
            var inspectRequested = arguments
                .Any(arg => arg.Equals("--inspect", StringComparison.OrdinalIgnoreCase));
            var suppressShortcutOffer = arguments
                .Any(arg => arg.Equals("--no-shortcut-offer", StringComparison.OrdinalIgnoreCase));
            if (!modifyRequested && !inspectRequested)
            {
                var startup = new StartupModeDialog { Owner = this };
                startup.ShowDialog();
                modifyRequested = startup.ModifyRequested;
            }

            CollectNavButtons();
            if (_sidebarCollapsed)
            {
                SidebarColumn.Width = new GridLength(0);
                NavPanel.Visibility = Visibility.Collapsed;
            }
            UpdateBreadcrumbs("Overview");
            ShowWelcome();
            if (modifyRequested && _elevation.AuthorizeRelaunchedSession(initiatingSid))
                ModeCombo.SelectedIndex = 1;
            await RunScanAsync();
            if (!suppressShortcutOffer)
                ShortcutOfferService.OfferIfNeeded(this);
        };

        _scan.StatusChanged += status =>
            Dispatcher.Invoke(() =>
            {
                if (StatusText is not null)
                    StatusText.Text = status;
            });
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
        SetContent(new TextBlock
        {
            Text = "Scan to review Windows privacy and security policies.\n\nInspect is read-only. Modify enables only the verified settings shown in this app.",
            FontSize = 13,
            Foreground = (Brush)FindResource("BrushTextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4)
        });
        UpdateBreadcrumbs("Ready");
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await RunScanAsync();
    private void CancelScanButton_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
    private async void MenuScan_Click(object sender, RoutedEventArgs e) => await RunScanAsync();

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuHome_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("home");
    private void MenuInventory_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("inventory");
    private void MenuServices_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("services");
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
        ScanProgress.Visibility = Visibility.Visible;
        CancelScanButton.Visibility = Visibility.Visible;

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
            ScanProgress.Visibility = Visibility.Collapsed;
            CancelScanButton.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateChrome()
    {
        if (_scan.Overview is not null)
        {
            var age = DateTime.UtcNow - _scan.Overview.LastScanUtc;
            var status = _scan.LastScanResult?.Status;
            var qualifier = status == ScanStatus.Partial ? " · partial evidence" :
                status == ScanStatus.CompletedWithWarnings ? " · warnings" :
                age > TimeSpan.FromMinutes(30) ? " · stale" : string.Empty;
            ScanTimeLabel.Text = $"Scanned {_scan.Overview.LastScanUtc:yyyy-MM-dd HH:mm} UTC{qualifier}";
            CatalogCountLabel.Text = $"Settings: {_scan.SettingsCatalog.Count}";
            ConflictCountLabel.Text = $"Explorer: {_scan.InventoryCatalog.Count}";

            var visibleDomains = _scan.SettingsCatalog.Select(item => item.ProductDomain).ToHashSet();
            foreach (var button in _navButtons)
            {
                if (button.Tag is not string tag || !tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase))
                    continue;
                button.Visibility = Enum.TryParse<ProductDomain>(tag["domain:".Length..], out var domain) && visibleDomains.Contains(domain)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
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
            SetContent(new HomeView(_scan, OpenSettingsList));
            UpdateBreadcrumbs("Overview");
            return;
        }

        if (tag == "inventory")
        {
            SetContent(new SystemInventoryView(_scan, OpenSetting));
            UpdateBreadcrumbs("System Explorer");
            return;
        }

        if (tag == "services")
        {
            SetContent(new ServicesView(_scan));
            UpdateBreadcrumbs("Services");
            return;
        }

        if (tag == "about")
        {
            SetContent(new AboutView(_scan));
            UpdateBreadcrumbs("About");
            return;
        }

        if (tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase))
        {
            var name = tag["domain:".Length..];
            if (Enum.TryParse<ProductDomain>(name, out var domain))
            {
                SetContent(new DomainView(_scan, domain, OpenCategory, OpenSetting));
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
                    SetContent(new CategoryView(
                        _scan,
                        domain,
                        category,
                        OpenSetting,
                        _elevation,
                        _changes,
                        RunScanAsync,
                        this,
                        completeModifyOperation: CompleteModifyOperation));
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

    private void OpenSettingsList(SettingsListTarget target)
    {
        var tag = $"category:{target.Domain}:{target.Category}";
        _currentNav = tag;
        var domainTag = $"domain:{target.Domain}";
        var button = _navButtons.FirstOrDefault(b => b.Tag as string == domainTag);
        if (button is not null) HighlightNav(button);
        SetContent(new CategoryView(_scan, target.Domain, target.Category, OpenSetting, _elevation,
            _changes, RunScanAsync, this, target.Filter, target.HighlightObjectId, CompleteModifyOperation));
        UpdateBreadcrumbs(target.Category, group: DomainGroup(target.Domain),
            domainName: NavigationBuilder.HumanizeDomain(target.Domain), domainTag: domainTag,
            categoryName: target.Category, categoryTag: tag);
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

        SetContent(new SettingDetailPage(detail, OpenSetting));
        if (mo.Bucket == CatalogBucket.SystemInventory)
        {
            BreadcrumbPanel.Children.Clear();
            AddBreadcrumbLink("Home", "home");
            AddBreadcrumbSep();
            AddBreadcrumbLink("System Explorer", "inventory");
            AddBreadcrumbSep();
            AddBreadcrumbText(detail.Title);
            _currentNav = $"setting:{objectId}";
            return;
        }
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
            or ProductDomain.CloudContent or ProductDomain.Device or ProductDomain.Speech or ProductDomain.Other => "Privacy",
        ProductDomain.Defender or ProductDomain.Firewall or ProductDomain.Biometrics or ProductDomain.LocalSecurity
            or ProductDomain.Network or ProductDomain.RemoteAccess => "Security",
        ProductDomain.Search or ProductDomain.Widgets or ProductDomain.Copilot or ProductDomain.Recall
            or ProductDomain.Edge or ProductDomain.OneDrive => "Windows Apps",
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

        var isRoot = string.Equals(current, "Overview", StringComparison.Ordinal)
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

    private void CompleteModifyOperation()
    {
        _elevation.ExitModifyMode();
        if (_startedForModify && ElevationService.IsProcessElevated())
        {
            _log.Auth("MainWindow", "Elevated process completed its confirmed batch and is exiting.");
            Application.Current.Shutdown();
            return;
        }
        ModeCombo.SelectedIndex = 0;
    }

    private void SetContent(object content)
    {
        ContentHost.Content = content;
        ContentHost.Opacity = 0;
        ContentHost.RenderTransform = new TranslateTransform(0, 4);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        ContentHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease });
        ((TranslateTransform)ContentHost.RenderTransform).BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(4, 0, TimeSpan.FromMilliseconds(170)) { EasingFunction = ease });
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

        SetContent(new SearchResultsView(_scan, q, OpenSettingsList, OpenSetting));
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
            _modeChangeInProgress = true;
            try
            {
                if (!_elevation.TryEnterModifyMode(this))
                {
                    ModeCombo.SelectedIndex = 0;
                    return;
                }

                StatusText.Text = "Modify mode authorized. Approved setting controls are enabled.";
                _log.Change("MainWindow", "Modify mode entered. Registry writes enabled with confirmation.");
                // Rebuild current view so value buttons become enabled.
                Navigate(_currentNav);
            }
            finally
            {
                _modeChangeInProgress = false;
            }
        }
        else
        {
            _elevation.ExitModifyMode();
            if (_startedForModify && ElevationService.IsProcessElevated())
            {
                _log.Auth("MainWindow", "Elevated Modify session ended; closing instead of retaining an elevated inspection shell.");
                MessageBox.Show(this,
                    "The elevated Modify session will now close. Reopen Windows Privacy Platform normally for Inspect mode.",
                    "Modify session ended", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
                return;
            }
            StatusText.Text = "Inspect mode is read-only.";
            Navigate(_currentNav);
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
            var lines = AtomicLocalFile.ReadAllLines(_appDataRoot, _prefsPath);
            if (lines.Length < 4)
                return;
            if (double.TryParse(lines[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var left) &&
                double.TryParse(lines[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var top) &&
                double.TryParse(lines[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
                double.TryParse(lines[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                var workArea = SystemParameters.WorkArea;
                var safeWidth = Math.Min(Math.Max(MinWidth, width), workArea.Width);
                var safeHeight = Math.Min(Math.Max(MinHeight, height), workArea.Height);
                Width = safeWidth;
                Height = safeHeight;
                Left = Math.Min(Math.Max(left, workArea.Left), workArea.Right - safeWidth);
                Top = Math.Min(Math.Max(top, workArea.Top), workArea.Bottom - safeHeight);
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
        _cts?.Cancel();
        try
        {
            var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            AtomicLocalFile.WriteAllLines(_appDataRoot, _prefsPath, new[]
            {
                bounds.Left.ToString("F0", CultureInfo.InvariantCulture),
                bounds.Top.ToString("F0", CultureInfo.InvariantCulture),
                bounds.Width.ToString("F0", CultureInfo.InvariantCulture),
                bounds.Height.ToString("F0", CultureInfo.InvariantCulture),
                _sidebarCollapsed.ToString()
            });
        }
        catch
        {
            // ignore preference save failures
        }
    }
}
