using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.App.Views;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace WindowsPrivacyPlatform.App;

/// <summary>Application shell for catalog-backed inspection and controlled changes.</summary>
public partial class MainWindow : FluentWindow
{
    private readonly ScanService _scan = new();
    private readonly IAuditLogger _log;
    private readonly ElevationService _elevation;
    private readonly PolicyChangeService _changes;
    private readonly InventoryChangeService _inventoryChanges;
    private readonly ApplicationPreferencesStore _preferencesStore;
    private readonly ApplicationPreferences _preferences;
    private readonly string _appDataRoot;
    private readonly string _prefsPath;
    private readonly List<Button> _navButtons = [];
    private readonly Stack<string> _backHistory = [];
    private readonly Stack<string> _forwardHistory = [];
    private readonly Dictionary<string, CategoryFilterState> _categoryFilters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConflictFilterState _conflictFilter = new();
    private readonly KnowledgeFilterState _knowledgeFilter = new();
    private readonly ServiceFilterState _serviceFilter = new();
    private readonly Dictionary<string, ConflictGroup> _knownConflicts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConflictGroup> _resolvedConflicts = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _sessionTimer;
    private CancellationTokenSource? _cts;
    private string _currentNav = "home";
    private bool _sidebarCollapsed;
    private bool _modeChangeInProgress;
    private bool _reportedExpiredSession;

    public MainWindow(IAuditLogger log, ApplicationPreferencesStore preferencesStore,
        ApplicationPreferences preferences)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _elevation = new ElevationService(_log);
        _elevation.SetSessionLifetime(_preferences.AdminSessionMinutes);
        _changes = new PolicyChangeService(_elevation, _log);
        _inventoryChanges = new InventoryChangeService(_scan, _elevation, _log);

        _appDataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPrivacyPlatform");
        Directory.CreateDirectory(_appDataRoot);
        _prefsPath = Path.Combine(_appDataRoot, "window.prefs");

        InitializeComponent();
        var product = ProductInfoReader.Read();
        Title = product.Name;
        ProductNameText.Text = product.Name;
        HeaderVersionText.Text = "v" + product.Version;
        FooterVersionText.Text = "v" + product.Version;
        RestoreWindowPreferences();

        _sessionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _sessionTimer.Tick += SessionTimer_Tick;
        _sessionTimer.Start();

        Loaded += MainWindow_Loaded;
        _scan.StatusChanged += status => Dispatcher.Invoke(() => StatusText.Text = status);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CollectNavButtons();
        SyncSectionForTag(_currentNav);
        if (_sidebarCollapsed)
        {
            SidebarColumn.Width = new GridLength(0);
            NavPanel.Visibility = Visibility.Collapsed;
        }

        ShowWelcome();
        if (!InitializeSession()) return;

        if (_preferences.ScanOnLaunch)
            await RunScanAsync();
        else
            StatusText.Text = "Ready. Scan when you want to collect local evidence.";

