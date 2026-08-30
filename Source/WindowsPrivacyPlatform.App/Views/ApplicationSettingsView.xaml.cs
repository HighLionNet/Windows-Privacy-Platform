using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class ApplicationSettingsView : UserControl
{
    private readonly ApplicationPreferences _preferences;
    private readonly ApplicationPreferencesStore _store;
    private readonly string _dataRoot;
    private readonly Action _preferencesChanged;
    private readonly IAuditLogger _log;
    private bool _loading = true;

    public ApplicationSettingsView(ApplicationPreferences preferences, ApplicationPreferencesStore store,
        string dataRoot, Action preferencesChanged, IAuditLogger log)
    {
        InitializeComponent();
        _preferences = preferences;
        _store = store;
        _dataRoot = dataRoot;
        _preferencesChanged = preferencesChanged;
        _log = log;

        foreach (var theme in Enum.GetValues<AppTheme>())
            ThemeBox.Items.Add(new ComboBoxItem { Content = ThemeManager.DisplayName(theme), Tag = theme });
        SelectTag(DefaultModeBox, preferences.DefaultMode.ToString());
        SelectTag(SessionLifetimeBox, preferences.AdminSessionMinutes.ToString());
        ThemeBox.SelectedItem = ThemeBox.Items.OfType<ComboBoxItem>().First(item => Equals(item.Tag, preferences.Theme));
        StartMaximizedBox.IsChecked = preferences.StartMaximized;
        RememberPositionBox.IsChecked = preferences.RememberWindowPosition;
        ScanOnLaunchBox.IsChecked = preferences.ScanOnLaunch;
        PopulateIntegrity();
        PopulateSessions();
        _loading = false;
    }

    private void PreferenceChanged(object sender, RoutedEventArgs e) => Save();
    private void PreferenceChanged(object sender, SelectionChangedEventArgs e) => Save();

    private void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThemeBox.SelectedItem is not ComboBoxItem { Tag: AppTheme theme }) return;
        _preferences.Theme = theme;
        ThemeManager.Apply(theme);
        Save();
    }

    private void Save()
    {
        if (_loading) return;
        if (DefaultModeBox.SelectedItem is ComboBoxItem { Tag: string mode } && Enum.TryParse(mode, out DefaultModePreference preference))
            _preferences.DefaultMode = preference;
        if (SessionLifetimeBox.SelectedItem is ComboBoxItem { Tag: string minutes } && int.TryParse(minutes, out var parsed))
            _preferences.AdminSessionMinutes = parsed;
        _preferences.StartMaximized = StartMaximizedBox.IsChecked == true;
        _preferences.RememberWindowPosition = RememberPositionBox.IsChecked == true;
        _preferences.ScanOnLaunch = ScanOnLaunchBox.IsChecked == true;
        _store.Save(_preferences);
        _preferencesChanged();
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_dataRoot);
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                UseShellExecute = false,
                ArgumentList = { _dataRoot }
            });
        }
        catch
        {
            MessageBox.Show(Window.GetWindow(this), "Windows could not open the local data folder.",
                "Folder unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PopulateIntegrity()
    {
        var product = ProductInfoReader.Read();
        var path = Environment.ProcessPath ?? string.Empty;
        VersionText.Text = product.Version;
        PathText.Text = path;
        CatalogHashText.Text = ManagedObjectCatalog.AuthorizationHash;
        HashText.Text = ComputeHash(path);
        SigningText.Text = SigningState(path);
        IntegrityStatusText.Text = BinaryIntegrityGuard.Status;
    }

    private void VerifyBinary_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var stepUp = new HighImpactStepUpService(_log);
        if (!stepUp.TryAuthorizeBinaryVerification(owner)) return;
        var accepted = BinaryIntegrityGuard.AcceptCurrent(_dataRoot, _log);
        IntegrityStatusText.Text = BinaryIntegrityGuard.Status;
        MessageBox.Show(owner, accepted ? "The current executable hash was recorded for later comparison." : "The executable hash could not be recorded.",
            accepted ? "Hash recorded" : "Recording failed", MessageBoxButton.OK,
            accepted ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void PopulateSessions()
    {
        var logRoot = Path.Combine(_dataRoot, "Logs");
        SessionList.ItemsSource = AuditSessionHistory.ReadRecent(logRoot).Select(session => new
        {
            session.User,
            session.Mode,
            Time = $"{session.StartUtc:yyyy-MM-dd HH:mm}–{session.EndUtc:HH:mm} UTC",
            Changes = $"{session.ChangesApplied} changes"
        }).ToList();
    }

    private static string ComputeHash(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch { return "Unavailable"; }
    }

    private static string SigningState(string path)
    {
        try
        {
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
            return string.IsNullOrWhiteSpace(certificate.Subject) ? "Signed" : "Signed — " + certificate.Subject;
        }
        catch { return "Unsigned community build"; }
    }

    private static void SelectTag(ComboBox box, string tag) =>
        box.SelectedItem = box.Items.OfType<ComboBoxItem>().FirstOrDefault(item =>
            string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase)) ?? box.Items[0];
}
