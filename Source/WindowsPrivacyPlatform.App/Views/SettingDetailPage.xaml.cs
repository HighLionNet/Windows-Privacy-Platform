using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SettingDetailPage : UserControl
{
    private readonly NativeToolLink? _nativeTool;

    public SettingDetailPage(SettingDetailView detail, Action<string> openSetting)
    {
        InitializeComponent();
        _nativeTool = detail.NativeTool;

        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.Bucket == CatalogBucket.SystemInventory
            ? "System Inventory · " + detail.DomainPath
            : "Settings · " + detail.DomainPath;

        ObservedText.Text = detail.CurrentStateDisplay ?? "Unknown";
        EffectiveText.Text = detail.EffectiveValueDisplay ?? "Unknown";
        if (detail.HasConflict)
            EffectiveText.Foreground = (Brush)FindResource("BrushConflict");
        SourceText.Text = detail.EffectiveSourceDisplay ?? "Unknown";

        SummaryText.Text = detail.Narrative.Summary;
        MechanicsText.Text = detail.Narrative.Mechanics;
        WhyItMattersText.Text = detail.Narrative.WhyItMatters;
        DailyImpactText.Text = detail.Narrative.ConsumerImpact;
        TechnicalLocationText.Text = detail.TechnicalLocation;
        WhenIgnoredText.Text = detail.Narrative.WhenIgnored;
        GuidanceText.Text = detail.Narrative.DecisionGuidance;
        SideEffectsText.Text = detail.Narrative.SideEffects;
        MisconceptionText.Text = detail.Narrative.CommonMisconception;
        MisconceptionPanel.Visibility = string.IsNullOrWhiteSpace(MisconceptionText.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;

        AccessBadgeText.Text = detail.IsWritable ? "CHANGE AVAILABLE" : "VIEW ONLY";
        AccessBadge.Style = (Style)FindResource(detail.IsWritable ? "BadgeSuccess" : "BadgeUnknown");

        if (!detail.IsWritable)
        {
            ViewOnlyPanel.Visibility = Visibility.Visible;
            ExclusionText.Text = detail.ExclusionReasonText;
            if (_nativeTool is { IsComplete: true })
            {
                NativeToolButton.Content = _nativeTool.Label;
                NativeToolButton.Visibility = Visibility.Visible;
            }
        }

        if (detail.Applicability != ApplicabilityState.Applicable)
        {
            ApplicabilityBadge.Visibility = Visibility.Visible;
            ApplicabilityPanel.Visibility = Visibility.Visible;
            ApplicabilityBadgeText.Text = CatalogPolicy.ApplicabilityBadgeText(detail.Applicability);
            ApplicabilityText.Text = detail.ApplicabilityReason;
        }
    }

    private void NativeToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_nativeTool is not { IsComplete: true })
            return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _nativeTool.Executable,
                Arguments = _nativeTool.Arguments,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                Window.GetWindow(this),
                "Windows could not open the native management tool. " + ex.Message,
                "Native tool unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
