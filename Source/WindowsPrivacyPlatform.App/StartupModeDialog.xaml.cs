using System.Windows;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App;

public partial class StartupModeDialog : Window
{
    public bool AdminRequested { get; private set; }

    public StartupModeDialog(DefaultModePreference preference)
    {
        InitializeComponent();
        Title = ProductInfoReader.Read().Name + " — Session mode";
        Loaded += (_, _) =>
        {
            if (preference == DefaultModePreference.ViewOnly) ViewOnlyButton.Focus();
            else AdminButton.Focus();
        };
    }

    private void ViewOnly_Click(object sender, RoutedEventArgs e)
    { AdminRequested = false; DialogResult = true; }

    private void Admin_Click(object sender, RoutedEventArgs e)
    { AdminRequested = true; DialogResult = true; }
}
