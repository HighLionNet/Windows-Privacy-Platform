using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.App.Views;

public partial class SettingDetailPage : UserControl
{
    public SettingDetailPage(SettingDetailView detail, Action<string> openSetting)
    {
        InitializeComponent();
        TitleText.Text = detail.Title;
        DomainPathText.Text = detail.Bucket == CatalogBucket.SystemInventory
            ? "System Explorer · observed component"
            : detail.DomainPath;
        ObservedText.Text = detail.CurrentStateDisplay ?? "Unknown";
        EffectiveText.Text = detail.EffectiveValueDisplay ?? "Unknown";
        SourceText.Text = detail.EffectiveSourceDisplay ?? "Unknown";
        if (detail.HasConflict) EffectiveText.Foreground = (Brush)FindResource("BrushConflict");

        SummaryText.Text = string.IsNullOrWhiteSpace(detail.Narrative.Summary)
            ? detail.Explanation.WhatIsIt
            : detail.Narrative.Summary;
        TechnicalLocationText.Text = TechnicalLocationFormatter.DirectPath(detail.TechnicalLocation);

        if (detail.Bucket == CatalogBucket.SystemInventory)
        {
            AccessBadgeText.Text = "READ ONLY";
            AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AccessBadgeText.Text = "EDITABLE";
            OptionsList.ItemsSource = detail.Options;
        }

        if (detail.Applicability != ApplicabilityState.Applicable)
        {
            ApplicabilityPanel.Visibility = Visibility.Visible;
            ApplicabilityText.Text = detail.ApplicabilityReason;
        }
    }
}