        if (!App.StartupOptions.SuppressShortcutOffer)
            ShortcutOfferService.OfferIfNeeded(this);
    }

    private bool InitializeSession()
    {
        var startup = App.StartupOptions;
        if (startup.AuthorizedAdminRelaunch)
        {
            if (!_elevation.AuthorizeRelaunchedSession(startup.InitiatingSid))
            {
                MessageBox.Show(this, "The Administrator relaunch marker was not accepted. No write authority was granted.",
                    "Administrator authorization failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return false;
            }
            SetModeChrome(admin: true);
            return true;
        }

        if (startup.ViewOnlyRelaunch)
        {
            if (ElevationService.IsProcessElevated())
            {
                MessageBox.Show(this, "Windows did not drop the elevated token. The app will close instead of keeping an elevated View-only window.",
                    "View-only restart failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return false;
            }
            SetModeChrome(admin: false);
            return true;
        }

        while (true)
        {
            var dialog = new StartupModeDialog(_preferences.DefaultMode) { Owner = this };
            if (dialog.ShowDialog() != true)
            {
                Close();
                return false;
            }

            if (!dialog.AdminRequested)
            {
                if (ElevationService.IsProcessElevated())
                {
                    if (_elevation.TryRelaunchViewOnly())
                    {
                        Application.Current.Shutdown();
                        return false;
                    }
                    MessageBox.Show(this, "Windows could not restart the app without elevation. The app will close.",
                        "View-only restart failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return false;
                }
                SetModeChrome(admin: false);
                return true;
            }

            var result = _elevation.TryEnterAdminMode(this);
            if (result == AdminEntryResult.Authorized)
            {
                SetModeChrome(admin: true);
                return true;
            }
            if (result == AdminEntryResult.RelaunchStarted)
            {
                Application.Current.Shutdown();
                return false;
            }

            MessageBox.Show(this, _elevation.LastError, "Administrator not authorized",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CollectNavButtons()
    {
        _navButtons.Clear();
        void Walk(DependencyObject parent)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is Button { Tag: string } button) _navButtons.Add(button);
                Walk(child);
            }
        }
        Walk(this);
        HighlightForTag(_currentNav);
    }

    private void ShowWelcome()
    {
        SetContent(new TextBlock
        {
            Text = "Ready",
            Style = (Style)FindResource("SectionTitle"),
            Margin = new Thickness(4)
        });
        UpdateBreadcrumbs("Ready");
    }

    private async Task RunScanAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        ScanButton.IsEnabled = false;
        ScanButtonLabel.Text = "Scanning…";
        ScanProgress.Visibility = Visibility.Visible;
        CancelScanButton.Visibility = Visibility.Visible;
        try
        {
            await _scan.RunScanAsync(_cts.Token);
            TrackConflicts();
            UpdateChrome();
            Navigate(_currentNav);
        }
        finally
        {
            ScanButton.IsEnabled = true;
            ScanButtonLabel.Text = "Scan";
            ScanProgress.Visibility = Visibility.Collapsed;
            CancelScanButton.Visibility = Visibility.Collapsed;
        }
    }

    private void TrackConflicts()
    {
        var current = (_scan.Query?.GetConflictGroups() ?? []).ToDictionary(group => group.GroupId, StringComparer.OrdinalIgnoreCase);
        foreach (var previous in _knownConflicts.Values.Where(group => !current.ContainsKey(group.GroupId)))
            _resolvedConflicts[previous.GroupId] = previous;
        foreach (var active in current.Keys)
            _resolvedConflicts.Remove(active);
        _knownConflicts.Clear();
        foreach (var group in current.Values)
            _knownConflicts[group.GroupId] = group;
    }

    private void UpdateChrome()
    {
        if (_scan.Overview is null) return;
        var age = DateTime.UtcNow - _scan.Overview.LastScanUtc;
        var status = _scan.LastScanResult?.Status;
        var qualifier = status == ScanStatus.Partial ? " · partial evidence" :
            status == ScanStatus.CompletedWithWarnings ? " · warnings" :
            age > TimeSpan.FromMinutes(30) ? " · stale" : string.Empty;
        ScanTimeLabel.Text = $"Scanned {_scan.Overview.LastScanUtc:yyyy-MM-dd HH:mm} UTC{qualifier}";
        CatalogCountLabel.Text = $"Settings: {_scan.SettingsCatalog.Count}";
        ConflictCountLabel.Text = $"Conflicts: {_scan.Query?.GetConflictGroups().Count ?? 0}";

        var visibleDomains = _scan.SettingsCatalog.Select(item => item.ProductDomain).ToHashSet();
        foreach (var button in _navButtons)
        {
            if (button.Tag is not string tag || !tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase)) continue;
            button.Visibility = Enum.TryParse<ProductDomain>(tag[7..], out var domain) && visibleDomains.Contains(domain)
                ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void GoTo(string tag)
    {
        if (!string.Equals(tag, _currentNav, StringComparison.OrdinalIgnoreCase))
        {
            _backHistory.Push(_currentNav);
            _forwardHistory.Clear();
            _currentNav = tag;
        }
        UpdateHistoryButtons();
        HighlightForTag(tag);
        Navigate(tag);
    }

    private void Navigate(string tag)
    {
        ConfigureScrollFor(tag);
        if (!_scan.HasScan && tag is not ("about" or "settings"))
        {
            ShowWelcome();
            return;
        }

        if (tag == "home")
        {
            SetContent(new HomeView(_scan, OpenSettingsList, destination => GoTo("posture:" + destination), () => GoTo("conflicts")));
            UpdateBreadcrumbs("Dashboard");
        }
        else if (tag is "inventory" or "services" or "explore:windows-services")
        {
            SetContent(new ServicesView(_scan, _elevation, _inventoryChanges, RunScanAsync, this, _serviceFilter, otherServices: false));
            UpdateBreadcrumbs("Windows services", group: HubNavigation.DisplayName(HubSection.Explore));
        }
        else if (tag == "explore:other-services")
        {
            SetContent(new ServicesView(_scan, _elevation, _inventoryChanges, RunScanAsync, this, _serviceFilter, otherServices: true));
            UpdateBreadcrumbs("Other services", group: HubNavigation.DisplayName(HubSection.Explore));
        }
        else if (tag == "explore:windows-tasks")
        {
            SetContent(new ScheduledTasksView(_scan, _inventoryChanges, _elevation, RunScanAsync, this, otherTasks: false));
            UpdateBreadcrumbs("Windows tasks", group: HubNavigation.DisplayName(HubSection.Explore));
        }
        else if (tag == "explore:other-tasks")
        {
            SetContent(new ScheduledTasksView(_scan, _inventoryChanges, _elevation, RunScanAsync, this, otherTasks: true));
            UpdateBreadcrumbs("Other tasks", group: HubNavigation.DisplayName(HubSection.Explore));
        }
        else if (tag.StartsWith("explore:", StringComparison.OrdinalIgnoreCase))
        {
            var page = tag[8..];
            SetContent(new SystemInventoryView(_scan, OpenSetting, page));
            UpdateBreadcrumbs(page switch
            {
                "system-apps" => "System apps", "other-apps" => "Other apps",
                "firewall-rules" => "Firewall rules", _ => "Features & capabilities"
            }, group: HubNavigation.DisplayName(HubSection.Explore));
        }
        else if (tag == "network:dns")
        {
            SetContent(new DnsResolutionView(_scan, OpenSettingsList));
            UpdateBreadcrumbs("DNS & name resolution", group: HubNavigation.DisplayName(HubSection.Network));
        }
        else if (tag == "network:adapters")
        {
            SetContent(new DnsResolutionView(_scan, OpenSettingsList, adaptersOnly: true));
            UpdateBreadcrumbs("Adapters & LAN", group: HubNavigation.DisplayName(HubSection.Network));
        }
        else if (tag.StartsWith("conflicts", StringComparison.OrdinalIgnoreCase))
        {
            var focus = tag.StartsWith("conflicts:", StringComparison.OrdinalIgnoreCase) ? tag[10..] : null;
            SetContent(new ConflictsView(_scan, _resolvedConflicts.Values.ToList(), _elevation, _changes,
                RunScanAsync, this, OpenSetting, _conflictFilter, focus));
            UpdateBreadcrumbs("Conflicts");
        }
        else if (tag == "knowledge")
        {
            SetContent(new KnowledgeExplorerView(_scan, OpenSettingsList, OpenSetting, _knowledgeFilter));
            UpdateBreadcrumbs("Knowledge Explorer");
        }
        else if (tag == "settings")
        {
            SetContent(new ApplicationSettingsView(_preferences, _preferencesStore, _appDataRoot, PreferencesChanged, _log));
            UpdateBreadcrumbs("App Settings");
        }
        else if (tag == "about")
        {
            SetContent(new AboutView(_scan));
            UpdateBreadcrumbs("About");
        }
        else if (tag.StartsWith("search:", StringComparison.OrdinalIgnoreCase))
        {
            var query = Uri.UnescapeDataString(tag[7..]);
            SetContent(new SearchResultsView(_scan, query, OpenSettingsList, OpenSetting));
            UpdateBreadcrumbs("Search: " + query);
        }
        else if (tag.StartsWith("posture:", StringComparison.OrdinalIgnoreCase))
        {
            var destination = tag[8..];
            SetContent(new PostureFindingsView(_scan, destination, OpenSettingsList));
            UpdateBreadcrumbs(destination switch { "high" => "High attention", "review" => "Review", _ => "Protections" });
        }
        else if (tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase) &&
                 Enum.TryParse<ProductDomain>(tag[7..], out var domain))
        {
            SetContent(new DomainView(_scan, domain, OpenCategory, OpenSetting));
            UpdateBreadcrumbs(NavigationBuilder.HumanizeDomain(domain), group: DomainGroup(domain),
                domainName: NavigationBuilder.HumanizeDomain(domain), domainTag: tag);
        }
        else if (TryParseCategory(tag, out var categoryDomain, out var category))
        {
            var filterState = CategoryState(categoryDomain, category);
            SetContent(new CategoryView(_scan, categoryDomain, category, OpenSetting,
                groupId => GoTo("conflicts:" + groupId), _elevation, _changes,
                RunScanAsync, this, completeModifyOperation: CompleteModifyOperation, filterState: filterState));
            UpdateBreadcrumbs(category, group: DomainGroup(categoryDomain),
                domainName: NavigationBuilder.HumanizeDomain(categoryDomain), domainTag: $"domain:{categoryDomain}",
                categoryName: category, categoryTag: tag);
        }
        else if (tag.StartsWith("setting:", StringComparison.OrdinalIgnoreCase))
            RenderSetting(tag[8..]);
        else
            ShowWelcome();
    }

    private void OpenCategory(ProductDomain domain, string category) => GoTo($"category:{domain}:{category}");

    private void OpenSettingsList(SettingsListTarget target)
    {
        var tag = $"category:{target.Domain}:{target.Category}";
        if (!string.Equals(tag, _currentNav, StringComparison.OrdinalIgnoreCase))
        {
            _backHistory.Push(_currentNav);
            _forwardHistory.Clear();
            _currentNav = tag;
        }
        UpdateHistoryButtons();
        HighlightForTag(tag);
        ConfigureScrollFor(tag);
        SetContent(new CategoryView(_scan, target.Domain, target.Category, OpenSetting,
            groupId => GoTo("conflicts:" + groupId), _elevation,
            _changes, RunScanAsync, this, target.Filter, target.HighlightObjectId, CompleteModifyOperation,
            CategoryState(target.Domain, target.Category)));
        UpdateBreadcrumbs(target.Category, group: DomainGroup(target.Domain),
            domainName: NavigationBuilder.HumanizeDomain(target.Domain), domainTag: $"domain:{target.Domain}",
            categoryName: target.Category, categoryTag: tag);
    }

    private void OpenSetting(string objectId) => GoTo("setting:" + objectId);

    private CategoryFilterState CategoryState(ProductDomain domain, string category)
    {
        var key = domain + ":" + category;
        if (!_categoryFilters.TryGetValue(key, out var state))
            _categoryFilters[key] = state = new CategoryFilterState();
        return state;
    }

    private void RenderSetting(string objectId)
    {
        var item = _scan.Catalog.FirstOrDefault(candidate => candidate.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));
        var detail = item is null ? null : NavigationBuilder.BuildDetail(item, _scan.Query);
        if (item is null || detail is null) return;
        SetContent(new SettingDetailPage(detail, item, _scan.Query, _elevation, _changes, RunScanAsync, this,
            _scan.Overview?.WindowsVersion ?? string.Empty, _scan.Overview?.WindowsEdition ?? string.Empty,
            groupId => GoTo("conflicts:" + groupId)));
        if (item.Bucket == CatalogBucket.SystemInventory)
        {
            BreadcrumbPanel.Children.Clear();
            AddBreadcrumbLink("Dashboard", "home"); AddBreadcrumbSep();
            AddBreadcrumbText(HubNavigation.DisplayName(HubSection.Explore)); AddBreadcrumbSep();
            AddBreadcrumbLink("Explore category", InventoryTag(item));
            AddBreadcrumbSep(); AddBreadcrumbText(detail.Title);
            return;
        }
        var category = string.IsNullOrWhiteSpace(item.SubCategory) ? item.ProductDomain.ToString() : item.SubCategory!;
        UpdateBreadcrumbs(detail.Title, group: DomainGroup(item.ProductDomain),
            domainName: NavigationBuilder.HumanizeDomain(item.ProductDomain), domainTag: $"domain:{item.ProductDomain}",
            categoryName: category, categoryTag: $"category:{item.ProductDomain}:{category}");
    }

    private static bool TryParseCategory(string tag, out ProductDomain domain, out string category)
    {
        domain = default; category = string.Empty;
        if (!tag.StartsWith("category:", StringComparison.OrdinalIgnoreCase)) return false;
        var rest = tag[9..];
        var separator = rest.IndexOf(':');
        if (separator <= 0 || !Enum.TryParse(rest[..separator], out domain)) return false;
        category = rest[(separator + 1)..];
        return category.Length > 0;
    }

    private void ConfigureScrollFor(string tag)
    {
        var listOwnsScroll = tag is "services" or "inventory" or "knowledge" ||
                             tag.StartsWith("explore:", StringComparison.OrdinalIgnoreCase);
        ContentScroll.VerticalScrollBarVisibility = listOwnsScroll ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        BackToTopButton.Visibility = Visibility.Collapsed;
    }

    private void SetContent(object content)
    {
        ContentHost.Content = content;
        ContentScroll.ScrollToTop();
        ContentHost.Opacity = 0;
        ContentHost.RenderTransform = new TranslateTransform(0, 4);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        ContentHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(142)) { EasingFunction = ease });
        ((TranslateTransform)ContentHost.RenderTransform).BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(4, 0, TimeSpan.FromMilliseconds(162)) { EasingFunction = ease });
    }

    private void SetModeChrome(bool admin)
    {
        AuditContext.Mode = admin ? SessionPresentation.Admin : SessionPresentation.ViewOnly;
        _modeChangeInProgress = true;
        ModeCombo.SelectedIndex = admin ? 1 : 0;
        _modeChangeInProgress = false;
        StatusText.Text = admin ? "Administrator mode" : "View-only mode";
        _reportedExpiredSession = false;
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_modeChangeInProgress || ModeCombo is null || StatusText is null) return;
        if (ModeCombo.SelectedIndex == 1)
        {
            _modeChangeInProgress = true;
            try
            {
                var result = _elevation.TryEnterAdminMode(this);
                if (result == AdminEntryResult.Authorized)
                {
                    SetModeChrome(admin: true);
                    Navigate(_currentNav);
                }
                else if (result == AdminEntryResult.RelaunchStarted)
                    Application.Current.Shutdown();
                else
                {
                    ModeCombo.SelectedIndex = 0;
                    StatusText.Text = _elevation.LastError;
                }
            }
            finally { _modeChangeInProgress = false; }
            return;
        }

        _modeChangeInProgress = true;
        try
        {
            if (ElevationService.IsProcessElevated())
            {
                if (!_elevation.ConfirmAdminModeExit(this) || !_elevation.TryRelaunchViewOnly())
                {
                    ModeCombo.SelectedIndex = 1;
                    StatusText.Text = string.IsNullOrWhiteSpace(_elevation.LastError)
                        ? "View-only restart was not completed." : _elevation.LastError;
                    return;
                }
                _elevation.ExitAdminMode();
                Application.Current.Shutdown();
                return;
            }
            _elevation.ExitAdminMode();
            SetModeChrome(admin: false);
            Navigate(_currentNav);
        }
        finally { _modeChangeInProgress = false; }
    }

    private void ModeCombo_DropDownOpened(object sender, EventArgs e)
    {
        if (ModeCombo.SelectedIndex != 1 || _elevation.IsAdminAuthorized) return;
        var result = _elevation.TryEnterAdminMode(this);
        if (result == AdminEntryResult.Authorized)
        {
            StatusText.Text = "Administrator authorization renewed.";
            _reportedExpiredSession = false;
            Navigate(_currentNav);
        }
        else if (result == AdminEntryResult.RelaunchStarted) Application.Current.Shutdown();
        else StatusText.Text = _elevation.LastError;
    }

    private void ModeSelectorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ModeCombo.Focus();
        ModeCombo.IsDropDownOpen = true;
        e.Handled = true;
    }

    private void SessionTimer_Tick(object? sender, EventArgs e)
    {
        if (ModeCombo.SelectedIndex != 1 || _elevation.IsAdminAuthorized || _reportedExpiredSession) return;
        _reportedExpiredSession = true;
        StatusText.Text = "Administrator authorization expired. Open Mode before the next protected action.";
        Navigate(_currentNav);
    }

    private void CompleteModifyOperation()
    {
        if (!SessionPresentation.KeepProcessOpenAfterApply)
            throw new InvalidOperationException("The product contract requires Apply to keep the session open.");
        StatusText.Text = "Apply complete. Evidence refreshed; Administrator mode remains open.";
        _log.Change("MainWindow", "Apply completed without ending the Administrator session.");
    }

    private void PreferencesChanged()
    {
        _elevation.SetSessionLifetime(_preferences.AdminSessionMinutes);
        Navigate(_currentNav);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_backHistory.Count == 0) return;
        _forwardHistory.Push(_currentNav);
        _currentNav = _backHistory.Pop();
        UpdateHistoryButtons(); HighlightForTag(_currentNav); Navigate(_currentNav);
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (_forwardHistory.Count == 0) return;
        _backHistory.Push(_currentNav);
        _currentNav = _forwardHistory.Pop();
        UpdateHistoryButtons(); HighlightForTag(_currentNav); Navigate(_currentNav);
    }

    private void UpdateHistoryButtons()
    {
        BackButton.IsEnabled = _backHistory.Count > 0;
        ForwardButton.IsEnabled = _forwardHistory.Count > 0;
    }

    private void HighlightForTag(string tag)
    {
        SyncSectionForTag(tag);
        var highlight = tag;
        if (TryParseCategory(tag, out var domain, out _)) highlight = "domain:" + domain;
        else if (tag.StartsWith("setting:", StringComparison.OrdinalIgnoreCase))
        {
            var item = _scan.Catalog.FirstOrDefault(candidate =>
                candidate.ObjectId.Equals(tag[8..], StringComparison.OrdinalIgnoreCase));
            if (item is null) return;
            highlight = item.Bucket == CatalogBucket.SystemInventory ? InventoryTag(item) : "domain:" + item.ProductDomain;
        }
        else if (tag.StartsWith("conflicts:", StringComparison.OrdinalIgnoreCase)) highlight = "conflicts";
        else if (tag.StartsWith("search:", StringComparison.OrdinalIgnoreCase) || tag.StartsWith("posture:", StringComparison.OrdinalIgnoreCase)) highlight = "home";
        var selected = _navButtons.FirstOrDefault(button => string.Equals(button.Tag as string, highlight, StringComparison.OrdinalIgnoreCase));
        foreach (var button in _navButtons)
            button.Style = (Style)FindResource(ReferenceEquals(button, selected) ? "SidebarButtonSelected" : "SidebarButton");
        selected?.BringIntoView();
    }

    private void RunSearch()
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        if (query.Length == 0 || _scan.Query is null) return;
        if (query.Length > 200) query = query[..200];
        GoTo("search:" + Uri.EscapeDataString(query));
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { SearchBox.Clear(); Keyboard.ClearFocus(); e.Handled = true; }
        else if (e.Key == Key.Enter) { RunSearch(); e.Handled = true; }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5 || (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control))
        { e.Handled = true; _ = RunScanAsync(); }
        else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        { e.Handled = true; SearchBox.Focus(); SearchBox.SelectAll(); }
        else if (e.Key == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt)
        { e.Handled = true; BackButton_Click(this, new RoutedEventArgs()); }
        else if (e.Key == Key.Right && Keyboard.Modifiers == ModifierKeys.Alt)
        { e.Handled = true; ForwardButton_Click(this, new RoutedEventArgs()); }
        else if (e.Key == Key.Escape && SearchBox.IsKeyboardFocusWithin)
        { SearchBox.Clear(); Keyboard.ClearFocus(); e.Handled = true; }
    }

    private static string DomainGroup(ProductDomain domain) => HubNavigation.DisplayName(HubNavigation.For(domain));

    private static string InventoryTag(ManagedObject item) => item.FeatureCategory switch
    {
        FeatureCategory.WindowsService => "explore:windows-services",
        FeatureCategory.ScheduledTask => "explore:windows-tasks",
        FeatureCategory.AppxPackage or FeatureCategory.ProvisionedPackage =>
            item.ObjectName.StartsWith("Microsoft.Windows", StringComparison.OrdinalIgnoreCase) ||
            item.ObjectName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)
                ? "explore:system-apps" : "explore:other-apps",
        FeatureCategory.FirewallRule => "explore:firewall-rules",
        _ => "explore:features"
    };

    private void SyncSectionForTag(string tag)
    {
        HubSection? section = null;
        if (tag.Equals("inventory", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("services", StringComparison.OrdinalIgnoreCase) ||
            tag.StartsWith("explore:", StringComparison.OrdinalIgnoreCase))
            section = HubSection.Explore;
        else if (tag.StartsWith("network:", StringComparison.OrdinalIgnoreCase))
            section = HubSection.Network;
        else if (TryParseCategory(tag, out var categoryDomain, out _))
            section = HubNavigation.For(categoryDomain);
        else if (tag.StartsWith("domain:", StringComparison.OrdinalIgnoreCase) &&
                 Enum.TryParse<ProductDomain>(tag[7..], out var domain))
            section = HubNavigation.For(domain);
        else if (tag.StartsWith("setting:", StringComparison.OrdinalIgnoreCase))
        {
            var item = _scan.Catalog.FirstOrDefault(candidate =>
                candidate.ObjectId.Equals(tag[8..], StringComparison.OrdinalIgnoreCase));
            if (item is not null)
                section = HubNavigation.For(item);
        }

        if (section.HasValue)
            SetActiveSection(section.Value, navigate: false);
    }

    private void SetActiveSection(HubSection section, bool navigate)
    {
        PrivacyNavPanel.Visibility = section == HubSection.Privacy ? Visibility.Visible : Visibility.Collapsed;
        SecurityNavPanel.Visibility = section == HubSection.Security ? Visibility.Visible : Visibility.Collapsed;
        NetworkNavPanel.Visibility = section == HubSection.Network ? Visibility.Visible : Visibility.Collapsed;
        ExploreNavPanel.Visibility = section == HubSection.Explore ? Visibility.Visible : Visibility.Collapsed;

        var accent = (Brush)FindResource(HubNavigation.AccentBrushKey(section));
        ActiveSectionTitle.Text = HubNavigation.DisplayName(section);
        ActiveSectionTitle.Foreground = accent;
        RailAccentBorder.BorderBrush = accent;

        var buttons = new Dictionary<HubSection, Button>
        {
            [HubSection.Privacy] = PrivacySectionButton,
            [HubSection.Security] = SecuritySectionButton,
            [HubSection.Network] = NetworkSectionButton,
            [HubSection.Explore] = ExploreSectionButton
        };
        foreach (var pair in buttons)
        {
            pair.Value.Style = (Style)FindResource(pair.Key == section ? "SectionToggleSelected" : "SectionToggle");
            pair.Value.ClearValue(Control.BorderBrushProperty);
        }
        buttons[section].BorderBrush = accent;

        if (!navigate)
            return;
        GoTo(section switch
        {
            HubSection.Privacy => "domain:ConsentStore",
            HubSection.Security => "domain:Defender",
            HubSection.Network => "network:dns",
            HubSection.Explore => "explore:windows-services",
            _ => "home"
        });
    }

    private void UpdateBreadcrumbs(string current, string? group = null, string? domainName = null,
        string? domainTag = null, string? categoryName = null, string? categoryTag = null)
    {
        BreadcrumbPanel.Children.Clear();
        AddBreadcrumbLink("Dashboard", "home");
        if (!string.IsNullOrEmpty(group)) { AddBreadcrumbSep(); AddBreadcrumbText(group); }
        if (!string.IsNullOrEmpty(domainName) && !string.IsNullOrEmpty(domainTag))
        { AddBreadcrumbSep(); AddBreadcrumbLink(domainName, domainTag); }
        if (!string.IsNullOrEmpty(categoryName) && !string.IsNullOrEmpty(categoryTag))
        {
            AddBreadcrumbSep();
            if (current == categoryName) AddBreadcrumbText(categoryName); else AddBreadcrumbLink(categoryName, categoryTag);
        }
        var root = current is "Dashboard" or "Ready";
        if (!root && current != domainName && current != categoryName) { AddBreadcrumbSep(); AddBreadcrumbText(current); }
    }

    private void AddBreadcrumbSep() => BreadcrumbPanel.Children.Add(new TextBlock
    { Text = " › ", Style = (Style)FindResource("BreadcrumbText"), Margin = new Thickness(2, 0, 2, 0) });

    private void AddBreadcrumbText(string text) => BreadcrumbPanel.Children.Add(new TextBlock
    { Text = text, Style = (Style)FindResource("BreadcrumbText"), FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("BrushTextPrimary") });

    private void AddBreadcrumbLink(string text, string tag)
    {
        var button = new Button { Content = text, Style = (Style)FindResource("BreadcrumbLink"), Tag = tag, Cursor = Cursors.Hand };
        button.Click += (_, _) => GoTo(tag);
        BreadcrumbPanel.Children.Add(button);
    }

    private void RestoreWindowPreferences()
    {
        var hasPreferences = File.Exists(_prefsPath);
        try
        {
            var lines = AtomicLocalFile.ReadAllLines(_appDataRoot, _prefsPath);
            if (_preferences.RememberWindowPosition && lines.Length >= 4 &&
                double.TryParse(lines[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var left) &&
                double.TryParse(lines[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var top) &&
                double.TryParse(lines[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
                double.TryParse(lines[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            {
                var area = SystemParameters.WorkArea;
                Width = Math.Min(Math.Max(MinWidth, width), area.Width);
                Height = Math.Min(Math.Max(MinHeight, height), area.Height);
                Left = Math.Min(Math.Max(left, area.Left), area.Right - Width);
                Top = Math.Min(Math.Max(top, area.Top), area.Bottom - Height);
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
            if (lines.Length >= 5 && bool.TryParse(lines[4], out var collapsed)) _sidebarCollapsed = collapsed;
            if (lines.Length >= 7 && !string.IsNullOrWhiteSpace(lines[6])) _currentNav = lines[6];
            if (_currentNav is "services" or "inventory") _currentNav = "explore:windows-services";
            if (_preferences.RememberWindowPosition && lines.Length >= 6 && Enum.TryParse<WindowState>(lines[5], out var state))
                WindowState = state == WindowState.Minimized ? WindowState.Normal : state;
            else if (_preferences.StartMaximized) WindowState = WindowState.Maximized;
        }
        catch { if (_preferences.StartMaximized) WindowState = WindowState.Maximized; }
        if (!hasPreferences && _preferences.StartMaximized) WindowState = WindowState.Maximized;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _cts?.Cancel();
        _sessionTimer.Stop();
        try
        {
            var bounds = _preferences.RememberWindowPosition
                ? (WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds)
                : new Rect(0, 0, 1320, 860);
            AtomicLocalFile.WriteAllLines(_appDataRoot, _prefsPath,
            [
                bounds.Left.ToString("F0", CultureInfo.InvariantCulture), bounds.Top.ToString("F0", CultureInfo.InvariantCulture),
                bounds.Width.ToString("F0", CultureInfo.InvariantCulture), bounds.Height.ToString("F0", CultureInfo.InvariantCulture),
                _sidebarCollapsed.ToString(), WindowState.ToString(), _currentNav
            ]);
        }
        catch { }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        // FluentWindow owns native chrome; make process shutdown explicit even if its
        // close path changes the HWND before WPF's OnMainWindowClose bookkeeping runs.
        if (Application.Current is not { } application || application.Dispatcher.HasShutdownStarted) return;
        application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
        {
            if (!application.Dispatcher.HasShutdownStarted)
                application.Shutdown();
        }));
    }

    private void ContentScroll_ScrollChanged(object sender, ScrollChangedEventArgs e) =>
        BackToTopButton.Visibility = ContentScroll.VerticalOffset > 450 ? Visibility.Visible : Visibility.Collapsed;
    private void BackToTopButton_Click(object sender, RoutedEventArgs e) => ContentScroll.ScrollToTop();
    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    { _sidebarCollapsed = !_sidebarCollapsed; SidebarColumn.Width = _sidebarCollapsed ? new GridLength(0) : new GridLength(252); NavPanel.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible; }
    private void PrivacySection_Click(object sender, RoutedEventArgs e) => SetActiveSection(HubSection.Privacy, navigate: true);
    private void SecuritySection_Click(object sender, RoutedEventArgs e) => SetActiveSection(HubSection.Security, navigate: true);
    private void NetworkSection_Click(object sender, RoutedEventArgs e) => SetActiveSection(HubSection.Network, navigate: true);
    private void ExploreSection_Click(object sender, RoutedEventArgs e) => SetActiveSection(HubSection.Explore, navigate: true);
    private void Nav_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string tag }) GoTo(tag); }
    private void NavigateFromMenu(string tag) => GoTo(tag);
    private void MenuHome_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("home");
    private void MenuInventory_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("explore:features");
    private void MenuServices_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("explore:windows-services");
    private void MenuConflicts_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("conflicts");
    private void MenuKnowledge_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("knowledge");
    private void MenuSettings_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("settings");
    private void SettingsButton_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("settings");
    private void MenuAbout_Click(object sender, RoutedEventArgs e) => NavigateFromMenu("about");
    private void MenuSearch_Click(object sender, RoutedEventArgs e) { SearchBox.Focus(); SearchBox.SelectAll(); }
    private async void MenuScan_Click(object sender, RoutedEventArgs e) => await RunScanAsync();
    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await RunScanAsync();
    private void CancelScanButton_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();
    private void MenuCopySearch_Click(object sender, RoutedEventArgs e)
    {
        var text = SearchBox.Text ?? string.Empty;
        if (text.Length == 0)
            text = Title ?? string.Empty;
        if (text.Length == 0)
            return;
        try { Clipboard.SetText(text); StatusText.Text = "Copied."; }
        catch { StatusText.Text = "Clipboard was not available."; }
    }
}
