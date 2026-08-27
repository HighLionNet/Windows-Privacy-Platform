using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Core;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App;

public partial class App : Application
{
    private Mutex? _singleInstance;

    public static StartupArguments StartupOptions { get; private set; } =
        new(false, null, false, false);

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (!CommandLinePolicy.TryParse(e.Args, out var startup, out var argumentError))
        {
            MessageBox.Show(argumentError + " The app will close.", "Windows Privacy Platform",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
        StartupOptions = startup;

        var relaunch = startup.AuthorizedAdminRelaunch || startup.ViewOnlyRelaunch;
        if (!relaunch && SingleInstanceCoordinator.HasExistingProcess())
        {
            SingleInstanceCoordinator.FocusExistingWindow();
            Shutdown();
            return;
        }

        var sid = CurrentSid().Replace('-', '_');
        _singleInstance = new Mutex(false, "Local\\HighLionNet.WindowsPrivacyPlatform." + sid);
        var first = false;
        try { first = _singleInstance.WaitOne(relaunch ? TimeSpan.FromSeconds(10) : TimeSpan.Zero); }
        catch (AbandonedMutexException) { first = true; }
        if (!first)
        {
            SingleInstanceCoordinator.FocusExistingWindow();
            Shutdown();
            return;
        }

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsPrivacyPlatform");
        Directory.CreateDirectory(appData);
        LocalDataSecurity.EnsurePrivateAcl(appData);
        var log = new AuditLogger();
        ProcessHardening.Apply(log);

        var preferencesStore = new ApplicationPreferencesStore(appData);
        var preferences = preferencesStore.Load();
        ThemeManager.Apply(preferences.Theme);

        base.OnStartup(e);
        var window = new MainWindow(log, preferencesStore, preferences);
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _singleInstance?.ReleaseMutex(); } catch { }
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static string CurrentSid()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? Environment.UserName;
        }
        catch
        {
            return Environment.UserName;
        }
    }
}

internal static class SingleInstanceCoordinator
{
    public static bool HasExistingProcess()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            return Process.GetProcessesByName(current.ProcessName).Any(process => process.Id != current.Id);
        }
        catch { return false; }
    }

    public static void FocusExistingWindow()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            var existing = Process.GetProcessesByName(current.ProcessName)
                .FirstOrDefault(process => process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero);
            if (existing is null) return;
            ShowWindow(existing.MainWindowHandle, 9);
            SetForegroundWindow(existing.MainWindowHandle);
        }
        catch { }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);
}
