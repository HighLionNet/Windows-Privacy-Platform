using System.Windows;

namespace WindowsPrivacyPlatform.App;

public partial class StartupModeDialog : Window
{
    public bool ModifyRequested { get; private set; }

    public StartupModeDialog()
    {
        InitializeComponent();
        Title = Models.ProductInfoReader.Read().Name + " — Session mode";
    }

    private void Inspect_Click(object sender, RoutedEventArgs e)
    {
        ModifyRequested = false;
        DialogResult = true;
    }

    private void Modify_Click(object sender, RoutedEventArgs e)
    {
        ModifyRequested = true;
        DialogResult = true;
    }
}
