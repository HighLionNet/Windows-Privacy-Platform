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
        SourceText.Text = TechnicalLocationFormatter.DirectPath(detail.TechnicalLocation);
        if (detail.HasConflict) EffectiveText.Foreground = (Brush)FindResource("BrushConflict");

        TechnicalLocationText.Text = TechnicalLocationFormatter.DirectPath(detail.TechnicalLocation);
        WhatText.Text = detail.Explanation.WhatIsIt;
        DoesText.Text = detail.Narrative.Mechanics;
        WhyText.Text = detail.Explanation.WhyItMatters;
        GuidanceText.Text = detail.Explanation.DecisionGuidance + " " + detail.Explanation.UserImpact;
        TradeoffsText.Text = string.Join("\n", new[]
        {
            detail.Explanation.SideEffects,
            detail.Explanation.Exceptions,
            detail.Explanation.CommonMisconceptions
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        RestartText.Text = detail.Applicability == ApplicabilityState.Applicable
            ? $"Supported on this scanned device. Restart expectation: {detail.RestartExpectation}."
            : detail.ApplicabilityReason;
        VerificationText.Text = $"Effective source: {detail.EffectiveSourceDisplay ?? "Unknown"}\n" +
                                $"Evidence confidence: {detail.Confidence}\n" +
                                $"Resolution: {detail.ResolutionReason ?? "No additional resolution explanation."}\n" +
                                $"Catalog ID: {detail.ObjectId}";

        if (detail.Bucket == CatalogBucket.SystemInventory)
        {
            AccessBadgeText.Text = "READ ONLY";
            AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            AccessBadgeText.Text = detail.IsWritable ? "CATALOG EDITABLE" : "VIEW ONLY";
            if (!detail.IsWritable) AccessBadge.Style = (Style)FindResource("BadgeUnknown");
            OptionsList.ItemsSource = detail.Options;
        }

        if (detail.Applicability != ApplicabilityState.Applicable)
        {
            ApplicabilityPanel.Visibility = Visibility.Visible;
            ApplicabilityText.Text = detail.ApplicabilityReason;
        }
    }
}
