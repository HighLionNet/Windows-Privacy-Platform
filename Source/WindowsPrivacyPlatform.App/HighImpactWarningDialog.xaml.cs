using System.Windows;

namespace WindowsPrivacyPlatform.App;

public partial class HighImpactWarningDialog : Window
{
    public HighImpactWarningDialog(string changes, string risks)
    {
        InitializeComponent();
        ChangeText.Text = changes;
        RiskText.Text = risks;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    private void Continue_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
}
