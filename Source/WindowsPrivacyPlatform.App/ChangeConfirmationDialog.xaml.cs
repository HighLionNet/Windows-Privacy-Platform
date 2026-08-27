using System.Windows;

namespace WindowsPrivacyPlatform.App;

public sealed record ChangeConfirmationItem(string Name, string Current, string Intended);

public partial class ChangeConfirmationDialog : Window
{
    public ChangeConfirmationDialog(IReadOnlyList<ChangeConfirmationItem> changes)
    {
        InitializeComponent();
        Title = Models.ProductInfoReader.Read().Name + " — Confirm changes";
        TitleText.Text = $"Review {changes.Count} change{(changes.Count == 1 ? string.Empty : "s")}";
        ChangesList.ItemsSource = changes;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
