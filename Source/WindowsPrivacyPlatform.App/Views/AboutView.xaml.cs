using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WindowsPrivacyPlatform.App.Services;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class AboutView : UserControl
{
    private readonly string _repositoryUrl;

    public AboutView(ScanService scan)
    {
        InitializeComponent();
        var product = ProductInfoReader.Read();
        _repositoryUrl = product.RepositoryUrl;

        NameText.Text = product.Name;
        VersionText.Text = product.Version;
        BuildText.Text = product.BuildIdentifier;
        CompanyText.Text = product.Company;
        CopyrightText.Text = product.Copyright;
        PathText.Text = Environment.ProcessPath ?? "Unavailable";
        HashText.Text = BinaryIntegrityGuard.CurrentHash;
        SigningText.Text = BinaryIntegrityGuard.SignatureStatus;
        OsText.Text = scan.Overview is null
            ? "Not detected"
            : $"{scan.Overview.WindowsEdition} · {scan.Overview.WindowsVersion} · build {scan.Overview.BuildNumber}";
        RepositoryButton.Content = string.IsNullOrWhiteSpace(_repositoryUrl) ? "Repository metadata unavailable" : _repositoryUrl;
        RepositoryButton.IsEnabled = !string.IsNullOrWhiteSpace(_repositoryUrl);
    }

    private void RepositoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_repositoryUrl))
            return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _repositoryUrl, UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show(Window.GetWindow(this), "The repository link could not be opened by Windows.",
                "Link unavailable", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
