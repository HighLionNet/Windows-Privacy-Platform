using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Logging;

namespace WindowsPrivacyPlatform.App;

/// <summary>
/// Application lifecycle, startup-mode selection, and last-resort exception reporting.
/// </summary>
public partial class App : Application
{
    private readonly IAuditLogger _log = new AuditLogger();
    private int _showingExceptionDialog;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        base.OnStartup(e);

        var resumeModify = e.Args.Any(arg =>
            arg.Equals("--resume-modify", StringComparison.OrdinalIgnoreCase));
        var requestModify = resumeModify && ElevationService.IsProcessElevated();

        if (!requestModify)
        {
            var selector = new StartupModeDialog();
            if (selector.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            requestModify = selector.SelectedMode == StartupModeChoice.Modify;
        }

        var window = new MainWindow(requestModify);
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log.Error("App.Dispatcher", e.Exception.ToString());
        ShowExceptionDialog("Windows Privacy Platform recovered from an unexpected interface error. " +
                            "No additional configuration changes were started. Review the local logs before retrying.");
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _log.Error("AppDomain", exception?.ToString() ?? e.ExceptionObject?.ToString() ?? "Unknown fatal error");

        try
        {
            Dispatcher.BeginInvoke(() => ShowExceptionDialog(
                "Windows Privacy Platform encountered a fatal background error. " +
                "Any in-progress operation has been stopped; verify system state before making another change."));
        }
        catch
        {
            // The dispatcher may already be shutting down; logging above is the final fallback.
        }
    }

    private void ShowExceptionDialog(string message)
    {
        if (Interlocked.Exchange(ref _showingExceptionDialog, 1) != 0)
            return;

        try
        {
            MessageBox.Show(
                MainWindow,
                message,
                "Unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Interlocked.Exchange(ref _showingExceptionDialog, 0);
        }
    }
}
