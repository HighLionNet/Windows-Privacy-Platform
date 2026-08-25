using System.Windows;

namespace WindowsPrivacyPlatform.App;

public enum StartupModeChoice
{
    Inspect,
    Modify
}

public partial class StartupModeDialog : Window
{
    public StartupModeChoice SelectedMode { get; private set; } = StartupModeChoice.Inspect;

    public StartupModeDialog()
    {
        InitializeComponent();
    }

    private void Inspect_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = StartupModeChoice.Inspect;
        DialogResult = true;
    }

    private void Modify_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = StartupModeChoice.Modify;
        DialogResult = true;
    }
}
